using Confluent.Kafka;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;
using Valora.Api.Infrastructure.Projections;
using Valora.Api.Application.Schemas;
using Valora.Api.Domain.Events;

namespace Valora.Api.Infrastructure.BackgroundJobs;

public class KafkaConsumer : BackgroundService
{
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly MongoDbContext _mongoDb;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConsumerConfig _consumerConfig;

    public KafkaConsumer(ILogger<KafkaConsumer> logger, MongoDbContext mongoDb, IServiceProvider serviceProvider, IConfiguration config)
    {
        _logger = logger;
        _mongoDb = mongoDb;
        _serviceProvider = serviceProvider;
        var bootstrap = config["Kafka:BootstrapServers"] ?? "localhost:9092";
        if (bootstrap.Contains("localhost")) bootstrap = bootstrap.Replace("localhost", "127.0.0.1");

        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = bootstrap,
            GroupId = "valora-read-model-group-v4", // Stable, new version
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            // Debug = "consumer,cgrp,topic,fetch", // Disabled for production
            SocketKeepaliveEnable = true,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 3000
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block startup
        _logger.LogInformation("KafkaConsumer starting.");

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig)
                // .SetLogHandler((_, log) => Console.WriteLine($"[KAFKA-LIB] {log.Level} {log.Name}: {log.Message}"))
                // .SetErrorHandler((_, err) => Console.WriteLine($"[KAFKA-LIB-ERROR] {err}"))
                .Build();
            
            // Subscribe to specific topics instead of wildcard for testing stability
            // consumer.Subscribe("^valora\\..*");
            consumer.Subscribe(new List<string> { 
                "valora.data.changed", 
                "valora.schema.changed", 
                "valora.fi.gl.created",
                "valora.fi.gl_account_created",
                "valora.fi.posted",
                "valora.mm.stock_moved",
                "valora.sd.so_billed",
                "valora.fi.masterdata",
                "valora.fi.updated"
            });

            _logger.LogInformation("KafkaConsumer subscribed to explicit topic list.");
            // Console.WriteLine("[CONSOLE DEBUG] KafkaConsumer: Subscribed to explicit list.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Non-blocking consume to prove loop is alive
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));
                    
                    if (consumeResult == null) 
                    {
                        // Console.WriteLine("[CONSOLE DEBUG] KafkaConsumer: No message (timeout). Loop alive.");
                        continue;
                    }

                    var topic = consumeResult.Topic;
                    
                    // Console.WriteLine($"[CONSOLE DEBUG] KafkaConsumer: RECEIVED {topic}");
                    
                    var tenantId = consumeResult.Message.Key;
                    var payload = consumeResult.Message.Value; 
                    
                    _logger.LogInformation("KafkaConsumer received: Topic={Topic}, Key={Key}, PayloadLen={PayloadLen}", topic, tenantId, payload?.Length);

                    // --- LOGGING START ---
                    // Log to MongoDB for Real-Time UI
                    try 
                    {
                        var logCollection = _mongoDb.GetCollection<BsonDocument>("System_KafkaLog");
                        var logEntry = new BsonDocument
                        {
                            { "Topic", topic },
                            { "Key", tenantId ?? "NULL" },
                            { "Payload", payload ?? "" },
                            { "Timestamp", DateTime.UtcNow },
                            { "Processed", false } // Initial state
                        };
                        // Fire and forget logging? No, let's await it to ensure order, but catch exceptions so main flow doesn't break.
                        await logCollection.InsertOneAsync(logEntry, cancellationToken: stoppingToken);
                    }
                    catch (Exception logEx)
                    {
                        _logger.LogError(logEx, "Failed to log Kafka message to MongoDB");
                    }
                    // --- LOGGING END ---

                    if (string.IsNullOrEmpty(tenantId))
                    {
                        _logger.LogWarning("Received message without TenantId Key for topic {Topic}. Skipping.", topic);
                        continue;
                    }

                    if (string.IsNullOrEmpty(payload)) 
                    {
                         _logger.LogWarning("Received empty payload for topic {Topic}. Skipping.", topic);
                         continue;
                    }

                    if (topic == "valora.data.changed")
                    {
                        await ProcessDataChanged(tenantId, payload, stoppingToken);
                    }
                    else if (topic == "valora.schema.changed")
                    {
                        await ProcessSchemaChanged(tenantId, payload, stoppingToken);
                    }
                    else if (topic == "valora.mm.stock_moved")
                    {
                        await ProcessStockMovement(payload, stoppingToken);
                        await ProcessProjection(topic, tenantId, payload, stoppingToken);
                    }
                    else if (topic == "valora.sd.so_billed")
                    {
                        await ProcessSalesOrderBilled(payload, stoppingToken);
                        await ProcessProjection(topic, tenantId, payload, stoppingToken);
                    }
                    else if (topic == "valora.fi.masterdata")
                    {
                        // Handle Master Data (GL Account) updates
                        await ProcessProjection(topic, tenantId, payload, stoppingToken);
                        
                        // Also trigger propagation to Journal Entries
                        await ProcessGLAccountPropagation(payload, stoppingToken);
                    }
                    else if (topic.StartsWith("valora."))
                    {
                        // Generic Projections
                        await ProcessProjection(topic, tenantId, payload, stoppingToken);
                    }

                    consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Kafka message");
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "KafkaConsumer fatal error during initialization or loop.");
        }
    }

    private async Task ProcessGLAccountPropagation(string payload, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        // Use the new Domain Service for consistency logic
        var consistencyService = scope.ServiceProvider.GetRequiredService<Valora.Api.Application.Finance.Services.FinanceDataConsistencyService>();
        
        // Deserialize payload to check if it is GLAccountUpdated
        try 
        {
            var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("EventType", out var eventTypeProp) && 
                eventTypeProp.GetString() == "GLAccountUpdated" &&
                doc.RootElement.TryGetProperty("Data", out var dataProp) &&
                dataProp.TryGetProperty("Id", out var idProp))
            {
                var glAccountId = idProp.GetString();
                if (!string.IsNullOrEmpty(glAccountId))
                {
                    await consistencyService.HandleGLAccountUpdatedAsync(Guid.Parse(glAccountId));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to propagate GL Account update");
        }
    }

    private async Task ProcessStockMovement(string payload, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<FiIntegrationService>();
        await service.HandleStockMovementAsync(payload, stoppingToken);
    }

    private async Task ProcessSalesOrderBilled(string payload, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<FiIntegrationService>();
        await service.HandleSalesOrderBilledAsync(payload, stoppingToken);
    }

    private async Task ProcessProjection(string topic, string tenantId, string payload, CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var projectionManager = scope.ServiceProvider.GetRequiredService<ProjectionManager>();
        await projectionManager.HandleEventAsync(topic, tenantId, payload);
    }

    private async Task ProcessDataChanged(string tenantId, string payload, CancellationToken stoppingToken)
    {
        var document = BsonDocument.Parse(payload);
        if (document.Contains("Id") && document.Contains("ModuleCode"))
        {
            var id = document["Id"].ToString();
            var moduleCode = document["ModuleCode"].ToString();
            var collectionName = $"Entity_{moduleCode}";
            
            document["_id"] = id; 
            document["TenantId"] = tenantId;
            
            // Flatten Data wrapper if present (Fix for double nesting)
            if (document.Contains("Data") && document["Data"].IsBsonDocument)
            {
                var dataDoc = document["Data"].AsBsonDocument;
                foreach (var element in dataDoc)
                {
                    // Don't overwrite system fields if they exist at root
                    if (element.Name == "Id" || element.Name == "TenantId" || element.Name == "_id" || element.Name == "ModuleCode") continue;
                    document[element.Name] = element.Value;
                }
                document.Remove("Data");
            }

            // Ensure audit fields are present in Mongo document
            if (!document.Contains("CreatedAt") && document.Contains("CreatedAt")) document["CreatedAt"] = document["CreatedAt"]; // redundant but explicit check
            // Map UpdatedAt/By if present
            
            var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);
            
            await collection.ReplaceOneAsync(
                filter: Builders<BsonDocument>.Filter.Eq("_id", id),
                replacement: document,
                options: new ReplaceOptions { IsUpsert = true },
                cancellationToken: stoppingToken
            );
            _logger.LogInformation("Consumed and saved message {Id} to Mongo collection {Collection}", id, collectionName);
        }
    }

    private async Task ProcessSchemaChanged(string tenantId, string payload, CancellationToken stoppingToken)
    {
        try 
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<SchemaChangedEvent>(payload, options);
            
            if (data != null)
            {
                // 1. Update Read Model (Mongo PlatformObjectTemplate)
                // We need to inject the schema into the correct version slot in Mongo
                
                var versionKey = $"v{data.Version}";
                var schemaDoc = BsonDocument.Parse(data.SchemaJson);
                
                // Add metadata to schemaDoc if missing
                if (!schemaDoc.Contains("isPublished")) schemaDoc["isPublished"] = true;
                if (!schemaDoc.Contains("updatedAt")) schemaDoc["updatedAt"] = DateTime.UtcNow;
                if (!schemaDoc.Contains("version")) schemaDoc["version"] = data.Version;

                // Path: environments.prod.screens.{module}.{version}
                // We assume 'prod' for now as the target for synced schemas
                var env = "prod";
                var module = data.ModuleCode;
                
                var updatePath = $"environments.{env}.screens.{module}.{versionKey}";
                
                var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
                var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
                
                // We use $set to update specific path. If path doesn't exist, Mongo creates it.
                // However, we must ensure the parent document exists.
                // Upsert with $set might fail if parents are null.
                // But typically TenantController ensures basic doc exists.
                
                var update = Builders<BsonDocument>.Update.Set(updatePath, schemaDoc);
                
                await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, stoppingToken);
                
                _logger.LogInformation("Updated Mongo Read Model for schema {Module} v{Version}", module, data.Version);

                // 2. Sync SQL Table (DDL)
                // Note: TenantController calls this synchronously too.
                // Calling it here makes it eventually consistent and redundant if Controller succeeds.
                // But it's safer to have it here for replayability.
                // Ideally, DDL should be idempotent.
                
                using var scope = _serviceProvider.CreateScope();
                var schemaSyncService = scope.ServiceProvider.GetRequiredService<ISchemaSyncService>();

                var moduleSchema = ModuleSchemaJson.FromRawJson(data.TenantId, data.ModuleCode, data.Version, data.SchemaJson);
                await schemaSyncService.SyncTableAsync(data.TenantId, moduleSchema);
                
                _logger.LogInformation("Processed schema change for {Module}", data.ModuleCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process schema change event");
        }
    }
}

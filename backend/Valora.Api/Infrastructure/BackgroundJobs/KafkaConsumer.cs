using Confluent.Kafka;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;
using Valora.Api.Infrastructure.Projections;
using Lab360.Application.Schemas;
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
        _consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "valora-read-model-group-v3", // Changed to force re-read
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false 
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block startup
        _logger.LogInformation("KafkaConsumer starting.");

        try
        {
            using var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            
            // Subscribe to explicit topics to avoid regex issues
            consumer.Subscribe(new[] 
            { 
                "valora.data.changed", 
                "valora.schema.changed", 
                "valora.fi.gl.created",
                "valora.fi.posted",
                "valora.mm.stock_moved",
                "valora.sd.so_billed"
            });

            _logger.LogInformation("KafkaConsumer subscribed to topics.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var topic = consumeResult.Topic;
                    var tenantId = consumeResult.Message.Key;
                    var payload = consumeResult.Message.Value; 
                    
                    _logger.LogInformation("KafkaConsumer received: Topic={Topic}, Key={Key}, PayloadLen={PayloadLen}", topic, tenantId, payload?.Length);

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

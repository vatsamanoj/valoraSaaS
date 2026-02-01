using System.Text.Json;
using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;
using Valora.Api.Application.Schemas;
using Dapper;
using Npgsql;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Events;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/tenants")]
public class TenantController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;
    private readonly ILogger<TenantController> _logger;
    private readonly ISchemaSyncService _schemaSyncService;
    private readonly ISchemaProvider _schemaProvider;
    private readonly IConfiguration _configuration;

    public TenantController(MongoDbContext mongoDb, ILogger<TenantController> logger, ISchemaSyncService schemaSyncService, ISchemaProvider schemaProvider, IConfiguration configuration)
    {
        _mongoDb = mongoDb;
        _logger = logger;
        _schemaSyncService = schemaSyncService;
        _schemaProvider = schemaProvider;
        _configuration = configuration;
    }

    [HttpPost("sync-schema")]
    public async Task<IActionResult> SyncSchema([FromBody] ModuleSchema schema)
    {
        // 1. Update SQL ObjectDefinition (Source of Truth)
        // We use ISchemaSyncService to handle the DDL sync, but we should also store the Definition itself.
        // For now, we rely on SyncTableAsync to do the DDL.
        // And we will emit an event to update Mongo Read Model.

        var schemaJson = ModuleSchemaJson.ToJson(schema);
        
        // 2. Invalidate Cache
        _schemaProvider.InvalidateCache(schema.TenantId, schema.Module);

        // 3. Sync SQL Table (DDL) - Synchronous for Admin Feedback
        await _schemaSyncService.SyncTableAsync(schema.TenantId, schema);

        // 4. Emit Schema Changed Event (for Mongo Read Model update)
        // We need access to PlatformDbContext to add Outbox message.
        // Since this controller injects MongoDbContext, we need to request PlatformDbContext from services or inject it.
        // But for this specific "Admin" controller, we might need to resolve it.
        var dbContext = HttpContext.RequestServices.GetRequiredService<PlatformDbContext>();

        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = schema.TenantId,
            Topic = "valora.schema.changed",
            Payload = JsonSerializer.Serialize(new SchemaChangedEvent
            {
                TenantId = schema.TenantId,
                ModuleCode = schema.Module,
                AggregateType = "Schema",
                AggregateId = $"{schema.Module}_v{schema.Version}",
                Version = schema.Version,
                SchemaJson = schemaJson
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.OutboxMessages.Add(outboxMessage);
        await dbContext.SaveChangesAsync();
        
        return Ok(ApiResult.Ok(schema.TenantId, schema.Module, "sync-schema", new { Message = $"Schema synced for {schema.Module} (v{schema.Version})" }));
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        // Load tenants ONLY from PlatformObjectTemplate as per new architecture
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        var filter = new BsonDocument();
        var docs = await collection.Find(filter).ToListAsync(cancellationToken);
        
        var result = docs.Select(d => new 
        {
            TenantId = d.GetValue("tenantId", "").AsString,
            Name = d.GetValue("tenantName", d.GetValue("tenantId", "")).AsString,
            Environment = "prod", // Default placeholder
            AdminEmail = "", // Not stored in template usually
            CreatedAt = d.TryGetValue("_id", out var id) && id.IsObjectId ? id.AsObjectId.CreationTime : DateTime.MinValue,
            Source = "PlatformObjectTemplate"
        })
        .Where(x => !string.IsNullOrEmpty(x.TenantId))
        .OrderBy(x => x.TenantId)
        .ToList();

        return Ok(ApiResult.Ok("system", "tenants", "list", result));
    }

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken cancellationToken)
    {
        // Fetch distinct TenantIds from PlatformObjectTemplate collection to serve as valid Source Templates
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        // We want to project just the tenantId. 
        // Since BsonDocument doesn't map easily to Distinct, we can Find all with projection.
        // Or use proper aggregation for distinct.
        
        var filter = new BsonDocument();
        var projection = Builders<BsonDocument>.Projection.Include("tenantId").Exclude("_id");
        
        // Getting all docs (assuming 1 doc per tenant usually)
        var docs = await collection.Find(filter).Project(projection).ToListAsync(cancellationToken);
        
        var templates = docs
            .Select(d => d.GetValue("tenantId", "").AsString)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        return Ok(ApiResult.Ok("system", "tenants", "templates", templates));
    }

    [HttpPost]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TenantId) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(ApiResult.Fail("system", "tenants", "create", new ApiError("Validation", "TenantId and Name are required.")));
        }

        // 1. Create Tenant in Supabase (Postgres)
        var connectionString = _configuration.GetConnectionString("WriteConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Ensure table exists
            await connection.ExecuteAsync(new CommandDefinition(@"
                CREATE TABLE IF NOT EXISTS tenants (
                    id text PRIMARY KEY,
                    name text,
                    created_at timestamptz DEFAULT now()
                );", cancellationToken: cancellationToken));

            // Insert tenant
            await connection.ExecuteAsync(new CommandDefinition(@"
                INSERT INTO tenants (id, name, created_at) 
                VALUES (@Id, @Name, @CreatedAt) 
                ON CONFLICT (id) DO NOTHING;",
                new { Id = request.TenantId, Name = request.Name, CreatedAt = DateTime.UtcNow }, cancellationToken: cancellationToken));

            _logger.LogInformation($"Ensured tenant {request.TenantId} exists in Supabase (Postgres).");
        }

        // 2. Clone Configuration in MongoDB (PlatformObjectTemplate)
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        // Check if target already exists
        var existingFilter = Builders<BsonDocument>.Filter.Eq("tenantId", request.TenantId);
        if (await collection.Find(existingFilter).AnyAsync(cancellationToken))
        {
             return BadRequest(ApiResult.Fail("system", "tenants", "create", new ApiError("Validation", $"Tenant '{request.TenantId}' already exists in configuration.")));
        }

        // Find Source Template
        var sourceTenantId = string.IsNullOrWhiteSpace(request.SourceTenantId) ? "LAB_001" : request.SourceTenantId;
        var sourceFilter = Builders<BsonDocument>.Filter.Eq("tenantId", sourceTenantId);
        var sourceDoc = await collection.Find(sourceFilter).FirstOrDefaultAsync(cancellationToken);

        if (sourceDoc != null)
        {
            // Clone the document
            // We convert to JSON and back to ensure deep copy and clean state
            var json = sourceDoc.ToJson();
            var newDoc = BsonDocument.Parse(json);
            
            // Set new identity
            newDoc["_id"] = ObjectId.GenerateNewId();
            newDoc["tenantId"] = request.TenantId;
            newDoc["tenantName"] = request.Name; // Ensure name is stored
            
            // Insert new configuration
            await collection.InsertOneAsync(newDoc, null, cancellationToken);
            
            _logger.LogInformation($"Cloned tenant configuration from {sourceTenantId} to {request.TenantId}");
        }
        else
        {
             // Create Empty Template if source not found
             var newDoc = new BsonDocument
             {
                 { "tenantId", request.TenantId },
                 { "tenantName", request.Name },
                 { "environments", new BsonDocument
                     {
                         { request.Environment.ToLowerInvariant(), new BsonDocument
                             {
                                 { "screens", new BsonDocument() }
                             }
                         }
                     }
                 }
             };
             await collection.InsertOneAsync(newDoc, null, cancellationToken);
             _logger.LogWarning($"Source tenant {sourceTenantId} not found. Created empty configuration for {request.TenantId}");
        }

        return Ok(ApiResult.Ok("system", "tenants", "create", new { TenantId = request.TenantId, Message = "Tenant created and configuration cloned." }));
    }

    [HttpGet("configurations")]
    public async Task<IActionResult> GetConfigurations(CancellationToken cancellationToken)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        // Return all documents
        var docs = await collection.Find(new BsonDocument()).ToListAsync(cancellationToken);
        
        // Convert ObjectId to string for JSON serialization
        var result = docs.Select(d => 
        {
            var dict = d.ToDictionary();
            // Ensure _id is a string, not an object
            if (dict.ContainsKey("_id"))
            {
                dict["_id"] = d["_id"].ToString();
            }
            return dict;
        });

        return Ok(ApiResult.Ok("system", "tenants", "configurations", result));
    }

    [HttpGet("configurations/by-tenant/{tenantId}")]
    public async Task<IActionResult> GetConfigurationByTenantId(string tenantId, CancellationToken cancellationToken)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        
        if (doc == null)
        {
            return NotFound(ApiResult.Fail("system", "tenants", "configuration", new ApiError("NotFound", $"Configuration for tenant '{tenantId}' not found.")));
        }
        
        var dict = doc.ToDictionary();
        if (dict.ContainsKey("_id"))
        {
            dict["_id"] = doc["_id"].ToString();
        }
        
        return Ok(ApiResult.Ok("system", "tenants", "configuration", dict));
    }

    [HttpPut("configurations/{id}")]
    public async Task<IActionResult> UpdateConfiguration(string id, [FromBody] object updatedConfig, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return BadRequest(ApiResult.Fail("system", "tenants", "update_configuration", new ApiError("Validation", "Invalid ID format.")));
        }

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        
        // Deserialize incoming JSON to BsonDocument
        var json = System.Text.Json.JsonSerializer.Serialize(updatedConfig);
        var doc = BsonDocument.Parse(json);
        
        // Preserve the original _id
        doc["_id"] = objectId;

        // Replace the document
        var result = await collection.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", objectId),
            doc,
            new ReplaceOptions { IsUpsert = false },
            cancellationToken);

        if (result.MatchedCount == 0)
        {
            return NotFound(ApiResult.Fail("system", "tenants", "update_configuration", new ApiError("NotFound", $"Configuration with ID {id} not found.")));
        }

        return Ok(ApiResult.Ok("system", "tenants", "update_configuration", new { Message = "Configuration updated successfully." }));
    }
}

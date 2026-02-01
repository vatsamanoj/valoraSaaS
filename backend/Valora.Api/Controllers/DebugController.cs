using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;
using Lab360.Application.Common.Security; // Added missing namespace
using Valora.Api.Application.Schemas;

using Microsoft.EntityFrameworkCore;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;
    private readonly ISchemaProvider _schemaProvider;
    private readonly PlatformDbContext _sqlDb;

    public DebugController(MongoDbContext mongoDb, ISchemaProvider schemaProvider, PlatformDbContext sqlDb)
    {
        _mongoDb = mongoDb;
        _schemaProvider = schemaProvider;
        _sqlDb = sqlDb;
    }

    [HttpGet("outbox")]
    public async Task<IActionResult> GetOutbox()
    {
        var messages = await _sqlDb.OutboxMessages
            .OrderByDescending(x => x.CreatedAt)
            .Take(10)
            .ToListAsync();
        return Ok(messages);
    }

    [HttpGet("schema/{module}")]
    public async Task<IActionResult> GetSchema(string module)
    {
        var schema = await _schemaProvider.GetSchemaAsync("LAB_001", module, CancellationToken.None);
        return Ok(schema);
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs()
    {
        var collection = _mongoDb.GetCollection<SystemLog>("SystemLogs");
        var logs = await collection.Find(_ => true)
            .SortByDescending(x => x.CreatedAt)
            .Limit(100)
            .ToListAsync();

        return Ok(ApiResult.Ok("system", "debug", "list", logs));
    }

    [HttpPut("logs/{id}/fix")]
    public async Task<IActionResult> MarkFixed(string id, [FromQuery] bool isFixed)
    {
        var collection = _mongoDb.GetCollection<SystemLog>("SystemLogs");
        var update = Builders<SystemLog>.Update
            .Set(x => x.IsFixed, isFixed)
            .Set(x => x.FixedAt, isFixed ? DateTime.UtcNow : null)
            .Set(x => x.FixedBy, isFixed ? "Admin" : null); // Ideally get from User context

        await collection.UpdateOneAsync(x => x.Id == id, update);
        return Ok(ApiResult.Ok("system", "debug", "fix"));
    }

    [HttpPost("sync-materials")]
    public async Task<IActionResult> SyncMaterials()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        // Fetch all materials from SQL and project them to Mongo
        var materials = await _sqlDb.MaterialMasters
            .Where(x => x.TenantId == tenantContext.TenantId)
            .ToListAsync();
            
        var collection = _mongoDb.GetCollection<MongoDB.Bson.BsonDocument>("Entity_Material");
        
        foreach (var mat in materials)
        {
            var doc = new MongoDB.Bson.BsonDocument
            {
                { "_id", mat.Id.ToString() },
                { "TenantId", mat.TenantId },
                { "MaterialCode", mat.MaterialCode },
                { "Description", mat.Description },
                { "MaterialType", "Raw Material" }, // Default as it's missing in Domain
                { "BaseUnitOfMeasure", mat.BaseUnitOfMeasure },
                { "StandardPrice", (double)mat.StandardPrice },
                { "CreatedAt", mat.CreatedAt },
                { "IsActive", true } // Default as it's missing in Domain
            };
            
            await collection.ReplaceOneAsync(
                MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", mat.Id.ToString()),
                doc,
                new ReplaceOptions { IsUpsert = true }
            );
        }
        
        return Ok(new { Count = materials.Count });
    }
}

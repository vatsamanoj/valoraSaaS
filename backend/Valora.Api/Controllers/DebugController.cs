using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public DebugController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
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

    [HttpPost("fix-logs")]
    public async Task<IActionResult> ClearAllLogs()
    {
        var collection = _mongoDb.GetCollection<SystemLog>("SystemLogs");
        var update = Builders<SystemLog>.Update
            .Set(x => x.IsFixed, true)
            .Set(x => x.FixedAt, DateTime.UtcNow)
            .Set(x => x.FixedBy, "Admin");

        await collection.UpdateManyAsync(x => !x.IsFixed, update);
        return Ok(ApiResult.Ok("system", "debug", "fix-all"));
    }
}

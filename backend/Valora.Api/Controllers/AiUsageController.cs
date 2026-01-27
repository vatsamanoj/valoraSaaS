using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("platform/ai-usage")]
public class AiUsageController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public AiUsageController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        var collection = _mongoDb.GetCollection<AiUsageLog>("AiUsageLogs");
        var logs = await collection.Find(_ => true)
            .SortByDescending(x => x.Timestamp)
            .Limit(100)
            .ToListAsync();

        return Ok(ApiResult.Ok("system", "ai-usage", "list", logs));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var collection = _mongoDb.GetCollection<AiUsageLog>("AiUsageLogs");
        
        // Group by Source and Count
        var aggregate = await collection.Aggregate()
            .Group(x => x.Source, g => new AiStats { Source = g.Key, Count = g.Count() })
            .ToListAsync();

        return Ok(ApiResult.Ok("system", "ai-usage", "stats", aggregate));
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteLogs()
    {
        var collection = _mongoDb.GetCollection<AiUsageLog>("AiUsageLogs");
        await collection.DeleteManyAsync(_ => true);
        return Ok(ApiResult.Ok("system", "ai-usage", "delete-all"));
    }
}

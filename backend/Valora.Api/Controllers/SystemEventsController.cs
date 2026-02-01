using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/system-events")]
public class SystemEventsController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public SystemEventsController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentEvents([FromQuery] int limit = 50)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("System_KafkaLog");

        var filter = Builders<BsonDocument>.Filter.Empty;
        var sort = Builders<BsonDocument>.Sort.Descending("Timestamp");

        var events = await collection.Find(filter)
            .Sort(sort)
            .Limit(limit)
            .ToListAsync();

        // Convert BsonDocument to cleaner JSON for UI
        var result = events.Select(e => new
        {
            Id = e["_id"].ToString(),
            Topic = e.GetValue("Topic", "").ToString(),
            Key = e.GetValue("Key", "").ToString(),
            Payload = e.GetValue("Payload", "").ToString(),
            Timestamp = e.GetValue("Timestamp", DateTime.MinValue).ToUniversalTime(),
            Processed = e.GetValue("Processed", false).AsBoolean
        });

        return Ok(result);
    }
}

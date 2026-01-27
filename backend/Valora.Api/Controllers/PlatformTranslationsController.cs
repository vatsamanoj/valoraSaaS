using Lab360.Application.Common.Security;
using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("platform/translations")]
public class PlatformTranslationsController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public PlatformTranslationsController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    [HttpGet("{language}")]
    public async Task<IActionResult> GetTranslations(string language, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformTranslations");
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId),
            Builders<BsonDocument>.Filter.Eq("language", language)
        );

        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "translations", "get", new { }));
        }

        var result = new BsonDocument();

        if (doc.TryGetValue("screens", out var screensValue) && screensValue.IsBsonDocument)
        {
            result["screens"] = screensValue;
        }

        if (doc.TryGetValue("messages", out var messagesValue) && messagesValue.IsBsonDocument)
        {
            result["messages"] = messagesValue;
        }

        if (doc.TryGetValue("common", out var commonValue) && commonValue.IsBsonDocument)
        {
            result["common"] = commonValue;
        }

        return Ok(ApiResult.Ok(tenantContext.TenantId, "translations", "get", result.ToDictionary()));
    }
}


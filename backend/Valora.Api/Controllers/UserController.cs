using Lab360.Application.Common.Security;
using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/user")]
public class UserController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public UserController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    [HttpGet("permissions")]
    public IActionResult GetPermissions()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var permissions = new List<string>();

        if (string.Equals(tenantContext.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase))
        {
            permissions.Add("PLATFORM_ADMIN_ACCESS");
            permissions.Add("STUDIO_ACCESS");
            permissions.Add("*");
        }
        else
        {
            permissions.Add("STUDIO_ACCESS");
        }

        return Ok(ApiResult.Ok(tenantContext.TenantId, "user", "permissions", permissions));
    }

    [HttpGet("preferences")]
    public async Task<IActionResult> GetPreferences(CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("UserPreferences");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "user", "preferences", new
            {
                mode = "light",
                color = "blue",
                language = "en",
                currency = "USD"
            }));
        }

        var mode = doc.TryGetValue("mode", out var modeValue) && modeValue.IsString
            ? modeValue.AsString
            : "light";

        var color = doc.TryGetValue("color", out var colorValue) && colorValue.IsString
            ? colorValue.AsString
            : "blue";

        var language = doc.TryGetValue("language", out var languageValue) && languageValue.IsString
            ? languageValue.AsString
            : "en";

        var currency = doc.TryGetValue("currency", out var currencyValue) && currencyValue.IsString
            ? currencyValue.AsString
            : "USD";

        return Ok(ApiResult.Ok(tenantContext.TenantId, "user", "preferences", new
        {
            mode,
            color,
            language,
            currency
        }));
    }

    [HttpPost("preferences")]
    public async Task<IActionResult> SavePreferences([FromBody] ThemePreferencesRequest request, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("UserPreferences");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken) ?? new BsonDocument();

        doc["tenantId"] = tenantContext.TenantId;

        if (!string.IsNullOrWhiteSpace(request.Mode))
        {
            doc["mode"] = request.Mode;
        }

        if (!string.IsNullOrWhiteSpace(request.Color))
        {
            doc["color"] = request.Color;
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            doc["language"] = request.Language;
        }

        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            doc["currency"] = request.Currency;
        }

        await collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        return Ok(ApiResult.Ok(tenantContext.TenantId, "user", "preferences-save", "Preferences saved successfully"));
    }
}

public class ThemePreferencesRequest
{
    public string? Mode { get; set; }
    public string? Color { get; set; }
    public string? Language { get; set; }
    public string? Currency { get; set; }
}

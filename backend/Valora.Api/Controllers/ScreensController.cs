using Lab360.Application.Common.Security;
using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/screens")]
public class ScreensController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;

    public ScreensController(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    [HttpGet]
    public async Task<IActionResult> GetScreens(CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        if (!doc.TryGetValue("features", out var featuresValue) || featuresValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        var features = featuresValue.AsBsonDocument;
        var dynamicScreens = features.TryGetValue("dynamicScreens", out var dsValue) && dsValue.ToBoolean();

        if (!dynamicScreens)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        if (!doc.TryGetValue("screenRights", out var rightsValue) || rightsValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        var environments = environmentsValue.AsBsonDocument;
        if (!environments.TryGetValue(tenantContext.Environment, out var envValue) || envValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", Array.Empty<object>()));
        }

        var screensDoc = screensValue.AsBsonDocument;
        var rightsDoc = rightsValue.AsBsonDocument;

        var result = new List<object>();

        foreach (var screenElement in screensDoc.Elements)
        {
            var screenName = screenElement.Name;

            if (!rightsDoc.TryGetValue(screenName, out var screenRightsValue) || screenRightsValue.IsBsonNull)
            {
                continue;
            }

            var screenRights = screenRightsValue.AsBsonDocument;
            var defaultRights = screenRights.GetValue("default", new BsonDocument()).AsBsonDocument;
            var visible = defaultRights.TryGetValue("visible", out var visibleValue) && visibleValue.ToBoolean();

            if (!visible)
            {
                continue;
            }

            var byRole = screenRights.GetValue("byRole", new BsonDocument()).AsBsonDocument;
            if (!byRole.TryGetValue(tenantContext.Role, out var roleActionsValue) || roleActionsValue.IsBsonNull)
            {
                continue;
            }

            var roleActions = roleActionsValue.AsBsonArray.Select(x => x.AsString).ToArray();
            if (roleActions.Length == 0)
            {
                continue;
            }

            var versionsDoc = screenElement.Value.AsBsonDocument;
            var versionEntry = versionsDoc.Elements
                .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    var name = e.Name;
                    var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                    if (!int.TryParse(numberPart, out var v))
                    {
                        v = 0;
                    }

                    return new
                    {
                        Version = v,
                        Document = e.Value.AsBsonDocument
                    };
                })
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (versionEntry == null || versionEntry.Version == 0)
            {
                continue;
            }

            result.Add(new
            {
                objectCode = screenName,
                version = versionEntry.Version,
                actions = roleActions
            });
        }

        return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "list", result));
    }

    [HttpGet("{objectCode}")]
    public async Task<IActionResult> GetScreen(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", "PlatformObjectTemplate not found")));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", "Environments not found")));
        }

        var environments = environmentsValue.AsBsonDocument;
        if (!environments.TryGetValue(tenantContext.Environment, out var envValue) || envValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", $"Environment {tenantContext.Environment} not found")));
        }

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", "Screens not found in environment")));
        }

        var screensDoc = screensValue.AsBsonDocument;
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
            if (screenElement.Name == null)
            {
                return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", $"Screen {objectCode} not found")));
            }

            screenValue = screenElement.Value;
        }

        var versionsDoc = screenValue.AsBsonDocument;
        var versionEntry = versionsDoc.Elements
            .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                var name = e.Name;
                var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                if (!int.TryParse(numberPart, out var v))
                {
                    v = 0;
                }

                return new
                {
                    Version = v,
                    Document = e.Value.AsBsonDocument
                };
            })
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        if (versionEntry == null || versionEntry.Version == 0)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, "screens", "get", new ApiError("NotFound", $"No versions found for screen {objectCode}")));
        }

        var versionDoc = versionEntry.Document;
        var result = versionDoc.ToDictionary();
        return Ok(ApiResult.Ok(tenantContext.TenantId, "screens", "get", result));
    }
}


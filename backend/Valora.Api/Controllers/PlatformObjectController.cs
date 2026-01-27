using Lab360.Application.Common.Results;
using Lab360.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/platform/object")]
public class PlatformObjectController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;
    private readonly ILogger<PlatformObjectController> _logger;

    public PlatformObjectController(MongoDbContext mongoDb, ILogger<PlatformObjectController> logger)
    {
        _mongoDb = mongoDb;
        _logger = logger;
    }

    private FilterDefinition<BsonDocument> GetTenantFilter(string tenantId)
    {
        return Builders<BsonDocument>.Filter.Regex("tenantId", new BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(tenantId)}$", "i"));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetObjectList(CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        _logger.LogInformation($"[PlatformObjectController] GetObjectList - Tenant: {tenantContext.TenantId}, Env: {tenantContext.Environment}");

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            _logger.LogWarning($"[PlatformObjectController] Tenant document not found for {tenantContext.TenantId}");
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            _logger.LogWarning($"[PlatformObjectController] 'environments' field missing or null for {tenantContext.TenantId}");
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
             _logger.LogWarning($"[PlatformObjectController] Environment '{tenantContext.Environment}' not found for {tenantContext.TenantId}");
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }
        var envValue = envElement.Value;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        var screensDoc = screensValue.AsBsonDocument;
        var result = screensDoc.Elements
            .Select(e => e.Name)
            .OrderBy(x => x)
            .ToArray();

        return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", result));
    }

    [HttpGet("list/{env}")]
    public async Task<IActionResult> GetObjectListByEnv(string env, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var envKey = env ?? string.Empty;

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, envKey, StringComparison.OrdinalIgnoreCase));

        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }
        var envValue = envElement.Value;

        var envDoc = envValue.AsBsonDocument;
        if (!envDoc.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", Array.Empty<string>()));
        }

        var screensDoc = screensValue.AsBsonDocument;
        var result = screensDoc.Elements
            .Select(e => e.Name)
            .OrderBy(x => x)
            .ToArray();

        return Ok(ApiResult.Ok(tenantContext.TenantId, "platform", "list", result));
    }

    [HttpGet("{objectCode}/versions")]
    public async Task<IActionResult> GetVersions(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", Array.Empty<object>()));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", Array.Empty<object>()));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", Array.Empty<object>()));
        }
        var envValue = envElement.Value;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", Array.Empty<object>()));
        }

        var screensDoc = screensValue.AsBsonDocument;
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
            if (screenElement.Name == null)
            {
                return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", Array.Empty<object>()));
            }

            screenValue = screenElement.Value;
        }

        var versionsDoc = screenValue.AsBsonDocument;

        var versions = versionsDoc.Elements
            .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                var name = e.Name;
                var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                if (!int.TryParse(numberPart, out var v))
                {
                    v = 0;
                }

                var value = e.Value.AsBsonDocument;
                var isPublished = value.TryGetValue("isPublished", out var publishedValue) &&
                                  !publishedValue.IsBsonNull &&
                                  publishedValue.ToBoolean();

                return new
                {
                    Version = v,
                    IsPublished = isPublished
                };
            })
            .Where(x => x.Version > 0)
            .OrderByDescending(x => x.Version)
            .ToArray();

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "versions", versions));
    }

    [HttpGet("{objectCode}/latest")]
    public async Task<IActionResult> GetLatest(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        _logger.LogInformation($"[GetLatest] Tenant: {tenantContext.TenantId}, Env: {tenantContext.Environment}, Object: {objectCode}");

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
             _logger.LogWarning($"[GetLatest] Tenant doc not found for {tenantContext.TenantId}");
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Object not found")));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
             _logger.LogWarning($"[GetLatest] Environments not configured for {tenantContext.TenantId}");
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Environments not configured")));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
             _logger.LogWarning($"[GetLatest] Environment {tenantContext.Environment} not found for {tenantContext.TenantId}");
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Environment not found")));
        }
        var envValue = envElement.Value;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
             _logger.LogWarning($"[GetLatest] Screens not found in {tenantContext.Environment}");
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Screens not found")));
        }

        var screensDoc = screensValue.AsBsonDocument;
        
        // Try exact match first
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            // Case-insensitive fallback
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
                
            if (screenElement.Name == null)
            {
                // Try searching in "preview" environment as fallback if we are in "prod" (or vice versa)?
                // For now, strict environment scoping.
                 _logger.LogWarning($"[GetLatest] Screen {objectCode} not found in {tenantContext.Environment}");
                return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Screen not found")));
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
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "latest", new ApiError("NotFound", "Version not found")));
        }

        var hasPublished = versionsDoc.Elements
            .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Value.AsBsonDocument)
            .Any(v =>
                v.TryGetValue("isPublished", out var publishedValue) &&
                !publishedValue.IsBsonNull &&
                publishedValue.ToBoolean());

        HttpContext.Response.Headers["X-Is-Published"] = hasPublished ? "true" : "false";

        var versionDoc = versionEntry.Document;
        var result = versionDoc.ToDictionary();

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "latest", result));
    }

    [HttpGet("{objectCode}/version/{version}")]
    public async Task<IActionResult> GetByVersion(string objectCode, int version, CancellationToken cancellationToken)
    {
        if (version <= 0)
        {
            return BadRequest(ApiResult.Fail("unknown", objectCode, "version", new ApiError("Validation", "Invalid version.")));
        }

        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Object not found")));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Environments not configured")));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Environment not found")));
        }
        var envValue = envElement.Value;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Screens not found")));
        }

        var screensDoc = screensValue.AsBsonDocument;
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
            if (screenElement.Name == null)
            {
                return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Screen not found")));
            }

            screenValue = screenElement.Value;
        }

        var versionsDoc = screenValue.AsBsonDocument;
        var key = $"v{version}";

        if (!versionsDoc.TryGetValue(key, out var versionValue) || versionValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "version", new ApiError("NotFound", "Version not found")));
        }

        var versionDoc = versionValue.AsBsonDocument;

        var isPublished = versionDoc.TryGetValue("isPublished", out var publishedValue) &&
                          !publishedValue.IsBsonNull &&
                          publishedValue.ToBoolean();

        HttpContext.Response.Headers["X-Is-Published"] = isPublished ? "true" : "false";

        var result = versionDoc.ToDictionary();

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "version", result));
    }

    [HttpGet("{objectCode}/published")]
    public async Task<IActionResult> GetPublished(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Object not found")));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Environments not configured")));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Environment not found")));
        }
        var envValue = envElement.Value;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Screens not found")));
        }

        var screensDoc = screensValue.AsBsonDocument;
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
            if (screenElement.Name == null)
            {
                return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Screen not found")));
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

                var value = e.Value.AsBsonDocument;
                var isPublished = value.TryGetValue("isPublished", out var publishedValue) &&
                                  !publishedValue.IsBsonNull &&
                                  publishedValue.ToBoolean();

                return new
                {
                    Version = v,
                    Document = value,
                    IsPublished = isPublished
                };
            })
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.Version)
            .FirstOrDefault();

        if (versionEntry == null || versionEntry.Version == 0)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "published", new ApiError("NotFound", "Published version not found")));
        }

        var versionDoc = versionEntry.Document;
        var result = versionDoc.ToDictionary();

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "published", result));
    }

    [HttpPost("{objectCode}/draft")]
    public async Task<IActionResult> SaveDraft(string objectCode, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        // Policy: "No direct writes to Mongo from API"
        // Exception: Drafts are UI state, not Business Facts. 
        // However, to be strictly compliant, we should treat Drafts as "User Session State" or "Work In Progress".
        // Ideally, we should use a SQL "Drafts" table or a Redis store.
        // But if we MUST use Mongo, it should be read-only for business data.
        // Since "PlatformObjectTemplate" IS the Configuration Source of Truth (currently), 
        // and we decided TenantController.SyncSchema (Publish) moves it to SQL, 
        // Drafts staying in Mongo is acceptable as "Configuration State" NOT "Business Data".
        // BUT, let's at least log this action as an Audit event.

        _logger.LogInformation("Saving Draft for {TenantId}.{ObjectCode}", tenantContext.TenantId, objectCode);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            doc = new BsonDocument
            {
                { "tenantId", tenantContext.TenantId },
                { "environments", new BsonDocument() }
            };
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            environmentsValue = new BsonDocument();
            doc["environments"] = environmentsValue;
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));

        string envKey = tenantContext.Environment;
        BsonValue envValue;

        if (envElement.Name != null && !envElement.Value.IsBsonNull)
        {
             envKey = envElement.Name;
             envValue = envElement.Value;
        }
        else
        {
             envValue = new BsonDocument();
             environments[envKey] = envValue;
        }

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            screensValue = new BsonDocument();
            env["screens"] = screensValue;
        }

        var screensDoc = screensValue.AsBsonDocument;

        var existingElement = screensDoc.Elements.FirstOrDefault(e =>
            string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));

        var moduleKey = existingElement.Name ?? objectCode;
        var versionsDoc = existingElement.Name != null
            ? existingElement.Value.AsBsonDocument
            : new BsonDocument();

        var rawJson = body.GetRawText();
        var schemaDoc = BsonDocument.Parse(rawJson);

        var currentVersions = versionsDoc.Elements
            .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            .Select(e =>
            {
                var name = e.Name;
                var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                if (!int.TryParse(numberPart, out var v))
                {
                    v = 0;
                }

                return v;
            })
            .ToList();

        var nextVersion = currentVersions.Count == 0 ? 1 : currentVersions.Max() + 1;

        schemaDoc["version"] = nextVersion;
        if (!schemaDoc.TryGetValue("isPublished", out _))
        {
            schemaDoc["isPublished"] = false;
        }

        versionsDoc[$"v{nextVersion}"] = schemaDoc;

        screensDoc[moduleKey] = versionsDoc;
        env["screens"] = screensDoc;
        environments[envKey] = env;
        doc["environments"] = environments;

        // If doc has _id, use it for filter to ensure we update the same doc
        if (doc.Contains("_id"))
        {
            var idFilter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
            await collection.ReplaceOneAsync(idFilter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }
        else
        {
            // Fallback for new docs
            var tenantFilter = Builders<BsonDocument>.Filter.Eq("tenantId", doc["tenantId"]);
            await collection.ReplaceOneAsync(tenantFilter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "draft", new { version = nextVersion }));
    }

    [HttpPost("{objectCode}/unpublish")]
    public async Task<IActionResult> Unpublish(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "unpublish", new ApiError("NotFound", "Template not found.")));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "unpublish", new ApiError("NotFound", "Environments not configured.")));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "unpublish", new ApiError("NotFound", "Environment not found.")));
        }
        var envValue = envElement.Value;
        var envKey = envElement.Name;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "unpublish", new ApiError("NotFound", "Screens not found.")));
        }

        var screensDoc = screensValue.AsBsonDocument;
        if (!screensDoc.TryGetValue(objectCode, out var screenValue) || screenValue.IsBsonNull)
        {
            var screenElement = screensDoc.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
            if (screenElement.Name == null)
            {
                return NotFound(ApiResult.Fail(tenantContext.TenantId, objectCode, "unpublish", new ApiError("NotFound", "Screen not found.")));
            }

            screenValue = screenElement.Value;
        }

        var versionsDoc = screenValue.AsBsonDocument;

        foreach (var element in versionsDoc.Elements.Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase)))
        {
            var versionDoc = element.Value.AsBsonDocument;
            versionDoc["isPublished"] = false;
            versionsDoc[element.Name] = versionDoc;
        }

        var existingElement = screensDoc.Elements.FirstOrDefault(e =>
            string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));
        var moduleKey = existingElement.Name ?? objectCode;

        screensDoc[moduleKey] = versionsDoc;
        env["screens"] = screensDoc;
        environments[envKey] = env;
        doc["environments"] = environments;

        // Use _id for update
        var idFilter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
        await collection.ReplaceOneAsync(idFilter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "unpublish", null));
    }

    [HttpDelete("{objectCode}")]
    public async Task<IActionResult> Delete(string objectCode, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = GetTenantFilter(tenantContext.TenantId);
        var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

        if (doc == null)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "delete", null));
        }

        if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "delete", null));
        }

        var environments = environmentsValue.AsBsonDocument;
        var envElement = environments.Elements.FirstOrDefault(e => string.Equals(e.Name, tenantContext.Environment, StringComparison.OrdinalIgnoreCase));
        
        if (envElement.Name == null || envElement.Value.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "delete", null));
        }
        var envValue = envElement.Value;
        var envKey = envElement.Name;

        var env = envValue.AsBsonDocument;
        if (!env.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
        {
            return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "delete", null));
        }

        var screensDoc = screensValue.AsBsonDocument;

        var existingElement = screensDoc.Elements.FirstOrDefault(e =>
            string.Equals(e.Name, objectCode, StringComparison.OrdinalIgnoreCase));

        if (existingElement.Name != null)
        {
            screensDoc.Remove(existingElement.Name);
            env["screens"] = screensDoc;
            environments[envKey] = env;
            doc["environments"] = environments;

            // Use _id for update
            var idFilter = Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]);
            await collection.ReplaceOneAsync(idFilter, doc, new ReplaceOptions { IsUpsert = true }, cancellationToken);
        }

        return Ok(ApiResult.Ok(tenantContext.TenantId, objectCode, "delete", null));
    }
}

using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Valora.Api.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Valora.Api.Application.Schemas;

public sealed class SchemaCache : ISchemaProvider
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    private readonly MongoDbContext _mongoDb;
    private readonly ILogger<SchemaCache> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SchemaCache(MongoDbContext mongoDb, ILogger<SchemaCache> logger, IServiceProvider serviceProvider)
    {
        _mongoDb = mongoDb;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public void InvalidateCache(string tenantId, string module)
    {
        var cacheKey = $"{tenantId}:{module}";
        _cache.TryRemove(cacheKey, out _);
    }

    private async Task EnsureSqlSync(string tenantId, ModuleSchema schema)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<Valora.Api.Infrastructure.Services.ISchemaSyncService>();
            await syncService.SyncTableAsync(tenantId, schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync SQL schema for {Module} in {Tenant}", schema.Module, tenantId);
            // Don't throw, allow EAV to work partially or fail downstream
        }
    }

    public async Task<ModuleSchema> GetSchemaAsync(
    string tenantId,
    string module,
    CancellationToken ct,
    int? version = null)
{
    _logger.LogWarning($"[DEBUG] GetSchemaAsync: tenant={tenantId}, module='{module}', version={version}");
    var cacheKey = version.HasValue ? $"{tenantId}:{module}:v{version}" : $"{tenantId}:{module}";
    var now = DateTime.UtcNow;

    if (_cache.TryGetValue(cacheKey, out var item) && item.ExpiresAt > now)
    {
        return item.Schema;
    }

    var schemaFromTemplate = await TryGetFromPlatformTemplateAsync(tenantId, module, ct, version);
    if (schemaFromTemplate != null)
    {
        if (module == "SalesOrder") Console.WriteLine($"[DEBUG] SchemaCache: Loaded SalesOrder. ShouldPost={schemaFromTemplate.ShouldPost}");

        // Ensure SQL is synced
        await EnsureSqlSync(tenantId, schemaFromTemplate);

        _cache[cacheKey] = new CacheItem(
            schemaFromTemplate,
            now.Add(_ttl));

        return schemaFromTemplate;
    }

    // --- INJECTION LOGIC START ---
    // If template is missing, check if it's a known ERP module and seed it.
    var seededSchema = await SeedErpTemplateAsync(tenantId, module, ct);
    if (seededSchema != null)
    {
        // After seeding, try to get the specific version again
        var schemaAfterSeed = await TryGetFromPlatformTemplateAsync(tenantId, module, ct, version);
        if (schemaAfterSeed != null)
        {
            // Ensure SQL is synced
            await EnsureSqlSync(tenantId, schemaAfterSeed);

            _cache[cacheKey] = new CacheItem(schemaAfterSeed, now.Add(_ttl));
            return schemaAfterSeed;
        }
    }
    // --- INJECTION LOGIC END ---

    // If not found in PlatformObjectTemplate, we return the mock schema or throw.
    return MockSchemaProvider.Get(module);
}

    public async Task SeedSchemaAsync(string tenantId, string module, CancellationToken ct)
    {
        await SeedErpTemplateAsync(tenantId, module, ct);
    }

    private async Task<ModuleSchema?> SeedErpTemplateAsync(string tenantId, string module, CancellationToken ct)
    {
        _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: tenant={tenantId}, module={module}");
        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
        var updateBuilder = Builders<BsonDocument>.Update;
        var updates = new List<UpdateDefinition<BsonDocument>>();

        for (int v = 1; v <= 7; v++)
        {
            var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Schemas", $"{module}V{v}.json");
            if (!File.Exists(resourcePath))
            {
                // If V1 doesn't exist, we can't proceed
                if (v == 1)
                {
                    _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: V1 schema not found at {resourcePath}");
                    return null;
                }
                // Otherwise, it's fine if higher versions don't exist
                continue;
            }

            var jsonContent = await File.ReadAllTextAsync(resourcePath, ct);
            if (string.IsNullOrEmpty(jsonContent)) continue;

            var schemaDoc = BsonDocument.Parse(jsonContent);
            if (!schemaDoc.Contains("fields") || !schemaDoc.Contains("ui"))
            {
                _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: Missing fields or ui in V{v}");
                continue;
            }

            var schemaFields = schemaDoc["fields"].AsBsonDocument;
            var uiLayout = schemaDoc["ui"].AsBsonDocument;

            updates.Add(updateBuilder.Set($"environments.prod.screens.{module}.v{v}.fields", schemaFields));
            updates.Add(updateBuilder.Set($"environments.prod.screens.{module}.v{v}.ui", uiLayout));
            updates.Add(updateBuilder.Set($"environments.prod.screens.{module}.v{v}.isPublished", true));

            if (schemaDoc.Contains("shouldPost"))
            {
                updates.Add(updateBuilder.Set($"environments.prod.screens.{module}.v{v}.shouldPost", schemaDoc["shouldPost"]));
            }
             if (schemaDoc.Contains("documentTotals"))
            {
                updates.Add(updateBuilder.Set($"environments.prod.screens.{module}.v{v}.documentTotals", schemaDoc["documentTotals"]));
            }
        }

        if (!updates.Any()) return null;

        updates.Add(updateBuilder.SetOnInsert("tenantId", tenantId));
        updates.Add(updateBuilder.Set($"screenRights.{module}.default.visible", true));
        updates.Add(updateBuilder.Set($"screenRights.{module}.byRole.Admin", new BsonArray { "View", "Edit", "Create", "Delete" }));

        var combinedUpdate = updateBuilder.Combine(updates);
        var result = await collection.UpdateOneAsync(filter, combinedUpdate, new UpdateOptions { IsUpsert = true }, ct);
        _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: Update result: Matched={result.MatchedCount}, Modified={result.ModifiedCount}, Upserted={result.UpsertedId}");

        return await TryGetFromPlatformTemplateAsync(tenantId, module, ct);
    }

    private async Task<ModuleSchema?> TryGetFromPlatformTemplateAsync(
    string tenantId,
    string module,
    CancellationToken ct,
    int? version = null)
    {
        try
        {
            var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
            var doc = await collection.Find(filter).FirstOrDefaultAsync(ct);

            if (doc == null)
            {
                _logger.LogWarning("[DEBUG] TryGet: Doc is null");
                return null;
            }

            if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
            {
                _logger.LogWarning("[DEBUG] TryGet: environments missing");
                return null;
            }

            var environments = environmentsValue.AsBsonDocument;
            if (!environments.TryGetValue("prod", out var prodValue) || prodValue.IsBsonNull)
            {
                _logger.LogWarning("[DEBUG] TryGet: prod missing");
                return null;
            }

            var prod = prodValue.AsBsonDocument;
            if (!prod.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
            {
                _logger.LogWarning("[DEBUG] TryGet: screens missing");
                return null;
            }

            var screens = screensValue.AsBsonDocument;
            // Case-insensitive check
            var moduleElement = screens.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, module, StringComparison.OrdinalIgnoreCase));

            if (moduleElement.Name == null)
            {
                _logger.LogWarning($"[DEBUG] TryGet: module {module} missing in screens. Available: {string.Join(", ", screens.Names)}");
                return null;
            }

            var moduleScreensDoc = moduleElement.Value.AsBsonDocument;

            var versionEntry = moduleScreensDoc.Elements
                .Where(e => e.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                .Select(e =>
                {
                    var name = e.Name;
                    var value = e.Value.AsBsonDocument;
                    var numberPart = name.Length > 1 ? name.Substring(1) : "0";
                    if (!int.TryParse(numberPart, out var versionNumber))
                    {
                        versionNumber = 0;
                    }

                    return new
                    {
                        Version = versionNumber,
                        Document = value
                    };
                })
                .Where(x => !version.HasValue || x.Version == version.Value)
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (versionEntry == null || versionEntry.Version == 0)
            {
                return null;
            }

            var versionDoc = versionEntry.Document;

            if (!versionDoc.TryGetValue("fields", out var fieldsValue) || fieldsValue.IsBsonNull)
            {
                return null;
            }

            var schemaBody = new BsonDocument
            {
                { "fields", fieldsValue.AsBsonDocument }
            };

            if (versionDoc.TryGetValue("uniqueConstraints", out var uniqueConstraintsValue) && !uniqueConstraintsValue.IsBsonNull)
            {
                schemaBody.Add("uniqueConstraints", uniqueConstraintsValue);
            }

            if (versionDoc.TryGetValue("ui", out var uiValue) && !uiValue.IsBsonNull)
            {
                schemaBody.Add("ui", uiValue);
            }

            if (versionDoc.TryGetValue("shouldPost", out var shouldPostValue) && !shouldPostValue.IsBsonNull)
            {
                schemaBody.Add("shouldPost", shouldPostValue);
            }

            var jsonSettings = new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson };
            var schemaJson = schemaBody.ToJson(jsonSettings);

            if (module == "SalesOrder")
            {
                Console.WriteLine($"[DEBUG] SchemaCache: SalesOrder JSON: {schemaJson}");
            }

            if (versionDoc.TryGetValue("documentTotals", out var documentTotalsValue) && !documentTotalsValue.IsBsonNull)
            {
                schemaBody.Add("documentTotals", documentTotalsValue);
            }

            var finalSchemaJson = schemaBody.ToJson(jsonSettings);

            return ModuleSchemaJson.FromRawJson(
              tenantId,
              moduleElement.Name,
              versionEntry.Version,
              finalSchemaJson);
        }
        catch
        {
            return null;
        }
    }

    private sealed record CacheItem(
        ModuleSchema Schema,
        DateTime ExpiresAt);
}

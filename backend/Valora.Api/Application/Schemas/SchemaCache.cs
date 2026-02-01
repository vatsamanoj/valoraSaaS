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
        CancellationToken ct)
    {
        _logger.LogWarning($"[DEBUG] GetSchemaAsync: tenant={tenantId}, module='{module}'");
        var cacheKey = $"{tenantId}:{module}";
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(cacheKey, out var item)
            && item.ExpiresAt > now)
        {
            return item.Schema;
        }

        // ModuleSchema collection is no longer in use.
        
        // DEV HACK: Removed. Use SeedSchemaAsync in Program.cs
        /*
        if (module == "SalesOrder")
        {
             await SeedErpTemplateAsync(tenantId, module, ct);
             Console.WriteLine("[DEBUG] SchemaCache: Re-seeded SalesOrder.");
        }
        */

        var schemaFromTemplate = await TryGetFromPlatformTemplateAsync(tenantId, module, ct);
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
             // Ensure SQL is synced
             await EnsureSqlSync(tenantId, seededSchema);

             _cache[cacheKey] = new CacheItem(seededSchema, now.Add(_ttl));
             return seededSchema;
        }
        // --- INJECTION LOGIC END ---

        // ModuleSchema collection is no longer in use.
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
        string? jsonContent = null;
        var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resources", "Schemas", $"{module}.json");

        if (File.Exists(resourcePath))
        {
            jsonContent = await File.ReadAllTextAsync(resourcePath, ct);
        }
        else
        {
             _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: File not found at {resourcePath}");
        }

        if (string.IsNullOrEmpty(jsonContent)) return null;

        var schemaDoc = BsonDocument.Parse(jsonContent);
        if (!schemaDoc.Contains("fields") || !schemaDoc.Contains("ui")) 
        {
             _logger.LogWarning("[DEBUG] SeedErpTemplateAsync: Missing fields or ui");
             return null;
        }

        var schemaFields = schemaDoc["fields"].AsBsonDocument;
        var uiLayout = schemaDoc["ui"].AsBsonDocument;

        // Construct the full PlatformObjectTemplate document structure
        // We need to fetch existing document to update it, or create new if not exists.
        // But since PlatformObjectTemplate is usually one BIG doc per tenant with all screens,
        // we should try to update the existing one.

        var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
        var filter = Builders<BsonDocument>.Filter.Eq("tenantId", tenantId);
        
        var update = Builders<BsonDocument>.Update
            .Set("tenantId", tenantId) // Ensure tenantId is set on upsert
            .Set($"environments.prod.screens.{module}.v1.fields", schemaFields)
            .Set($"environments.prod.screens.{module}.v1.ui", uiLayout)
            .Set($"environments.prod.screens.{module}.v1.isPublished", true)
            // Ensure rights exist so ScreensController picks it up
            .Set($"screenRights.{module}.default.visible", true)
            .Set($"screenRights.{module}.byRole.Admin", new BsonArray { "View", "Edit", "Create", "Delete" });

        if (schemaDoc.Contains("shouldPost"))
        {
            update = update.Set($"environments.prod.screens.{module}.v1.shouldPost", schemaDoc["shouldPost"]);
        }

        // Use UpdateOne with Upsert?
        // If the doc doesn't exist at all, we'd need to create the whole structure. 
        // Assuming at least an empty doc exists for the tenant.
        
        var result = await collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
        _logger.LogWarning($"[DEBUG] SeedErpTemplateAsync: Update result: Matched={result.MatchedCount}, Modified={result.ModifiedCount}, Upserted={result.UpsertedId}");

        // Now fetch it back to return the ModuleSchema
        return await TryGetFromPlatformTemplateAsync(tenantId, module, ct);
    }

    private async Task<ModuleSchema?> TryGetFromPlatformTemplateAsync(
        string tenantId,
        string module,
        CancellationToken ct)
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

            return ModuleSchemaJson.FromRawJson(
                tenantId,
                moduleElement.Name,
                versionEntry.Version,
                schemaJson);
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

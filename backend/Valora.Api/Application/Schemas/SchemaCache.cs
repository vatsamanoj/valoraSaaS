using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Concurrent;
using Valora.Api.Infrastructure.Persistence;

namespace Lab360.Application.Schemas;

public sealed class SchemaCache : ISchemaProvider
{
    private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(10);

    private readonly MongoDbContext _mongoDb;

    public SchemaCache(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public void InvalidateCache(string tenantId, string module)
    {
        var cacheKey = $"{tenantId}:{module}";
        _cache.TryRemove(cacheKey, out _);
    }

    public async Task<ModuleSchema> GetSchemaAsync(
        string tenantId,
        string module,
        CancellationToken ct)
    {
        var cacheKey = $"{tenantId}:{module}";
        var now = DateTime.UtcNow;

        if (_cache.TryGetValue(cacheKey, out var item)
            && item.ExpiresAt > now)
        {
            return item.Schema;
        }

        var schemaFromTemplate = await TryGetFromPlatformTemplateAsync(tenantId, module, ct);
        if (schemaFromTemplate != null)
        {
            _cache[cacheKey] = new CacheItem(
                schemaFromTemplate,
                now.Add(_ttl));

            return schemaFromTemplate;
        }

        // ModuleSchema collection is no longer in use.
        // If not found in PlatformObjectTemplate, we return the mock schema or throw.
        return MockSchemaProvider.Get(module);
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
                return null;
            }

            if (!doc.TryGetValue("environments", out var environmentsValue) || environmentsValue.IsBsonNull)
            {
                return null;
            }

            var environments = environmentsValue.AsBsonDocument;
            if (!environments.TryGetValue("prod", out var prodValue) || prodValue.IsBsonNull)
            {
                return null;
            }

            var prod = prodValue.AsBsonDocument;
            if (!prod.TryGetValue("screens", out var screensValue) || screensValue.IsBsonNull)
            {
                return null;
            }

            var screens = screensValue.AsBsonDocument;
            var moduleElement = screens.Elements.FirstOrDefault(e =>
                string.Equals(e.Name, module, StringComparison.OrdinalIgnoreCase));

            if (moduleElement.Name == null)
            {
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

            var jsonSettings = new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson };
            var schemaJson = schemaBody.ToJson(jsonSettings);

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

using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Projections;

public class MongoProjectionRepository
{
    private readonly MongoDbContext _mongoDb;

    public MongoProjectionRepository(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public async Task UpsertFullProjectionAsync(string aggregateType, string id, string tenantId, object data)
    {
        var collectionName = $"Entity_{aggregateType}";
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);

        BsonDocument document;
        if (data is BsonDocument doc)
        {
            document = doc;
        }
        else
        {
            document = data.ToBsonDocument();
        }
        
        // Ensure standard fields
        if (document.Contains("_id")) document.Remove("_id"); // Let Mongo handle or set explicitly
        document["_id"] = id;
        document["TenantId"] = tenantId;
        document["_projectedAt"] = DateTime.UtcNow;

        // Ensure Version (xmin) is stored as Long (Int64) because MongoDB doesn't support UInt32 natively
        if (document.Contains("Version"))
        {
             var v = document["Version"];
             if (v.IsInt32) document["Version"] = new BsonInt64(v.AsInt32);
             // if it is already Int64 or String, leave it. 
             // EF Core 'uint' might be serialized as Int32 if small enough or Int64.
             // We force it to Int64 to match our read logic.
        }

        // --- DATE FIX: Convert ISO String dates to BSON Dates ---
        // Recursively walk the document and convert known date fields
        ConvertDates(document);
        // --------------------------------------------------------

        await collection.ReplaceOneAsync(
            filter: Builders<BsonDocument>.Filter.Eq("_id", id),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true }
        );
    }

    private void ConvertDates(BsonValue value)
    {
        if (value.IsBsonDocument)
        {
            var doc = value.AsBsonDocument;
            foreach (var element in doc.ToList()) // ToList to allow modification
            {
                // Check if the value looks like a date string
                if (element.Value.IsString)
                {
                    var str = element.Value.AsString;
                    // Simple heuristic: ISO8601 length (approx) + starts with 20xx
                    if (str.Length >= 10 && (str.StartsWith("20") || str.StartsWith("19")) && DateTime.TryParse(str, out var dt))
                    {
                        // Check if key implies date
                        if (element.Name.Contains("Date") || element.Name.Contains("Time") || element.Name.Contains("At"))
                        {
                             doc[element.Name] = new BsonDateTime(dt.ToUniversalTime());
                        }
                    }
                }
                else if (element.Value.IsBsonArray || element.Value.IsBsonDocument)
                {
                    ConvertDates(element.Value);
                }
            }
        }
        else if (value.IsBsonArray)
        {
            var arr = value.AsBsonArray;
            foreach (var item in arr)
            {
                ConvertDates(item);
            }
        }
    }

    public async Task DeleteProjectionAsync(string aggregateType, string id)
    {
        var collectionName = $"Entity_{aggregateType}";
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);

        await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id));
    }
}

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
        var collectionName = $"full_{aggregateType}";
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

        await collection.ReplaceOneAsync(
            filter: Builders<BsonDocument>.Filter.Eq("_id", id),
            replacement: document,
            options: new ReplaceOptions { IsUpsert = true }
        );
    }

    public async Task DeleteProjectionAsync(string aggregateType, string id)
    {
        var collectionName = $"full_{aggregateType}";
        var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);

        await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id));
    }
}

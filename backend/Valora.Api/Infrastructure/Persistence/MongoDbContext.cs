using MongoDB.Driver;

namespace Valora.Api.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IConfiguration config)
    {
        var client = new MongoClient(config["MongoDb:ConnectionString"] ?? "mongodb://localhost:27017");
        _database = client.GetDatabase(config["MongoDb:DatabaseName"] ?? "ValoraReadDb");
    }

    public IMongoCollection<T> GetCollection<T>(string name) => _database.GetCollection<T>(name);
    public IMongoDatabase Database => _database;
}

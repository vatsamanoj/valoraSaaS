using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Net;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {
        // Force TLS 1.2
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        string connString = "mongodb+srv://vatsamanoj_db_user:cVmX5s193hAEpBd4@cluster0.k6mzgv0.mongodb.net/ValoraReadDb?authSource=admin&retryWrites=true&w=majority&tls=true";
        string dbName = "ValoraReadDb";

        Console.WriteLine("Testing MongoDB Connection...");

        try
        {
            var settings = MongoClientSettings.FromConnectionString(connString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
            
            var client = new MongoClient(settings);
            var db = client.GetDatabase(dbName);

            var command = new BsonDocument("ping", 1);
            db.RunCommand<BsonDocument>(command);
            
            Console.WriteLine("SUCCESS: Connected to MongoDB!");
            
            // List collections
            var collections = db.ListCollectionNames().ToList();
            Console.WriteLine($"Collections found: {collections.Count}");
            foreach (var col in collections)
            {
                Console.WriteLine($" - {col}");
            }

            const string platformCollectionName = "PlatformObjectTemplate";
            if (collections.Contains(platformCollectionName))
            {
                var platformCollection = db.GetCollection<BsonDocument>(platformCollectionName);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", "TENANT_LAB_001");
                var doc = platformCollection.Find(filter).FirstOrDefault();

                if (doc != null)
                {
                    Console.WriteLine("PlatformObjectTemplate document TENANT_LAB_001 exists.");
                }
                else
                {
                    Console.WriteLine("PlatformObjectTemplate collection exists but TENANT_LAB_001 not found.");
                }
            }
            else
            {
                Console.WriteLine("PlatformObjectTemplate collection not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: Failed to connect to MongoDB.");
            Console.WriteLine(ex.Message);
        }
    }
}

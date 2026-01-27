using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Threading.Tasks;

namespace MongoReader
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting MongoReader...");
            try 
            {
                Console.WriteLine("Connecting to Mongo...");
                // Use the connection string from appsettings.Development.json
                var connectionString = "mongodb+srv://vatsamanoj_db_user:cVmX5s193hAEpBd4@cluster0.k6mzgv0.mongodb.net/ValoraReadDb?authSource=admin&retryWrites=true&w=majority&tls=true";
                var client = new MongoClient(connectionString);
                var db = client.GetDatabase("ValoraReadDb");
                var collection = db.GetCollection<BsonDocument>("PlatformObjectTemplate");

                Console.WriteLine("Querying...");
                var filter = Builders<BsonDocument>.Filter.Eq("tenantId", "LAB003");
                var doc = await collection.Find(filter).FirstOrDefaultAsync();
                Console.WriteLine("Query finished.");

                if (doc != null)
                {
                    Console.WriteLine("Found document for LAB003:");
                    Console.WriteLine(doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true }));
                }
                else
                {
                    Console.WriteLine("Document not found for LAB003. Available tenants:");
                    var count = await collection.CountDocumentsAsync(new BsonDocument());
                    Console.WriteLine($"Total documents: {count}");
                    
                    var all = await collection.Find(new BsonDocument()).ToListAsync();
                    foreach(var d in all)
                    {
                        Console.WriteLine($"- {d.GetValue("tenantId", "N/A")}");
                    }
                }
                Console.WriteLine("Done.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}

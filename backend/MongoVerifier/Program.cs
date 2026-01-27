using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoVerifier;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Connecting to MongoDB...");
        var connectionString = "mongodb+srv://vatsamanoj_db_user:cVmX5s193hAEpBd4@cluster0.k6mzgv0.mongodb.net/ValoraReadDb?authSource=admin&retryWrites=true&w=majority&tls=true";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("ValoraReadDb");
        
        var collectionsToCheck = new[] { "full_GLAccount", "full_EmployeePayroll", "full_JournalEntry" };

        foreach (var collectionName in collectionsToCheck)
        {
            Console.WriteLine($"\n--- Checking collection: {collectionName} ---");
            var collection = database.GetCollection<BsonDocument>(collectionName);
            
            try 
            {
                var count = await collection.CountDocumentsAsync(new BsonDocument());
                Console.WriteLine($"Total documents: {count}");

                if (count > 0)
                {
                    var documents = await collection.Find(new BsonDocument())
                        .Sort(Builders<BsonDocument>.Sort.Descending("_projectedAt"))
                        .Limit(3)
                        .ToListAsync();
                        
                    foreach (var doc in documents)
                    {
                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine(doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true }));
                    }
                }
                else
                {
                    Console.WriteLine("No documents found yet.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing MongoDB: {ex.Message}");
            }
        }
    }
}

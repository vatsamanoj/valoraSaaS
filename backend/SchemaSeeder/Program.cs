using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace SchemaSeeder;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Connecting to MongoDB...");
        
        // Build configuration from appsettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Valora.Api"))
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = configuration["MongoDb:ConnectionString"]!;
        var databaseName = configuration["MongoDb:DatabaseName"]!;
        
        Console.WriteLine($"Database: {databaseName}");
        
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        // PlatformObjectTemplate is the correct collection for dynamic screen schemas
        var collection = database.GetCollection<BsonDocument>("PlatformObjectTemplate");

        var patientSchema = new
        {
            objectType = "Master",
            fields = new Dictionary<string, object>
            {
                ["Name"] = new
                {
                    required = true,
                    maxLength = 100,
                    storage = "Core", // Maps to 'patients' table
                    ui = new { type = "text", label = "Full Name" }
                },
                ["Age"] = new
                {
                    required = false,
                    storage = "Core", // Maps to 'patients' table
                    ui = new { type = "number", label = "Age" }
                },
                ["Gender"] = new
                {
                    required = false,
                    maxLength = 20,
                    storage = "Core", // Maps to 'patients' table
                    ui = new { type = "select", label = "Gender", options = new[] { "Male", "Female", "Other" } }
                },
                ["Uhid"] = new
                {
                    required = false,
                    maxLength = 50,
                    unique = true,
                    storage = "Core", // Maps to 'patients' table
                    ui = new { type = "text", label = "UHID" }
                },
                ["BloodGroup"] = new
                {
                    required = false,
                    storage = "Extension", // Maps to 'entity_extensions' table
                    ui = new { type = "select", label = "Blood Group", options = new[] { "A+", "A-", "B+", "B-", "O+", "O-", "AB+", "AB-" } }
                },
                ["InsuranceProvider"] = new
                {
                    required = false,
                    maxLength = 100,
                    storage = "Extension", // Maps to 'entity_extensions' table
                    ui = new { type = "text", label = "Insurance Provider" }
                }
            },
            ui = new
            {
                title = "Patient Registration",
                icon = "user",
                layout = new[] { "Name", "Age", "Gender", "Uhid", "BloodGroup", "InsuranceProvider" },
                listFields = new[] { "Name", "Uhid", "Age", "Gender", "BloodGroup" },
                filterFields = new[] { "Name", "Uhid", "BloodGroup" }
            }
        };

        var schemaJson = JsonSerializer.Serialize(patientSchema);

        var doc = new BsonDocument
        {
            { "TenantId", "LAB_001" },
            { "Module", "Patient" },
            { "Version", 1 },
            { "SchemaJson", schemaJson },
            { "CreatedAt", DateTime.UtcNow }
        };

        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("TenantId", "LAB_001"),
            Builders<BsonDocument>.Filter.Eq("Module", "Patient")
        );

        await collection.ReplaceOneAsync(filter, doc, new ReplaceOptions { IsUpsert = true });

        Console.WriteLine("Schema seeded successfully for Tenant: LAB_001, Module: Patient");
    }
}

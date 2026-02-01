using Microsoft.Extensions.Configuration;
using MongoVerifier;

Console.WriteLine("=== Valora Sales Order MongoDB Verification Tool ===\n");

// Build configuration from appsettings
var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Valora.Api"))
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();

var connectionString = configuration["MongoDb:ConnectionString"]!;
var databaseName = configuration["MongoDb:DatabaseName"]!;
var tenantId = args.Length > 0 ? args[0] : "valora";

Console.WriteLine($"Connection: {connectionString.Substring(0, Math.Min(50, connectionString.Length))}...");
Console.WriteLine($"Database: {databaseName}");
Console.WriteLine($"Tenant: {tenantId}\n");

try
{
    var verifier = new SalesOrderVerification(connectionString, databaseName, tenantId, Console.Out);

    // Run all verifications
    var allPassed = await verifier.RunAllVerificationsAsync();

    // Optionally print detailed schema for V1
    if (args.Contains("--details") || args.Contains("-d"))
    {
        await verifier.PrintSchemaDetailsAsync("v1");
    }

    Environment.Exit(allPassed ? 0 : 1);
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    Environment.Exit(1);
}

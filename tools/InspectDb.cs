using Npgsql;
using System;
using System.Data;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string connString = "Host=aws-1-ap-south-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ndzbkywblnbbshpxuqzk;Password=Ranwar@Jan2025;Timeout=30;Pooling=true;SslMode=Require;Trust Server Certificate=true;";

        try
        {
            using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();
            Console.WriteLine("Connected.");

            // Check OutboxMessages
            Console.WriteLine("Checking OutboxMessages...");
            var sql = "SELECT COUNT(*) FROM \"OutboxMessages\"";
            using var cmd = new NpgsqlCommand(sql, conn);
            var count = await cmd.ExecuteScalarAsync();
            Console.WriteLine($"Total Outbox Messages: {count}");

            sql = "SELECT \"Status\", COUNT(*) FROM \"OutboxMessages\" GROUP BY \"Status\"";
            using var cmd2 = new NpgsqlCommand(sql, conn);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                Console.WriteLine($"Status: {reader2[0]}, Count: {reader2[1]}");
            }
            reader2.Close();

            // Check OutboxMessages
        sql = "SELECT \"Topic\", \"Status\", COUNT(*) FROM \"OutboxMessages\" GROUP BY \"Topic\", \"Status\"";
        using var cmd4 = new NpgsqlCommand(sql, conn);
        using var reader4 = await cmd4.ExecuteReaderAsync();
        while (await reader4.ReadAsync())
        {
            Console.WriteLine($"Topic: {reader4[0]}, Status: {reader4[1]}, Count: {reader4[2]}");
        }

            // Check JournalEntries
            Console.WriteLine("Checking JournalEntries...");
            sql = "SELECT COUNT(*) FROM \"JournalEntry\"";
            using var cmd3 = new NpgsqlCommand(sql, conn);
            var countJe = await cmd3.ExecuteScalarAsync();
            Console.WriteLine($"Total Journal Entries: {countJe}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

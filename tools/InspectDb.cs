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

            // 2. Search for Triggers on OutboxMessages
            Console.WriteLine("Searching for Triggers on OutboxMessages...");
            sql = @"
                SELECT tgname, pg_get_triggerdef(oid)
                FROM pg_trigger
                WHERE tgrelid = 'public.""OutboxMessages""'::regclass;
            ";
            
            using var cmd2 = new NpgsqlCommand(sql, conn);
            using var reader2 = await cmd2.ExecuteReaderAsync();
            while (await reader2.ReadAsync())
            {
                Console.WriteLine($"--- Trigger: {reader2["tgname"]} ---");
                Console.WriteLine(reader2["pg_get_triggerdef"]);
                Console.WriteLine("------------------------------------------------");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

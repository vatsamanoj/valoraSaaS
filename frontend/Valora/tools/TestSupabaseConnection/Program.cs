using Npgsql;
using System;
using System.Linq;
using System.Net;

class Program
{
    static void Main(string[] args)
    {
        string host = "db.ndzbkywblnbbshpxuqzk.supabase.co";
        string ip = host;
        
        try 
        {
            var addresses = Dns.GetHostAddresses(host);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ipv4 != null) 
            {
                ip = ipv4.ToString();
                Console.WriteLine($"Resolved {host} to IPv4: {ip}");
            }
        }
        catch {}

        // Testing Connection Pooler (IPv4/IPv6)
        string connString = $"Host={host};Port=5432;Database=postgres;Username=postgres;Password=Ranwar@Jan2025;Timeout=10;Pooling=false;SslMode=Require;Trust Server Certificate=true";

        Console.WriteLine("Testing Supabase Pooler Connection...");
        Console.WriteLine($"Target: {ip}:6543");
        Console.WriteLine("User: postgres.ndzbkywblnbbshpxuqzk"); 

        try
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                Console.WriteLine("SUCCESS: Connected to Supabase!");
                
                using (var cmd = new NpgsqlCommand(@"
                    DROP TABLE IF EXISTS ""patients"";
                    DROP TABLE IF EXISTS ""entity_extensions"";
                    DROP TABLE IF EXISTS ""OutboxMessages"";
                    DROP TABLE IF EXISTS ""PlatformObjectData"";
                    DROP TABLE IF EXISTS ""__EFMigrationsHistory"";
                ", conn))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Dropped all tables successfully.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR: Failed to connect to Supabase.");
            Console.WriteLine(ex.Message);
            
            if (ex.InnerException != null)
            {
                 Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
            }
        }
    }
}

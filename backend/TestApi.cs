using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        using var client = new HttpClient();
        var content = new StringContent("{\"Name\": \"TestEntity\", \"Value\": 123}", Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", "LAB_001");
        client.DefaultRequestHeaders.Add("X-User-Id", "tester");

        try
        {
            var response = await client.PostAsync("http://localhost:5028/api/data/test_module", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {response.StatusCode}");
            Console.WriteLine($"Body: {responseBody}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}

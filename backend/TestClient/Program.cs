using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TestClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Tenant-Id", "LAB_001");
            client.DefaultRequestHeaders.Add("X-User-Id", "tester");
            client.DefaultRequestHeaders.Add("X-Environment", "dev");

            string baseUrl = "http://localhost:5028";

            // 1. Create GL Account (for Lookup)
            Console.WriteLine("\nCreating GL Account 'Revenue Account'...");
            var glPayload = new
            {
                AccountCode = "4000-" + new Random().Next(100, 999),
                Name = "Revenue Account",
                Type = "Revenue"
            };
            var glContent = new StringContent(JsonSerializer.Serialize(glPayload), Encoding.UTF8, "application/json");
            var glRes = await client.PostAsync($"{baseUrl}/api/finance/gl-accounts", glContent);
            Console.WriteLine($"Create GL: {glRes.StatusCode}");

            // 2. Create Material (for Items Lookup)
            Console.WriteLine("\nCreating Material 'WIDGET-01'...");
            var matPayload = new
            {
                MaterialCode = "WIDGET-01",
                Description = "Test Widget",
                BaseUnitOfMeasure = "EA",
                StandardPrice = 10.00
            };
            var matContent = new StringContent(JsonSerializer.Serialize(matPayload), Encoding.UTF8, "application/json");
            var matRes = await client.PostAsync($"{baseUrl}/api/materials/materials", matContent);
            Console.WriteLine($"Create Material: {matRes.StatusCode}");

            // CHECK SCHEMA
            Console.WriteLine("\nChecking Schema for 'shouldPost'...");
            var schemaRes = await client.GetAsync($"{baseUrl}/api/platform/object/SalesOrder/latest");
            var schemaBody = await schemaRes.Content.ReadAsStringAsync();
            Console.WriteLine($"Schema Body: {schemaBody}");

            Console.WriteLine("Waiting for Projections...");
            await Task.Delay(4000);

            // 3. Create Valid SalesOrder
            Console.WriteLine("\nCreating Valid SalesOrder...");
            var payload = new
            {
                Action = "Create",
                Data = new
                {
                    OrderNumber = "SO-TEST-" + new Random().Next(1000, 9999),
                    OrderDate = DateTime.UtcNow,
                    CustomerId = "Revenue Account", // Valid
                    Currency = "USD",
                    Items = new[] 
                    {
                        new { MaterialCode = "WIDGET-01", Quantity = 5 } // Valid
                    }
                }
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{baseUrl}/api/data/SalesOrder", content);
            Console.WriteLine($"Valid SO Response: {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseBody);

            string id = null;
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;
                if (root.TryGetProperty("data", out var data) && data.TryGetProperty("id", out var idProp))
                    id = idProp.GetString();
                else if (root.TryGetProperty("id", out var idDirect))
                    id = idDirect.GetString();
            }
            catch { }

            // 4. Test Invalid Customer
            Console.WriteLine("\nTesting Invalid Customer...");
            var invalidCustomerPayload = new
            {
                Action = "Create",
                Data = new
                {
                    OrderNumber = "SO-TEST-INV-CUST",
                    OrderDate = DateTime.UtcNow,
                    CustomerId = "Invalid Account", // Invalid
                    Currency = "USD",
                    Items = new[] 
                    {
                        new { MaterialCode = "WIDGET-01", Quantity = 5 }
                    }
                }
            };
            var invCustContent = new StringContent(JsonSerializer.Serialize(invalidCustomerPayload), Encoding.UTF8, "application/json");
            var invCustRes = await client.PostAsync($"{baseUrl}/api/data/SalesOrder", invCustContent);
            Console.WriteLine($"Invalid Customer Response: {invCustRes.StatusCode}"); // Expect BadRequest
            Console.WriteLine(await invCustRes.Content.ReadAsStringAsync());

            // 5. Test Empty Items
            Console.WriteLine("\nTesting Empty Items...");
            var emptyItemsPayload = new
            {
                Action = "Create",
                Data = new
                {
                    OrderNumber = "SO-TEST-EMPTY",
                    OrderDate = DateTime.UtcNow,
                    CustomerId = "Revenue Account",
                    Currency = "USD",
                    Items = new object[] { } // Empty
                }
            };
            var emptyItemsContent = new StringContent(JsonSerializer.Serialize(emptyItemsPayload), Encoding.UTF8, "application/json");
            var emptyItemsRes = await client.PostAsync($"{baseUrl}/api/data/SalesOrder", emptyItemsContent);
            Console.WriteLine($"Empty Items Response: {emptyItemsRes.StatusCode}"); // Expect BadRequest
            Console.WriteLine(await emptyItemsRes.Content.ReadAsStringAsync());
            // 6. Test Auto-Posting (Journal Entry Creation)
            Console.WriteLine("\nTesting Auto-Posting (Check JE)...");
            
            // Wait for processing of Valid SalesOrder (which should have auto-posted due to Schema change)
            await Task.Delay(4000); 

            // We need to query Journal Entries to see if one was created for our Sales Order
            // The Reference in JE should match the SO ID.
            
            // Extract ID from Step 3
            if (!string.IsNullOrEmpty(id))
            {
                var jeQuery = new
                {
                    Module = "JournalEntry",
                    Options = new 
                    {
                        Filters = new { Reference = id }
                    }
                };
                var jeContent = new StringContent(JsonSerializer.Serialize(jeQuery), Encoding.UTF8, "application/json");
                var jeRes = await client.PostAsync($"{baseUrl}/api/query/ExecuteQuery", jeContent);
                Console.WriteLine($"JE Query Response: {jeRes.StatusCode}");
                var jeBody = await jeRes.Content.ReadAsStringAsync();
                Console.WriteLine(jeBody);
                
                if (jeBody.Contains(id)) Console.WriteLine("SUCCESS: Journal Entry found for Sales Order!");
                else Console.WriteLine("FAILURE: No Journal Entry found.");
            }
            else
            {
                Console.WriteLine("FAILURE: No Sales Order ID found to check.");
            }
        }
    }
}

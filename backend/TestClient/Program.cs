using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Collections.Generic;

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

            // Define Schemas
            var doctorSchema = new
            {
                module = "Doctor",
                objectType = "Master",
                fields = new
                {
                    Name = new { required = true, ui = new { type = "text" } },
                    Specialization = new { ui = new { type = "text" } },
                    LicenseNumber = new { ui = new { type = "text" } }
                }
            };

            var patientSchema = new
            {
                module = "Patient",
                objectType = "Master",
                fields = new
                {
                    Name = new { required = true, ui = new { type = "text" } },
                    Age = new { ui = new { type = "number" } },
                    Gender = new { ui = new { type = "text" } }
                }
            };

            var billingSchema = new
            {
                module = "PathologyBilling",
                objectType = "Transaction",
                fields = new
                {
                    BillNo = new { required = true, ui = new { type = "text" } },
                    Amount = new { ui = new { type = "number" } },
                    PatientId = new { ui = new { type = "text" } } // Assuming simplified relation for now
                }
            };

            // 1. Publish Schemas (Save Draft)
            // Note: In real app, we might need to publish via a specific endpoint or just saving draft triggers sync if configured.
            // Based on previous analysis, `SaveDraft` triggers `ReplaceOneAsync` in Mongo.
            // AND we need `SchemaSyncService` to run.
            // The `KafkaConsumer` listens to events, or `SchemaSyncService` is called directly?
            // Actually, `SchemaSyncService` is usually called when schema changes.
            // In `PlatformObjectController.SaveDraft`, it saves to Mongo.
            // Does it trigger Sync?
            // The code I read earlier for `PlatformObjectController` does NOT seem to trigger Sync or publish event directly in `SaveDraft`.
            // However, there is `ScreenPublishService` mentioned in memories.
            // And `KafkaConsumer` triggers `ISchemaSyncService.SyncTableAsync`.
            // So I need to ensure the schema is "Published" or synced.
            // Let's try `SaveDraft` and see if there is a manual sync endpoint or if I need to rely on something else.
            // Wait, I saw `api/tenants/sync-schema` in the original TestClient!
            // Let's check `TenantController` for that endpoint.
            
            await PublishSchema(client, "Doctor", doctorSchema);
            await PublishSchema(client, "Patient", patientSchema);
            await PublishSchema(client, "PathologyBilling", billingSchema);

            // 2. Create Data
            Console.WriteLine("\n--- Creating Data ---");
            var doctorId = await CreateEntity(client, "Doctor", new { Name = "Dr. Smith", Specialization = "Cardiology", LicenseNumber = "LIC-123" });
            var patientId = await CreateEntity(client, "Patient", new { Name = "John Doe", Age = 30, Gender = "Male" });
            var billingId = await CreateEntity(client, "PathologyBilling", new { BillNo = "BILL-001", Amount = 150.50, PatientId = patientId });

            // 4. Test Finance Module
            Console.WriteLine("\n--- Testing Finance Module ---");
            
            // Create GL Accounts (Unique codes to avoid duplicates)
            var suffix = DateTime.Now.Ticks.ToString().Substring(12);
            var cashCode = "1000" + suffix;
            var revCode = "4000" + suffix;

            var cashAccountId = await CreateGLAccount(client, cashCode, "Cash on Hand", "Asset");
            var revenueAccountId = await CreateGLAccount(client, revCode, "Sales Revenue", "Revenue");

            if (cashAccountId != null && revenueAccountId != null)
            {
                // Post Journal Entry (Cash Sale)
                await PostJournalEntry(client, cashAccountId, revenueAccountId, 1000.00m, "Daily Sales");
            }
        }

        static async Task<string?> CreateGLAccount(HttpClient client, string code, string name, string type)
        {
            Console.WriteLine($"Creating GL Account {code} ({name})...");
            var payload = new { AccountCode = code, Name = name, Type = type };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://localhost:5028/api/finance/gl-accounts", content);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Success!");
                var json = JsonNode.Parse(responseBody);
                return json?["data"]?["id"]?.ToString();
            }
            else
            {
                Console.WriteLine($"Failed: {response.StatusCode} - {responseBody}");
                return null;
            }
        }

        static async Task PostJournalEntry(HttpClient client, string debitAccountId, string creditAccountId, decimal amount, string description)
        {
            Console.WriteLine("Posting Journal Entry...");
            var payload = new
            {
                PostingDate = DateTime.UtcNow,
                DocumentNumber = $"JE-{DateTime.UtcNow.Ticks}",
                Description = description,
                Reference = "REF-001",
                Lines = new List<object>
                {
                    new { GLAccountId = debitAccountId, Debit = amount, Credit = 0, Description = "Debit Cash" },
                    new { GLAccountId = creditAccountId, Debit = 0, Credit = amount, Description = "Credit Revenue" }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://localhost:5028/api/finance/journal-entries", content);
            
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Journal Entry Posted Successfully!");
            }
            else
            {
                Console.WriteLine($"Failed to Post Journal: {response.StatusCode} - {responseBody}");
            }
        }

        static async Task PublishSchema(HttpClient client, string module, object schema)
        {
            Console.WriteLine($"Publishing Schema for {module}...");
            var json = JsonSerializer.Serialize(schema);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            // Using the endpoint from original TestClient which seems to force sync
            // Re-constructing the payload to match what `TenantController/SyncSchema` expects if it exists
            // The original TestClient used `api/tenants/sync-schema` with a specific payload structure.
            // Let's try to match that structure.
            var syncPayload = new
            {
                TenantId = "LAB_001",
                Module = module,
                Version = 1,
                // ... other fields if needed, but the Controller expects ModuleSchema which has Fields etc.
                // Wait, TenantController.SyncSchema expects [FromBody] ModuleSchema schema.
                // Let's assume 'schema' object passed here matches ModuleSchema structure roughly or we adapt it.
                // The 'schema' object defined in Main has 'module', 'objectType', 'fields'.
                // We need to add 'TenantId' and 'Version' to it.
            };
            
            // Hacky merge for test client simplicity
            var schemaJson = JsonSerializer.Serialize(schema);
            var node = JsonNode.Parse(schemaJson)?.AsObject();
            if (node != null)
            {
                node["tenantId"] = "LAB_001";
                node["version"] = 1;
            }

            var syncContent = new StringContent(node?.ToString() ?? "", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://localhost:5028/api/tenants/sync-schema", syncContent);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Schema Synced: OK");
            }
            else
            {
                Console.WriteLine($"Schema Sync Failed: {response.StatusCode}");
            }
        }

        static async Task<string?> CreateEntity(HttpClient client, string module, object data)
        {
            Console.WriteLine($"Creating {module}...");
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"http://localhost:5028/api/data/{module}", content);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Create Response: OK");
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseJson = JsonNode.Parse(responseBody);
                var id = responseJson?["data"]?["id"]?.ToString();
                // Fallback for wrapped/unwrapped
                if (id == null) id = responseJson?["id"]?.ToString();
                
                Console.WriteLine($"Created ID: {id}");
                return id;
            }
            else
            {
                Console.WriteLine($"Create Failed: {response.StatusCode}");
                return null;
            }
        }

        static async Task VerifyEntity(HttpClient client, string module, string? id)
        {
            if (string.IsNullOrEmpty(id)) return;
            
            Console.WriteLine($"Verifying {module} {id}...");
            // QueryController is usually api/query or similar, let's assume QueryController exists and maps to api/query
            // Based on file list, QueryController.cs exists.
            var response = await client.GetAsync($"http://localhost:5028/api/query/{module}/{id}");
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Verify Response: OK");
            }
            else
            {
                Console.WriteLine($"Verify Failed: {response.StatusCode}");
            }
        }
    }
}
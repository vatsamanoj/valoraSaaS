using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Application.Schemas.TemplateConfig;

namespace SchemaInjector;

/// <summary>
/// Injects Smart Projection configuration into existing MongoDB schemas for all versions.
/// This ensures backward compatibility while enabling smart projection features.
/// 
/// NOTE: ModuleSchema is a C# model class, NOT a MongoDB collection.
/// Dynamic screen schemas are stored in the PlatformObjectTemplate collection.
/// </summary>
class InjectSmartProjectionConfig
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Smart Projection Config Injector ===\n");

        // Build configuration from appsettings
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Valora.Api"))
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = configuration["MongoDb:ConnectionString"]!;
        var databaseName = configuration["MongoDb:DatabaseName"]!;
        
        Console.WriteLine($"Database: {databaseName}");
        Console.WriteLine();

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        
        // PlatformObjectTemplate is the correct collection for dynamic screen schemas
        // ModuleSchema is a C# model class, NOT a MongoDB collection
        var collection = database.GetCollection<BsonDocument>("PlatformObjectTemplate");

        // Get all schemas
        var schemas = await collection.Find(_ => true).ToListAsync();
        Console.WriteLine($"Found {schemas.Count} schemas to process\n");

        int updated = 0;
        int skipped = 0;
        int failed = 0;

        foreach (var schema in schemas)
        {
            try
            {
                // PlatformObjectTemplate uses tenantId (lowercase) field
                var tenantId = schema.Contains("tenantId") ? schema["tenantId"].AsString : "unknown";
                
                Console.WriteLine($"Processing: {tenantId}");

                // Check if already has SmartProjection
                if (schema.Contains("SmartProjection"))
                {
                    Console.WriteLine($"  -> Already has SmartProjection, skipping");
                    skipped++;
                    continue;
                }

                // Determine object type from schema structure
                var objectType = "Transaction"; // Default
                if (schema.Contains("environments"))
                {
                    var envDoc = schema["environments"].AsBsonDocument;
                    if (envDoc.Names.Any())
                    {
                        var firstEnv = envDoc[envDoc.Names.First()].AsBsonDocument;
                        if (firstEnv.Contains("screens"))
                        {
                            var screens = firstEnv["screens"].AsBsonDocument;
                            if (screens.Names.Any())
                            {
                                var firstScreen = screens[screens.Names.First()].AsBsonDocument;
                                if (firstScreen.Names.Any())
                                {
                                    var firstVersion = firstScreen[firstScreen.Names.First()].AsBsonDocument;
                                    if (firstVersion.Contains("shouldPost"))
                                    {
                                        objectType = firstVersion["shouldPost"].AsBoolean ? "Transaction" : "Master";
                                    }
                                }
                            }
                        }
                    }
                }

                // Create appropriate smart projection config
                var smartProjection = CreateSmartProjectionConfig(objectType, tenantId);

                // Convert to BsonDocument
                var configJson = JsonSerializer.Serialize(smartProjection, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                });
                var configDoc = BsonDocument.Parse(configJson);

                // Update schema
                var update = Builders<BsonDocument>.Update.Set("SmartProjection", configDoc);
                await collection.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", schema["_id"]),
                    update);

                Console.WriteLine($"  -> Injected SmartProjection config ({smartProjection.Indexes.Count} indexes)");
                updated++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  -> ERROR: {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"\n=== Summary ===");
        Console.WriteLine($"Updated: {updated}");
        Console.WriteLine($"Skipped: {skipped}");
        Console.WriteLine($"Failed:  {failed}");
        Console.WriteLine($"Total:   {schemas.Count}");
    }

    /// <summary>
    /// Creates a SmartProjectionConfig appropriate for the module and object type.
    /// </summary>
    private static SmartProjectionConfig CreateSmartProjectionConfig(string objectType, string module)
    {
        var config = new SmartProjectionConfig
        {
            AutoOptimize = true,
            QueryPatternTracking = new QueryPatternConfig
            {
                Enabled = true,
                SampleRate = 0.1,
                MaxPatterns = 1000,
                MinQueryCountForAutoIndex = 100,
                AnalysisWindowHours = 24,
                AutoCreateIndexes = true,
                AutoSuggestDenormalizations = true
            }
        };

        // Add module-specific indexes
        switch (module.ToLowerInvariant())
        {
            case "sales":
                AddSalesIndexes(config, objectType);
                break;

            case "finance":
            case "financial":
                AddFinanceIndexes(config, objectType);
                break;

            case "materials":
            case "inventory":
                AddMaterialsIndexes(config, objectType);
                break;

            case "humancapital":
            case "hr":
                AddHumanCapitalIndexes(config, objectType);
                break;

            default:
                AddGenericIndexes(config, objectType);
                break;
        }

        // Add common indexes for all schemas
        AddCommonIndexes(config);

        return config;
    }

    private static void AddSalesIndexes(SmartProjectionConfig config, string objectType)
    {
        if (objectType == "Transaction")
        {
            // Sales Order specific indexes
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_status_date",
                Fields = new Dictionary<string, int> { { "Status", 1 }, { "OrderDate", -1 } },
                Type = IndexType.Compound
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_customer",
                Fields = new Dictionary<string, int> { { "CustomerId", 1 } },
                Type = IndexType.Standard
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_order_number",
                Fields = new Dictionary<string, int> { { "OrderNumber", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_billing_status",
                Fields = new Dictionary<string, int> { { "BillingStatus", 1 }, { "OrderDate", -1 } },
                Type = IndexType.Compound
            });

            // Text search index
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_search",
                Fields = new Dictionary<string, int> { { "CustomerName", 1 }, { "Notes", 1 } },
                Type = IndexType.Text
            });
        }
        else // Master data
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_customer_code",
                Fields = new Dictionary<string, int> { { "Code", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_customer_name",
                Fields = new Dictionary<string, int> { { "Name", 1 } },
                Type = IndexType.Standard,
                Collation = new CollationConfig { Locale = "en", Strength = 2 }
            });
        }
    }

    private static void AddFinanceIndexes(SmartProjectionConfig config, string objectType)
    {
        if (objectType == "Transaction")
        {
            // Journal Entry specific indexes
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_posting_date",
                Fields = new Dictionary<string, int> { { "PostingDate", -1 } },
                Type = IndexType.Standard
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_entry_number",
                Fields = new Dictionary<string, int> { { "EntryNumber", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_status_period",
                Fields = new Dictionary<string, int> { { "Status", 1 }, { "FiscalPeriod", 1 } },
                Type = IndexType.Compound
            });

            // Partial index for unposted entries
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_unposted",
                Fields = new Dictionary<string, int> { { "Status", 1 } },
                Type = IndexType.Standard,
                IsSparse = true,
                PartialFilterExpression = "{ \"Status\": \"Draft\" }"
            });
        }
        else // GL Accounts, etc.
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_account_code",
                Fields = new Dictionary<string, int> { { "AccountCode", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_account_type",
                Fields = new Dictionary<string, int> { { "AccountType", 1 }, { "AccountCode", 1 } },
                Type = IndexType.Compound
            });
        }
    }

    private static void AddMaterialsIndexes(SmartProjectionConfig config, string objectType)
    {
        if (objectType == "Transaction")
        {
            // Stock Movement indexes
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_movement_date",
                Fields = new Dictionary<string, int> { { "MovementDate", -1 } },
                Type = IndexType.Standard
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_material_warehouse",
                Fields = new Dictionary<string, int> { { "MaterialId", 1 }, { "WarehouseId", 1 } },
                Type = IndexType.Compound
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_document_number",
                Fields = new Dictionary<string, int> { { "DocumentNumber", 1 } },
                Type = IndexType.Standard
            });
        }
        else // Material Master
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_material_code",
                Fields = new Dictionary<string, int> { { "MaterialCode", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_material_type",
                Fields = new Dictionary<string, int> { { "MaterialType", 1 } },
                Type = IndexType.Standard
            });
        }
    }

    private static void AddHumanCapitalIndexes(SmartProjectionConfig config, string objectType)
    {
        if (objectType == "Transaction")
        {
            // Payroll indexes
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_payroll_period",
                Fields = new Dictionary<string, int> { { "PayrollPeriod", 1 }, { "EmployeeId", 1 } },
                Type = IndexType.Compound
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_employee_status",
                Fields = new Dictionary<string, int> { { "EmployeeId", 1 }, { "Status", 1 } },
                Type = IndexType.Compound
            });
        }
        else // Employee Master
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_employee_code",
                Fields = new Dictionary<string, int> { { "EmployeeCode", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_department",
                Fields = new Dictionary<string, int> { { "DepartmentId", 1 } },
                Type = IndexType.Standard
            });
        }
    }

    private static void AddGenericIndexes(SmartProjectionConfig config, string objectType)
    {
        if (objectType == "Transaction")
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_status_date",
                Fields = new Dictionary<string, int> { { "Status", 1 }, { "DocumentDate", -1 } },
                Type = IndexType.Compound
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_document_number",
                Fields = new Dictionary<string, int> { { "DocumentNumber", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });
        }
        else
        {
            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_code",
                Fields = new Dictionary<string, int> { { "Code", 1 } },
                Type = IndexType.Standard,
                IsUnique = true
            });

            config.Indexes.Add(new IndexConfig
            {
                Name = "idx_name",
                Fields = new Dictionary<string, int> { { "Name", 1 } },
                Type = IndexType.Standard
            });
        }
    }

    private static void AddCommonIndexes(SmartProjectionConfig config)
    {
        // IsActive flag for soft deletes
        config.Indexes.Add(new IndexConfig
        {
            Name = "idx_is_active",
            Fields = new Dictionary<string, int> { { "IsActive", 1 } },
            Type = IndexType.Standard
        });

        // Created/Updated timestamps
        config.Indexes.Add(new IndexConfig
        {
            Name = "idx_created_at",
            Fields = new Dictionary<string, int> { { "CreatedAt", -1 } },
            Type = IndexType.Standard
        });

        config.Indexes.Add(new IndexConfig
        {
            Name = "idx_updated_at",
            Fields = new Dictionary<string, int> { { "UpdatedAt", -1 } },
            Type = IndexType.Standard
        });
    }
}

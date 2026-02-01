using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MongoDB.Bson;

// Script to inject Sales Order template with new configuration into MongoDB
Console.WriteLine("=== Injecting Sales Order Template Extension into MongoDB ===\n");

// Build configuration from appsettings
var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Valora.Api"))
    .AddJsonFile("appsettings.Development.json", optional: false)
    .Build();

var connectionString = configuration["MongoDb:ConnectionString"]!;
var databaseName = configuration["MongoDb:DatabaseName"]!;

Console.WriteLine($"Database: {databaseName}");

try
{
    var client = new MongoClient(connectionString);
    var database = client.GetDatabase(databaseName);
    var collection = database.GetCollection<BsonDocument>("PlatformObjectTemplate");

    // Build the extended SalesOrder schema document
    var salesOrderSchema = new BsonDocument
    {
        ["tenantId"] = "LAB003",
        ["environments"] = new BsonDocument
        {
            ["prod"] = new BsonDocument
            {
                ["screens"] = new BsonDocument
                {
                    ["SalesOrder"] = new BsonDocument
                    {
                        ["v1"] = new BsonDocument
                        {
                            ["isPublished"] = true,
                            ["shouldPost"] = true,
                            ["fields"] = new BsonDocument
                            {
                                ["OrderNumber"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Order Number",
                                    ["required"] = true,
                                    ["readonly"] = true,
                                    ["isSystem"] = true
                                },
                                ["OrderDate"] = new BsonDocument
                                {
                                    ["type"] = "Date",
                                    ["label"] = "Order Date",
                                    ["required"] = true,
                                    ["defaultValue"] = "Today",
                                    ["isSystem"] = true
                                },
                                ["CustomerId"] = new BsonDocument
                                {
                                    ["type"] = "Lookup",
                                    ["label"] = "Customer (GL Account)",
                                    ["required"] = true,
                                    ["isSystem"] = true,
                                    ["ui"] = new BsonDocument
                                    {
                                        ["type"] = "lookup",
                                        ["lookupModule"] = "GLAccount",
                                        ["lookupField"] = "Name",
                                        ["filter"] = new BsonDocument { ["Type"] = 3 }
                                    }
                                },
                                ["Currency"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Currency",
                                    ["required"] = true,
                                    ["defaultValue"] = "USD",
                                    ["isSystem"] = true
                                },
                                ["TotalAmount"] = new BsonDocument
                                {
                                    ["type"] = "Money",
                                    ["label"] = "Total Amount",
                                    ["readonly"] = true,
                                    ["isSystem"] = true
                                },
                                ["Status"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Status",
                                    ["readonly"] = true,
                                    ["isSystem"] = true
                                },
                                ["ShippingAddress"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Shipping Address",
                                    ["multiline"] = true,
                                    ["isSystem"] = true
                                },
                                ["BillingAddress"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Billing Address",
                                    ["multiline"] = true,
                                    ["isSystem"] = true
                                },
                                ["Items"] = new BsonDocument
                                {
                                    ["type"] = "Grid",
                                    ["label"] = "Order Items",
                                    ["isSystem"] = true,
                                    ["required"] = true,
                                    ["columns"] = new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            ["key"] = "MaterialCode",
                                            ["label"] = "Product",
                                            ["type"] = "lookup",
                                            ["lookupModule"] = "Material",
                                            ["lookupField"] = "MaterialCode",
                                            ["filter"] = new BsonDocument(),
                                            ["searchStrategy"] = "wildcard"
                                        },
                                        new BsonDocument { ["key"] = "Quantity", ["label"] = "Qty", ["type"] = "Number" },
                                        new BsonDocument { ["key"] = "UnitPrice", ["label"] = "Unit Price", ["type"] = "Money" },
                                        new BsonDocument { ["key"] = "LineTotal", ["label"] = "Total", ["type"] = "Money", ["readonly"] = true }
                                    }
                                },
                                ["SpecialInstructions"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Special Instructions (Custom)",
                                    ["required"] = false
                                },
                                ["Priority"] = new BsonDocument
                                {
                                    ["type"] = "Text",
                                    ["label"] = "Delivery Priority (Custom)",
                                    ["required"] = false,
                                    ["options"] = new BsonArray { "High", "Medium", "Low" }
                                }
                            },
                            ["ui"] = new BsonDocument
                            {
                                ["listFields"] = new BsonArray { "OrderNumber", "OrderDate", "CustomerId", "TotalAmount", "Status", "Priority" },
                                ["formGroups"] = new BsonArray
                                {
                                    new BsonDocument
                                    {
                                        ["title"] = "Order Header",
                                        ["fields"] = new BsonArray { "OrderNumber", "OrderDate", "CustomerId", "Currency", "Status" }
                                    },
                                    new BsonDocument
                                    {
                                        ["title"] = "Logistics",
                                        ["fields"] = new BsonArray { "ShippingAddress", "BillingAddress", "Priority", "SpecialInstructions" }
                                    },
                                    new BsonDocument
                                    {
                                        ["title"] = "Line Items",
                                        ["fields"] = new BsonArray { "Items" }
                                    },
                                    new BsonDocument
                                    {
                                        ["title"] = "Financials",
                                        ["fields"] = new BsonArray { "TotalAmount" }
                                    }
                                }
                            },
                            // ===== NEW: Calculation Rules =====
                            ["calculationRules"] = new BsonDocument
                            {
                                ["serverSide"] = new BsonDocument
                                {
                                    ["lineItemCalculations"] = new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            ["targetField"] = "LineTotal",
                                            ["formula"] = "{Quantity} * {UnitPrice}",
                                            ["trigger"] = "onChange",
                                            ["fields"] = new BsonArray { "Quantity", "UnitPrice" }
                                        }
                                    },
                                    ["documentCalculations"] = new BsonArray
                                    {
                                        new BsonDocument
                                        {
                                            ["targetField"] = "TotalAmount",
                                            ["formula"] = "SUM({Items.LineTotal})",
                                            ["trigger"] = "onLineChange"
                                        }
                                    },
                                    ["complexCalculations"] = new BsonArray()
                                },
                                ["clientSide"] = new BsonDocument
                                {
                                    ["onLoad"] = "console.log('Sales Order loaded');",
                                    ["onBeforeSave"] = "if (data.TotalAmount <= 0) { throw new Error('Total must be greater than 0'); }",
                                    ["customFunctions"] = new BsonDocument()
                                }
                            },
                            // ===== NEW: Document Totals =====
                            ["documentTotals"] = new BsonDocument
                            {
                                ["fields"] = new BsonDocument
                                {
                                    ["subTotal"] = new BsonDocument
                                    {
                                        ["source"] = "CALCULATED",
                                        ["formula"] = "SUM({Items.LineTotal})",
                                        ["label"] = "Sub Total",
                                        ["displayPosition"] = "footer",
                                        ["decimalPlaces"] = 2
                                    },
                                    ["taxTotal"] = new BsonDocument
                                    {
                                        ["source"] = "CALCULATED",
                                        ["formula"] = "0",
                                        ["label"] = "Tax",
                                        ["displayPosition"] = "footer",
                                        ["decimalPlaces"] = 2
                                    },
                                    ["grandTotal"] = new BsonDocument
                                    {
                                        ["source"] = "FORMULA",
                                        ["formula"] = "{SubTotal}",
                                        ["label"] = "Grand Total",
                                        ["displayPosition"] = "footer",
                                        ["decimalPlaces"] = 2,
                                        ["isReadOnly"] = true,
                                        ["highlight"] = true
                                    }
                                },
                                ["displayConfig"] = new BsonDocument
                                {
                                    ["layout"] = "stacked",
                                    ["position"] = "bottom",
                                    ["currencySymbol"] = "$",
                                    ["showSeparator"] = true
                                }
                            },
                            // ===== NEW: Attachment Configuration =====
                            ["attachmentConfig"] = new BsonDocument
                            {
                                ["documentLevel"] = new BsonDocument
                                {
                                    ["enabled"] = true,
                                    ["maxFiles"] = 10,
                                    ["maxFileSizeMB"] = 50,
                                    ["allowedTypes"] = new BsonArray { "pdf", "doc", "docx", "jpg", "png" },
                                    ["categories"] = new BsonArray
                                    {
                                        new BsonDocument { ["id"] = "contract", ["label"] = "Sales Contract", ["required"] = false },
                                        new BsonDocument { ["id"] = "po", ["label"] = "Purchase Order", ["required"] = false },
                                        new BsonDocument { ["id"] = "invoice", ["label"] = "Customer Invoice", ["required"] = false },
                                        new BsonDocument { ["id"] = "shipping", ["label"] = "Shipping Docs", ["required"] = false }
                                    },
                                    ["storageProvider"] = "primary"
                                },
                                ["lineLevel"] = new BsonDocument
                                {
                                    ["enabled"] = true,
                                    ["maxFiles"] = 3,
                                    ["maxFileSizeMB"] = 10,
                                    ["allowedTypes"] = new BsonArray { "jpg", "png", "pdf" },
                                    ["categories"] = new BsonArray
                                    {
                                        new BsonDocument { ["id"] = "product_image", ["label"] = "Product Image", ["required"] = false },
                                        new BsonDocument { ["id"] = "spec_sheet", ["label"] = "Specification", ["required"] = false }
                                    },
                                    ["storageProvider"] = "primary",
                                    ["gridColumn"] = new BsonDocument
                                    {
                                        ["width"] = "100px",
                                        ["showCount"] = true,
                                        ["allowPreview"] = true
                                    }
                                }
                            },
                            // ===== NEW: Cloud Storage Configuration =====
                            ["cloudStorage"] = new BsonDocument
                            {
                                ["providers"] = new BsonArray
                                {
                                    new BsonDocument
                                    {
                                        ["id"] = "primary",
                                        ["provider"] = "aws_s3",
                                        ["isDefault"] = true,
                                        ["config"] = new BsonDocument
                                        {
                                            ["bucketName"] = "valora-documents-prod",
                                            ["region"] = "us-east-1",
                                            ["basePath"] = "tenants/{tenantId}/salesorders/{documentId}",
                                            ["encryption"] = "AES256"
                                        },
                                        ["credentials"] = new BsonDocument
                                        {
                                            ["accessKeyId"] = "encrypted:AQIDAHg...",
                                            ["secretAccessKey"] = "encrypted:AQIDAHg..."
                                        },
                                        ["lifecycleRules"] = new BsonDocument
                                        {
                                            ["autoDeleteAfterDays"] = BsonNull.Value,
                                            ["moveToColdStorageAfterDays"] = 90
                                        }
                                    }
                                },
                                ["globalSettings"] = new BsonDocument
                                {
                                    ["virusScanEnabled"] = true,
                                    ["generateThumbnails"] = true,
                                    ["thumbnailSizes"] = new BsonArray { "100x100", "300x300" },
                                    ["allowedMimeTypes"] = new BsonArray
                                    {
                                        "application/pdf",
                                        "image/jpeg",
                                        "image/png",
                                        "application/msword",
                                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                                    },
                                    ["maxFileSizeMB"] = 100
                                }
                            }
                        }
                    }
                }
            }
        }
    };

    // Create filter to find existing document
    var filter = Builders<BsonDocument>.Filter.Eq("tenantId", "LAB003");
    
    // Check if document exists
    var existingDoc = collection.Find(filter).FirstOrDefault();
    
    if (existingDoc != null)
    {
        Console.WriteLine("Found existing document for LAB003, updating...");
        
        // Update the specific path: environments.prod.screens.SalesOrder.v1
        var update = Builders<BsonDocument>.Update.Set("environments.prod.screens.SalesOrder.v1", 
            salesOrderSchema["environments"].AsBsonDocument["prod"].AsBsonDocument["screens"].AsBsonDocument["SalesOrder"].AsBsonDocument["v1"]);
        
        var result = collection.UpdateOne(filter, update);
        Console.WriteLine($"Update result: Matched={result.MatchedCount}, Modified={result.ModifiedCount}");
    }
    else
    {
        Console.WriteLine("Creating new document for LAB003...");
        collection.InsertOne(salesOrderSchema);
        Console.WriteLine("Document inserted successfully");
    }

    // Verify the update
    var verifyDoc = collection.Find(filter).FirstOrDefault();
    if (verifyDoc != null)
    {
        Console.WriteLine("\n=== Verification ===");
        Console.WriteLine($"TenantId: {verifyDoc["tenantId"]}");
        
        var env = verifyDoc["environments"].AsBsonDocument;
        var prod = env["prod"].AsBsonDocument;
        var screens = prod["screens"].AsBsonDocument;
        var salesOrder = screens["SalesOrder"].AsBsonDocument;
        var v1 = salesOrder["v1"].AsBsonDocument;
        
        Console.WriteLine($"Environment: prod");
        Console.WriteLine($"Screen: SalesOrder");
        Console.WriteLine($"Version: v1");
        Console.WriteLine($"IsPublished: {v1["isPublished"]}");
        Console.WriteLine($"ShouldPost: {v1["shouldPost"]}");
        Console.WriteLine($"Has calculationRules: {v1.Contains("calculationRules")}");
        Console.WriteLine($"Has documentTotals: {v1.Contains("documentTotals")}");
        Console.WriteLine($"Has attachmentConfig: {v1.Contains("attachmentConfig")}");
        Console.WriteLine($"Has cloudStorage: {v1.Contains("cloudStorage")}");
        
        if (v1.Contains("calculationRules"))
        {
            var calcRules = v1["calculationRules"].AsBsonDocument;
            var serverSide = calcRules["serverSide"].AsBsonDocument;
            Console.WriteLine($"  - LineItemCalculations: {serverSide["lineItemCalculations"].AsBsonArray.Count}");
            Console.WriteLine($"  - DocumentCalculations: {serverSide["documentCalculations"].AsBsonArray.Count}");
        }
        
        if (v1.Contains("documentTotals"))
        {
            var docTotals = v1["documentTotals"].AsBsonDocument;
            Console.WriteLine($"  - Total Fields: {docTotals["fields"].AsBsonDocument.ElementCount}");
        }
        
        if (v1.Contains("attachmentConfig"))
        {
            var attachConfig = v1["attachmentConfig"].AsBsonDocument;
            var docLevel = attachConfig["documentLevel"].AsBsonDocument;
            var lineLevel = attachConfig["lineLevel"].AsBsonDocument;
            Console.WriteLine($"  - DocumentLevel.Enabled: {docLevel["enabled"]}");
            Console.WriteLine($"  - LineLevel.Enabled: {lineLevel["enabled"]}");
        }
        
        if (v1.Contains("cloudStorage"))
        {
            var cloudStorage = v1["cloudStorage"].AsBsonDocument;
            Console.WriteLine($"  - Providers: {cloudStorage["providers"].AsBsonArray.Count}");
            Console.WriteLine($"  - VirusScanEnabled: {cloudStorage["globalSettings"].AsBsonDocument["virusScanEnabled"]}");
        }
    }

    Console.WriteLine("\n=== Injection completed successfully ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Injection failed: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}

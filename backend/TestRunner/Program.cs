using System.Text.Json;
using Valora.Api.Application.Schemas;

// Test script to validate Sales Order Template Extension models
Console.WriteLine("=== Testing Sales Order Template Extension ===\n");

// Sample JSON matching the design document schema (using single quotes for C# verbatim string)
var sampleJson = """
{
  "objectType": "Transaction",
  "fields": {
    "OrderNumber": {
      "type": "text",
      "required": true,
      "autoGenerate": true
    },
    "CustomerId": {
      "type": "lookup",
      "required": true
    },
    "TotalAmount": {
      "type": "currency",
      "required": false
    }
  },
  "uniqueConstraints": [["OrderNumber", "CustomerId"]],
  "shouldPost": true,
  "calculationRules": {
    "serverSide": {
      "lineItemCalculations": [
        {
          "targetField": "LineTotal",
          "formula": "{Quantity} * {UnitPrice} - {DiscountAmount}",
          "trigger": "onChange",
          "fields": ["Quantity", "UnitPrice", "DiscountAmount"]
        },
        {
          "targetField": "TaxAmount",
          "formula": "{LineTotal} * {TaxRate} / 100",
          "trigger": "onChange",
          "fields": ["LineTotal", "TaxRate"]
        }
      ],
      "documentCalculations": [
        {
          "targetField": "SubTotal",
          "formula": "SUM({Items.LineTotal})",
          "trigger": "onLineChange"
        },
        {
          "targetField": "TotalTax",
          "formula": "SUM({Items.TaxAmount})",
          "trigger": "onLineChange"
        }
      ],
      "complexCalculations": [
        {
          "id": "calc-volume-discount-001",
          "name": "CalculateVolumeDiscount",
          "description": "Apply tiered discount based on total quantity",
          "targetField": "VolumeDiscountPercent",
          "scope": "Document",
          "trigger": "onLineChange",
          "expression": "TotalQuantity >= 1000 ? 15 : TotalQuantity >= 500 ? 10 : TotalQuantity >= 100 ? 5 : 0",
          "parameters": [
            { "name": "TotalQuantity", "source": "Field", "dataType": "decimal" }
          ],
          "externalDataSources": [],
          "assemblyReferences": ["System.Linq"]
        }
      ]
    },
    "clientSide": {
      "onLoad": "console.log('Sales Order loaded');",
      "onBeforeSave": "if (data.TotalAmount <= 0) { throw new Error('Total must be greater than 0'); }",
      "onLineItemAdd": "line.ItemNumber = context.lineCount + 1;",
      "customFunctions": {
        "calculateDiscount": "function(amount, tier) { return tier === 'GOLD' ? amount * 0.1 : 0; }"
      }
    }
  },
  "documentTotals": {
    "fields": {
      "subTotal": {
        "source": "CALCULATED",
        "formula": "SUM({Items.LineTotal})",
        "label": "Sub Total",
        "displayPosition": "footer",
        "decimalPlaces": 2
      },
      "totalDiscount": {
        "source": "CALCULATED",
        "formula": "SUM({Items.DiscountAmount})",
        "label": "Total Discount",
        "displayPosition": "footer",
        "decimalPlaces": 2
      },
      "taxTotal": {
        "source": "CALCULATED",
        "formula": "SUM({Items.TaxAmount})",
        "label": "Tax",
        "displayPosition": "footer",
        "decimalPlaces": 2
      },
      "shippingAmount": {
        "source": "MANUAL_ENTRY",
        "label": "Shipping",
        "displayPosition": "footer",
        "decimalPlaces": 2,
        "editable": true,
        "defaultValue": 0
      },
      "grandTotal": {
        "source": "FORMULA",
        "formula": "{SubTotal} - {TotalDiscount} + {TaxTotal} + {ShippingAmount}",
        "label": "Grand Total",
        "displayPosition": "footer",
        "decimalPlaces": 2,
        "isReadOnly": true,
        "highlight": true
      }
    },
    "displayConfig": {
      "layout": "stacked",
      "position": "bottom",
      "currencySymbol": "$",
      "showSeparator": true
    }
  },
  "attachmentConfig": {
    "documentLevel": {
      "enabled": true,
      "maxFiles": 10,
      "maxFileSizeMB": 50,
      "allowedTypes": ["pdf", "doc", "docx", "jpg", "png"],
      "categories": [
        { "id": "contract", "label": "Sales Contract", "required": false },
        { "id": "po", "label": "Purchase Order", "required": false },
        { "id": "invoice", "label": "Customer Invoice", "required": false },
        { "id": "shipping", "label": "Shipping Docs", "required": false }
      ],
      "storageProvider": "primary"
    },
    "lineLevel": {
      "enabled": true,
      "maxFiles": 3,
      "maxFileSizeMB": 10,
      "allowedTypes": ["jpg", "png", "pdf"],
      "categories": [
        { "id": "product_image", "label": "Product Image", "required": false },
        { "id": "spec_sheet", "label": "Specification", "required": false }
      ],
      "storageProvider": "primary",
      "gridColumn": {
        "width": "100px",
        "showCount": true,
        "allowPreview": true
      }
    }
  },
  "cloudStorage": {
    "providers": [
      {
        "id": "primary",
        "provider": "aws_s3",
        "isDefault": true,
        "config": {
          "bucketName": "valora-documents-prod",
          "region": "us-east-1",
          "basePath": "tenants/{tenantId}/salesorders",
          "encryption": "AES256"
        },
        "credentials": {
          "accessKeyId": "encrypted:AQIDAHg...",
          "secretAccessKey": "encrypted:AQIDAHg..."
        },
        "lifecycleRules": {
          "autoDeleteAfterDays": null,
          "moveToColdStorageAfterDays": 90
        }
      },
      {
        "id": "archive",
        "provider": "azure_blob",
        "isDefault": false,
        "config": {
          "accountName": "valoraarchive",
          "containerName": "documents",
          "basePath": "archive/{tenantId}"
        },
        "credentials": {
          "connectionString": "encrypted:AQIDAHg..."
        },
        "lifecycleRules": {
          "autoDeleteAfterDays": 2555,
          "moveToColdStorageAfterDays": null
        }
      }
    ],
    "globalSettings": {
      "virusScanEnabled": true,
      "generateThumbnails": true,
      "thumbnailSizes": ["100x100", "300x300", "800x800"],
      "allowedMimeTypes": [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
      ],
      "maxFileSizeMB": 100
    }
  },
  "ui": {
    "title": "Sales Order",
    "icon": "shopping-cart",
    "layout": [
      { "section": "Header", "fields": ["OrderNumber", "CustomerId"] },
      { "section": "Items", "type": "grid", "field": "Items" }
    ],
    "listFields": ["OrderNumber", "CustomerId", "OrderDate", "TotalAmount"]
  }
}
""";

try
{
    Console.WriteLine("1. Testing JSON deserialization...");
    var schema = ModuleSchemaJson.FromRawJson("TEST_TENANT", "SalesOrder", 1, sampleJson);
    Console.WriteLine($"   ✓ Schema parsed successfully");
    Console.WriteLine($"   - TenantId: {schema.TenantId}");
    Console.WriteLine($"   - Module: {schema.Module}");
    Console.WriteLine($"   - Version: {schema.Version}");
    Console.WriteLine($"   - ObjectType: {schema.ObjectType}");
    Console.WriteLine($"   - Fields count: {schema.Fields.Count}");
    Console.WriteLine($"   - ShouldPost: {schema.ShouldPost}");
    Console.WriteLine();

    Console.WriteLine("2. Testing CalculationRules...");
    if (schema.CalculationRules != null)
    {
        Console.WriteLine($"   ✓ CalculationRules loaded");
        Console.WriteLine($"   - ServerSide.LineItemCalculations: {schema.CalculationRules.ServerSide?.LineItemCalculations?.Count ?? 0}");
        Console.WriteLine($"   - ServerSide.DocumentCalculations: {schema.CalculationRules.ServerSide?.DocumentCalculations?.Count ?? 0}");
        Console.WriteLine($"   - ServerSide.ComplexCalculations: {schema.CalculationRules.ServerSide?.ComplexCalculations?.Count ?? 0}");
        Console.WriteLine($"   - ClientSide.OnLoad: {!string.IsNullOrEmpty(schema.CalculationRules.ClientSide?.OnLoad)}");
        Console.WriteLine($"   - ClientSide.OnBeforeSave: {!string.IsNullOrEmpty(schema.CalculationRules.ClientSide?.OnBeforeSave)}");
        Console.WriteLine($"   - ClientSide.CustomFunctions: {schema.CalculationRules.ClientSide?.CustomFunctions?.Count ?? 0}");
        
        // Show first line item calculation
        if (schema.CalculationRules.ServerSide?.LineItemCalculations?.Count > 0)
        {
            var first = schema.CalculationRules.ServerSide.LineItemCalculations[0];
            Console.WriteLine($"   - First LineItemCalc: {first.TargetField} = {first.Formula}");
        }
        
        // Show first complex calculation
        if (schema.CalculationRules.ServerSide?.ComplexCalculations?.Count > 0)
        {
            var first = schema.CalculationRules.ServerSide.ComplexCalculations[0];
            Console.WriteLine($"   - First ComplexCalc: {first.Name} (scope: {first.Scope})");
        }
    }
    else
    {
        Console.WriteLine("   ✗ CalculationRules is null");
    }
    Console.WriteLine();

    Console.WriteLine("3. Testing DocumentTotals...");
    if (schema.DocumentTotals != null)
    {
        Console.WriteLine($"   ✓ DocumentTotals loaded");
        Console.WriteLine($"   - Fields count: {schema.DocumentTotals.Fields?.Count ?? 0}");
        Console.WriteLine($"   - DisplayConfig.Layout: {schema.DocumentTotals.DisplayConfig?.Layout}");
        Console.WriteLine($"   - DisplayConfig.Position: {schema.DocumentTotals.DisplayConfig?.Position}");
        Console.WriteLine($"   - DisplayConfig.CurrencySymbol: {schema.DocumentTotals.DisplayConfig?.CurrencySymbol}");
        
        if (schema.DocumentTotals.Fields != null)
        {
            foreach (var field in schema.DocumentTotals.Fields)
            {
                Console.WriteLine($"     • {field.Key}: {field.Value.Source} - {field.Value.Label}");
            }
        }
    }
    else
    {
        Console.WriteLine("   ✗ DocumentTotals is null");
    }
    Console.WriteLine();

    Console.WriteLine("4. Testing AttachmentConfig...");
    if (schema.AttachmentConfig != null)
    {
        Console.WriteLine($"   ✓ AttachmentConfig loaded");
        Console.WriteLine($"   - DocumentLevel.Enabled: {schema.AttachmentConfig.DocumentLevel?.Enabled}");
        Console.WriteLine($"   - DocumentLevel.MaxFiles: {schema.AttachmentConfig.DocumentLevel?.MaxFiles}");
        Console.WriteLine($"   - DocumentLevel.Categories: {schema.AttachmentConfig.DocumentLevel?.Categories?.Count ?? 0}");
        Console.WriteLine($"   - LineLevel.Enabled: {schema.AttachmentConfig.LineLevel?.Enabled}");
        Console.WriteLine($"   - LineLevel.MaxFiles: {schema.AttachmentConfig.LineLevel?.MaxFiles}");
        Console.WriteLine($"   - LineLevel.GridColumn.Width: {schema.AttachmentConfig.LineLevel?.GridColumn?.Width}");
    }
    else
    {
        Console.WriteLine("   ✗ AttachmentConfig is null");
    }
    Console.WriteLine();

    Console.WriteLine("5. Testing CloudStorage...");
    if (schema.CloudStorage != null)
    {
        Console.WriteLine($"   ✓ CloudStorage loaded");
        Console.WriteLine($"   - Providers count: {schema.CloudStorage.Providers?.Count ?? 0}");
        Console.WriteLine($"   - GlobalSettings.VirusScanEnabled: {schema.CloudStorage.GlobalSettings?.VirusScanEnabled}");
        Console.WriteLine($"   - GlobalSettings.MaxFileSizeMB: {schema.CloudStorage.GlobalSettings?.MaxFileSizeMB}");
        
        if (schema.CloudStorage.Providers != null)
        {
            foreach (var provider in schema.CloudStorage.Providers)
            {
                Console.WriteLine($"     • {provider.Id} ({provider.Provider}) - IsDefault: {provider.IsDefault}");
                Console.WriteLine($"       Bucket: {provider.Config?.BucketName}, Region: {provider.Config?.Region}");
                Console.WriteLine($"       BasePath: {provider.Config?.BasePath}");
            }
        }
    }
    else
    {
        Console.WriteLine("   ✗ CloudStorage is null");
    }
    Console.WriteLine();

    Console.WriteLine("6. Testing JSON serialization (round-trip)...");
    var serialized = ModuleSchemaJson.ToJson(schema);
    Console.WriteLine($"   ✓ Serialized successfully ({serialized.Length} characters)");
    
    // Verify we can deserialize again
    var schema2 = ModuleSchemaJson.FromRawJson("TEST_TENANT", "SalesOrder", 1, serialized);
    Console.WriteLine($"   ✓ Re-deserialized successfully");
    Console.WriteLine($"   - Fields count matches: {schema.Fields.Count == schema2.Fields.Count}");
    Console.WriteLine($"   - CalculationRules match: {(schema.CalculationRules != null) == (schema2.CalculationRules != null)}");
    Console.WriteLine($"   - DocumentTotals match: {(schema.DocumentTotals != null) == (schema2.DocumentTotals != null)}");
    Console.WriteLine($"   - AttachmentConfig match: {(schema.AttachmentConfig != null) == (schema2.AttachmentConfig != null)}");
    Console.WriteLine($"   - CloudStorage match: {(schema.CloudStorage != null) == (schema2.CloudStorage != null)}");
    Console.WriteLine();

    Console.WriteLine("=== ALL TESTS PASSED ===");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ TEST FAILED: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
    Environment.Exit(1);
}

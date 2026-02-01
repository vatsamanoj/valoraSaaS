# Sales Order Template Extension Design

## Overview
Extension to `PlatformObjectTemplate` collection for Sales Order dynamic screen with:
- Calculation rules (server-side formulas + client-side JS)
- Document totals configuration
- Multi-level attachments (line + document)
- Generic cloud storage configuration

---

## 1. Extended PlatformObjectTemplate Schema

### MongoDB Document Structure

```json
{
  "tenantId": "LAB_001",
  "environments": {
    "prod": {
      "screens": {
        "SalesOrder": {
          "v1": {
            "isPublished": true,
            "fields": { /* existing fields */ },
            "ui": { /* existing UI config */ },
            
            // ===== NEW SECTIONS =====
            
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
                    "externalDataSources": []
                  },
                  {
                    "id": "calc-customer-tier-pricing-002",
                    "name": "CalculateCustomerTierPrice",
                    "description": "Apply customer tier discount to unit price",
                    "targetField": "TierDiscountedPrice",
                    "scope": "LineItem",
                    "trigger": "onChange",
                    "expression": "CustomerTier == \"PLATINUM\" ? UnitPrice * 0.75 : CustomerTier == \"GOLD\" ? UnitPrice * 0.85 : CustomerTier == \"SILVER\" ? UnitPrice * 0.90 : UnitPrice",
                    "parameters": [
                      { "name": "UnitPrice", "source": "Field", "dataType": "decimal", "isRequired": true },
                      { "name": "CustomerTier", "source": "Context", "dataType": "string", "isRequired": true }
                    ]
                  },
                  {
                    "id": "calc-shipping-cost-003",
                    "name": "CalculateShippingCost",
                    "description": "Calculate shipping based on weight, zone, and service level",
                    "targetField": "ShippingCost",
                    "scope": "Document",
                    "trigger": "onSave",
                    "expression": null,
                    "codeBlock": "// Complex shipping calculation with external data lookup\nvar baseRate = ShippingZone == \"LOCAL\" ? 10m : ShippingZone == \"NATIONAL\" ? 25m : 50m;\nvar weightFactor = TotalWeight <= 1 ? 1m : TotalWeight <= 5 ? 1.5m : TotalWeight <= 10 ? 2m : 3m;\nvar serviceMultiplier = ServiceLevel == \"EXPRESS\" ? 2m : ServiceLevel == \"OVERNIGHT\" ? 3m : 1m;\nreturn baseRate * weightFactor * serviceMultiplier;",
                    "parameters": [
                      { "name": "TotalWeight", "source": "Field", "dataType": "decimal" },
                      { "name": "ShippingZone", "source": "Field", "dataType": "string" },
                      { "name": "ServiceLevel", "source": "Field", "dataType": "string" }
                    ],
                    "externalDataSources": [
                      {
                        "name": "ShippingRates",
                        "sourceType": "SqlQuery",
                        "queryOrEndpoint": "SELECT * FROM ShippingRates WHERE Zone = @Zone AND Active = true",
                        "parameters": { "Zone": "{ShippingZone}" }
                      }
                    ],
                    "assemblyReferences": ["System.Linq", "Valora.Api.Domain"]
                  }
                ]
              },
              "clientSide": {
                "onLoad": "// Custom JS for screen initialization\nconsole.log('Sales Order loaded');",
                "onBeforeSave": "// Validation before save\nif (data.TotalAmount <= 0) { throw new Error('Total must be greater than 0'); }",
                "onLineItemAdd": "// Custom logic when adding line\nline.ItemNumber = context.lineCount + 1;",
                "customFunctions": {
                  "calculateDiscount": "function(amount, tier) { return tier === 'GOLD' ? amount * 0.1 : 0; }"
                }
              }
            },
            
            "documentTotals": {
              "fields": {
                "subTotal": {
                  "source": "SUM_LINE_TOTALS",
                  "label": "Sub Total",
                  "displayPosition": "footer",
                  "decimalPlaces": 2
                },
                "discountTotal": {
                  "source": "CALCULATED",
                  "formula": "{SubTotal} * {DiscountPercent} / 100",
                  "label": "Discount",
                  "displayPosition": "footer",
                  "decimalPlaces": 2,
                  "editable": true
                },
                "taxTotal": {
                  "source": "SUM_LINE_TAX",
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
                  "formula": "{SubTotal} - {DiscountTotal} + {TaxTotal} + {ShippingAmount}",
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
                "storageProvider": "primary" // References cloudStorage.providers
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
                  "provider": "aws_s3", // aws_s3, gcp_storage, azure_blob
                  "isDefault": true,
                  "config": {
                    "bucketName": "valora-documents-prod",
                    "region": "us-east-1",
                    "basePath": "tenants/{tenantId}/salesorders",
                    "encryption": "AES256"
                  },
                  "credentials": {
                    "accessKeyId": "encrypted:AQIDAHg...",
                    "secretAccessKey": "encrypted:AQIDAHg...",
                    "sessionToken": "encrypted:AQIDAHg..."
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
                  }
                }
              ],
              "globalSettings": {
                "virusScanEnabled": true,
                "generateThumbnails": true,
                "thumbnailSizes": ["100x100", "300x300"],
                "allowedMimeTypes": ["application/pdf", "image/*", "application/msword"],
                "maxFileSizeMB": 100
              }
            }
          }
        }
      }
    }
  }
}
```

---

## 2. C# Model Classes

### Calculation Rules Models

```csharp
// File: Application/Schemas/TemplateConfig/CalculationRulesConfig.cs
namespace Valora.Api.Application.Schemas.TemplateConfig;

public class CalculationRulesConfig
{
    public ServerSideCalculations ServerSide { get; set; } = new();
    public ClientSideCalculations ClientSide { get; set; } = new();
}

public class ServerSideCalculations
{
    public List<LineItemCalculation> LineItemCalculations { get; set; } = new();
    public List<DocumentCalculation> DocumentCalculations { get; set; } = new();
    public List<ComplexCalculation> ComplexCalculations { get; set; } = new();
}

public class LineItemCalculation
{
    public string TargetField { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty; // "{Quantity} * {UnitPrice}"
    public string Trigger { get; set; } = "onChange"; // onChange, onLoad, manual
    public List<string> DependentFields { get; set; } = new();
    public string? Condition { get; set; } // Optional: "{Quantity} > 10"
}

public class DocumentCalculation
{
    public string TargetField { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty; // "SUM({Items.LineTotal})"
    public string Trigger { get; set; } = "onLineChange";
}

/// <summary>
/// Complex calculation using C# code stored in template.
/// Uses Roslyn or DynamicExpresso for safe execution.
/// </summary>
public class ComplexCalculation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty; // "CalculateVolumeDiscount"
    public string Description { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty; // Where to store result
    public CalculationScope Scope { get; set; } = CalculationScope.LineItem; // LineItem or Document
    public string Trigger { get; set; } = "onChange"; // onChange, onSave, manual
    
    /// <summary>
    /// C# expression or code block.
    /// Example: "Quantity > 100 ? UnitPrice * 0.9 : UnitPrice"
    /// </summary>
    public string Expression { get; set; } = string.Empty;
    
    /// <summary>
    /// Full C# code with usings and method body for complex logic.
    /// Only used when Expression is insufficient.
    /// </summary>
    public string? CodeBlock { get; set; }
    
    /// <summary>
    /// Input parameters the calculation needs.
    /// </summary>
    public List<CalculationParameter> Parameters { get; set; } = new();
    
    /// <summary>
    /// External data sources (lookup tables, reference data).
    /// </summary>
    public List<ExternalDataSource> ExternalDataSources { get; set; } = new();
    
    /// <summary>
    /// Assembly references for compilation.
    /// </summary>
    public List<string> AssemblyReferences { get; set; } = new();
}

public enum CalculationScope
{
    LineItem,
    Document
}

public class CalculationParameter
{
    public string Name { get; set; } = string.Empty; // "Quantity", "CustomerTier"
    public string Source { get; set; } = string.Empty; // "Field", "Context", "External"
    public string? DataType { get; set; } // "decimal", "string", "DateTime"
    public bool IsRequired { get; set; } = true;
}

public class ExternalDataSource
{
    public string Name { get; set; } = string.Empty; // "DiscountTiers"
    public string SourceType { get; set; } = string.Empty; // "SqlQuery", "ApiEndpoint", "StaticTable"
    public string? QueryOrEndpoint { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class ClientSideCalculations
{
    public string? OnLoad { get; set; }
    public string? OnBeforeSave { get; set; }
    public string? OnLineItemAdd { get; set; }
    public string? OnLineItemRemove { get; set; }
    public Dictionary<string, string> CustomFunctions { get; set; } = new();
}
```

### Document Totals Models

```csharp
// File: Application/Schemas/TemplateConfig/DocumentTotalsConfig.cs
namespace Valora.Api.Application.Schemas.TemplateConfig;

public class DocumentTotalsConfig
{
    public Dictionary<string, TotalFieldConfig> Fields { get; set; } = new();
    public TotalsDisplayConfig DisplayConfig { get; set; } = new();
}

public class TotalFieldConfig
{
    public string Source { get; set; } = string.Empty; 
    // SUM_LINE_TOTALS, SUM_LINE_TAX, CALCULATED, MANUAL_ENTRY, FORMULA
    
    public string? Formula { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DisplayPosition { get; set; } = "footer"; // footer, sidebar, header
    public int DecimalPlaces { get; set; } = 2;
    public bool Editable { get; set; } = false;
    public bool IsReadOnly { get; set; } = true;
    public bool Highlight { get; set; } = false;
    public decimal? DefaultValue { get; set; }
}

public class TotalsDisplayConfig
{
    public string Layout { get; set; } = "stacked"; // stacked, horizontal, grid
    public string Position { get; set; } = "bottom"; // bottom, top, right
    public string? CurrencySymbol { get; set; }
    public bool ShowSeparator { get; set; } = true;
}
```

### Attachment Configuration Models

```csharp
// File: Application/Schemas/TemplateConfig/AttachmentConfig.cs
namespace Valora.Api.Application.Schemas.TemplateConfig;

public class AttachmentConfig
{
    public DocumentLevelAttachmentConfig DocumentLevel { get; set; } = new();
    public LineLevelAttachmentConfig LineLevel { get; set; } = new();
}

public class DocumentLevelAttachmentConfig
{
    public bool Enabled { get; set; } = false;
    public int MaxFiles { get; set; } = 10;
    public int MaxFileSizeMB { get; set; } = 50;
    public List<string> AllowedTypes { get; set; } = new();
    public List<AttachmentCategory> Categories { get; set; } = new();
    public string StorageProvider { get; set; } = "primary";
}

public class LineLevelAttachmentConfig
{
    public bool Enabled { get; set; } = false;
    public int MaxFiles { get; set; } = 3;
    public int MaxFileSizeMB { get; set; } = 10;
    public List<string> AllowedTypes { get; set; } = new();
    public List<AttachmentCategory> Categories { get; set; } = new();
    public string StorageProvider { get; set; } = "primary";
    public GridColumnConfig GridColumn { get; set; } = new();
}

public class AttachmentCategory
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Required { get; set; } = false;
    public int? MaxFilesPerCategory { get; set; }
}

public class GridColumnConfig
{
    public string Width { get; set; } = "100px";
    public bool ShowCount { get; set; } = true;
    public bool AllowPreview { get; set; } = true;
}
```

### Cloud Storage Configuration Models

```csharp
// File: Application/Schemas/TemplateConfig/CloudStorageConfig.cs
namespace Valora.Api.Application.Schemas.TemplateConfig;

public class CloudStorageConfig
{
    public List<StorageProviderConfig> Providers { get; set; } = new();
    public GlobalStorageSettings GlobalSettings { get; set; } = new();
}

public class StorageProviderConfig
{
    public string Id { get; set; } = string.Empty; // "primary", "archive"
    public string Provider { get; set; } = string.Empty; // aws_s3, gcp_storage, azure_blob
    public bool IsDefault { get; set; } = false;
    public ProviderConfig Config { get; set; } = new();
    public EncryptedCredentials Credentials { get; set; } = new();
    public LifecycleRules? LifecycleRules { get; set; }
}

public class ProviderConfig
{
    // AWS S3
    public string? BucketName { get; set; }
    public string? Region { get; set; }
    
    // Azure Blob
    public string? AccountName { get; set; }
    public string? ContainerName { get; set; }
    
    // GCP Storage
    public string? ProjectId { get; set; }
    public string? BucketName { get; set; }
    
    // Common
    public string BasePath { get; set; } = string.Empty; // "tenants/{tenantId}/documents"
    public string? Encryption { get; set; } // AES256, aws:kms
}

public class EncryptedCredentials
{
    // AWS
    public string? AccessKeyId { get; set; } // encrypted
    public string? SecretAccessKey { get; set; } // encrypted
    public string? SessionToken { get; set; } // encrypted (optional)
    
    // Azure
    public string? ConnectionString { get; set; } // encrypted
    
    // GCP
    public string? ServiceAccountKey { get; set; } // encrypted JSON
}

public class LifecycleRules
{
    public int? AutoDeleteAfterDays { get; set; }
    public int? MoveToColdStorageAfterDays { get; set; }
}

public class GlobalStorageSettings
{
    public bool VirusScanEnabled { get; set; } = true;
    public bool GenerateThumbnails { get; set; } = true;
    public List<string> ThumbnailSizes { get; set; } = new() { "100x100", "300x300" };
    public List<string> AllowedMimeTypes { get; set; } = new();
    public int MaxFileSizeMB { get; set; } = 100;
}
```

---

## 3. Updated ModuleSchema

```csharp
// File: Application/Schemas/ModuleSchema.cs
using Valora.Api.Application.Schemas.TemplateConfig;

public sealed record ModuleSchema(
    string TenantId,
    string Module,
    int Version,
    string ObjectType,
    Dictionary<string, FieldRule> Fields,
    List<string[]>? UniqueConstraints = null,
    ModuleUi? Ui = null,
    bool ShouldPost = false,
    
    // ===== NEW PROPERTIES =====
    CalculationRulesConfig? CalculationRules = null,
    DocumentTotalsConfig? DocumentTotals = null,
    AttachmentConfig? AttachmentConfig = null,
    CloudStorageConfig? CloudStorage = null
);
```

---

## 4. SQL Tables for Attachment Metadata

```sql
-- Document-level attachments
CREATE TABLE DocumentAttachments (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(50) NOT NULL,
    DocumentType VARCHAR(100) NOT NULL, -- 'SalesOrder', 'PurchaseOrder'
    DocumentId UUID NOT NULL,
    CategoryId VARCHAR(50) NOT NULL,
    FileName VARCHAR(500) NOT NULL,
    OriginalFileName VARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    MimeType VARCHAR(100) NOT NULL,
    StorageProvider VARCHAR(50) NOT NULL, -- 'primary', 'archive'
    StoragePath VARCHAR(1000) NOT NULL, -- Full path in bucket
    StorageUrl VARCHAR(2000), -- Pre-signed URL (temporary)
    Checksum VARCHAR(64), -- SHA-256 for integrity
    UploadedBy VARCHAR(100) NOT NULL,
    UploadedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ExpiresAt TIMESTAMP WITH TIME ZONE, -- For pre-signed URLs
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE,
    DeletedBy VARCHAR(100)
);

-- Line-item attachments
CREATE TABLE LineItemAttachments (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(50) NOT NULL,
    DocumentType VARCHAR(100) NOT NULL,
    DocumentId UUID NOT NULL,
    LineItemId UUID NOT NULL, -- References the line item
    LineItemNumber INTEGER NOT NULL,
    CategoryId VARCHAR(50) NOT NULL,
    FileName VARCHAR(500) NOT NULL,
    OriginalFileName VARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    MimeType VARCHAR(100) NOT NULL,
    StorageProvider VARCHAR(50) NOT NULL,
    StoragePath VARCHAR(1000) NOT NULL,
    StorageUrl VARCHAR(2000),
    Checksum VARCHAR(64),
    UploadedBy VARCHAR(100) NOT NULL,
    UploadedAt TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    ExpiresAt TIMESTAMP WITH TIME ZONE,
    IsDeleted BOOLEAN DEFAULT FALSE,
    DeletedAt TIMESTAMP WITH TIME ZONE,
    DeletedBy VARCHAR(100)
);

-- Indexes
CREATE INDEX IX_DocumentAttachments_Document ON DocumentAttachments(TenantId, DocumentType, DocumentId);
CREATE INDEX IX_DocumentAttachments_Category ON DocumentAttachments(TenantId, CategoryId);
CREATE INDEX IX_LineItemAttachments_Line ON LineItemAttachments(TenantId, DocumentType, DocumentId, LineItemId);
```

---

## 5. Cloud Storage Service Interface

```csharp
// File: Domain/Services/ICloudStorageService.cs
namespace Valora.Api.Domain.Services;

public interface ICloudStorageService
{
    string ProviderName { get; }
    
    Task<UploadResult> UploadAsync(
        Stream fileStream, 
        string fileName, 
        string contentType,
        StorageProviderConfig config,
        CancellationToken ct = default);
    
    Task<Stream> DownloadAsync(
        string storagePath, 
        StorageProviderConfig config,
        CancellationToken ct = default);
    
    Task<string> GeneratePreSignedUrlAsync(
        string storagePath, 
        TimeSpan expiry,
        StorageProviderConfig config,
        CancellationToken ct = default);
    
    Task DeleteAsync(
        string storagePath, 
        StorageProviderConfig config,
        CancellationToken ct = default);
    
    Task<bool> ExistsAsync(
        string storagePath, 
        StorageProviderConfig config,
        CancellationToken ct = default);
}

public class UploadResult
{
    public string StoragePath { get; set; } = string.Empty;
    public string StorageUrl { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public long FileSize { get; set; }
}

// File: Application/Services/ITemplateCalculationService.cs
namespace Valora.Api.Application.Services;

/// <summary>
/// Service for executing C# calculations stored in templates.
/// Uses DynamicExpresso for safe expression evaluation.
/// </summary>
public interface ITemplateCalculationService
{
    /// <summary>
    /// Executes a simple expression against a data object.
    /// </summary>
    Task<object?> EvaluateExpressionAsync(
        string expression,
        Dictionary<string, object> parameters,
        CancellationToken ct = default);
    
    /// <summary>
    /// Executes a complex calculation from template configuration.
    /// </summary>
    Task<CalculationResult> ExecuteComplexCalculationAsync(
        ComplexCalculation calculation,
        CalculationContext context,
        CancellationToken ct = default);
    
    /// <summary>
    /// Validates that a C# expression is syntactically correct and safe.
    /// </summary>
    Task<ValidationResult> ValidateExpressionAsync(string expression);
    
    /// <summary>
    /// Pre-compiles frequently used calculations for performance.
    /// </summary>
    Task PrecompileCalculationAsync(ComplexCalculation calculation);
}

public class CalculationContext
{
    public string TenantId { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public Dictionary<string, object> LineItem { get; set; } = new();
    public List<Dictionary<string, object>> AllLineItems { get; set; } = new();
    public Dictionary<string, object> DocumentFields { get; set; } = new();
    public Dictionary<string, object> ExternalData { get; set; } = new();
    public Dictionary<string, object> UserContext { get; set; } = new();
}

public class CalculationResult
{
    public bool Success { get; set; }
    public object? Value { get; set; }
    public string? Error { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public List<string> Logs { get; set; } = new();
}

// File: Infrastructure/Services/TemplateCalculationService.cs
using DynamicExpresso;

namespace Valora.Api.Infrastructure.Services;

public class TemplateCalculationService : ITemplateCalculationService
{
    private readonly Interpreter _interpreter;
    private readonly ILogger<TemplateCalculationService> _logger;
    private readonly ConcurrentDictionary<string, Lambda> _compiledCache = new();
    
    public TemplateCalculationService(ILogger<TemplateCalculationService> logger)
    {
        _logger = logger;
        
        // Initialize DynamicExpresso with safe settings
        _interpreter = new Interpreter(InterpreterOptions.Default | InterpreterOptions.LambdaExpressions)
            .EnableAssignment(AssignmentOperators.None) // Disable assignment for safety
            .Reference(typeof(Math))
            .Reference(typeof(Enumerable))
            .Reference(typeof(DateTime))
            .Reference(typeof(decimal));
    }
    
    public async Task<object?> EvaluateExpressionAsync(
        string expression,
        Dictionary<string, object> parameters,
        CancellationToken ct = default)
    {
        try
        {
            var lambda = _interpreter.Parse(expression, parameters.Keys.ToArray());
            var result = lambda.Invoke(parameters.Values.ToArray());
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evaluate expression: {Expression}", expression);
            throw new CalculationException($"Expression evaluation failed: {ex.Message}", ex);
        }
    }
    
    public async Task<CalculationResult> ExecuteComplexCalculationAsync(
        ComplexCalculation calculation,
        CalculationContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var logs = new List<string>();
        
        try
        {
            // Build parameter dictionary
            var parameters = new Dictionary<string, object>();
            
            // Add line item fields
            foreach (var kvp in context.LineItem)
            {
                parameters[kvp.Key] = kvp.Value;
            }
            
            // Add document-level fields
            parameters["Document"] = context.DocumentFields;
            parameters["LineItems"] = context.AllLineItems;
            parameters["User"] = context.UserContext;
            
            // Load external data if needed
            foreach (var dataSource in calculation.ExternalDataSources)
            {
                var externalData = await LoadExternalDataAsync(dataSource, context);
                parameters[dataSource.Name] = externalData;
                logs.Add($"Loaded external data: {dataSource.Name}");
            }
            
            // Execute expression or code block
            object? result;
            if (!string.IsNullOrEmpty(calculation.Expression))
            {
                result = await EvaluateExpressionAsync(calculation.Expression, parameters, ct);
            }
            else if (!string.IsNullOrEmpty(calculation.CodeBlock))
            {
                result = await ExecuteCodeBlockAsync(calculation.CodeBlock, parameters, ct);
            }
            else
            {
                throw new InvalidOperationException("No expression or code block provided");
            }
            
            stopwatch.Stop();
            
            return new CalculationResult
            {
                Success = true,
                Value = result,
                ExecutionTime = stopwatch.Elapsed,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Calculation {CalculationId} failed", calculation.Id);
            
            return new CalculationResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTime = stopwatch.Elapsed,
                Logs = logs
            };
        }
    }
    
    public Task<ValidationResult> ValidateExpressionAsync(string expression)
    {
        try
        {
            // Try parsing to check syntax
            _interpreter.Parse(expression);
            return Task.FromResult(ValidationResult.Success());
        }
        catch (ParseException ex)
        {
            return Task.FromResult(ValidationResult.Failure(ex.Message));
        }
    }
    
    public Task PrecompileCalculationAsync(ComplexCalculation calculation)
    {
        if (!string.IsNullOrEmpty(calculation.Expression))
        {
            var paramNames = calculation.Parameters.Select(p => p.Name).ToArray();
            var lambda = _interpreter.Parse(calculation.Expression, paramNames);
            _compiledCache[calculation.Id] = lambda;
        }
        
        return Task.CompletedTask;
    }
    
    private async Task<object?> ExecuteCodeBlockAsync(string codeBlock, Dictionary<string, object> parameters, CancellationToken ct)
    {
        // For full code blocks, use C# scripting or Roslyn
        // This is a simplified example - production would need proper sandboxing
        throw new NotImplementedException("Code block execution requires Roslyn scripting");
    }
    
    private async Task<object?> LoadExternalDataAsync(ExternalDataSource dataSource, CalculationContext context)
    {
        // Implementation depends on data source type
        // SQL queries, API calls, or static tables
        return null;
    }
}

public class CalculationException : Exception
{
    public CalculationException(string message, Exception inner) : base(message, inner) { }
}
```

---

## 6. Frontend Integration

### React Hook for Template-Driven Calculations

```typescript
// hooks/useTemplateCalculations.ts
export function useTemplateCalculations(schema: ModuleSchema) {
  const calculateLineItem = useCallback((line: any, lineIndex: number) => {
    const rules = schema.calculationRules?.serverSide?.lineItemCalculations || [];
    const updatedLine = { ...line };
    
    rules.forEach(rule => {
      if (rule.formula) {
        const value = evaluateFormula(rule.formula, updatedLine, lineIndex);
        updatedLine[rule.targetField] = value;
      }
    });
    
    return updatedLine;
  }, [schema]);
  
  const calculateDocumentTotals = useCallback((document: any) => {
    const totals = schema.documentTotals;
    if (!totals) return {};
    
    const result: Record<string, number> = {};
    
    Object.entries(totals.fields).forEach(([key, config]) => {
      switch (config.source) {
        case 'SUM_LINE_TOTALS':
          result[key] = document.items?.reduce((sum: number, item: any) => 
            sum + (item.LineTotal || 0), 0) || 0;
          break;
        case 'SUM_LINE_TAX':
          result[key] = document.items?.reduce((sum: number, item: any) => 
            sum + (item.TaxAmount || 0), 0) || 0;
          break;
        case 'FORMULA':
          result[key] = evaluateFormula(config.formula || '', { ...document, ...result });
          break;
        case 'MANUAL_ENTRY':
          result[key] = document[key] ?? config.defaultValue ?? 0;
          break;
      }
    });
    
    return result;
  }, [schema]);
  
  return { calculateLineItem, calculateDocumentTotals };
}
```

### Attachment Component

```typescript
// components/TemplateAttachmentUpload.tsx
interface TemplateAttachmentUploadProps {
  config: AttachmentConfig['documentLevel'] | AttachmentConfig['lineLevel'];
  documentId: string;
  lineItemId?: string;
  onUploadComplete: (attachments: Attachment[]) => void;
}

export const TemplateAttachmentUpload: React.FC<TemplateAttachmentUploadProps> = ({
  config,
  documentId,
  lineItemId,
  onUploadComplete
}) => {
  const { t } = useTheme();
  const [uploading, setUploading] = useState(false);
  
  const handleFileSelect = async (files: FileList) => {
    if (!config.enabled) return;
    
    // Validate file count
    if (files.length > config.maxFiles) {
      toast.error(`Maximum ${config.maxFiles} files allowed`);
      return;
    }
    
    // Validate file sizes
    for (const file of Array.from(files)) {
      if (file.size > config.maxFileSizeMB * 1024 * 1024) {
        toast.error(`File ${file.name} exceeds ${config.maxFileSizeMB}MB limit`);
        return;
      }
      
      const ext = file.name.split('.').pop()?.toLowerCase();
      if (!config.allowedTypes.includes(ext || '')) {
        toast.error(`File type .${ext} not allowed`);
        return;
      }
    }
    
    setUploading(true);
    try {
      const uploaded = await uploadAttachments(
        Array.from(files),
        documentId,
        lineItemId,
        config.storageProvider
      );
      onUploadComplete(uploaded);
    } finally {
      setUploading(false);
    }
  };
  
  return (
    <div className="attachment-upload">
      {config.categories?.map(category => (
        <div key={category.id} className="category-section">
          <label>{category.label}</label>
          <input
            type="file"
            multiple
            accept={config.allowedTypes.map(t => `.${t}`).join(',')}
            onChange={(e) => e.target.files && handleFileSelect(e.target.files)}
            disabled={uploading}
          />
        </div>
      ))}
    </div>
  );
};
```

---

## 7. Complete SalesOrder Template Example

```json
{
  "tenantId": "LAB_001",
  "environments": {
    "prod": {
      "screens": {
        "SalesOrder": {
          "v1": {
            "isPublished": true,
            "fields": {
              "OrderNumber": {
                "type": "text",
                "required": true,
                "autoGenerate": true,
                "pattern": "SO-{YYYY}-{SEQ:6}",
                "ui": { "label": "Order Number", "readOnly": true }
              },
              "CustomerId": {
                "type": "lookup",
                "required": true,
                "ui": { 
                  "label": "Customer",
                  "lookup": "Customer",
                  "lookupField": "CustomerCode",
                  "displayField": "CustomerName"
                }
              },
              "OrderDate": {
                "type": "date",
                "required": true,
                "ui": { "label": "Order Date" }
              },
              "Currency": {
                "type": "select",
                "required": true,
                "ui": { 
                  "label": "Currency",
                  "options": ["USD", "EUR", "GBP", "INR"]
                }
              },
              "Items": {
                "type": "grid",
                "required": true,
                "ui": {
                  "label": "Line Items",
                  "gridConfig": {
                    "columns": [
                      { "field": "ItemNumber", "type": "number", "width": "80px", "readOnly": true },
                      { "field": "ProductId", "type": "lookup", "lookupModule": "Product", "width": "150px" },
                      { "field": "Description", "type": "text", "width": "200px" },
                      { "field": "Quantity", "type": "number", "width": "100px" },
                      { "field": "UnitPrice", "type": "currency", "width": "120px" },
                      { "field": "DiscountPercent", "type": "number", "width": "100px" },
                      { "field": "DiscountAmount", "type": "currency", "width": "120px" },
                      { "field": "TaxRate", "type": "number", "width": "80px" },
                      { "field": "TaxAmount", "type": "currency", "width": "120px", "readOnly": true },
                      { "field": "LineTotal", "type": "currency", "width": "120px", "readOnly": true },
                      { "field": "Attachments", "type": "attachment", "width": "100px" }
                    ]
                  }
                }
              }
            },
            "ui": {
              "title": "Sales Order",
              "icon": "shopping-cart",
              "layout": [
                { "section": "Header", "fields": ["OrderNumber", "CustomerId", "OrderDate", "Currency"] },
                { "section": "Items", "type": "grid", "field": "Items" }
              ],
              "listFields": ["OrderNumber", "CustomerId", "OrderDate", "TotalAmount"]
            },
            
            "calculationRules": {
              "serverSide": {
                "lineItemCalculations": [
                  {
                    "targetField": "DiscountAmount",
                    "formula": "{Quantity} * {UnitPrice} * {DiscountPercent} / 100",
                    "trigger": "onChange",
                    "fields": ["Quantity", "UnitPrice", "DiscountPercent"]
                  },
                  {
                    "targetField": "LineTotal",
                    "formula": "({Quantity} * {UnitPrice}) - {DiscountAmount}",
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
                  },
                  {
                    "targetField": "TotalDiscount",
                    "formula": "SUM({Items.DiscountAmount})",
                    "trigger": "onLineChange"
                  }
                ]
              },
              "clientSide": {
                "onLoad": "// Initialize customer discount tier\nif (data.CustomerId) {\n  fetchCustomerTier(data.CustomerId).then(tier => {\n    context.customerTier = tier;\n  });\n}",
                "onBeforeSave": "// Validate minimum order amount\nif (data.GrandTotal < 100) {\n  throw new Error('Minimum order amount is $100');\n}\n// Validate required attachments\nconst hasContract = data.attachments?.some(a => a.categoryId === 'contract');\nif (data.TotalAmount > 10000 && !hasContract) {\n  throw new Error('Sales orders over $10,000 require a contract attachment');\n}",
                "onLineItemAdd": "// Auto-number line items\nline.ItemNumber = (data.Items?.length || 0) + 1;\n// Apply customer discount tier\nif (context.customerTier === 'GOLD') {\n  line.DiscountPercent = 10;\n} else if (context.customerTier === 'SILVER') {\n  line.DiscountPercent = 5;\n}",
                "customFunctions": {
                  "fetchCustomerTier": "async function(customerId) {\n  const response = await fetch(`/api/customers/${customerId}/tier`);\n  return response.json();\n}"
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
                "allowedTypes": ["pdf", "doc", "docx", "jpg", "png", "xls", "xlsx"],
                "categories": [
                  { "id": "contract", "label": "Sales Contract", "required": false },
                  { "id": "po", "label": "Customer Purchase Order", "required": false },
                  { "id": "quote", "label": "Quotation", "required": false },
                  { "id": "approval", "label": "Management Approval", "required": false },
                  { "id": "shipping", "label": "Shipping Documents", "required": false }
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
                  { "id": "spec_sheet", "label": "Specification Sheet", "required": false },
                  { "id": "warranty", "label": "Warranty Document", "required": false }
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
                    "basePath": "tenants/{tenantId}/salesorders/{documentId}",
                    "encryption": "AES256"
                  },
                  "credentials": {
                    "accessKeyId": "encrypted:AQIDAHg...",
                    "secretAccessKey": "encrypted:AQIDAHg..."
                  },
                  "lifecycleRules": {
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
                    "basePath": "archive/{tenantId}/salesorders"
                  },
                  "credentials": {
                    "connectionString": "encrypted:AQIDAHg..."
                  },
                  "lifecycleRules": {
                    "autoDeleteAfterDays": 2555
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
                  "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                  "application/vnd.ms-excel",
                  "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                ],
                "maxFileSizeMB": 100
              }
            },
            
            "shouldPost": true
          }
        }
      }
    }
  }
}
```

---

## 8. Implementation Checklist

### Backend Tasks
- [ ] Create `TemplateConfig` folder with all model classes
- [ ] Update `ModuleSchema.cs` with new properties
- [ ] Update `ModuleSchemaJson.cs` to parse new sections
- [ ] Create SQL migration for attachment tables
- [ ] Implement `ICloudStorageService` interface
- [ ] Create AWS S3 storage provider implementation
- [ ] Create Azure Blob storage provider implementation
- [ ] Create GCP Storage provider implementation
- [ ] Implement credential encryption/decryption service
- [ ] Create attachment upload/download API endpoints
- [ ] Implement server-side calculation engine
- [ ] Add calculation execution to `CreateEntityCommandHandler`

### Frontend Tasks
- [ ] Create `useTemplateCalculations` hook
- [ ] Create `TemplateAttachmentUpload` component
- [ ] Create `DocumentTotals` component
- [ ] Update `DynamicForm` to execute client-side JS
- [ ] Add attachment column to grid component
- [ ] Create attachment preview modal
- [ ] Implement formula evaluation in grid
- [ ] Add totals footer to document forms

### Security Tasks
- [ ] Implement credential encryption at rest
- [ ] Add pre-signed URL generation with expiry
- [ ] Validate file types and sizes server-side
- [ ] Add virus scanning integration
- [ ] Implement attachment access control

---

## Summary

This design extends the `PlatformObjectTemplate` collection with:

1. **Calculation Rules** - Server-side formulas for line items and document totals, plus client-side JavaScript for complex business logic
2. **Document Totals** - Configurable footer totals with formulas, manual entry fields, and display options
3. **Attachments** - Both document-level and line-level with categories, limits, and storage provider selection
4. **Cloud Storage** - Generic provider configuration supporting AWS S3, Azure Blob, and GCP Storage with encrypted credentials

All configuration is template-driven and stored in MongoDB's `PlatformObjectTemplate` collection, making it fully dynamic per tenant and environment.
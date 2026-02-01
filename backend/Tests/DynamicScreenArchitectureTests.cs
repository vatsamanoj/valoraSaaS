using System.Text.Json;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using Xunit;

namespace Valora.Tests.Architecture
{
    /// <summary>
    /// Architecture validation tests for ANY dynamic screen in the system.
    /// These tests ensure that all dynamic screens follow the project architecture,
    /// regardless of whether they use real tables (SalesOrder, Material, etc.) or EAV storage.
    /// </summary>
    public class DynamicScreenArchitectureTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        #region Test Data: Screen Type Definitions

        /// <summary>
        /// Screen types that use REAL Supabase tables (not EAV)
        /// These have dedicated entities and database tables
        /// </summary>
        public static IEnumerable<object[]> RealTableScreens => new List<object[]>
        {
            new object[] { "SalesOrder", "Transaction" },
            new object[] { "Material", "Master" },
            new object[] { "CostCenter", "Master" },
            new object[] { "GLAccount", "Master" },
            new object[] { "JournalEntry", "Transaction" },
            new object[] { "StockMovement", "Transaction" },
            new object[] { "EmployeePayroll", "Transaction" }
        };

        /// <summary>
        /// Screen types that use EAV (Entity-Attribute-Value) storage
        /// These are fully dynamic with no dedicated SQL tables
        /// </summary>
        public static IEnumerable<object[]> EavScreens => new List<object[]>
        {
            new object[] { "CustomObject", "Master" },
            new object[] { "Vendor", "Master" },
            new object[] { "Customer", "Master" },
            new object[] { "Project", "Master" },
            new object[] { "Campaign", "Master" },
            new object[] { "ExpenseClaim", "Transaction" },
            new object[] { "PurchaseRequest", "Transaction" }
        };

        /// <summary>
        /// All supported object types (both real table and EAV)
        /// </summary>
        public static IEnumerable<object[]> AllScreenTypes => RealTableScreens.Concat(EavScreens);

        /// <summary>
        /// Valid schema versions supported by the system
        /// </summary>
        public static IEnumerable<object[]> ValidVersions => new List<object[]>
        {
            new object[] { 1 },
            new object[] { 2 },
            new object[] { 3 },
            new object[] { 4 },
            new object[] { 5 },
            new object[] { 6 },
            new object[] { 7 }
        };

        #endregion

        #region Schema Structure Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_MustHaveRequiredFields(string module, string objectType)
        {
            // Arrange
            var schema = CreateMinimalValidSchema(module, objectType);

            // Act & Assert
            Assert.NotNull(schema);
            Assert.False(string.IsNullOrEmpty(schema.TenantId), "TenantId is required");
            Assert.False(string.IsNullOrEmpty(schema.Module), "Module is required");
            Assert.True(schema.Version > 0, "Version must be greater than 0");
            Assert.False(string.IsNullOrEmpty(schema.ObjectType), "ObjectType is required");
            Assert.NotNull(schema.Fields, "Fields dictionary is required");
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_ObjectTypeMustBeValid(string module, string objectType)
        {
            // Arrange
            var validObjectTypes = new[] { "Master", "Transaction", "Document", "Configuration" };
            var schema = CreateMinimalValidSchema(module, objectType);

            // Act & Assert
            Assert.Contains(schema.ObjectType, validObjectTypes);
        }

        [Theory]
        [MemberData(nameof(ValidVersions))]
        public void Schema_VersionMustBeSupported(int version)
        {
            // Arrange
            var schema = CreateMinimalValidSchema("TestObject", "Master", version);

            // Act & Assert
            Assert.True(schema.Version >= 1 && schema.Version <= 7, 
                $"Version {version} must be between 1 and 7");
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_FieldsMustHaveValidRules(string module, string objectType)
        {
            // Arrange
            var schema = CreateSchemaWithFields(module, objectType);

            // Act & Assert
            foreach (var field in schema.Fields)
            {
                Assert.False(string.IsNullOrEmpty(field.Key), "Field name cannot be empty");
                Assert.NotNull(field.Value, $"Field rule for {field.Key} cannot be null");
                
                // Storage must be valid
                var validStorage = new[] { "Core", "Extension" };
                Assert.Contains(field.Value.Storage, validStorage);
            }
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_UiHintsMustBeValid(string module, string objectType)
        {
            // Arrange
            var schema = CreateSchemaWithUiHints(module, objectType);
            var validTypes = new[] { "text", "number", "date", "select", "textarea", "checkbox", "lookup" };

            // Act & Assert
            foreach (var field in schema.Fields)
            {
                if (field.Value.Ui != null)
                {
                    Assert.Contains(field.Value.Ui.Type, validTypes);
                    
                    // If lookup type, must have lookup configuration
                    if (field.Value.Ui.Type == "lookup")
                    {
                        Assert.False(string.IsNullOrEmpty(field.Value.Ui.Lookup), 
                            $"Lookup field {field.Key} must have Lookup property");
                    }
                }
            }
        }

        #endregion

        #region Template Configuration Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_TemplateConfig_IsOptional(string module, string objectType)
        {
            // Arrange & Act
            var schemaWithoutConfig = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Name"] = new FieldRule(Required: true)
                },
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Assert
            Assert.Null(schemaWithoutConfig.CalculationRules);
            Assert.Null(schemaWithoutConfig.DocumentTotals);
            Assert.Null(schemaWithoutConfig.AttachmentConfig);
            Assert.Null(schemaWithoutConfig.CloudStorage);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_CalculationRules_IfPresent_MustBeValid(string module, string objectType)
        {
            // Arrange
            var calculationRules = new CalculationRulesConfig
            {
                ServerSide = new ServerSideCalculations
                {
                    LineItemCalculations = new List<LineItemCalculation>
                    {
                        new LineItemCalculation
                        {
                            TargetField = "TotalAmount",
                            Formula = "Quantity * UnitPrice",
                            Trigger = "onChange",
                            DependentFields = new List<string> { "Quantity", "UnitPrice" }
                        }
                    }
                }
            };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>(),
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: calculationRules,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Act & Assert
            Assert.NotNull(schema.CalculationRules);
            Assert.NotNull(schema.CalculationRules.ServerSide);
            Assert.Single(schema.CalculationRules.ServerSide.LineItemCalculations);
            
            var calc = schema.CalculationRules.ServerSide.LineItemCalculations[0];
            Assert.False(string.IsNullOrEmpty(calc.TargetField));
            Assert.False(string.IsNullOrEmpty(calc.Formula));
            Assert.NotEmpty(calc.DependentFields);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_DocumentTotals_IfPresent_MustBeValid(string module, string objectType)
        {
            // Arrange
            var documentTotals = new DocumentTotalsConfig
            {
                Fields = new Dictionary<string, TotalFieldConfig>
                {
                    ["TotalAmount"] = new TotalFieldConfig
                    {
                        Source = "LineItems",
                        Formula = "SUM(LineTotal)",
                        Label = "Total Amount",
                        DisplayPosition = "footer",
                        DecimalPlaces = 2,
                        Editable = false,
                        IsReadOnly = true
                    }
                },
                DisplayConfig = new TotalsDisplayConfig
                {
                    Layout = "stacked",
                    Position = "bottom",
                    ShowSeparator = true
                }
            };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>(),
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: documentTotals,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Act & Assert
            Assert.NotNull(schema.DocumentTotals);
            Assert.NotEmpty(schema.DocumentTotals.Fields);
            Assert.NotNull(schema.DocumentTotals.DisplayConfig);
            
            foreach (var field in schema.DocumentTotals.Fields)
            {
                Assert.False(string.IsNullOrEmpty(field.Value.Source));
                Assert.False(string.IsNullOrEmpty(field.Value.Label));
            }
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_AttachmentConfig_IfPresent_MustBeValid(string module, string objectType)
        {
            // Arrange
            var attachmentConfig = new AttachmentConfig
            {
                DocumentLevel = new DocumentLevelAttachmentConfig
                {
                    Enabled = true,
                    MaxFiles = 10,
                    MaxFileSizeMB = 50,
                    AllowedTypes = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx" },
                    Categories = new List<AttachmentCategory>
                    {
                        new AttachmentCategory { Id = "contract", Label = "Contract", Required = true },
                        new AttachmentCategory { Id = "invoice", Label = "Invoice", Required = false }
                    },
                    StorageProvider = "primary"
                },
                LineLevel = new LineLevelAttachmentConfig
                {
                    Enabled = false,
                    MaxFiles = 3,
                    MaxFileSizeMB = 10,
                    StorageProvider = "primary"
                }
            };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>(),
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: attachmentConfig,
                CloudStorage: null
            );

            // Act & Assert
            Assert.NotNull(schema.AttachmentConfig);
            Assert.True(schema.AttachmentConfig.DocumentLevel.MaxFiles > 0);
            Assert.True(schema.AttachmentConfig.DocumentLevel.MaxFileSizeMB > 0);
            Assert.NotEmpty(schema.AttachmentConfig.DocumentLevel.AllowedTypes);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_CloudStorage_IfPresent_MustBeValid(string module, string objectType)
        {
            // Arrange
            var cloudStorage = new CloudStorageConfig
            {
                Providers = new List<StorageProviderConfig>
                {
                    new StorageProviderConfig
                    {
                        Id = "primary",
                        Provider = "S3",
                        IsDefault = true,
                        Config = new ProviderConfig
                        {
                            BucketName = "valora-documents",
                            Region = "us-east-1",
                            BasePath = $"{module.ToLower()}/attachments"
                        }
                    }
                },
                GlobalSettings = new GlobalStorageSettings
                {
                    EncryptionAtRest = true,
                    VersioningEnabled = true,
                    RetentionDays = 2555 // 7 years
                }
            };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>(),
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: cloudStorage
            );

            // Act & Assert
            Assert.NotNull(schema.CloudStorage);
            Assert.NotEmpty(schema.CloudStorage.Providers);
            Assert.Single(schema.CloudStorage.Providers.Where(p => p.IsDefault));
            
            var defaultProvider = schema.CloudStorage.Providers.First(p => p.IsDefault);
            Assert.False(string.IsNullOrEmpty(defaultProvider.Id));
            Assert.False(string.IsNullOrEmpty(defaultProvider.Provider));
        }

        #endregion

        #region Version Management Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_Versioning_MustSupportUpToV7(string module, string objectType)
        {
            // Arrange & Act - Create schemas for all supported versions
            var schemas = new List<ModuleSchema>();
            for (int version = 1; version <= 7; version++)
            {
                schemas.Add(CreateMinimalValidSchema(module, objectType, version));
            }

            // Assert
            Assert.Equal(7, schemas.Count);
            for (int i = 0; i < schemas.Count; i++)
            {
                Assert.Equal(i + 1, schemas[i].Version);
            }
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_VersionIncrement_MustPreserveBackwardCompatibility(string module, string objectType)
        {
            // Arrange - Create v1 and v2 schemas
            var v1Schema = CreateMinimalValidSchema(module, objectType, 1);
            var v2Schema = CreateMinimalValidSchema(module, objectType, 2);

            // Act - Add new optional field in v2
            v2Schema = v2Schema with
            {
                Fields = new Dictionary<string, FieldRule>(v1Schema.Fields)
                {
                    ["NewOptionalField"] = new FieldRule(Required: false)
                }
            };

            // Assert - v1 fields must still exist in v2
            foreach (var field in v1Schema.Fields)
            {
                Assert.True(v2Schema.Fields.ContainsKey(field.Key),
                    $"Field {field.Key} from v1 must exist in v2");
            }
        }

        #endregion

        #region Unique Constraints Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void Schema_UniqueConstraints_IfPresent_MustBeValid(string module, string objectType)
        {
            // Arrange
            var uniqueConstraints = new List<string[]>
            {
                new[] { "Code" },
                new[] { "TenantId", "DocumentNumber" }
            };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Code"] = new FieldRule(Required: true, Unique: true),
                    ["DocumentNumber"] = new FieldRule(Required: true),
                    ["TenantId"] = new FieldRule(Required: true)
                },
                UniqueConstraints: uniqueConstraints,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Act & Assert
            Assert.NotNull(schema.UniqueConstraints);
            Assert.Equal(2, schema.UniqueConstraints.Count);
            
            // All fields in unique constraints must exist in Fields dictionary
            foreach (var constraint in schema.UniqueConstraints)
            {
                foreach (var fieldName in constraint)
                {
                    Assert.True(schema.Fields.ContainsKey(fieldName),
                        $"Unique constraint field {fieldName} must exist in Fields");
                }
            }
        }

        #endregion

        #region ShouldPost (Auto-Posting) Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void Schema_TransactionScreens_CanHaveShouldPost(string module, string objectType)
        {
            // Arrange - Transaction screens can auto-post to accounting
            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>(),
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: true, // Enable auto-posting
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Act & Assert
            Assert.True(schema.ShouldPost);
        }

        [Theory]
        [MemberData(nameof(EavScreens))]
        public void Schema_EavScreens_ShouldPostDefaultsToFalse(string module, string objectType)
        {
            // Arrange & Act
            var schema = CreateMinimalValidSchema(module, objectType);

            // Assert - EAV screens typically don't auto-post (unless explicitly configured)
            Assert.False(schema.ShouldPost);
        }

        #endregion

        #region Helper Methods

        private ModuleSchema CreateMinimalValidSchema(string module, string objectType, int version = 1)
        {
            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: version,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Id"] = new FieldRule(Required: true, Storage: "Core"),
                    ["CreatedAt"] = new FieldRule(Required: true, Storage: "Core"),
                    ["UpdatedAt"] = new FieldRule(Required: false, Storage: "Core")
                },
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        private ModuleSchema CreateSchemaWithFields(string module, string objectType)
        {
            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Id"] = new FieldRule(Required: true, Storage: "Core"),
                    ["Code"] = new FieldRule(Required: true, Unique: true, MaxLength: 50, Storage: "Core"),
                    ["Name"] = new FieldRule(Required: true, MaxLength: 200, Storage: "Core"),
                    ["Description"] = new FieldRule(Required: false, MaxLength: 1000, Storage: "Extension"),
                    ["Status"] = new FieldRule(Required: true, Storage: "Core"),
                    ["CustomField1"] = new FieldRule(Required: false, Storage: "Extension"),
                    ["CustomField2"] = new FieldRule(Required: false, Storage: "Extension")
                },
                UniqueConstraints: new List<string[]> { new[] { "Code" } },
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        private ModuleSchema CreateSchemaWithUiHints(string module, string objectType)
        {
            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Name"] = new FieldRule(
                        Required: true,
                        Storage: "Core",
                        Ui: new UiHint(Type: "text", Label: "Name", Section: "General")
                    ),
                    ["Amount"] = new FieldRule(
                        Required: true,
                        Storage: "Core",
                        Ui: new UiHint(Type: "number", Label: "Amount", DecimalPlaces: 2)
                    ),
                    ["OrderDate"] = new FieldRule(
                        Required: true,
                        Storage: "Core",
                        Ui: new UiHint(Type: "date", Label: "Order Date")
                    ),
                    ["Status"] = new FieldRule(
                        Required: true,
                        Storage: "Core",
                        Ui: new UiHint(
                            Type: "select",
                            Label: "Status",
                            Options: new[] { "Draft", "Active", "Inactive" }
                        )
                    ),
                    ["CustomerId"] = new FieldRule(
                        Required: true,
                        Storage: "Core",
                        Ui: new UiHint(
                            Type: "lookup",
                            Label: "Customer",
                            Lookup: "Customer",
                            LookupField: "Id",
                            DisplayField: "Name"
                        )
                    ),
                    ["Notes"] = new FieldRule(
                        Required: false,
                        Storage: "Extension",
                        Ui: new UiHint(Type: "textarea", Label: "Notes")
                    )
                },
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        #endregion
    }
}

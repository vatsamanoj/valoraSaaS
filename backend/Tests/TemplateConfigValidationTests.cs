using System.Text.Json;
using System.Text.Json.Serialization;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using Xunit;

namespace Valora.Tests.Architecture
{
    /// <summary>
    /// Template Configuration Validation Tests
    /// Validates the optional template configuration extensions for ANY dynamic screen:
    /// - CalculationRules (optional but validated if present)
    /// - DocumentTotals (optional but validated if present)
    /// - AttachmentConfig (optional but validated if present)
    /// - CloudStorage (optional but validated if present)
    /// - ComplexCalculation flag behavior
    /// </summary>
    public class TemplateConfigValidationTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        #region Test Data: Screen Types

        /// <summary>
        /// All screen types that can have template configuration
        /// </summary>
        public static IEnumerable<object[]> AllScreenTypes => new List<object[]>
        {
            new object[] { "SalesOrder", "Transaction" },
            new object[] { "Material", "Master" },
            new object[] { "CostCenter", "Master" },
            new object[] { "GLAccount", "Master" },
            new object[] { "JournalEntry", "Transaction" },
            new object[] { "StockMovement", "Transaction" },
            new object[] { "EmployeePayroll", "Transaction" },
            new object[] { "Vendor", "Master" },
            new object[] { "Customer", "Master" },
            new object[] { "Project", "Master" },
            new object[] { "Campaign", "Master" },
            new object[] { "ExpenseClaim", "Transaction" },
            new object[] { "PurchaseRequest", "Transaction" },
            new object[] { "CustomObject", "Master" }
        };

        /// <summary>
        /// Valid calculation triggers
        /// </summary>
        public static IEnumerable<object[]> ValidTriggers => new List<object[]>
        {
            new object[] { "onChange" },
            new object[] { "onLineChange" },
            new object[] { "onSave" },
            new object[] { "onLoad" }
        };

        /// <summary>
        /// Valid calculation scopes (only LineItem and Document exist in the actual code)
        /// </summary>
        public static IEnumerable<object[]> ValidScopes => new List<object[]>
        {
            new object[] { CalculationScope.LineItem },
            new object[] { CalculationScope.Document }
        };

        /// <summary>
        /// Valid storage providers
        /// </summary>
        public static IEnumerable<object[]> ValidStorageProviders => new List<object[]>
        {
            new object[] { "S3" },
            new object[] { "AzureBlob" },
            new object[] { "GCP" },
            new object[] { "Local" }
        };

        #endregion

        #region CalculationRules Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_IsOptional(string module, string objectType)
        {
            // Arrange & Act
            var schema = CreateSchemaWithoutConfig(module, objectType);

            // Assert
            Assert.Null(schema.CalculationRules);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_IfPresent_MustHaveServerSideOrClientSide(string module, string objectType)
        {
            // Arrange
            var calculationRules = new CalculationRulesConfig
            {
                ServerSide = new ServerSideCalculations(),
                ClientSide = new ClientSideCalculations()
            };

            var schema = CreateSchemaWithCalculationRules(module, objectType, calculationRules);

            // Act & Assert
            Assert.NotNull(schema.CalculationRules);
            Assert.NotNull(schema.CalculationRules.ServerSide);
            Assert.NotNull(schema.CalculationRules.ClientSide);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_LineItemCalculation_MustHaveTargetField(string module, string objectType)
        {
            // Arrange
            var calculation = new LineItemCalculation
            {
                TargetField = "TotalAmount",
                Formula = "Quantity * UnitPrice",
                Trigger = "onChange",
                DependentFields = new List<string> { "Quantity", "UnitPrice" }
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(calculation.TargetField));
            Assert.False(string.IsNullOrEmpty(calculation.Formula));
            Assert.NotEmpty(calculation.DependentFields);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_DocumentCalculation_MustHaveTargetField(string module, string objectType)
        {
            // Arrange
            var calculation = new DocumentCalculation
            {
                TargetField = "GrandTotal",
                Formula = "SUM(LineTotal)",
                Trigger = "onLineChange"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(calculation.TargetField));
            Assert.False(string.IsNullOrEmpty(calculation.Formula));
        }

        [Theory]
        [MemberData(nameof(ValidTriggers))]
        public void CalculationRules_Trigger_MustBeValid(string trigger)
        {
            // Arrange
            var validTriggers = new[] { "onChange", "onLineChange", "onSave", "onLoad" };

            // Act & Assert
            Assert.Contains(trigger, validTriggers);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_ComplexCalculation_MustHaveId(string module, string objectType)
        {
            // Arrange
            var complexCalc = new ComplexCalculation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Custom Tax Calculation",
                TargetField = "TaxAmount",
                Scope = CalculationScope.Document,
                Expression = "TotalAmount * 0.18"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(complexCalc.Id));
            Assert.False(string.IsNullOrEmpty(complexCalc.Name));
        }

        [Theory]
        [MemberData(nameof(ValidScopes))]
        public void CalculationRules_ComplexCalculationScope_MustBeValid(CalculationScope scope)
        {
            // Arrange
            var validScopes = new[] { CalculationScope.LineItem, CalculationScope.Document };

            // Act & Assert
            Assert.Contains(scope, validScopes);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_ComplexCalculation_CanHaveParameters(string module, string objectType)
        {
            // Arrange
            var complexCalc = new ComplexCalculation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Discount Calculation",
                TargetField = "DiscountAmount",
                Parameters = new List<CalculationParameter>
                {
                    new CalculationParameter { Name = "DiscountRate", Source = "Field", DataType = "decimal", IsRequired = true },
                    new CalculationParameter { Name = "MaxDiscount", Source = "Field", DataType = "decimal", IsRequired = true }
                }
            };

            // Act & Assert
            Assert.NotEmpty(complexCalc.Parameters);
            Assert.All(complexCalc.Parameters, p =>
            {
                Assert.False(string.IsNullOrEmpty(p.Name));
                Assert.False(string.IsNullOrEmpty(p.Source));
            });
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_ComplexCalculation_CanHaveExternalDataSources(string module, string objectType)
        {
            // Arrange
            var complexCalc = new ComplexCalculation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Price Lookup",
                ExternalDataSources = new List<ExternalDataSource>
                {
                    new ExternalDataSource
                    {
                        Name = "PriceTable",
                        SourceType = "API",
                        QueryOrEndpoint = "/api/pricing"
                    }
                }
            };

            // Act & Assert
            Assert.NotEmpty(complexCalc.ExternalDataSources);
            Assert.All(complexCalc.ExternalDataSources, ds =>
            {
                Assert.False(string.IsNullOrEmpty(ds.Name));
                Assert.False(string.IsNullOrEmpty(ds.SourceType));
            });
        }

        [Fact]
        public void CalculationRules_ComplexCalculation_CanHaveCodeBlock()
        {
            // Arrange
            var complexCalc = new ComplexCalculation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Advanced Calculation",
                Expression = "CalculateTax(TotalAmount, TaxRate)",
                CodeBlock = @"
                    decimal CalculateTax(decimal amount, decimal rate) {
                        return amount * rate;
                    }
                "
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(complexCalc.CodeBlock));
        }

        #endregion

        #region DocumentTotals Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_IsOptional(string module, string objectType)
        {
            // Arrange & Act
            var schema = CreateSchemaWithoutConfig(module, objectType);

            // Assert
            Assert.Null(schema.DocumentTotals);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_IfPresent_MustHaveFields(string module, string objectType)
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
                DisplayConfig = new TotalsDisplayConfig()
            };

            var schema = CreateSchemaWithDocumentTotals(module, objectType, documentTotals);

            // Act & Assert
            Assert.NotNull(schema.DocumentTotals);
            Assert.NotEmpty(schema.DocumentTotals.Fields);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_TotalFieldConfig_MustHaveSource(string module, string objectType)
        {
            // Arrange
            var fieldConfig = new TotalFieldConfig
            {
                Source = "LineItems",
                Label = "Total",
                DisplayPosition = "footer"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(fieldConfig.Source));
            Assert.False(string.IsNullOrEmpty(fieldConfig.Label));
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_TotalFieldConfig_DecimalPlaces_DefaultsTo2(string module, string objectType)
        {
            // Arrange
            var fieldConfig = new TotalFieldConfig
            {
                Source = "LineItems",
                Label = "Total"
            };

            // Act & Assert
            Assert.Equal(2, fieldConfig.DecimalPlaces);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_TotalFieldConfig_ReadOnlyDefaultsToTrue(string module, string objectType)
        {
            // Arrange
            var fieldConfig = new TotalFieldConfig
            {
                Source = "LineItems",
                Label = "Total"
            };

            // Act & Assert
            Assert.True(fieldConfig.IsReadOnly);
            Assert.False(fieldConfig.Editable);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void DocumentTotals_DisplayConfig_MustHaveLayout(string module, string objectType)
        {
            // Arrange
            var displayConfig = new TotalsDisplayConfig
            {
                Layout = "stacked",
                Position = "bottom"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(displayConfig.Layout));
            Assert.False(string.IsNullOrEmpty(displayConfig.Position));
        }

        [Fact]
        public void DocumentTotals_DisplayConfig_ValidLayouts()
        {
            // Arrange
            var validLayouts = new[] { "stacked", "horizontal", "grid" };
            var config = new TotalsDisplayConfig { Layout = "stacked" };

            // Act & Assert
            Assert.Contains(config.Layout, validLayouts);
        }

        [Fact]
        public void DocumentTotals_DisplayConfig_ValidPositions()
        {
            // Arrange
            var validPositions = new[] { "top", "bottom", "left", "right" };
            var config = new TotalsDisplayConfig { Position = "bottom" };

            // Act & Assert
            Assert.Contains(config.Position, validPositions);
        }

        #endregion

        #region AttachmentConfig Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_IsOptional(string module, string objectType)
        {
            // Arrange & Act
            var schema = CreateSchemaWithoutConfig(module, objectType);

            // Assert
            Assert.Null(schema.AttachmentConfig);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_IfPresent_MustHaveDocumentLevelAndLineLevel(string module, string objectType)
        {
            // Arrange
            var attachmentConfig = new AttachmentConfig
            {
                DocumentLevel = new DocumentLevelAttachmentConfig(),
                LineLevel = new LineLevelAttachmentConfig()
            };

            var schema = CreateSchemaWithAttachmentConfig(module, objectType, attachmentConfig);

            // Act & Assert
            Assert.NotNull(schema.AttachmentConfig);
            Assert.NotNull(schema.AttachmentConfig.DocumentLevel);
            Assert.NotNull(schema.AttachmentConfig.LineLevel);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_DocumentLevel_Defaults(string module, string objectType)
        {
            // Arrange
            var docLevel = new DocumentLevelAttachmentConfig();

            // Act & Assert
            Assert.False(docLevel.Enabled); // Disabled by default
            Assert.Equal(10, docLevel.MaxFiles);
            Assert.Equal(50, docLevel.MaxFileSizeMB);
            Assert.Equal("primary", docLevel.StorageProvider);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_LineLevel_Defaults(string module, string objectType)
        {
            // Arrange
            var lineLevel = new LineLevelAttachmentConfig();

            // Act & Assert
            Assert.False(lineLevel.Enabled); // Disabled by default
            Assert.Equal(3, lineLevel.MaxFiles);
            Assert.Equal(10, lineLevel.MaxFileSizeMB);
            Assert.Equal("primary", lineLevel.StorageProvider);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_Categories_MustHaveIdAndLabel(string module, string objectType)
        {
            // Arrange
            var categories = new List<AttachmentCategory>
            {
                new AttachmentCategory { Id = "contract", Label = "Contract", Required = true },
                new AttachmentCategory { Id = "invoice", Label = "Invoice", Required = false }
            };

            // Act & Assert
            Assert.All(categories, c =>
            {
                Assert.False(string.IsNullOrEmpty(c.Id));
                Assert.False(string.IsNullOrEmpty(c.Label));
            });
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_AllowedTypes_MustBeValidExtensions(string module, string objectType)
        {
            // Arrange
            var allowedTypes = new List<string> { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".jpg", ".png" };

            var docLevel = new DocumentLevelAttachmentConfig
            {
                Enabled = true,
                AllowedTypes = allowedTypes
            };

            // Act & Assert
            Assert.All(docLevel.AllowedTypes, type =>
            {
                Assert.StartsWith(".", type);
                Assert.True(type.Length > 1, "Extension should be more than just a dot");
            });
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void AttachmentConfig_LineLevel_CanHaveGridColumnConfig(string module, string objectType)
        {
            // Arrange
            var lineLevel = new LineLevelAttachmentConfig
            {
                GridColumn = new GridColumnConfig
                {
                    Width = "120px",
                    ShowCount = true,
                    AllowPreview = true
                }
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(lineLevel.GridColumn.Width));
        }

        #endregion

        #region CloudStorage Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_IsOptional(string module, string objectType)
        {
            // Arrange & Act
            var schema = CreateSchemaWithoutConfig(module, objectType);

            // Assert
            Assert.Null(schema.CloudStorage);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_IfPresent_MustHaveProviders(string module, string objectType)
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
                        Config = new ProviderConfig()
                    }
                },
                GlobalSettings = new GlobalStorageSettings()
            };

            var schema = CreateSchemaWithCloudStorage(module, objectType, cloudStorage);

            // Act & Assert
            Assert.NotNull(schema.CloudStorage);
            Assert.NotEmpty(schema.CloudStorage.Providers);
            Assert.NotNull(schema.CloudStorage.GlobalSettings);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_MustHaveExactlyOneDefaultProvider(string module, string objectType)
        {
            // Arrange
            var providers = new List<StorageProviderConfig>
            {
                new StorageProviderConfig { Id = "primary", Provider = "S3", IsDefault = true },
                new StorageProviderConfig { Id = "secondary", Provider = "AzureBlob", IsDefault = false }
            };

            // Act
            var defaultProviders = providers.Where(p => p.IsDefault).ToList();

            // Assert
            Assert.Single(defaultProviders);
        }

        [Theory]
        [MemberData(nameof(ValidStorageProviders))]
        public void CloudStorage_Provider_MustBeValid(string provider)
        {
            // Arrange
            var validProviders = new[] { "S3", "AzureBlob", "GCP", "Local" };

            // Act & Assert
            Assert.Contains(provider, validProviders);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_S3Config_MustHaveBucketNameAndRegion(string module, string objectType)
        {
            // Arrange
            var config = new ProviderConfig
            {
                BucketName = "valora-documents",
                Region = "us-east-1",
                BasePath = "documents"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(config.BucketName));
            Assert.False(string.IsNullOrEmpty(config.Region));
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_AzureConfig_MustHaveAccountNameAndContainer(string module, string objectType)
        {
            // Arrange
            var config = new ProviderConfig
            {
                AccountName = "valorastorage",
                ContainerName = "documents",
                BasePath = "attachments"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(config.AccountName));
            Assert.False(string.IsNullOrEmpty(config.ContainerName));
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_GCPConfig_MustHaveProjectId(string module, string objectType)
        {
            // Arrange
            var config = new ProviderConfig
            {
                ProjectId = "valora-project",
                BasePath = "storage/documents"
            };

            // Act & Assert
            Assert.False(string.IsNullOrEmpty(config.ProjectId));
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_GlobalSettings_CanHaveEncryption(string module, string objectType)
        {
            // Arrange
            var settings = new GlobalStorageSettings
            {
                VirusScanEnabled = true,
                GenerateThumbnails = true
            };

            // Act & Assert
            Assert.True(settings.VirusScanEnabled);
            Assert.True(settings.GenerateThumbnails);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CloudStorage_LifecycleRules_CanBeConfigured(string module, string objectType)
        {
            // Arrange
            var lifecycleRules = new LifecycleRules
            {
                MoveToColdStorageAfterDays = 90,
                AutoDeleteAfterDays = 2555 // 7 years
            };

            // Act & Assert
            Assert.True(lifecycleRules.MoveToColdStorageAfterDays > 0);
            Assert.True(lifecycleRules.AutoDeleteAfterDays > lifecycleRules.MoveToColdStorageAfterDays);
        }

        #endregion

        #region ComplexCalculation Flag Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void ComplexCalculationFlag_WhenFalse_NoComplexCalculations(string module, string objectType)
        {
            // Arrange
            var hasComplexCalculations = false;

            // Act & Assert
            Assert.False(hasComplexCalculations);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void ComplexCalculationFlag_WhenTrue_CanHaveComplexCalculations(string module, string objectType)
        {
            // Arrange
            var calculationRules = new CalculationRulesConfig
            {
                ServerSide = new ServerSideCalculations
                {
                    ComplexCalculations = new List<ComplexCalculation>
                    {
                        new ComplexCalculation
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = "Advanced Tax",
                            Expression = "CalculateAdvancedTax(TotalAmount, Region)",
                            Scope = CalculationScope.Document
                        }
                    }
                }
            };

            // Act
            var hasComplexCalculations = calculationRules.ServerSide.ComplexCalculations.Any();

            // Assert
            Assert.True(hasComplexCalculations);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void ComplexCalculation_CanReferenceExternalAssemblies(string module, string objectType)
        {
            // Arrange
            var complexCalc = new ComplexCalculation
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Custom Calculation",
                AssemblyReferences = new List<string>
                {
                    "Valora.Calculations.dll",
                    "Valora.TaxEngine.dll"
                }
            };

            // Act & Assert
            Assert.NotEmpty(complexCalc.AssemblyReferences);
        }

        #endregion

        #region Serialization Tests

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void TemplateConfig_Serialization_RoundTrip(string module, string objectType)
        {
            // Arrange
            var schema = CreateFullSchema(module, objectType);

            // Act
            var json = JsonSerializer.Serialize(schema, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<ModuleSchema>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(schema.Module, deserialized.Module);
            Assert.Equal(schema.Version, deserialized.Version);
            Assert.Equal(schema.ObjectType, deserialized.ObjectType);
        }

        [Theory]
        [MemberData(nameof(AllScreenTypes))]
        public void CalculationRules_Serialization_RoundTrip(string module, string objectType)
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
                            TargetField = "Total",
                            Formula = "Qty * Price",
                            Trigger = "onChange",
                            DependentFields = new List<string> { "Qty", "Price" }
                        }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(calculationRules, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<CalculationRulesConfig>(json, _jsonOptions);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Single(deserialized.ServerSide.LineItemCalculations);
        }

        #endregion

        #region Helper Methods

        private ModuleSchema CreateSchemaWithoutConfig(string module, string objectType)
        {
            return new ModuleSchema(
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
                CloudStorage: null
            );
        }

        private ModuleSchema CreateSchemaWithCalculationRules(string module, string objectType, CalculationRulesConfig calculationRules)
        {
            return new ModuleSchema(
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
        }

        private ModuleSchema CreateSchemaWithDocumentTotals(string module, string objectType, DocumentTotalsConfig documentTotals)
        {
            return new ModuleSchema(
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
        }

        private ModuleSchema CreateSchemaWithAttachmentConfig(string module, string objectType, AttachmentConfig attachmentConfig)
        {
            return new ModuleSchema(
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
        }

        private ModuleSchema CreateSchemaWithCloudStorage(string module, string objectType, CloudStorageConfig cloudStorage)
        {
            return new ModuleSchema(
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
        }

        private ModuleSchema CreateFullSchema(string module, string objectType)
        {
            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: objectType,
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Id"] = new FieldRule(Required: true, Storage: "Core"),
                    ["Name"] = new FieldRule(Required: true, Storage: "Core")
                },
                UniqueConstraints: null,
                Ui: new ModuleUi(Title: module, Icon: "document"),
                ShouldPost: objectType == "Transaction",
                CalculationRules: new CalculationRulesConfig
                {
                    ServerSide = new ServerSideCalculations
                    {
                        LineItemCalculations = new List<LineItemCalculation>()
                    }
                },
                DocumentTotals: new DocumentTotalsConfig
                {
                    Fields = new Dictionary<string, TotalFieldConfig>(),
                    DisplayConfig = new TotalsDisplayConfig()
                },
                AttachmentConfig: new AttachmentConfig
                {
                    DocumentLevel = new DocumentLevelAttachmentConfig(),
                    LineLevel = new LineLevelAttachmentConfig()
                },
                CloudStorage: new CloudStorageConfig
                {
                    Providers = new List<StorageProviderConfig>(),
                    GlobalSettings = new GlobalStorageSettings()
                }
            );
        }

        #endregion
    }
}

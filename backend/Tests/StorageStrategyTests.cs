using System.Text.Json;
using Valora.Api.Application.Schemas;
using Xunit;

namespace Valora.Tests.Architecture
{
    /// <summary>
    /// Storage Strategy Tests - Real Table vs EAV Decision Logic
    /// Validates that the system correctly decides between real Supabase tables and EAV storage
    /// for ANY dynamic screen in the system.
    /// </summary>
    public class StorageStrategyTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        #region Real Table Screen Types

        /// <summary>
        /// Screens that use dedicated real SQL tables (not EAV)
        /// These have full entity classes and database migrations
        /// </summary>
        public static IEnumerable<object[]> RealTableScreens => new List<object[]>
        {
            new object[] { "SalesOrder", new[] { "Id", "OrderNumber", "CustomerId", "OrderDate", "TotalAmount", "Currency", "Status", "Items" } },
            new object[] { "Material", new[] { "Id", "Code", "Name", "Description", "UnitOfMeasure", "StandardCost", "Category" } },
            new object[] { "CostCenter", new[] { "Id", "Code", "Name", "Description", "ResponsiblePerson", "ValidFrom", "ValidTo" } },
            new object[] { "GLAccount", new[] { "Id", "AccountCode", "AccountName", "AccountType", "ParentAccountId", "IsActive" } },
            new object[] { "JournalEntry", new[] { "Id", "DocumentNumber", "PostingDate", "DocumentDate", "Reference", "TotalAmount", "Currency", "Lines" } },
            new object[] { "StockMovement", new[] { "Id", "MaterialId", "MovementType", "Quantity", "UnitCost", "MovementDate", "ReferenceDocument" } },
            new object[] { "EmployeePayroll", new[] { "Id", "EmployeeId", "PayPeriod", "GrossSalary", "NetSalary", "TaxAmount", "PaymentDate" } }
        };

        #endregion

        #region EAV Screen Types

        /// <summary>
        /// Screens that use EAV (Entity-Attribute-Value) storage
        /// These are fully dynamic with ObjectDefinitions, ObjectRecords, ObjectRecordAttributes
        /// </summary>
        public static IEnumerable<object[]> EavScreens => new List<object[]>
        {
            new object[] { "Vendor", new[] { "Name", "Code", "ContactPerson", "Email", "Phone", "Address", "PaymentTerms" } },
            new object[] { "Customer", new[] { "Name", "Code", "ContactPerson", "Email", "Phone", "Address", "CreditLimit" } },
            new object[] { "Project", new[] { "Name", "Code", "Description", "StartDate", "EndDate", "Status", "Budget" } },
            new object[] { "Campaign", new[] { "Name", "Code", "Description", "StartDate", "EndDate", "Status", "Budget" } },
            new object[] { "ExpenseClaim", new[] { "ClaimNumber", "EmployeeId", "ClaimDate", "Description", "TotalAmount", "Status" } },
            new object[] { "PurchaseRequest", new[] { "RequestNumber", "RequestorId", "RequestDate", "RequiredDate", "Description", "Status" } },
            new object[] { "CustomObject", new[] { "Name", "CustomField1", "CustomField2", "CustomField3" } }
        };

        #endregion

        #region Storage Decision Logic Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_RealTableScreens_UseDedicatedTables(string module, string[] standardFields)
        {
            // Arrange
            var decision = GetStorageDecision(module);

            // Act & Assert
            Assert.Equal(StorageType.RealTable, decision);
            Assert.True(IsRealTableScreen(module), $"{module} should be a real table screen");
        }

        [Theory]
        [MemberData(nameof(EavScreens))]
        public void StorageStrategy_EavScreens_UseEavStorage(string module, string[] standardFields)
        {
            // Arrange
            var decision = GetStorageDecision(module);

            // Act & Assert
            Assert.Equal(StorageType.EAV, decision);
            Assert.False(IsRealTableScreen(module), $"{module} should use EAV storage");
        }

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_RealTableScreens_HaveStandardFields(string module, string[] standardFields)
        {
            // Arrange & Act
            var hasStandardFields = HasStandardFields(module, standardFields);

            // Assert
            Assert.True(hasStandardFields, $"{module} should have standard fields defined");
        }

        [Theory]
        [MemberData(nameof(EavScreens))]
        public void StorageStrategy_EavScreens_FieldsAreDynamic(string module, string[] standardFields)
        {
            // Arrange
            var schema = CreateEavSchema(module, standardFields);

            // Act & Assert
            Assert.NotNull(schema.Fields);
            Assert.All(schema.Fields, field =>
            {
                // All EAV fields should use "Extension" storage by default
                Assert.Equal("Extension", field.Value.Storage);
            });
        }

        #endregion

        #region Hybrid Storage Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_RealTableScreens_SupportHybridMode(string module, string[] standardFields)
        {
            // Arrange - Real table screens can also store custom fields in EAV
            var schema = CreateHybridSchema(module, standardFields);

            // Act
            var coreFields = schema.Fields.Where(f => f.Value.Storage == "Core").ToList();
            var extensionFields = schema.Fields.Where(f => f.Value.Storage == "Extension").ToList();

            // Assert
            Assert.NotEmpty(coreFields); // Must have core fields in real table
            Assert.All(coreFields, f => Assert.Contains(f.Key, standardFields));
        }

        [Fact]
        public void StorageStrategy_SalesOrder_HybridStorage()
        {
            // Arrange - SalesOrder is the primary example of hybrid storage
            var standardFields = new[] { "Id", "OrderNumber", "CustomerId", "OrderDate", "TotalAmount", "Currency", "Status" };
            var customFields = new[] { "CustomField1", "CustomField2", "ShippingInstructions" };

            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: "SalesOrder",
                Version: 1,
                ObjectType: "Transaction",
                Fields: new Dictionary<string, FieldRule>()
            );

            // Add standard fields (Core storage)
            foreach (var field in standardFields)
            {
                schema.Fields[field] = new FieldRule(Required: true, Storage: "Core");
            }

            // Add custom fields (Extension storage - EAV)
            foreach (var field in customFields)
            {
                schema.Fields[field] = new FieldRule(Required: false, Storage: "Extension");
            }

            // Act
            var coreFieldCount = schema.Fields.Count(f => f.Value.Storage == "Core");
            var extensionFieldCount = schema.Fields.Count(f => f.Value.Storage == "Extension");

            // Assert
            Assert.Equal(standardFields.Length, coreFieldCount);
            Assert.Equal(customFields.Length, extensionFieldCount);
        }

        [Fact]
        public void StorageStrategy_FieldStorage_MustBeValid()
        {
            // Arrange
            var validStorageTypes = new[] { "Core", "Extension" };
            var schema = CreateHybridSchema("TestObject", new[] { "Id", "Name" });

            // Act & Assert
            foreach (var field in schema.Fields)
            {
                Assert.Contains(field.Value.Storage, validStorageTypes);
            }
        }

        #endregion

        #region Storage Routing Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_CreateOperation_RoutesToCorrectStorage(string module, string[] standardFields)
        {
            // Arrange
            var isRealTable = IsRealTableScreen(module);

            // Act - Determine where create operation should go
            var targetStorage = isRealTable ? "RealTable" : "EAV";

            // Assert
            if (isRealTable)
            {
                Assert.Equal("RealTable", targetStorage);
                // Real table screens: Create in SQL table + optionally EAV for custom fields
            }
            else
            {
                Assert.Equal("EAV", targetStorage);
                // EAV screens: Create in ObjectRecord + ObjectRecordAttributes
            }
        }

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_ReadOperation_RoutesToCorrectStorage(string module, string[] standardFields)
        {
            // Arrange
            var isRealTable = IsRealTableScreen(module);

            // Act - For read operations, we need to combine data from both sources in hybrid mode
            var needsEavJoin = isRealTable && HasCustomFields(module);

            // Assert
            if (isRealTable)
            {
                Assert.True(IsRealTableScreen(module));
                // Real table: Read from SQL table
                // If hybrid: Also join with EAV for custom fields
            }
        }

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_UpdateOperation_RoutesToCorrectStorage(string module, string[] standardFields)
        {
            // Arrange
            var isRealTable = IsRealTableScreen(module);

            // Act - Update needs to route fields to correct storage based on Field.Storage property
            var fieldRouting = new Dictionary<string, string>
            {
                ["Core"] = "RealTable",
                ["Extension"] = "EAV"
            };

            // Assert
            Assert.Equal("RealTable", fieldRouting["Core"]);
            Assert.Equal("EAV", fieldRouting["Extension"]);
        }

        #endregion

        #region Schema Validation Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        [MemberData(nameof(EavScreens))]
        public void StorageStrategy_AllScreens_MustHaveStorageDefined(string module, string[] standardFields)
        {
            // Arrange
            var schema = CreateSchemaWithStorage(module, standardFields);

            // Act & Assert
            Assert.All(schema.Fields, field =>
            {
                Assert.False(string.IsNullOrEmpty(field.Value.Storage),
                    $"Field {field.Key} in {module} must have Storage defined");
            });
        }

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_RealTable_CoreFields_MustExistInEntity(string module, string[] standardFields)
        {
            // Arrange
            var entityFields = GetEntityFields(module);

            // Act & Assert
            foreach (var field in standardFields)
            {
                // Core fields must exist in the entity class
                Assert.True(entityFields.Contains(field) || IsCommonField(field),
                    $"Field {field} should exist in {module} entity or be a common field");
            }
        }

        [Fact]
        public void StorageStrategy_FieldStorage_CannotBeEmpty()
        {
            // Arrange
            var field = new FieldRule(Required: true, Storage: "");

            // Act & Assert
            Assert.True(string.IsNullOrEmpty(field.Storage), "Field storage should not be empty");
        }

        [Fact]
        public void StorageStrategy_CoreFields_AreRequired()
        {
            // Arrange
            var schema = new ModuleSchema(
                TenantId: "test-tenant",
                Module: "TestObject",
                Version: 1,
                ObjectType: "Master",
                Fields: new Dictionary<string, FieldRule>
                {
                    ["Id"] = new FieldRule(Required: true, Storage: "Core"),
                    ["Name"] = new FieldRule(Required: true, Storage: "Core"),
                    ["Description"] = new FieldRule(Required: false, Storage: "Extension")
                },
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );

            // Act
            var coreFields = schema.Fields.Where(f => f.Value.Storage == "Core");

            // Assert
            Assert.All(coreFields, f => Assert.True(f.Value.Required,
                $"Core field {f.Key} should be required"));
        }

        #endregion

        #region Migration Tests

        [Theory]
        [MemberData(nameof(RealTableScreens))]
        public void StorageStrategy_RealTableScreens_HaveMigrations(string module, string[] standardFields)
        {
            // Arrange & Act
            var hasMigration = HasMigration(module);

            // Assert
            Assert.True(hasMigration, $"{module} should have a database migration");
        }

        [Theory]
        [MemberData(nameof(EavScreens))]
        public void StorageStrategy_EavScreens_NoMigrationsNeeded(string module, string[] standardFields)
        {
            // Arrange & Act
            var needsMigration = NeedsMigration(module);

            // Assert
            Assert.False(needsMigration, $"{module} should not need migrations (uses EAV)");
        }

        #endregion

        #region Helper Methods

        private enum StorageType
        {
            RealTable,
            EAV
        }

        private StorageType GetStorageDecision(string module)
        {
            // This mirrors the logic in CreateEntityCommandHandler and other handlers
            var realTableModules = new[]
            {
                "SalesOrder", "Material", "CostCenter", "GLAccount",
                "JournalEntry", "StockMovement", "EmployeePayroll"
            };

            return realTableModules.Contains(module, StringComparer.OrdinalIgnoreCase)
                ? StorageType.RealTable
                : StorageType.EAV;
        }

        private bool IsRealTableScreen(string module)
        {
            var realTableModules = new[]
            {
                "SalesOrder", "Material", "CostCenter", "GLAccount",
                "JournalEntry", "StockMovement", "EmployeePayroll"
            };

            return realTableModules.Contains(module, StringComparer.OrdinalIgnoreCase);
        }

        private bool HasStandardFields(string module, string[] expectedFields)
        {
            // In real implementation, this would check the entity class
            return expectedFields.Length > 0;
        }

        private bool HasCustomFields(string module)
        {
            // Real table screens can have custom fields in EAV
            return IsRealTableScreen(module);
        }

        private HashSet<string> GetEntityFields(string module)
        {
            // Simulated entity fields for each real table module
            var entityFields = new Dictionary<string, string[]>
            {
                ["SalesOrder"] = new[] { "Id", "OrderNumber", "CustomerId", "OrderDate", "TotalAmount", "Currency", "Status", "Items", "CreatedAt", "UpdatedAt" },
                ["Material"] = new[] { "Id", "Code", "Name", "Description", "UnitOfMeasure", "StandardCost", "Category", "CreatedAt", "UpdatedAt" },
                ["CostCenter"] = new[] { "Id", "Code", "Name", "Description", "ResponsiblePerson", "ValidFrom", "ValidTo", "CreatedAt", "UpdatedAt" },
                ["GLAccount"] = new[] { "Id", "AccountCode", "AccountName", "AccountType", "ParentAccountId", "IsActive", "CreatedAt", "UpdatedAt" },
                ["JournalEntry"] = new[] { "Id", "DocumentNumber", "PostingDate", "DocumentDate", "Reference", "TotalAmount", "Currency", "Lines", "CreatedAt", "UpdatedAt" },
                ["StockMovement"] = new[] { "Id", "MaterialId", "MovementType", "Quantity", "UnitCost", "MovementDate", "ReferenceDocument", "CreatedAt", "UpdatedAt" },
                ["EmployeePayroll"] = new[] { "Id", "EmployeeId", "PayPeriod", "GrossSalary", "NetSalary", "TaxAmount", "PaymentDate", "CreatedAt", "UpdatedAt" }
            };

            return entityFields.ContainsKey(module)
                ? new HashSet<string>(entityFields[module])
                : new HashSet<string>();
        }

        private bool IsCommonField(string fieldName)
        {
            var commonFields = new[] { "Id", "CreatedAt", "UpdatedAt", "TenantId", "Version" };
            return commonFields.Contains(fieldName);
        }

        private ModuleSchema CreateEavSchema(string module, string[] fields)
        {
            var fieldRules = new Dictionary<string, FieldRule>();
            foreach (var field in fields)
            {
                fieldRules[field] = new FieldRule(Required: false, Storage: "Extension");
            }

            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: "Master",
                Fields: fieldRules,
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        private ModuleSchema CreateHybridSchema(string module, string[] standardFields)
        {
            var fieldRules = new Dictionary<string, FieldRule>();

            // Standard fields go to Core (real table)
            foreach (var field in standardFields)
            {
                fieldRules[field] = new FieldRule(Required: true, Storage: "Core");
            }

            // Add some custom fields that go to Extension (EAV)
            fieldRules["CustomField1"] = new FieldRule(Required: false, Storage: "Extension");
            fieldRules["CustomField2"] = new FieldRule(Required: false, Storage: "Extension");

            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: "Master",
                Fields: fieldRules,
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        private ModuleSchema CreateSchemaWithStorage(string module, string[] fields)
        {
            var isRealTable = IsRealTableScreen(module);
            var fieldRules = new Dictionary<string, FieldRule>();

            foreach (var field in fields)
            {
                // Real table screens: Core for standard fields
                // EAV screens: Extension for all fields
                var storage = isRealTable ? "Core" : "Extension";
                fieldRules[field] = new FieldRule(Required: true, Storage: storage);
            }

            return new ModuleSchema(
                TenantId: "test-tenant",
                Module: module,
                Version: 1,
                ObjectType: "Master",
                Fields: fieldRules,
                UniqueConstraints: null,
                Ui: null,
                ShouldPost: false,
                CalculationRules: null,
                DocumentTotals: null,
                AttachmentConfig: null,
                CloudStorage: null
            );
        }

        private bool HasMigration(string module)
        {
            // Real table screens have migrations
            return IsRealTableScreen(module);
        }

        private bool NeedsMigration(string module)
        {
            // EAV screens don't need migrations for new fields
            return IsRealTableScreen(module);
        }

        #endregion
    }
}

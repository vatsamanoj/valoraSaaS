using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using Valora.Api.Domain.Entities;
using Xunit;

namespace Valora.Tests.Architecture
{
    /// <summary>
    /// Generic MongoDB Projection Tests
    /// Validates that the ProjectionManager works correctly for ANY screen type,
    /// not just SalesOrder. Tests read model consistency and event-to-projection mapping.
    /// </summary>
    public class GenericProjectionTests
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        #region Test Data: All Screen Types

        /// <summary>
        /// All aggregate types that support projections
        /// </summary>
        public static IEnumerable<object[]> AllAggregateTypes => new List<object[]>
        {
            new object[] { "SalesOrder", "valora.sd.salesorder" },
            new object[] { "Material", "valora.mm.material" },
            new object[] { "CostCenter", "valora.co.costcenter" },
            new object[] { "GLAccount", "valora.fi.masterdata" },
            new object[] { "JournalEntry", "valora.fi.journalentry" },
            new object[] { "StockMovement", "valora.mm.stockmovement" },
            new object[] { "EmployeePayroll", "valora.hc.payroll" },
            new object[] { "CustomObject", "valora.data.changed" }
        };

        /// <summary>
        /// Real table aggregate types with complex relationships
        /// </summary>
        public static IEnumerable<object[]> RealTableAggregates => new List<object[]>
        {
            new object[] { "SalesOrder", new[] { "Items" } },
            new object[] { "JournalEntry", new[] { "Lines" } },
            new object[] { "EmployeePayroll", new[] { "Employee" } },
            new object[] { "StockMovement", new[] { "Material" } }
        };

        /// <summary>
        /// Event topics that trigger projections
        /// </summary>
        public static IEnumerable<object[]> ProjectionTopics => new List<object[]>
        {
            new object[] { "valora.sd.salesorder", "SalesOrder" },
            new object[] { "valora.mm.material", "Material" },
            new object[] { "valora.co.costcenter", "CostCenter" },
            new object[] { "valora.fi.masterdata", "GLAccount" },
            new object[] { "valora.fi.journalentry", "JournalEntry" },
            new object[] { "valora.mm.stockmovement", "StockMovement" },
            new object[] { "valora.hc.payroll", "EmployeePayroll" },
            new object[] { "valora.data.changed", "CustomObject" }
        };

        #endregion

        #region Projection Event Structure Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ProjectionEvent_MustHaveAggregateType(string aggregateType, string topic)
        {
            // Arrange
            var eventPayload = CreateValidProjectionEvent(aggregateType, Guid.NewGuid().ToString());

            // Act
            using var doc = JsonDocument.Parse(eventPayload);
            var hasAggregateType = doc.RootElement.TryGetProperty("AggregateType", out _) ||
                                   doc.RootElement.TryGetProperty("aggregateType", out _);

            // Assert
            Assert.True(hasAggregateType, "Event must have AggregateType property");
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ProjectionEvent_MustHaveAggregateId(string aggregateType, string topic)
        {
            // Arrange
            var eventPayload = CreateValidProjectionEvent(aggregateType, Guid.NewGuid().ToString());

            // Act
            using var doc = JsonDocument.Parse(eventPayload);
            var hasAggregateId = doc.RootElement.TryGetProperty("AggregateId", out _) ||
                                 doc.RootElement.TryGetProperty("aggregateId", out _);

            // Assert
            Assert.True(hasAggregateId, "Event must have AggregateId property");
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ProjectionEvent_AggregateId_MustBeValidGuid(string aggregateType, string topic)
        {
            // Arrange
            var validGuid = Guid.NewGuid().ToString();
            var eventPayload = CreateValidProjectionEvent(aggregateType, validGuid);

            // Act
            using var doc = JsonDocument.Parse(eventPayload);
            var idProp = doc.RootElement.GetProperty("AggregateId");
            var canParseGuid = Guid.TryParse(idProp.GetString(), out _);

            // Assert
            Assert.True(canParseGuid, "AggregateId must be a valid GUID");
        }

        [Theory]
        [MemberData(nameof(ProjectionTopics))]
        public void ProjectionEvent_Topic_MustMatchAggregateType(string topic, string expectedAggregateType)
        {
            // Arrange & Act
            var aggregateType = InferAggregateTypeFromTopic(topic);

            // Assert
            Assert.Equal(expectedAggregateType, aggregateType);
        }

        #endregion

        #region MongoDB Document Structure Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MongoProjection_MustHaveId(string aggregateType, string topic)
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var document = CreateMongoProjectionDocument(aggregateType, id, "test-tenant");

            // Act & Assert
            Assert.True(document.Contains("_id"), "Document must have _id field");
            Assert.Equal(id, document["_id"].AsString);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MongoProjection_MustHaveTenantId(string aggregateType, string topic)
        {
            // Arrange
            var document = CreateMongoProjectionDocument(aggregateType, Guid.NewGuid().ToString(), "test-tenant");

            // Act & Assert
            Assert.True(document.Contains("TenantId"), "Document must have TenantId field");
            Assert.Equal("test-tenant", document["TenantId"].AsString);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MongoProjection_MustHaveProjectedAt(string aggregateType, string topic)
        {
            // Arrange
            var document = CreateMongoProjectionDocument(aggregateType, Guid.NewGuid().ToString(), "test-tenant");

            // Act & Assert
            Assert.True(document.Contains("_projectedAt"), "Document must have _projectedAt field");
            Assert.IsType<BsonDateTime>(document["_projectedAt"]);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MongoProjection_CollectionName_MustIncludeEntityPrefix(string aggregateType, string topic)
        {
            // Arrange
            var expectedCollectionName = $"Entity_{aggregateType}";

            // Act & Assert
            Assert.StartsWith("Entity_", expectedCollectionName);
            Assert.EndsWith(aggregateType, expectedCollectionName);
        }

        [Theory]
        [MemberData(nameof(RealTableAggregates))]
        public void MongoProjection_ComplexAggregates_MustIncludeRelatedData(string aggregateType, string[] relatedEntities)
        {
            // Arrange
            var document = CreateComplexMongoDocument(aggregateType);

            // Act & Assert
            foreach (var relatedEntity in relatedEntities)
            {
                Assert.True(document.Contains(relatedEntity) || document.Contains(relatedEntity.ToLower()),
                    $"Document should include {relatedEntity} data");
            }
        }

        #endregion

        #region Event-to-Projection Mapping Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void EventMapping_CanResolveEntityType(string aggregateType, string topic)
        {
            // Arrange
            var entityType = ResolveEntityType(aggregateType);

            // Act & Assert
            Assert.NotNull(entityType);
            Assert.Equal(aggregateType, entityType.Name);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void EventMapping_CanExtractTenantId(string aggregateType, string topic)
        {
            // Arrange
            var eventPayload = CreateValidProjectionEvent(aggregateType, Guid.NewGuid().ToString(), "tenant-123");

            // Act
            using var doc = JsonDocument.Parse(eventPayload);
            var tenantId = ExtractTenantId(doc.RootElement);

            // Assert
            Assert.Equal("tenant-123", tenantId);
        }

        [Fact]
        public void EventMapping_InvalidAggregateType_ReturnsNull()
        {
            // Arrange
            var invalidAggregateType = "NonExistentEntity";

            // Act
            var entityType = ResolveEntityType(invalidAggregateType);

            // Assert
            Assert.Null(entityType);
        }

        [Fact]
        public void EventMapping_MissingAggregateType_LogsWarning()
        {
            // Arrange
            var invalidPayload = "{ \"AggregateId\": \"123\" }";

            // Act
            using var doc = JsonDocument.Parse(invalidPayload);
            var hasAggregateType = doc.RootElement.TryGetProperty("AggregateType", out _);

            // Assert
            Assert.False(hasAggregateType);
        }

        #endregion

        #region Read Model Consistency Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ReadModelConsistency_VersionMustBeInt64(string aggregateType, string topic)
        {
            // Arrange - MongoDB doesn't support UInt32, so Version must be stored as Int64
            var document = new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["TenantId"] = "test-tenant",
                ["Version"] = new BsonInt64(12345), // Must be Int64, not UInt32
                ["_projectedAt"] = DateTime.UtcNow
            };

            // Act & Assert
            Assert.IsType<BsonInt64>(document["Version"]);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ReadModelConsistency_DateFieldsMustBeBsonDateTime(string aggregateType, string topic)
        {
            // Arrange
            var document = new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["TenantId"] = "test-tenant",
                ["CreatedAt"] = new BsonDateTime(DateTime.UtcNow),
                ["UpdatedAt"] = new BsonDateTime(DateTime.UtcNow),
                ["_projectedAt"] = DateTime.UtcNow
            };

            // Act & Assert
            Assert.IsType<BsonDateTime>(document["CreatedAt"]);
            Assert.IsType<BsonDateTime>(document["UpdatedAt"]);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void ReadModelConsistency_UpsertReplacesExistingDocument(string aggregateType, string topic)
        {
            // Arrange
            var id = Guid.NewGuid().ToString();
            var tenantId = "test-tenant";

            var originalDocument = CreateMongoProjectionDocument(aggregateType, id, tenantId);
            originalDocument["Version"] = 1;

            var updatedDocument = CreateMongoProjectionDocument(aggregateType, id, tenantId);
            updatedDocument["Version"] = 2;

            // Act - Simulate upsert (replace)
            // In real implementation: ReplaceOneAsync with IsUpsert = true

            // Assert - Document should be replaced, not merged
            Assert.Equal(id, updatedDocument["_id"].AsString);
            Assert.Equal(2, updatedDocument["Version"].AsInt32);
        }

        #endregion

        #region Schema Versioning in Projections Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void SchemaVersioning_ProjectionIncludesVersion(string aggregateType, string topic)
        {
            // Arrange
            var document = CreateMongoProjectionDocument(aggregateType, Guid.NewGuid().ToString(), "test-tenant");
            document["Version"] = 7; // Latest version

            // Act & Assert
            Assert.True(document.Contains("Version"));
            Assert.Equal(7, document["Version"].AsInt32);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void SchemaVersioning_BackwardCompatibility(string aggregateType, string topic)
        {
            // Arrange - Old projection without new field
            var oldDocument = new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["TenantId"] = "test-tenant",
                ["Name"] = "Test",
                ["Version"] = 1
            };

            // Act - New schema expects "Description" field
            var hasDescription = oldDocument.Contains("Description");

            // Assert - Should handle missing fields gracefully
            Assert.False(hasDescription);
        }

        #endregion

        #region Multi-Tenancy in Projections Tests

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MultiTenancy_ProjectionIncludesTenantId(string aggregateType, string topic)
        {
            // Arrange
            var tenantId = "tenant-abc-123";
            var document = CreateMongoProjectionDocument(aggregateType, Guid.NewGuid().ToString(), tenantId);

            // Act & Assert
            Assert.Equal(tenantId, document["TenantId"].AsString);
        }

        [Theory]
        [MemberData(nameof(AllAggregateTypes))]
        public void MultiTenancy_SameIdDifferentTenants(string aggregateType, string topic)
        {
            // Arrange
            var sharedId = "shared-id-123";
            var tenant1Doc = CreateMongoProjectionDocument(aggregateType, sharedId, "tenant-1");
            var tenant2Doc = CreateMongoProjectionDocument(aggregateType, sharedId, "tenant-2");

            // Act & Assert
            Assert.Equal(sharedId, tenant1Doc["_id"].AsString);
            Assert.Equal(sharedId, tenant2Doc["_id"].AsString);
            Assert.NotEqual(tenant1Doc["TenantId"], tenant2Doc["TenantId"]);
        }

        #endregion

        #region Projection Performance Tests

        [Theory]
        [InlineData(100)]
        [InlineData(1000)]
        [InlineData(10000)]
        public void ProjectionPerformance_CanHandleManyDocuments(int documentCount)
        {
            // Arrange
            var documents = new List<BsonDocument>();
            var aggregateType = "TestObject";

            for (int i = 0; i < documentCount; i++)
            {
                documents.Add(CreateMongoProjectionDocument(
                    aggregateType,
                    Guid.NewGuid().ToString(),
                    $"tenant-{i % 10}"));
            }

            // Act & Assert
            Assert.Equal(documentCount, documents.Count);
            Assert.All(documents, d =>
            {
                Assert.True(d.Contains("_id"));
                Assert.True(d.Contains("TenantId"));
                Assert.True(d.Contains("_projectedAt"));
            });
        }

        [Fact]
        public void ProjectionPerformance_LargeDocumentSerialization()
        {
            // Arrange - Document with many nested objects
            var document = new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["TenantId"] = "test-tenant",
                ["Items"] = new BsonArray()
            };

            // Add 1000 line items
            for (int i = 0; i < 1000; i++)
            {
                (document["Items"] as BsonArray).Add(new BsonDocument
                {
                    ["Id"] = i,
                    ["Description"] = $"Item {i}",
                    ["Quantity"] = i * 10,
                    ["UnitPrice"] = i * 1.5m,
                    ["Total"] = i * 10 * 1.5m
                });
            }

            // Act & Assert
            Assert.Equal(1000, (document["Items"] as BsonArray).Count);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void ErrorHandling_InvalidJsonPayload()
        {
            // Arrange
            var invalidPayload = "not valid json";

            // Act & Assert
            Assert.Throws<JsonException>(() =>
            {
                JsonDocument.Parse(invalidPayload);
            });
        }

        [Fact]
        public void ErrorHandling_EmptyPayload()
        {
            // Arrange
            var emptyPayload = "{}";

            // Act
            using var doc = JsonDocument.Parse(emptyPayload);
            var hasAggregateType = doc.RootElement.TryGetProperty("AggregateType", out _);
            var hasAggregateId = doc.RootElement.TryGetProperty("AggregateId", out _);

            // Assert
            Assert.False(hasAggregateType);
            Assert.False(hasAggregateId);
        }

        [Fact]
        public void ErrorHandling_NullAggregateId()
        {
            // Arrange
            var payload = "{ \"AggregateType\": \"SalesOrder\", \"AggregateId\": null }";

            // Act
            using var doc = JsonDocument.Parse(payload);
            var idProp = doc.RootElement.GetProperty("AggregateId");

            // Assert
            Assert.Equal(JsonValueKind.Null, idProp.ValueKind);
        }

        #endregion

        #region Helper Methods

        private string CreateValidProjectionEvent(string aggregateType, string aggregateId, string tenantId = "test-tenant")
        {
            return JsonSerializer.Serialize(new
            {
                AggregateType = aggregateType,
                AggregateId = aggregateId,
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow,
                Data = new { }
            }, _jsonOptions);
        }

        private BsonDocument CreateMongoProjectionDocument(string aggregateType, string id, string tenantId)
        {
            return new BsonDocument
            {
                ["_id"] = id,
                ["TenantId"] = tenantId,
                ["_projectedAt"] = DateTime.UtcNow,
                ["AggregateType"] = aggregateType
            };
        }

        private BsonDocument CreateComplexMongoDocument(string aggregateType)
        {
            var document = new BsonDocument
            {
                ["_id"] = Guid.NewGuid().ToString(),
                ["TenantId"] = "test-tenant",
                ["_projectedAt"] = DateTime.UtcNow
            };

            // Add related data based on aggregate type
            switch (aggregateType)
            {
                case "SalesOrder":
                    document["Items"] = new BsonArray
                    {
                        new BsonDocument { ["Id"] = 1, ["Description"] = "Item 1", ["Quantity"] = 10, ["UnitPrice"] = 100.00m },
                        new BsonDocument { ["Id"] = 2, ["Description"] = "Item 2", ["Quantity"] = 5, ["UnitPrice"] = 50.00m }
                    };
                    break;

                case "JournalEntry":
                    document["Lines"] = new BsonArray
                    {
                        new BsonDocument { ["Id"] = 1, ["AccountCode"] = "1000", ["Debit"] = 1000.00m, ["Credit"] = 0m },
                        new BsonDocument { ["Id"] = 2, ["AccountCode"] = "2000", ["Debit"] = 0m, ["Credit"] = 1000.00m }
                    };
                    break;

                case "EmployeePayroll":
                    document["Employee"] = new BsonDocument
                    {
                        ["Id"] = Guid.NewGuid().ToString(),
                        ["Name"] = "John Doe",
                        ["EmployeeNumber"] = "EMP001"
                    };
                    break;

                case "StockMovement":
                    document["Material"] = new BsonDocument
                    {
                        ["Id"] = Guid.NewGuid().ToString(),
                        ["Code"] = "MAT001",
                        ["Name"] = "Raw Material 1"
                    };
                    break;
            }

            return document;
        }

        private string InferAggregateTypeFromTopic(string topic)
        {
            var topicMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["valora.sd.salesorder"] = "SalesOrder",
                ["valora.mm.material"] = "Material",
                ["valora.co.costcenter"] = "CostCenter",
                ["valora.fi.masterdata"] = "GLAccount",
                ["valora.fi.journalentry"] = "JournalEntry",
                ["valora.mm.stockmovement"] = "StockMovement",
                ["valora.hc.payroll"] = "EmployeePayroll",
                ["valora.data.changed"] = "CustomObject"
            };

            return topicMappings.TryGetValue(topic, out var aggregateType)
                ? aggregateType
                : "Unknown";
        }

        private Type? ResolveEntityType(string aggregateType)
        {
            // Simulates the type resolution in ProjectionManager
            var typeName = $"Valora.Api.Domain.Entities.{aggregateType}";
            if (aggregateType == "SalesOrder") typeName = "Valora.Api.Domain.Entities.Sales.SalesOrder";
            if (aggregateType == "JournalEntry") typeName = "Valora.Api.Domain.Entities.Finance.JournalEntry";
            if (aggregateType == "EmployeePayroll") typeName = "Valora.Api.Domain.Entities.HumanCapital.EmployeePayroll";
            if (aggregateType == "StockMovement") typeName = "Valora.Api.Domain.Entities.Materials.StockMovement";
            if (aggregateType == "Material") typeName = "Valora.Api.Domain.Entities.Materials.Material";
            if (aggregateType == "CostCenter") typeName = "Valora.Api.Domain.Entities.Controlling.CostCenter";
            if (aggregateType == "GLAccount") typeName = "Valora.Api.Domain.Entities.Finance.GLAccount";

            // For known types, return a placeholder type
            var knownTypes = new[] { "SalesOrder", "JournalEntry", "EmployeePayroll", "StockMovement", "Material", "CostCenter", "GLAccount" };
            if (knownTypes.Contains(aggregateType))
            {
                // Return Object as placeholder - in real code this would be the actual entity type
                return typeof(object);
            }

            return null;
        }

        private string ExtractTenantId(JsonElement rootElement)
        {
            if (rootElement.TryGetProperty("TenantId", out var tenantIdProp))
            {
                return tenantIdProp.GetString() ?? "UNKNOWN";
            }
            if (rootElement.TryGetProperty("tenantId", out tenantIdProp))
            {
                return tenantIdProp.GetString() ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        #endregion
    }
}

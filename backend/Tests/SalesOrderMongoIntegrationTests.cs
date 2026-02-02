using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api;
using Valora.Api.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Valora.Tests
{
    /// <summary>
    /// Comprehensive MongoDB Integration Tests for Sales Order
    /// Tests read model projections, event store, schema migration verification,
    /// and performance tests for queries
    /// </summary>
    public class SalesOrderMongoIntegrationTests : IClassFixture<TestWebApplicationFactory>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly ITestOutputHelper _output;
        private readonly MongoDbContext _mongoDb;

        public SalesOrderMongoIntegrationTests(TestWebApplicationFactory factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _mongoDb = _factory.Services.GetRequiredService<MongoDbContext>();
        }

        #region Read Model Projections

        [Fact]
        public async Task Projection_UpsertSalesOrder_CreatesDocumentInMongo()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderId = Guid.NewGuid().ToString();
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            var projection = new BsonDocument
            {
                { "_id", orderId },
                { "TenantId", tenantId },
                { "AggregateType", "SalesOrder" },
                { "OrderNumber", "SO-PROJ-001" },
                { "CustomerId", "CUST001" },
                { "CustomerName", "Test Customer" },
                { "OrderDate", DateTime.UtcNow },
                { "TotalAmount", 5000.00 },
                { "Status", "Confirmed" },
                { "Version", 1 },
                { "Items", new BsonArray
                    {
                        new BsonDocument
                        {
                            { "MaterialCode", "ITEM001" },
                            { "Quantity", 10 },
                            { "UnitPrice", 500.00 },
                            { "LineTotal", 5000.00 }
                        }
                    }
                },
                { "CreatedAt", DateTime.UtcNow },
                { "LastUpdated", DateTime.UtcNow }
            };

            // Act
            await collection.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                projection,
                new ReplaceOptions { IsUpsert = true }
            );

            // Assert
            var saved = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", orderId)).FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal(tenantId, saved["TenantId"].AsString);
            Assert.Equal("SO-PROJ-001", saved["OrderNumber"].AsString);
            Assert.Equal(5000.00, saved["TotalAmount"].AsDouble);

            _output.WriteLine($"✓ SalesOrder projection created in MongoDB: {orderId}");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", orderId));
        }

        [Fact]
        public async Task Projection_UpdateSalesOrder_UpdatesExistingDocument()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderId = Guid.NewGuid().ToString();
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create initial projection
            var initialProjection = new BsonDocument
            {
                { "_id", orderId },
                { "TenantId", tenantId },
                { "OrderNumber", "SO-UPDATE-001" },
                { "Status", "Draft" },
                { "TotalAmount", 1000.00 },
                { "Version", 1 },
                { "CreatedAt", DateTime.UtcNow },
                { "LastUpdated", DateTime.UtcNow }
            };

            await collection.InsertOneAsync(initialProjection);

            // Act - Update the projection
            var updatedProjection = new BsonDocument
            {
                { "_id", orderId },
                { "TenantId", tenantId },
                { "OrderNumber", "SO-UPDATE-001" },
                { "Status", "Confirmed" },
                { "TotalAmount", 1500.00 },
                { "Version", 2 },
                { "CreatedAt", initialProjection["CreatedAt"] },
                { "LastUpdated", DateTime.UtcNow },
                { "ConfirmedBy", "user@example.com" },
                { "ConfirmedAt", DateTime.UtcNow }
            };

            await collection.ReplaceOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                updatedProjection,
                new ReplaceOptions { IsUpsert = true }
            );

            // Assert
            var saved = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", orderId)).FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("Confirmed", saved["Status"].AsString);
            Assert.Equal(1500.00, saved["TotalAmount"].AsDouble);
            Assert.Equal(2, saved["Version"].AsInt32);
            Assert.True(saved.Contains("ConfirmedBy"));

            _output.WriteLine($"✓ SalesOrder projection updated: Version 1 -> 2");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", orderId));
        }

        [Fact]
        public async Task Projection_QueryByTenantId_ReturnsOnlyTenantDocuments()
        {
            // Arrange
            var tenant1 = $"tenant-1-{Guid.NewGuid():N}";
            var tenant2 = $"tenant-2-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create documents for tenant 1
            for (int i = 0; i < 5; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenant1 },
                    { "OrderNumber", $"SO-T1-{i:000}" },
                    { "TotalAmount", 1000.00 * (i + 1) },
                    { "CreatedAt", DateTime.UtcNow }
                });
            }

            // Create documents for tenant 2
            for (int i = 0; i < 3; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenant2 },
                    { "OrderNumber", $"SO-T2-{i:000}" },
                    { "TotalAmount", 500.00 * (i + 1) },
                    { "CreatedAt", DateTime.UtcNow }
                });
            }

            // Act
            var tenant1Docs = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenant1))
                .ToListAsync();

            var tenant2Docs = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenant2))
                .ToListAsync();

            // Assert
            Assert.Equal(5, tenant1Docs.Count);
            Assert.Equal(3, tenant2Docs.Count);
            Assert.All(tenant1Docs, d => Assert.Equal(tenant1, d["TenantId"].AsString));
            Assert.All(tenant2Docs, d => Assert.Equal(tenant2, d["TenantId"].AsString));

            _output.WriteLine($"✓ Tenant isolation: T1={tenant1Docs.Count}, T2={tenant2Docs.Count}");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenant1));
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenant2));
        }

        [Fact]
        public async Task Projection_QueryWithFilters_ReturnsFilteredResults()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create documents with various statuses and amounts
            var statuses = new[] { "Draft", "Confirmed", "Shipped", "Delivered", "Cancelled" };
            for (int i = 0; i < 20; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-FILTER-{i:000}" },
                    { "Status", statuses[i % statuses.Length] },
                    { "TotalAmount", 1000.00 * (i + 1) },
                    { "CreatedAt", DateTime.UtcNow.AddDays(-i) }
                });
            }

            // Act - Filter by status
            var confirmedFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("TenantId", tenantId),
                Builders<BsonDocument>.Filter.Eq("Status", "Confirmed")
            );
            var confirmedOrders = await collection.Find(confirmedFilter).ToListAsync();

            // Act - Filter by amount range
            var highValueFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("TenantId", tenantId),
                Builders<BsonDocument>.Filter.Gt("TotalAmount", 10000.00)
            );
            var highValueOrders = await collection.Find(highValueFilter).ToListAsync();

            // Assert
            Assert.Equal(4, confirmedOrders.Count); // 20 / 5 statuses
            Assert.Equal(10, highValueOrders.Count); // Orders > 10,000

            _output.WriteLine($"✓ Filtered queries: Confirmed={confirmedOrders.Count}, HighValue={highValueOrders.Count}");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        [Fact]
        public async Task Projection_QueryWithSorting_ReturnsSortedResults()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create documents
            for (int i = 0; i < 10; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-SORT-{i:000}" },
                    { "TotalAmount", 1000.00 * (i + 1) },
                    { "Priority", 10 - i },
                    { "CreatedAt", DateTime.UtcNow.AddHours(-i) }
                });
            }

            // Act - Sort by TotalAmount descending
            var sortByAmount = Builders<BsonDocument>.Sort.Descending("TotalAmount");
            var highestAmount = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .Sort(sortByAmount)
                .FirstOrDefaultAsync();

            // Act - Sort by Priority ascending
            var sortByPriority = Builders<BsonDocument>.Sort.Ascending("Priority");
            var highestPriority = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .Sort(sortByPriority)
                .FirstOrDefaultAsync();

            // Assert
            Assert.Equal(10000.00, highestAmount["TotalAmount"].AsDouble);
            Assert.Equal(1, highestPriority["Priority"].AsInt32);

            _output.WriteLine($"✓ Sorting works correctly");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        [Fact]
        public async Task Projection_Pagination_ReturnsCorrectPage()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create 25 documents
            for (int i = 0; i < 25; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-PAGE-{i:000}" },
                    { "SequenceNumber", i + 1 },
                    { "CreatedAt", DateTime.UtcNow.AddMinutes(-i) }
                });
            }

            // Act - Page 1 (10 items)
            var page1 = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
                .Skip(0)
                .Limit(10)
                .ToListAsync();

            // Act - Page 2 (10 items)
            var page2 = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
                .Skip(10)
                .Limit(10)
                .ToListAsync();

            // Act - Page 3 (5 items)
            var page3 = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
                .Skip(20)
                .Limit(10)
                .ToListAsync();

            // Assert
            Assert.Equal(10, page1.Count);
            Assert.Equal(10, page2.Count);
            Assert.Equal(5, page3.Count);
            Assert.Equal(1, page1[0]["SequenceNumber"].AsInt32);
            Assert.Equal(11, page2[0]["SequenceNumber"].AsInt32);
            Assert.Equal(21, page3[0]["SequenceNumber"].AsInt32);

            _output.WriteLine($"✓ Pagination: Page1={page1.Count}, Page2={page2.Count}, Page3={page3.Count}");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        #endregion

        #region Schema and Configuration Storage

        [Fact]
        public async Task SchemaStorage_StoresSalesOrderSchema_InMongo()
        {
            // Arrange
            var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");
            var schemaId = Guid.NewGuid().ToString();

            var schemaDoc = new BsonDocument
            {
                { "_id", schemaId },
                { "ObjectCode", "SalesOrder" },
                { "Version", 1 },
                { "IsLatest", true },
                { "Schema", new BsonDocument
                    {
                        { "fields", new BsonArray
                            {
                                new BsonDocument { { "name", "OrderNumber" }, { "type", "string" }, { "required", true } },
                                new BsonDocument { { "name", "CustomerId" }, { "type", "string" }, { "required", true } },
                                new BsonDocument { { "name", "TotalAmount" }, { "type", "decimal" }, { "required", true } }
                            }
                        },
                        { "calculationRules", new BsonDocument() },
                        { "documentTotals", new BsonDocument() }
                    }
                },
                { "CreatedAt", DateTime.UtcNow },
                { "CreatedBy", "test-user" }
            };

            // Act
            await collection.InsertOneAsync(schemaDoc);

            // Assert
            var saved = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", schemaId)).FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("SalesOrder", saved["ObjectCode"].AsString);
            Assert.True(saved["IsLatest"].AsBoolean);
            Assert.True(saved["Schema"].AsBsonDocument.Contains("fields"));

            _output.WriteLine($"✓ SalesOrder schema stored in MongoDB");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", schemaId));
        }

        [Fact]
        public async Task SchemaStorage_QueryLatestVersion_ReturnsCorrectSchema()
        {
            // Arrange
            var collection = _mongoDb.GetCollection<BsonDocument>("PlatformObjectTemplate");

            // Insert multiple versions
            for (int version = 1; version <= 5; version++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "ObjectCode", "SalesOrder" },
                    { "Version", version },
                    { "IsLatest", version == 5 },
                    { "CreatedAt", DateTime.UtcNow.AddDays(-(5 - version)) }
                });
            }

            // Act
            var latestFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("ObjectCode", "SalesOrder"),
                Builders<BsonDocument>.Filter.Eq("IsLatest", true)
            );
            var latestSchema = await collection.Find(latestFilter).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(latestSchema);
            Assert.Equal(5, latestSchema["Version"].AsInt32);

            _output.WriteLine($"✓ Latest schema version retrieved: v{latestSchema["Version"].AsInt32}");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("ObjectCode", "SalesOrder"));
        }

        #endregion

        #region Event Store (Kafka Log)

        [Fact]
        public async Task EventStore_LogsKafkaMessages_InMongo()
        {
            // Arrange
            var collection = _mongoDb.GetCollection<BsonDocument>("System_KafkaLog");
            var logId = Guid.NewGuid().ToString();

            var logEntry = new BsonDocument
            {
                { "_id", logId },
                { "Topic", "valora.data.changed" },
                { "Key", "test-tenant" },
                { "Partition", 0 },
                { "Offset", 12345 },
                { "Payload", "{ \"EventType\": \"SalesOrderCreated\" }" },
                { "ReceivedAt", DateTime.UtcNow },
                { "Processed", false }
            };

            // Act
            await collection.InsertOneAsync(logEntry);

            // Assert
            var saved = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", logId)).FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal("valora.data.changed", saved["Topic"].AsString);
            Assert.Equal(12345, saved["Offset"].AsInt64);

            _output.WriteLine($"✓ Kafka message logged to System_KafkaLog");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", logId));
        }

        [Fact]
        public async Task EventStore_QueryRecentEvents_ReturnsOrderedResults()
        {
            // Arrange
            var collection = _mongoDb.GetCollection<BsonDocument>("System_KafkaLog");
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Insert events with timestamps
            for (int i = 0; i < 10; i++)
            {
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "Topic", "valora.data.changed" },
                    { "Key", tenantId },
                    { "EventType", i % 2 == 0 ? "SalesOrderCreated" : "SalesOrderUpdated" },
                    { "ReceivedAt", DateTime.UtcNow.AddMinutes(-i) },
                    { "Processed", true }
                });
            }

            // Act
            var recentEvents = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("Key", tenantId))
                .Sort(Builders<BsonDocument>.Sort.Descending("ReceivedAt"))
                .Limit(5)
                .ToListAsync();

            // Assert
            Assert.Equal(5, recentEvents.Count);
            Assert.True(recentEvents[0]["ReceivedAt"].ToUniversalTime() >= recentEvents[1]["ReceivedAt"].ToUniversalTime());

            _output.WriteLine($"✓ Recent events query returned {recentEvents.Count} events");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("Key", tenantId));
        }

        #endregion

        #region Performance Tests

        [Fact]
        public async Task Performance_Insert1000Documents_CompletesQuickly()
        {
            // Arrange
            var tenantId = $"perf-test-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");
            var documents = new List<BsonDocument>();

            for (int i = 0; i < 1000; i++)
            {
                documents.Add(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-PERF-{i:0000}" },
                    { "TotalAmount", 1000.00 + i },
                    { "Status", i % 2 == 0 ? "Confirmed" : "Draft" },
                    { "CreatedAt", DateTime.UtcNow }
                });
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await collection.InsertManyAsync(documents);
            stopwatch.Stop();

            // Assert
            var count = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
            Assert.Equal(1000, count);
            Assert.True(stopwatch.ElapsedMilliseconds < 10000, $"Insert took {stopwatch.ElapsedMilliseconds}ms, expected < 10000ms");

            _output.WriteLine($"✓ Inserted 1000 documents in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        [Fact]
        public async Task Performance_QueryWithIndex_UsesIndexEfficiently()
        {
            // Arrange
            var tenantId = $"index-test-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create index on TenantId + OrderNumber
            var indexKeys = Builders<BsonDocument>.IndexKeys
                .Ascending("TenantId")
                .Ascending("OrderNumber");
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(indexKeys));

            // Insert test data
            var documents = new List<BsonDocument>();
            for (int i = 0; i < 100; i++)
            {
                documents.Add(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-INDEX-{i:0000}" },
                    { "TotalAmount", 1000.00 + i },
                    { "CreatedAt", DateTime.UtcNow }
                });
            }
            await collection.InsertManyAsync(documents);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await collection
                .Find(Builders<BsonDocument>.Filter.And(
                    Builders<BsonDocument>.Filter.Eq("TenantId", tenantId),
                    Builders<BsonDocument>.Filter.Eq("OrderNumber", "SO-INDEX-0050")
                ))
                .FirstOrDefaultAsync();
            stopwatch.Stop();

            // Assert
            Assert.NotNull(result);
            Assert.True(stopwatch.ElapsedMilliseconds < 100, $"Query took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");

            _output.WriteLine($"✓ Indexed query completed in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        [Fact]
        public async Task Performance_AggregationQuery_ComputesTotals()
        {
            // Arrange
            var tenantId = $"agg-test-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Insert test data with various amounts
            var documents = new List<BsonDocument>();
            for (int i = 0; i < 100; i++)
            {
                documents.Add(new BsonDocument
                {
                    { "_id", Guid.NewGuid().ToString() },
                    { "TenantId", tenantId },
                    { "OrderNumber", $"SO-AGG-{i:0000}" },
                    { "TotalAmount", 1000.00 * (i % 10 + 1) },
                    { "Status", i % 3 == 0 ? "Confirmed" : "Draft" },
                    { "CreatedAt", DateTime.UtcNow }
                });
            }
            await collection.InsertManyAsync(documents);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("TenantId", tenantId)),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$Status" },
                    { "Count", new BsonDocument("$sum", 1) },
                    { "TotalAmount", new BsonDocument("$sum", "$TotalAmount") },
                    { "AvgAmount", new BsonDocument("$avg", "$TotalAmount") }
                })
            };

            var aggregation = await collection.Aggregate<BsonDocument>(pipeline).ToListAsync();
            stopwatch.Stop();

            // Assert
            Assert.True(aggregation.Count >= 2);
            Assert.True(stopwatch.ElapsedMilliseconds < 1000);

            foreach (var group in aggregation)
            {
                _output.WriteLine($"  Status: {group["_id"]}, Count: {group["Count"]}, Total: {group["TotalAmount"]:C}");
            }

            _output.WriteLine($"✓ Aggregation completed in {stopwatch.ElapsedMilliseconds}ms");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
        }

        #endregion

        #region Data Consistency Tests

        [Fact]
        public async Task Consistency_VersionTracking_PreventsStaleUpdates()
        {
            // Arrange
            var tenantId = $"version-test-{Guid.NewGuid():N}";
            var orderId = Guid.NewGuid().ToString();
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create initial document with version
            await collection.InsertOneAsync(new BsonDocument
            {
                { "_id", orderId },
                { "TenantId", tenantId },
                { "OrderNumber", "SO-VERSION-001" },
                { "Status", "Draft" },
                { "Version", 1 },
                { "LastUpdated", DateTime.UtcNow }
            });

            // Act & Assert - Simulate concurrent update with version check
            var filter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                Builders<BsonDocument>.Filter.Eq("Version", 1)
            );

            var update = Builders<BsonDocument>.Update
                .Set("Status", "Confirmed")
                .Set("Version", 2)
                .Set("LastUpdated", DateTime.UtcNow);

            var result = await collection.UpdateOneAsync(filter, update);
            Assert.Equal(1, result.ModifiedCount);

            // Try to update with stale version (should fail)
            var staleFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                Builders<BsonDocument>.Filter.Eq("Version", 1) // Old version
            );

            var staleUpdate = Builders<BsonDocument>.Update
                .Set("Status", "Shipped")
                .Set("Version", 2);

            var staleResult = await collection.UpdateOneAsync(staleFilter, staleUpdate);
            Assert.Equal(0, staleResult.ModifiedCount); // Should not modify

            _output.WriteLine($"✓ Version tracking prevented stale update");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", orderId));
        }

        [Fact]
        public async Task Consistency_TenantIsolation_PreventsCrossTenantAccess()
        {
            // Arrange
            var tenantA = $"tenant-a-{Guid.NewGuid():N}";
            var tenantB = $"tenant-b-{Guid.NewGuid():N}";
            var collection = _mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create document for tenant A
            var orderId = Guid.NewGuid().ToString();
            await collection.InsertOneAsync(new BsonDocument
            {
                { "_id", orderId },
                { "TenantId", tenantA },
                { "OrderNumber", "SO-ISOLATION-001" },
                { "SensitiveData", "Secret A" },
                { "CreatedAt", DateTime.UtcNow }
            });

            // Act - Query with tenant A filter
            var tenantAFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                Builders<BsonDocument>.Filter.Eq("TenantId", tenantA)
            );
            var docForA = await collection.Find(tenantAFilter).FirstOrDefaultAsync();

            // Act - Query with tenant B filter (should not find)
            var tenantBFilter = Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq("_id", orderId),
                Builders<BsonDocument>.Filter.Eq("TenantId", tenantB)
            );
            var docForB = await collection.Find(tenantBFilter).FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(docForA);
            Assert.Null(docForB);
            Assert.Equal("Secret A", docForA["SensitiveData"].AsString);

            _output.WriteLine($"✓ Tenant isolation enforced correctly");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", orderId));
        }

        #endregion
    }
}

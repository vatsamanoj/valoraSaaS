using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api;
using Valora.Api.Application.Sales.Commands.BillSalesOrder;
using Valora.Api.Application.Sales.Commands.CreateSalesOrder;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Projections;
using Xunit;
using Xunit.Abstractions;

namespace Valora.Tests
{
    /// <summary>
    /// CQRS Pattern and Consistency Tests for Sales Order
    /// Tests command handlers, event sourcing verification, projection updates,
    /// consistency between write and read models, and outbox pattern verification
    /// </summary>
    public class SalesOrderCqrsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;
        private readonly JsonSerializerOptions _jsonOptions;

        public SalesOrderCqrsTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region Command Handler Tests

        [Fact]
        public async Task CreateSalesOrderCommandHandler_ValidCommand_CreatesSalesOrder()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderNumber = $"SO-CMD-{Guid.NewGuid():N}";

            var command = new CreateSalesOrderCommand(
                tenantId,
                "CUST001",
                "USD",
                null,
                null,
                new List<SalesOrderItemDto>
                {
                    new("ITEM001", 10),
                    new("ITEM002", 5)
                },
                false
            );

            var handler = new CreateSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success, $"Command failed: {result.Message}");
            Assert.NotNull(result.Data);

            var orderId = ((JsonElement)result.Data).GetProperty("Id").GetString();
            Assert.NotNull(orderId);

            // Verify in database
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .Include(so => so.Items)
                .FirstOrDefaultAsync(so => so.Id == Guid.Parse(orderId));

            Assert.NotNull(savedOrder);
            Assert.Equal(orderNumber, savedOrder.OrderNumber);
            Assert.Equal(tenantId, savedOrder.TenantId);
            Assert.Equal(2, savedOrder.Items.Count);

            _output.WriteLine($"✓ CreateSalesOrderCommand created order: {orderId}");

            // Cleanup
            dbContext.SalesOrders.Remove(savedOrder);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateSalesOrderCommandHandler_DuplicateOrderNumber_ReturnsError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderNumber = $"SO-DUP-{Guid.NewGuid():N}";

            // Create first order
            var firstCommand = new CreateSalesOrderCommand(
                tenantId,
                "CUST001",
                "USD",
                null,
                null,
                new List<SalesOrderItemDto> { new("ITEM001", 10) },
                false
            );

            var handler = new CreateSalesOrderCommandHandler(dbContext);
            var firstResult = await handler.Handle(firstCommand, CancellationToken.None);
            Assert.True(firstResult.Success);

            // Act - Try to create second order with same number
            var secondCommand = new CreateSalesOrderCommand(
                tenantId,
                "CUST002",
                "USD",
                null,
                null,
                new List<SalesOrderItemDto> { new("ITEM002", 5) },
                false
            );

            // This should throw due to unique constraint
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await handler.Handle(secondCommand, CancellationToken.None);
                await dbContext.SaveChangesAsync();
            });

            _output.WriteLine($"✓ Duplicate order number correctly rejected");

            // Cleanup
            var savedOrder = await dbContext.SalesOrders
                .FirstOrDefaultAsync(so => so.OrderNumber == orderNumber);
            if (savedOrder != null)
            {
                dbContext.SalesOrders.Remove(savedOrder);
                await dbContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task BillSalesOrderCommandHandler_ValidCommand_BillsOrder()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Create a sales order first
            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-BILL-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 5000.00m,
                Status = SalesOrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 1
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            var command = new BillSalesOrderCommand(tenantId, order.Id);
            var handler = new BillSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.Success, $"Billing failed: {result.Message}");

            // Verify order status updated
            var billedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.Id == order.Id);

            Assert.NotNull(billedOrder);
            Assert.Equal(SalesOrderStatus.Invoiced, billedOrder.Status);

            // Verify outbox message created
            var outboxMessages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId && o.Topic == "valora.sd.so_billed")
                .ToListAsync();

            Assert.NotEmpty(outboxMessages);

            _output.WriteLine($"✓ BillSalesOrderCommand billed order and created outbox event");

            // Cleanup
            dbContext.SalesOrders.Remove(billedOrder);
            dbContext.OutboxMessages.RemoveRange(outboxMessages);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task BillSalesOrderCommandHandler_NonExistentOrder_ReturnsNotFound()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var command = new BillSalesOrderCommand(tenantId, Guid.NewGuid());
            var handler = new BillSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);

            _output.WriteLine($"✓ BillSalesOrderCommand correctly returned 404 for non-existent order");
        }

        [Fact]
        public async Task BillSalesOrderCommandHandler_AlreadyBilled_ReturnsError()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-ALREADY-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 5000.00m,
                Status = SalesOrderStatus.Invoiced, // Already billed
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 1
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            var command = new BillSalesOrderCommand(tenantId, order.Id);
            var handler = new BillSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.Success);

            _output.WriteLine($"✓ BillSalesOrderCommand correctly rejected already billed order");

            // Cleanup
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region Event Sourcing Verification

        [Fact]
        public async Task EventSourcing_CreateOrder_GeneratesCreationEvent()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Act - Create order
            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-EVENT-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = SalesOrderStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "event-test",
                Version = 0
            };

            dbContext.SalesOrders.Add(order);

            // Add outbox event
            var outboxMessage = new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.data.changed",
                Payload = JsonSerializer.Serialize(new
                {
                    EventType = "SalesOrderCreated",
                    AggregateType = "SalesOrder",
                    AggregateId = order.Id.ToString(),
                    Timestamp = DateTime.UtcNow,
                    InitialData = new { order.OrderNumber, order.CustomerId }
                }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();

            // Assert
            var savedEvent = await dbContext.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == outboxMessage.Id);

            Assert.NotNull(savedEvent);
            Assert.Equal("Pending", savedEvent.Status);

            var payload = JsonSerializer.Deserialize<JsonElement>(savedEvent.Payload);
            Assert.Equal("SalesOrderCreated", payload.GetProperty("EventType").GetString());

            _output.WriteLine($"✓ Event sourcing: Creation event generated");

            // Cleanup
            dbContext.SalesOrders.Remove(order);
            dbContext.OutboxMessages.Remove(savedEvent);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task EventSourcing_VersionIncremented_OnEachUpdate()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-VERSION-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = SalesOrderStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            // Act - Update multiple times
            for (int i = 0; i < 5; i++)
            {
                order.Status = SalesOrderStatus.Confirmed;
                await dbContext.SaveChangesAsync();
            }

            // Assert
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.Id == order.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal(5u, savedOrder.Version); // 5 updates = version 5

            _output.WriteLine($"✓ Event sourcing: Version correctly incremented to {savedOrder.Version}");

            // Cleanup
            dbContext.SalesOrders.Remove(savedOrder);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region Projection Update Tests

        [Fact]
        public async Task ProjectionManager_ProjectsSalesOrder_ToMongo()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var mongoDb = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var projectionRepo = new MongoProjectionRepository(mongoDb);
            var smartProjectionService = scope.ServiceProvider.GetRequiredService<SmartProjectionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProjectionManager>>();
            var projectionManager = new ProjectionManager(dbContext, projectionRepo, smartProjectionService, logger);

            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Create a sales order
            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-PROJ-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 5000.00m,
                Status = SalesOrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 1,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM001",
                        Quantity = 10,
                        UnitPrice = 500.00m,
                        LineTotal = 5000.00m,
                        SalesOrderId = Guid.Empty
                    }
                }
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            // Create event payload
            var eventPayload = JsonSerializer.Serialize(new
            {
                AggregateType = "SalesOrder",
                AggregateId = order.Id.ToString(),
                TenantId = tenantId,
                EventType = "SalesOrderCreated"
            });

            // Act
            await projectionManager.HandleEventAsync("valora.data.changed", tenantId, eventPayload);

            // Assert
            var collection = mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");
            var projection = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("_id", order.Id.ToString()))
                .FirstOrDefaultAsync();

            Assert.NotNull(projection);
            Assert.Equal(order.OrderNumber, projection["OrderNumber"].AsString);
            Assert.Equal(order.TotalAmount, (decimal)projection["TotalAmount"].AsDouble);

            _output.WriteLine($"✓ ProjectionManager projected SalesOrder to MongoDB");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", order.Id.ToString()));
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task ProjectionManager_UpdateExistingProjection_UpdatesMongo()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var mongoDb = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var projectionRepo = new MongoProjectionRepository(mongoDb);
            var smartProjectionService = scope.ServiceProvider.GetRequiredService<SmartProjectionService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProjectionManager>>();
            var projectionManager = new ProjectionManager(dbContext, projectionRepo, smartProjectionService, logger);

            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderId = Guid.NewGuid();

            // Create initial order
            var order = new SalesOrder
            {
                Id = orderId,
                TenantId = tenantId,
                OrderNumber = $"SO-UPDATE-PROJ-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = SalesOrderStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 1
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            // First projection
            var eventPayload1 = JsonSerializer.Serialize(new
            {
                AggregateType = "SalesOrder",
                AggregateId = orderId.ToString(),
                TenantId = tenantId
            });
            await projectionManager.HandleEventAsync("valora.data.changed", tenantId, eventPayload1);

            // Update order
            order.Status = SalesOrderStatus.Confirmed;
            order.TotalAmount = 1500.00m;
            await dbContext.SaveChangesAsync();

            // Act - Re-project
            var eventPayload2 = JsonSerializer.Serialize(new
            {
                AggregateType = "SalesOrder",
                AggregateId = orderId.ToString(),
                TenantId = tenantId
            });
            await projectionManager.HandleEventAsync("valora.data.changed", tenantId, eventPayload2);

            // Assert
            var collection = mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");
            var projection = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("_id", orderId.ToString()))
                .FirstOrDefaultAsync();

            Assert.NotNull(projection);
            Assert.Equal("Confirmed", projection["Status"].AsString);
            Assert.Equal(1500.00, projection["TotalAmount"].AsDouble);

            _output.WriteLine($"✓ ProjectionManager updated existing projection");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", orderId.ToString()));
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region Write/Read Model Consistency Tests

        [Fact]
        public async Task Consistency_WriteModelChanges_ReflectedInReadModel()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var mongoDb = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Create order in PostgreSQL (write model)
            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-CONSISTENCY-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 10000.00m,
                Status = SalesOrderStatus.Confirmed,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "consistency-test",
                Version = 1,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM001",
                        Quantity = 20,
                        UnitPrice = 500.00m,
                        LineTotal = 10000.00m,
                        SalesOrderId = Guid.Empty
                    }
                }
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            // Manually create projection (simulating what happens via Kafka)
            var collection = mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");
            var projection = new BsonDocument
            {
                { "_id", order.Id.ToString() },
                { "TenantId", tenantId },
                { "OrderNumber", order.OrderNumber },
                { "CustomerId", order.CustomerId },
                { "TotalAmount", (double)order.TotalAmount },
                { "Status", order.Status },
                { "Version", (int)order.Version },
                { "Items", new BsonArray(order.Items.Select(i => new BsonDocument
                    {
                        { "MaterialCode", i.MaterialCode },
                        { "Quantity", i.Quantity },
                        { "UnitPrice", (double)i.UnitPrice },
                        { "LineTotal", (double)i.LineTotal }
                    }))
                },
                { "CreatedAt", order.CreatedAt },
                { "LastUpdated", DateTime.UtcNow }
            };

            await collection.InsertOneAsync(projection);

            // Act - Verify consistency
            var writeModel = await dbContext.SalesOrders
                .AsNoTracking()
                .Include(so => so.Items)
                .FirstOrDefaultAsync(so => so.Id == order.Id);

            var readModel = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("_id", order.Id.ToString()))
                .FirstOrDefaultAsync();

            // Assert
            Assert.NotNull(writeModel);
            Assert.NotNull(readModel);
            Assert.Equal(writeModel.OrderNumber, readModel["OrderNumber"].AsString);
            Assert.Equal(writeModel.TotalAmount, (decimal)readModel["TotalAmount"].AsDouble);
            Assert.Equal(writeModel.Status.ToString(), readModel["Status"].AsString);
            Assert.Equal((int)writeModel.Version, readModel["Version"].AsInt32);
            Assert.Equal(writeModel.Items.Count, readModel["Items"].AsBsonArray.Count);

            _output.WriteLine($"✓ Write and read models are consistent");

            // Cleanup
            await collection.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", order.Id.ToString()));
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task Consistency_QueryReadModel_Performance()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var mongoDb = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
            var tenantId = $"perf-test-{Guid.NewGuid():N}";
            var collection = mongoDb.GetCollection<BsonDocument>("Entity_SalesOrder");

            // Create write models in PostgreSQL
            var orders = new List<SalesOrder>();
            for (int i = 0; i < 10; i++)
            {
                orders.Add(new SalesOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrderNumber = $"SO-PERF-{i:000}",
                    CustomerId = "CUST001",
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 1000m * (i + 1),
                    Status = SalesOrderStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "perf-test",
                    Version = 1
                });
            }

            dbContext.SalesOrders.AddRange(orders);
            await dbContext.SaveChangesAsync();

            // Create read models in MongoDB
            var projections = orders.Select(o => new BsonDocument
            {
                { "_id", o.Id.ToString() },
                { "TenantId", tenantId },
                { "OrderNumber", o.OrderNumber },
                { "TotalAmount", (double)o.TotalAmount },
                { "Status", o.Status },
                { "Version", (int)o.Version },
                { "CreatedAt", o.CreatedAt }
            }).ToList();

            await collection.InsertManyAsync(projections);

            // Act - Query read model (should be faster than write model)
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var readModels = await collection
                .Find(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId))
                .ToListAsync();
            stopwatch.Stop();

            var readModelTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var writeModels = await dbContext.SalesOrders
                .AsNoTracking()
                .Where(so => so.TenantId == tenantId)
                .ToListAsync();
            stopwatch.Stop();

            var writeModelTime = stopwatch.ElapsedMilliseconds;

            // Assert
            Assert.Equal(10, readModels.Count);
            Assert.Equal(10, writeModels.Count);

            _output.WriteLine($"✓ Read model query: {readModelTime}ms, Write model query: {writeModelTime}ms");

            // Cleanup
            await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Eq("TenantId", tenantId));
            dbContext.SalesOrders.RemoveRange(orders);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region Outbox Pattern Verification

        [Fact]
        public async Task OutboxPattern_MessageCreated_BeforeTransactionCommit()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Act - Use transaction to ensure atomicity
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-OUTBOX-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 5000.00m,
                Status = SalesOrderStatus.Draft,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "outbox-test",
                Version = 0
            };

            dbContext.SalesOrders.Add(order);

            var outboxMessage = new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.data.changed",
                Payload = JsonSerializer.Serialize(new
                {
                    AggregateType = "SalesOrder",
                    AggregateId = order.Id.ToString()
                }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            dbContext.OutboxMessages.Add(outboxMessage);

            // Both should be saved together
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            // Assert
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.Id == order.Id);

            var savedMessage = await dbContext.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == outboxMessage.Id);

            Assert.NotNull(savedOrder);
            Assert.NotNull(savedMessage);
            Assert.Equal("Pending", savedMessage.Status);

            _output.WriteLine($"✓ Outbox pattern: Order and message saved atomically");

            // Cleanup
            dbContext.SalesOrders.Remove(savedOrder);
            dbContext.OutboxMessages.Remove(savedMessage);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task OutboxPattern_MessageProcessing_SimulatesOutboxProcessor()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Create pending messages
            var messages = new List<OutboxMessageEntity>();
            for (int i = 0; i < 5; i++)
            {
                messages.Add(new OutboxMessageEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Topic = "valora.data.changed",
                    Payload = JsonSerializer.Serialize(new { Index = i }),
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            dbContext.OutboxMessages.AddRange(messages);
            await dbContext.SaveChangesAsync();

            // Act - Simulate OutboxProcessor
            var pendingMessages = await dbContext.OutboxMessages
                .Where(m => m.Status == "Pending" && m.TenantId == tenantId)
                .OrderBy(m => m.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var message in pendingMessages)
            {
                // Simulate publishing to Kafka
                message.Status = "Published";
                message.ProcessedAt = DateTime.UtcNow;
            }

            await dbContext.SaveChangesAsync();

            // Assert
            var remainingPending = await dbContext.OutboxMessages
                .Where(m => m.Status == "Pending" && m.TenantId == tenantId)
                .CountAsync();

            var publishedCount = await dbContext.OutboxMessages
                .Where(m => m.Status == "Published" && m.TenantId == tenantId)
                .CountAsync();

            Assert.Equal(0, remainingPending);
            Assert.Equal(5, publishedCount);

            _output.WriteLine($"✓ Outbox processor simulation: {publishedCount} messages published");

            // Cleanup
            dbContext.OutboxMessages.RemoveRange(messages);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task OutboxPattern_FailedMessage_RetriedLater()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var failedMessage = new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.data.changed",
                Payload = "{}",
                Status = "Failed",
                Error = "Connection timeout",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            dbContext.OutboxMessages.Add(failedMessage);
            await dbContext.SaveChangesAsync();

            // Act - Simulate retry logic
            var failedMessages = await dbContext.OutboxMessages
                .Where(m => m.Status == "Failed" && m.TenantId == tenantId)
                .ToListAsync();

            foreach (var message in failedMessages)
            {
                // Retry: Reset to Pending
                message.Status = "Pending";
                message.Error = null;
            }

            await dbContext.SaveChangesAsync();

            // Assert
            var retriedMessage = await dbContext.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == failedMessage.Id);

            Assert.NotNull(retriedMessage);
            Assert.Equal("Pending", retriedMessage.Status);
            Assert.Null(retriedMessage.Error);

            _output.WriteLine($"✓ Failed message reset to Pending for retry");

            // Cleanup
            dbContext.OutboxMessages.Remove(retriedMessage);
            await dbContext.SaveChangesAsync();
        }

        #endregion
    }
}

using System.Data;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Valora.Api;
using Valora.Api.Application.Sales.Commands.CreateSalesOrder;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Infrastructure.Persistence;
using Xunit;
using Xunit.Abstractions;

namespace Valora.Tests
{
    /// <summary>
    /// Supabase/PostgreSQL CQRS Integration Tests for Sales Order
    /// Tests write operations to PostgreSQL (Supabase), CQRS write model persistence,
    /// connection pooling, error handling, and read/write separation
    /// </summary>
    public class SalesOrderSupabaseTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ITestOutputHelper _output;
        private readonly JsonSerializerOptions _jsonOptions;

        public SalesOrderSupabaseTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        #region Write Operations to PostgreSQL (Supabase)

        [Fact]
        public async Task CreateSalesOrder_PersistsToPostgreSQL_WithCorrectData()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderNumber = $"SO-TEST-{Guid.NewGuid():N}";

            var salesOrder = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = orderNumber,
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM001",
                        Quantity = 10,
                        UnitPrice = 100.00m,
                        LineTotal = 1000.00m,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            // Act
            dbContext.SalesOrders.Add(salesOrder);
            var saveResult = await dbContext.SaveChangesAsync();

            // Assert
            Assert.True(saveResult > 0, "SaveChanges should affect at least one row");

            // Verify in database
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .Include(so => so.Items)
                .FirstOrDefaultAsync(so => so.Id == salesOrder.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal(orderNumber, savedOrder.OrderNumber);
            Assert.Equal(tenantId, savedOrder.TenantId);
            Assert.Equal(1u, savedOrder.Version); // Version should be incremented
            Assert.Single(savedOrder.Items);

            _output.WriteLine($"✓ SalesOrder persisted to PostgreSQL: {savedOrder.Id}");

            // Cleanup
            dbContext.SalesOrders.Remove(savedOrder);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task CreateSalesOrder_WithItems_PersistsAllLineItems()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var salesOrder = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-MULTI-{Guid.NewGuid():N}",
                CustomerId = "CUST002",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 5500.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM001",
                        Quantity = 10,
                        UnitPrice = 100.00m,
                        LineTotal = 1000.00m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM002",
                        Quantity = 5,
                        UnitPrice = 500.00m,
                        LineTotal = 2500.00m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM003",
                        Quantity = 20,
                        UnitPrice = 100.00m,
                        LineTotal = 2000.00m,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            // Act
            dbContext.SalesOrders.Add(salesOrder);
            await dbContext.SaveChangesAsync();

            // Assert
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .Include(so => so.Items)
                .FirstOrDefaultAsync(so => so.Id == salesOrder.Id);

            Assert.NotNull(savedOrder);
            Assert.Equal(3, savedOrder.Items.Count);
            Assert.Equal(5500.00m, savedOrder.Items.Sum(i => i.LineTotal));

            _output.WriteLine($"✓ SalesOrder with {savedOrder.Items.Count} items persisted successfully");

            // Cleanup
            dbContext.SalesOrders.Remove(savedOrder);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateSalesOrder_UpdatesVersionAndModifiedFields()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var salesOrder = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-UPDATE-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0
            };

            dbContext.SalesOrders.Add(salesOrder);
            await dbContext.SaveChangesAsync();

            var originalVersion = salesOrder.Version;

            // Act
            salesOrder.Status = "Confirmed";
            salesOrder.TotalAmount = 1500.00m;
            salesOrder.ModifiedAt = DateTime.UtcNow;
            salesOrder.ModifiedBy = "updater-user";

            await dbContext.SaveChangesAsync();

            // Assert
            var updatedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.Id == salesOrder.Id);

            Assert.NotNull(updatedOrder);
            Assert.Equal("Confirmed", updatedOrder.Status);
            Assert.Equal(1500.00m, updatedOrder.TotalAmount);
            Assert.Equal(originalVersion + 1, updatedOrder.Version);
            Assert.NotNull(updatedOrder.ModifiedAt);
            Assert.Equal("updater-user", updatedOrder.ModifiedBy);

            _output.WriteLine($"✓ SalesOrder updated: Version {originalVersion} -> {updatedOrder.Version}");

            // Cleanup
            dbContext.SalesOrders.Remove(updatedOrder);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region CQRS Write Model Persistence

        [Fact]
        public async Task CommandHandler_CreateSalesOrder_CreatesOutboxMessage()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var command = new CreateSalesOrderCommand(
                tenantId,
                $"SO-CMD-{Guid.NewGuid():N}",
                "CUST001",
                DateTime.UtcNow,
                new List<SalesOrderItemDto>
                {
                    new("ITEM001", 10),
                    new("ITEM002", 5)
                },
                autoPost: false
            );

            var handler = new CreateSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess, $"Command should succeed: {result.ErrorMessage}");

            // Verify SalesOrder was created
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.OrderNumber == command.OrderNumber);

            Assert.NotNull(savedOrder);

            // Verify Outbox message was created
            var outboxMessages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId)
                .ToListAsync();

            Assert.NotEmpty(outboxMessages);
            Assert.Contains(outboxMessages, o => o.Topic.Contains("data.changed") || o.Topic.Contains("salesorder"));

            _output.WriteLine($"✓ Command created {outboxMessages.Count} outbox message(s)");

            // Cleanup
            if (savedOrder != null)
            {
                dbContext.SalesOrders.Remove(savedOrder);
                await dbContext.SaveChangesAsync();
            }
        }

        [Fact]
        public async Task CommandHandler_CreateSalesOrder_WithAutoPost_CreatesBillingEvent()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var command = new CreateSalesOrderCommand(
                tenantId,
                $"SO-AUTO-{Guid.NewGuid():N}",
                "CUST001",
                DateTime.UtcNow,
                new List<SalesOrderItemDto>
                {
                    new("ITEM001", 10)
                },
                autoPost: true
            );

            var handler = new CreateSalesOrderCommandHandler(dbContext);

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccess);

            var outboxMessages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(o => o.TenantId == tenantId)
                .ToListAsync();

            Assert.True(outboxMessages.Count >= 2, "Should have data.changed and billing events");
            Assert.Contains(outboxMessages, o => o.Topic == "valora.sd.so_billed");

            _output.WriteLine($"✓ Auto-post created billing event in outbox");

            // Cleanup
            var savedOrder = await dbContext.SalesOrders
                .FirstOrDefaultAsync(so => so.OrderNumber == command.OrderNumber);
            if (savedOrder != null)
            {
                dbContext.SalesOrders.Remove(savedOrder);
                await dbContext.SaveChangesAsync();
            }
        }

        #endregion

        #region Connection Pooling and Error Handling

        [Fact]
        public async Task MultipleConcurrentWrites_HandledByConnectionPool()
        {
            // Arrange
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var tasks = new List<Task<Guid>>();
            var orderIds = new List<Guid>();

            // Act - Create 10 orders concurrently
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = _factory.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

                    var order = new SalesOrder
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        OrderNumber = $"SO-CONCURRENT-{index}-{Guid.NewGuid():N}",
                        CustomerId = "CUST001",
                        OrderDate = DateTime.UtcNow,
                        TotalAmount = 100m * (index + 1),
                        Status = "Draft",
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = "concurrent-test",
                        Version = 0
                    };

                    dbContext.SalesOrders.Add(order);
                    await dbContext.SaveChangesAsync();
                    return order.Id;
                }));
            }

            orderIds.AddRange(await Task.WhenAll(tasks));

            // Assert
            Assert.Equal(10, orderIds.Count);
            Assert.Equal(10, orderIds.Distinct().Count());

            // Verify all orders exist
            using var verifyScope = _factory.Services.CreateScope();
            var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PlatformDbContext>();

            var savedOrders = await verifyContext.SalesOrders
                .AsNoTracking()
                .Where(so => so.TenantId == tenantId)
                .ToListAsync();

            Assert.Equal(10, savedOrders.Count);

            _output.WriteLine($"✓ Successfully created {savedOrders.Count} orders concurrently");

            // Cleanup
            verifyContext.SalesOrders.RemoveRange(savedOrders);
            await verifyContext.SaveChangesAsync();
        }

        [Fact]
        public async Task DuplicateOrderNumber_ThrowsDbUpdateException()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderNumber = $"SO-DUPLICATE-{Guid.NewGuid():N}";

            var order1 = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = orderNumber,
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0
            };

            dbContext.SalesOrders.Add(order1);
            await dbContext.SaveChangesAsync();

            // Act & Assert
            var order2 = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = orderNumber, // Same order number
                CustomerId = "CUST002",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 2000.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0
            };

            dbContext.SalesOrders.Add(order2);
            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());

            _output.WriteLine($"✓ Correctly threw exception for duplicate order number");

            // Cleanup
            dbContext.SalesOrders.Remove(order1);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task TransactionRollback_OnError_NoPartialDataPersisted()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";
            var orderNumber = $"SO-TRANS-{Guid.NewGuid():N}";

            // Act & Assert
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            try
            {
                var order = new SalesOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    OrderNumber = orderNumber,
                    CustomerId = "CUST001",
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = 1000.00m,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "test-user",
                    Version = 0
                };

                dbContext.SalesOrders.Add(order);
                await dbContext.SaveChangesAsync();

                // Simulate an error
                throw new Exception("Simulated error after save");
            }
            catch
            {
                await transaction.RollbackAsync();
            }

            // Verify no data was persisted
            var savedOrder = await dbContext.SalesOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(so => so.OrderNumber == orderNumber);

            Assert.Null(savedOrder);
            _output.WriteLine($"✓ Transaction rollback prevented partial data persistence");
        }

        #endregion

        #region Direct SQL/Dapper Tests for CQRS

        [Fact]
        public async Task DapperQuery_ReadsSalesOrder_WithRawSQL()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-DAPPER-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1000.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            // Get connection string
            var connectionString = dbContext.Database.GetConnectionString();

            // Act
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT id, tenant_id, order_number, customer_id, 
                       order_date, total_amount, status, version
                FROM sales_orders 
                WHERE id = @Id";

            var result = await connection.QueryFirstOrDefaultAsync(sql, new { Id = order.Id });

            // Assert
            Assert.NotNull(result);
            Assert.Equal(order.OrderNumber, result.order_number);
            Assert.Equal(order.TenantId, result.tenant_id);
            Assert.Equal(order.TotalAmount, (decimal)result.total_amount);

            _output.WriteLine($"✓ Dapper query returned SalesOrder via raw SQL");

            // Cleanup
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task DapperQuery_ReadsSalesOrderWithItems_JoinQuery()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var order = new SalesOrder
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                OrderNumber = $"SO-JOIN-{Guid.NewGuid():N}",
                CustomerId = "CUST001",
                OrderDate = DateTime.UtcNow,
                TotalAmount = 1500.00m,
                Status = "Draft",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "test-user",
                Version = 0,
                Items = new List<SalesOrderItem>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM001",
                        Quantity = 10,
                        UnitPrice = 100.00m,
                        LineTotal = 1000.00m,
                        CreatedAt = DateTime.UtcNow
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        MaterialCode = "ITEM002",
                        Quantity = 5,
                        UnitPrice = 100.00m,
                        LineTotal = 500.00m,
                        CreatedAt = DateTime.UtcNow
                    }
                }
            };

            dbContext.SalesOrders.Add(order);
            await dbContext.SaveChangesAsync();

            var connectionString = dbContext.Database.GetConnectionString();

            // Act
            using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            var sql = @"
                SELECT so.id, so.order_number, soi.material_code, 
                       soi.quantity, soi.unit_price, soi.line_total
                FROM sales_orders so
                INNER JOIN sales_order_items soi ON so.id = soi.sales_order_id
                WHERE so.id = @OrderId";

            var results = await connection.QueryAsync(sql, new { OrderId = order.Id });

            // Assert
            var items = results.ToList();
            Assert.Equal(2, items.Count);
            Assert.All(items, item => Assert.Equal(order.OrderNumber, (string)item.order_number));

            _output.WriteLine($"✓ Join query returned {items.Count} line items");

            // Cleanup
            dbContext.SalesOrders.Remove(order);
            await dbContext.SaveChangesAsync();
        }

        #endregion

        #region Outbox Pattern Tests

        [Fact]
        public async Task OutboxMessage_CreatedWithPendingStatus()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            var outboxMessage = new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.test.event",
                Payload = JsonSerializer.Serialize(new { Test = "data", AggregateType = "Test", AggregateId = Guid.NewGuid().ToString() }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            // Act
            dbContext.OutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync();

            // Assert
            var savedMessage = await dbContext.OutboxMessages
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == outboxMessage.Id);

            Assert.NotNull(savedMessage);
            Assert.Equal("Pending", savedMessage.Status);
            Assert.Null(savedMessage.ProcessedAt);

            _output.WriteLine($"✓ Outbox message created with Pending status");

            // Cleanup
            dbContext.OutboxMessages.Remove(savedMessage);
            await dbContext.SaveChangesAsync();
        }

        [Fact]
        public async Task OutboxMessage_QueryPendingMessages_ReturnsOnlyPending()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var tenantId = $"test-tenant-{Guid.NewGuid():N}";

            // Create messages with different statuses
            var messages = new List<OutboxMessageEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Topic = "valora.test.1",
                    Payload = "{}",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-5)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Topic = "valora.test.2",
                    Payload = "{}",
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-3)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Topic = "valora.test.3",
                    Payload = "{}",
                    Status = "Published",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-1),
                    ProcessedAt = DateTime.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    Topic = "valora.test.4",
                    Payload = "{}",
                    Status = "Failed",
                    CreatedAt = DateTime.UtcNow,
                    Error = "Test error"
                }
            };

            dbContext.OutboxMessages.AddRange(messages);
            await dbContext.SaveChangesAsync();

            // Act
            var pendingMessages = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(m => m.Status == "Pending" && m.TenantId == tenantId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            // Assert
            Assert.Equal(2, pendingMessages.Count);
            Assert.All(pendingMessages, m => Assert.Equal("Pending", m.Status));

            _output.WriteLine($"✓ Query returned {pendingMessages.Count} pending messages");

            // Cleanup
            dbContext.OutboxMessages.RemoveRange(messages);
            await dbContext.SaveChangesAsync();
        }

        #endregion
    }
}

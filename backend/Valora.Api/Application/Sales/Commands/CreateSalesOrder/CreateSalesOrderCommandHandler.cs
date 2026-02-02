using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Infrastructure.Persistence;

using System.Text.Json;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Events;

namespace Valora.Api.Application.Sales.Commands.CreateSalesOrder;

public class CreateSalesOrderCommandHandler : IRequestHandler<CreateSalesOrderCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public CreateSalesOrderCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(CreateSalesOrderCommand request, CancellationToken cancellationToken)
    {
        // Validate Customer (GL Account)
        // We assume CustomerId holds the GL Account Name as configured in Schema lookup
        var glAccount = await _dbContext.GLAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(gl => gl.TenantId == request.TenantId && gl.Name == request.CustomerId, cancellationToken);

        if (glAccount == null)
        {
            return ApiResult.Fail(request.TenantId, "SD", "create-so", new ApiError("Validation", $"Customer (GL Account) '{request.CustomerId}' not found."));
        }

        if (request.Items == null || !request.Items.Any())
        {
            return ApiResult.Fail(request.TenantId, "SD", "create-so", new ApiError("Validation", "Sales Order must have at least one item."));
        }

        // Fetch Materials to get Price
        var materialCodes = request.Items.Select(i => i.MaterialCode).ToList();
        var materials = await _dbContext.MaterialMasters
            .Where(m => m.TenantId == request.TenantId && materialCodes.Contains(m.MaterialCode))
            .ToDictionaryAsync(m => m.MaterialCode, m => m.StandardPrice, cancellationToken);

        if (materials.Count != materialCodes.Distinct().Count())
        {
            return ApiResult.Fail(request.TenantId, "SD", "create-so", new ApiError("Validation", "One or more materials not found."));
        }

        var order = new SalesOrder
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            OrderNumber = request.OrderNumber ?? $"SO-{Guid.NewGuid():N}",
            OrderDate = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            Currency = request.Currency,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            Status = request.AutoPost ? SalesOrderStatus.Invoiced : SalesOrderStatus.Draft,
            Items = new List<SalesOrderItem>()
        };

        foreach (var item in request.Items)
        {
            var price = materials[item.MaterialCode];
            var lineTotal = price * item.Quantity;

            order.Items.Add(new SalesOrderItem
            {
                Id = Guid.NewGuid(),
                MaterialCode = item.MaterialCode,
                Quantity = item.Quantity,
                UnitPrice = price,
                LineTotal = lineTotal
            });
        }

        order.TotalAmount = order.Items.Sum(i => i.LineTotal);

        _dbContext.SalesOrders.Add(order);
        
        // Auto-Post Logic (Generate Event)
        if (request.AutoPost)
        {
             Console.WriteLine($"[DEBUG] CreateSalesOrder: Auto-Posting enabled. Emitting valora.sd.so_billed.");
             var evt = new SalesOrderBilledEvent
            {
                TenantId = request.TenantId,
                AggregateId = order.Id.ToString(),
                SalesOrderId = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId, // GL Account Name
                TotalAmount = order.TotalAmount,
                Currency = order.Currency,
                BillingDate = DateTime.UtcNow
            };

            _dbContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                Topic = "valora.sd.so_billed",
                Payload = JsonSerializer.Serialize(evt),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "SD", "create-so", order.Id);
    }
}

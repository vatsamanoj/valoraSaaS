using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Domain.Events;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Sales.Commands.BillSalesOrder;

public class BillSalesOrderCommandHandler : IRequestHandler<BillSalesOrderCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public BillSalesOrderCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(BillSalesOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _dbContext.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.SalesOrderId && o.TenantId == request.TenantId, cancellationToken);

        if (order == null)
        {
            return ApiResult.Fail(request.TenantId, "SD", "bill-so", new ApiError("NotFound", "Sales Order not found."));
        }

        if (order.Status == SalesOrderStatus.Invoiced)
        {
            return ApiResult.Fail(request.TenantId, "SD", "bill-so", new ApiError("BusinessRule", "Order is already invoiced."));
        }

        order.Status = SalesOrderStatus.Invoiced;
        _dbContext.SalesOrders.Update(order);

        // Outbox Event for FI Posting (SD-FI Integration)
        var evt = new SalesOrderBilledEvent
        {
            TenantId = request.TenantId,
            AggregateId = order.Id.ToString(),
            SalesOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "SD", "bill-so", order.Id);
    }
}

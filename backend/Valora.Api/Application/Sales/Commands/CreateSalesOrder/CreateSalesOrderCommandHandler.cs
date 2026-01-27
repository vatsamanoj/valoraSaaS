using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Infrastructure.Persistence;

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
        if (!request.Items.Any())
        {
            return ApiResult.Fail(request.TenantId, "SD", "create-so", new ApiError("Validation", "Sales Order must have items."));
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
            OrderNumber = $"SO-{DateTime.UtcNow.Ticks}", // Simple generation
            OrderDate = DateTime.UtcNow,
            CustomerId = request.CustomerId,
            Currency = request.Currency,
            Status = SalesOrderStatus.Draft,
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "SD", "create-so", order.Id);
    }
}

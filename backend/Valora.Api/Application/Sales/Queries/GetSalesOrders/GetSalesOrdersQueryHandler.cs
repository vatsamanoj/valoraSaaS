using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Sales.Queries.GetSalesOrders;

public class GetSalesOrdersQueryHandler : IRequestHandler<GetSalesOrdersQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public GetSalesOrdersQueryHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(GetSalesOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _dbContext.SalesOrders
            .Where(o => o.TenantId == request.TenantId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new 
            {
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.CustomerId,
                o.TotalAmount,
                o.Currency,
                Status = o.Status.ToString(),
                ItemCount = o.Items.Count
            })
            .ToListAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "SD", "list-so", orders);
    }
}

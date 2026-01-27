using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Materials.Queries.GetStockLevels;

public class GetStockLevelsQueryHandler : IRequestHandler<GetStockLevelsQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public GetStockLevelsQueryHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
    {
        var stocks = await _dbContext.MaterialMasters
            .Where(m => m.TenantId == request.TenantId)
            .Select(m => new 
            {
                m.Id,
                m.MaterialCode,
                m.Description,
                m.BaseUnitOfMeasure,
                m.CurrentStock,
                m.StandardPrice,
                TotalValue = m.CurrentStock * m.StandardPrice
            })
            .ToListAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "MM", "list-stock", stocks);
    }
}

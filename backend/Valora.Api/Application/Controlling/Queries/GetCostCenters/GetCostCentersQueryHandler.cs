using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Controlling.Queries.GetCostCenters;

public class GetCostCentersQueryHandler : IRequestHandler<GetCostCentersQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public GetCostCentersQueryHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(GetCostCentersQuery request, CancellationToken cancellationToken)
    {
        var costCenters = await _dbContext.CostCenters
            .Where(cc => cc.TenantId == request.TenantId)
            .OrderBy(cc => cc.Code)
            .Select(cc => new 
            {
                cc.Id,
                cc.Code,
                cc.Name,
                cc.IsActive
            })
            .ToListAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "CO", "list-costcenters", costCenters);
    }
}

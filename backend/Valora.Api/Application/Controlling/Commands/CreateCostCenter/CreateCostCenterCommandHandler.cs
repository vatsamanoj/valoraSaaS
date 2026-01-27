using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Domain.Entities.Controlling;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Controlling.Commands.CreateCostCenter;

public class CreateCostCenterCommandHandler : IRequestHandler<CreateCostCenterCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public CreateCostCenterCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(CreateCostCenterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
        {
            return ApiResult.Fail(request.TenantId, "CO", "create-costcenter", new ApiError("Validation", "Code and Name are required."));
        }

        var costCenter = new CostCenter
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Code = request.Code,
            Name = request.Name,
            IsActive = true
        };

        _dbContext.CostCenters.Add(costCenter);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "CO", "create-costcenter", costCenter.Id);
    }
}

using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Domain.Entities.Materials;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Materials.Commands.CreateMaterial;

public class CreateMaterialCommandHandler : IRequestHandler<CreateMaterialCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public CreateMaterialCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.MaterialCode))
        {
            return ApiResult.Fail(request.TenantId, "MM", "create-material", new ApiError("Validation", "Material Code is required."));
        }

        var material = new MaterialMaster
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MaterialCode = request.MaterialCode,
            Description = request.Description,
            BaseUnitOfMeasure = request.BaseUnitOfMeasure,
            StandardPrice = request.StandardPrice,
            CurrentStock = 0 // Initial stock is 0
        };

        _dbContext.MaterialMasters.Add(material);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "MM", "create-material", material.Id);
    }
}

using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Materials.Commands.CreateMaterial;

public record CreateMaterialCommand(
    string TenantId, 
    string MaterialCode, 
    string Description, 
    string BaseUnitOfMeasure,
    decimal StandardPrice
) : IRequest<ApiResult>;

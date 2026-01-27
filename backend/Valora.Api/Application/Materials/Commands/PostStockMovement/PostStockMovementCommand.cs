using MediatR;
using Lab360.Application.Common.Results;
using Valora.Api.Domain.Entities.Materials;

namespace Valora.Api.Application.Materials.Commands.PostStockMovement;

public record PostStockMovementCommand(
    string TenantId,
    Guid MaterialId,
    MovementType MovementType,
    decimal Quantity,
    DateTime MovementDate
) : IRequest<ApiResult>;

using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Controlling.Commands.CreateCostCenter;

public record CreateCostCenterCommand(string TenantId, string Code, string Name) : IRequest<ApiResult>;

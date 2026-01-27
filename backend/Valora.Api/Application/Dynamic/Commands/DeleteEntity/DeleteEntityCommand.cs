using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Dynamic.Commands.DeleteEntity;

public record DeleteEntityCommand(
    string TenantId,
    string Module,
    string Id) : IRequest<ApiResult>;

using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Dynamic.Queries.GetEntity;

public record GetEntityQuery(
    string TenantId,
    string Module,
    string Id) : IRequest<ApiResult>;

using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Controlling.Queries.GetCostCenters;

public record GetCostCentersQuery(string TenantId) : IRequest<ApiResult>;

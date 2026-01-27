using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Materials.Queries.GetStockLevels;

public record GetStockLevelsQuery(string TenantId) : IRequest<ApiResult>;

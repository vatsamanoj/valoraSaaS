using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.Dynamic.Queries.ListEntities;

public record ListEntitiesQuery(
    string TenantId,
    string Module,
    int Page = 1,
    int PageSize = 20,
    Dictionary<string, object>? Filters = null,
    string? SortBy = null,
    bool SortDesc = false
) : IRequest<ApiResult>;

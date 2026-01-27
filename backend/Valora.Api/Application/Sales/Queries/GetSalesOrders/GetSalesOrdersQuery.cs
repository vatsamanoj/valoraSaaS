using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Sales.Queries.GetSalesOrders;

public record GetSalesOrdersQuery(string TenantId) : IRequest<ApiResult>;

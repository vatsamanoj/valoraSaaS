using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Sales.Commands.BillSalesOrder;

public record BillSalesOrderCommand(string TenantId, Guid SalesOrderId) : IRequest<ApiResult>;

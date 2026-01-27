using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.Sales.Commands.CreateSalesOrder;

public record SalesOrderItemDto(string MaterialCode, decimal Quantity);

public record CreateSalesOrderCommand(
    string TenantId, 
    string CustomerId, 
    string Currency,
    List<SalesOrderItemDto> Items
) : IRequest<ApiResult>;

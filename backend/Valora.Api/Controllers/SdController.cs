using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Sales.Commands.BillSalesOrder;
using Valora.Api.Application.Sales.Commands.CreateSalesOrder;
using Valora.Api.Application.Sales.Queries.GetSalesOrders;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/sales")]
public class SdController : ControllerBase
{
    private readonly IMediator _mediator;

    public SdController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetSalesOrders()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetSalesOrdersQuery(tenantContext.TenantId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("orders")]
    public async Task<IActionResult> CreateSalesOrder([FromBody] CreateSalesOrderRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var command = new CreateSalesOrderCommand(
            tenantContext.TenantId,
            request.CustomerId,
            request.Currency,
            request.Items
        );
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("orders/{id}/bill")]
    public async Task<IActionResult> BillSalesOrder(Guid id)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var command = new BillSalesOrderCommand(tenantContext.TenantId, id);
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public record CreateSalesOrderRequest(string CustomerId, string Currency, List<SalesOrderItemDto> Items);

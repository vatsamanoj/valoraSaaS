using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Controlling.Commands.CreateCostCenter;
using Valora.Api.Application.Controlling.Queries.GetCostCenters;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/controlling")]
public class CoController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("cost-centers")]
    public async Task<IActionResult> GetCostCenters()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetCostCentersQuery(tenantContext.TenantId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("cost-centers")]
    public async Task<IActionResult> CreateCostCenter([FromBody] CreateCostCenterRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var command = new CreateCostCenterCommand(tenantContext.TenantId, request.Code, request.Name);
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public record CreateCostCenterRequest(string Code, string Name);

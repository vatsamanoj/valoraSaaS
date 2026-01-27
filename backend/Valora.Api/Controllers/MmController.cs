using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Materials.Commands.CreateMaterial;
using Valora.Api.Application.Materials.Commands.PostStockMovement;
using Valora.Api.Application.Materials.Queries.GetStockLevels;
using Valora.Api.Domain.Entities.Materials;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/materials")]
public class MmController : ControllerBase
{
    private readonly IMediator _mediator;

    public MmController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("stock")]
    public async Task<IActionResult> GetStockLevels()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetStockLevelsQuery(tenantContext.TenantId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("materials")]
    public async Task<IActionResult> CreateMaterial([FromBody] CreateMaterialRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var command = new CreateMaterialCommand(
            tenantContext.TenantId, 
            request.MaterialCode, 
            request.Description, 
            request.BaseUnitOfMeasure,
            request.StandardPrice
        );
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("movements")]
    public async Task<IActionResult> PostStockMovement([FromBody] PostStockMovementRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var command = new PostStockMovementCommand(
            tenantContext.TenantId,
            request.MaterialId,
            request.MovementType,
            request.Quantity,
            request.MovementDate
        );
        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public record CreateMaterialRequest(string MaterialCode, string Description, string BaseUnitOfMeasure, decimal StandardPrice);
public record PostStockMovementRequest(Guid MaterialId, MovementType MovementType, decimal Quantity, DateTime MovementDate);

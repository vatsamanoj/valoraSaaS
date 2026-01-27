using System.Text.Json;
using Lab360.Application.Common.Results;
using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Dynamic.Commands.CreateEntity;
using Valora.Api.Application.Dynamic.Commands.DeleteEntity;
using Valora.Api.Application.Dynamic.Commands.UpdateEntity;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/data")]
public class GenericDataController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GenericDataController> _logger;

    public GenericDataController(IMediator mediator, ILogger<GenericDataController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("{module}")]
    public async Task<IActionResult> Create(string module, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new CreateEntityCommand(tenantContext.TenantId, module, body, userContext.UserId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPut("{module}/{id}")]
    public async Task<IActionResult> Update(string module, string id, [FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new UpdateEntityCommand(tenantContext.TenantId, module, id, body, userContext.UserId);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Code == "NotFound"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{module}/{id}")]
    public async Task<IActionResult> Delete(string module, string id, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        var command = new DeleteEntityCommand(tenantContext.TenantId, module, id);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Code == "NotFound"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }
}

using Lab360.Application.Common.Results;
using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Dynamic.Queries.GetEntity;
using Valora.Api.Application.Dynamic.Queries.ListEntities;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/query")]
public class QueryController : ControllerBase
{
    private readonly IMediator _mediator;

    public QueryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{module}")]
    public async Task<IActionResult> GetList(string module, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        var query = new ListEntitiesQuery(tenantContext.TenantId, module, page, pageSize);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpGet("{module}/{id}")]
    public async Task<IActionResult> GetById(string module, string id)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        var query = new GetEntityQuery(tenantContext.TenantId, module, id);
        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Code == "NotFound"))
                return NotFound(result);
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpPost("ExecuteQuery")]
    public async Task<IActionResult> ExecuteQuery([FromBody] ExecuteQueryRequest request, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        var query = new ListEntitiesQuery(
            tenantContext.TenantId,
            request.Module,
            request.Options?.Page ?? 1,
            request.Options?.PageSize ?? 20,
            request.Options?.Filters,
            request.Options?.SortBy,
            request.Options?.SortDesc ?? false
        );

        var result = await _mediator.Send(query, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

public class ExecuteQueryOptions
{
    public Dictionary<string, object>? Filters { get; set; }
    public int? Page { get; set; }
    public int? PageSize { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDesc { get; set; }
}

public class ExecuteQueryRequest
{
    public string Module { get; set; } = string.Empty;
    public ExecuteQueryOptions? Options { get; set; }
}

using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.Finance.Commands.CreateGLAccount;
using Valora.Api.Application.Finance.Commands.PostJournalEntry;
using Valora.Api.Application.Finance.Queries.GetGLAccounts;
using Valora.Api.Application.Finance.Queries.GetJournalEntries;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;

    public FinanceController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("gl-accounts")]
    public async Task<IActionResult> GetGLAccounts()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetGLAccountsQuery(tenantContext.TenantId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 20,
        [FromQuery] string? documentNumber = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetJournalEntriesQuery(tenantContext.TenantId, page, pageSize, documentNumber, startDate, endDate);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("gl-accounts")]
    public async Task<IActionResult> CreateGLAccount([FromBody] CreateGLAccountRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new CreateGLAccountCommand(
            tenantContext.TenantId,
            request.AccountCode,
            request.Name,
            request.Type,
            userContext.UserId
        );

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("journal-entries")]
    public async Task<IActionResult> PostJournalEntry([FromBody] PostJournalEntryRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new PostJournalEntryCommand(
            tenantContext.TenantId,
            request.PostingDate,
            request.DocumentNumber,
            request.Description,
            request.Reference,
            request.Lines,
            userContext.UserId
        );

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public record CreateGLAccountRequest(string AccountCode, string Name, string Type);
public record PostJournalEntryRequest(
    DateTime PostingDate, 
    string DocumentNumber, 
    string Description, 
    string Reference, 
    List<JournalEntryLineDto> Lines
);

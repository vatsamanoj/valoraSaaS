using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Application.Finance.Commands.CreateGLAccount;
using Valora.Api.Application.Finance.Commands.UpdateGLAccount;
using Valora.Api.Application.Finance.Commands.PostJournalEntry;
using Valora.Api.Application.Finance.Queries.GetGLAccounts;
using Valora.Api.Application.Finance.Queries.GetJournalEntries;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/finance")]
public class FinanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly Infrastructure.Persistence.PlatformDbContext _dbContext;

    public FinanceController(IMediator mediator, Infrastructure.Persistence.PlatformDbContext dbContext)
    {
        _mediator = mediator;
        _dbContext = dbContext;
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

    [HttpPut("gl-accounts/{id}")]
    public async Task<IActionResult> UpdateGLAccount(Guid id, [FromBody] UpdateGLAccountRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new UpdateGLAccountCommand(
            tenantContext.TenantId,
            id,
            request.Name,
            request.Type,
            request.IsActive,
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

    [HttpPut("journal-entries/{id}")]
    public async Task<IActionResult> UpdateJournalEntry(Guid id, [FromBody] UpdateJournalEntryRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new Valora.Api.Application.Finance.Commands.UpdateJournalEntry.UpdateJournalEntryCommand(
            tenantContext.TenantId,
            id,
            request.PostingDate,
            request.Description,
            request.Reference,
            request.Lines,
            userContext.UserId,
            request.Version
        );

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }

    [HttpPost("sync-gl")]
    public async Task<IActionResult> SyncGL()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        // Only allow for TEST_TENANT or Admin
        
        var accounts = await _dbContext.GLAccounts
            .Where(a => a.TenantId == tenantContext.TenantId)
            .ToListAsync();

        foreach (var a in accounts)
        {
            _dbContext.OutboxMessages.Add(new Domain.Entities.OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = a.TenantId,
                Topic = "valora.fi.gl_account_created",
                Payload = System.Text.Json.JsonSerializer.Serialize(new { AggregateType = "GLAccount", AggregateId = a.Id.ToString() }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
        return Ok($"Synced {accounts.Count} accounts");
    }

    [HttpPost("sync-je")]
    public async Task<IActionResult> SyncJournalEntries()
    {
        // Sync ALL tenants for repair purposes
        var entries = await _dbContext.JournalEntries
            .ToListAsync();

        foreach (var je in entries)
        {
            _dbContext.OutboxMessages.Add(new Domain.Entities.OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = je.TenantId,
                Topic = "valora.fi.updated",
                Payload = System.Text.Json.JsonSerializer.Serialize(new { AggregateType = "JournalEntry", AggregateId = je.Id.ToString() }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
        return Ok($"Synced {entries.Count} Journal Entries");
    }
}

public class CreateGLAccountRequest
{
    public required string AccountCode { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
}

public class UpdateGLAccountRequest
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public bool IsActive { get; set; }
}

public record PostJournalEntryRequest(
    DateTime PostingDate, 
    string DocumentNumber, 
    string Description, 
    string Reference, 
    List<JournalEntryLineDto> Lines
);

public record UpdateJournalEntryRequest(
    DateTime PostingDate, 
    string Description, 
    string Reference, 
    List<JournalEntryLineDto> Lines,
    uint? Version // xmin
);

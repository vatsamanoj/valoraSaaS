using Lab360.Application.Common.Results;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Application.Common.Engines;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Finance.Commands.UpdateJournalEntry;

public class UpdateJournalEntryCommandHandler : FinancialDocumentUpdateEngine<JournalEntry, JournalEntryLine, UpdateJournalEntryCommand>
{
    public UpdateJournalEntryCommandHandler(PlatformDbContext dbContext) : base(dbContext)
    {
    }

    protected override async Task<JournalEntry?> LoadDocumentAsync(UpdateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        return await _dbContext.JournalEntries
            .Include(j => j.Lines)
            .FirstOrDefaultAsync(j => j.Id == request.Id && j.TenantId == request.TenantId, cancellationToken);
    }

    protected override ApiResult BuildNotFoundResult(UpdateJournalEntryCommand request)
    {
        return ApiResult.Fail(request.TenantId, "FI", "update-je", new ApiError("Not_Found", "Journal Entry not found"));
    }

    protected override ApiResult Validate(JournalEntry doc, UpdateJournalEntryCommand request)
    {
        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
            return ApiResult.Fail(request.TenantId, "FI", "update-je", new ApiError("Validation", $"Journal Entry is not balanced. Debit: {totalDebit}, Credit: {totalCredit}"));

        // Validate Accounts exist
        var accountIds = request.Lines.Select(l => l.GLAccountId).Distinct().ToList();
        var existingCount = _dbContext.GLAccounts.Count(a => accountIds.Contains(a.Id) && a.TenantId == request.TenantId);
        if (existingCount != accountIds.Count)
        {
             return ApiResult.Fail(request.TenantId, "FI", "update-je", new ApiError("Validation", "One or more GL Accounts do not exist or belong to another tenant"));
        }
        
        return ApiResult.Ok(request.TenantId, "FI", "update-je", null);
    }

    protected override void ApplyStrictConcurrency(JournalEntry doc, UpdateJournalEntryCommand request)
    {
        if (request.Version.HasValue)
        {
            _dbContext.Entry(doc).Property("Version").OriginalValue = request.Version.Value;
        }
    }

    protected override void ApplyTrackedUpdate(JournalEntry doc, UpdateJournalEntryCommand request)
    {
        doc.Description = request.Description;
        doc.Reference = request.Reference;
        doc.PostingDate = request.PostingDate;
        doc.UpdatedBy = request.UserId;
        doc.UpdatedAt = DateTime.UtcNow;
    }

    protected override async Task ApplyAuthoritativeUpdateAsync(JournalEntry doc, UpdateJournalEntryCommand request, CancellationToken cancellationToken)
    {
        // Rule #3 Attempt 1: ExecuteUpdateAsync for header AND increment version
        await _dbContext.JournalEntries
            .Where(x => x.Id == doc.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Description, request.Description)
                .SetProperty(x => x.Reference, request.Reference)
                .SetProperty(x => x.PostingDate, request.PostingDate)
                .SetProperty(x => x.UpdatedBy, request.UserId)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow)
                .SetProperty(x => x.Version, x => x.Version + 1), 
                cancellationToken);
        
        // Manually bump version for the result, since the tracked entity is stale
        doc.Version++;
    }

    protected override async Task ReplaceLinesAsync(JournalEntry doc, UpdateJournalEntryCommand request, int attempt, CancellationToken cancellationToken)
    {
        if (attempt == 0)
        {
             // Attempt 0: Tracked Entities (Standard EF)
             // We clear the collection. EF Core will track these as deletions.
             doc.Lines.Clear();
             
             // Add new lines
             foreach (var line in request.Lines)
             {
                doc.Lines.Add(new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = doc.Id,
                    GLAccountId = line.GLAccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Description = line.Description
                });
             }
        }
        else
        {
            // Attempt 1: ExecuteDeleteAsync + Add (Authoritative)
            await _dbContext.JournalEntryLines
                .Where(x => x.JournalEntryId == doc.Id)
                .ExecuteDeleteAsync(cancellationToken);
            
            // We use DbSet directly to add new lines because doc is detached in the Engine
            foreach (var line in request.Lines)
            {
                _dbContext.JournalEntryLines.Add(new JournalEntryLine
                {
                    Id = Guid.NewGuid(),
                    JournalEntryId = doc.Id, // FK link
                    GLAccountId = line.GLAccountId,
                    Debit = line.Debit,
                    Credit = line.Credit,
                    Description = line.Description
                });
            }
        }
    }

    protected override void AddOutbox(JournalEntry doc, UpdateJournalEntryCommand request)
    {
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.fi.updated",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                AggregateType = "JournalEntry",
                AggregateId = doc.Id.ToString(),
                EventType = "JournalEntryUpdated",
                Id = doc.Id,
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);
    }

    protected override ApiResult BuildSuccessResult(JournalEntry doc, UpdateJournalEntryCommand request)
    {
        return ApiResult.Ok(request.TenantId, "FI", "update-je", new { Id = doc.Id, Version = doc.Version });
    }
}

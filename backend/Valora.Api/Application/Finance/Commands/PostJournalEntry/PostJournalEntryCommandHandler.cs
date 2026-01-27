using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Finance.Commands.PostJournalEntry;

public class PostJournalEntryCommandHandler : IRequestHandler<PostJournalEntryCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<PostJournalEntryCommandHandler> _logger;

    public PostJournalEntryCommandHandler(PlatformDbContext dbContext, ILogger<PostJournalEntryCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(PostJournalEntryCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines == null || !request.Lines.Any())
        {
            return ApiResult.Fail(request.TenantId, "FI", "post-je", new ApiError("Validation", "At least one line is required"));
        }

        // 1. Validate Double Entry (Debits == Credits)
        var totalDebit = request.Lines.Sum(l => l.Debit);
        var totalCredit = request.Lines.Sum(l => l.Credit);

        if (totalDebit != totalCredit)
        {
            return ApiResult.Fail(request.TenantId, "FI", "post-je", new ApiError("Validation", $"Journal Entry is not balanced. Debit: {totalDebit}, Credit: {totalCredit}"));
        }

        // 2. Validate Accounts exist
        var accountIds = request.Lines.Select(l => l.GLAccountId).Distinct().ToList();
        var existingCount = _dbContext.GLAccounts.Count(a => accountIds.Contains(a.Id) && a.TenantId == request.TenantId);
        if (existingCount != accountIds.Count)
        {
             return ApiResult.Fail(request.TenantId, "FI", "post-je", new ApiError("Validation", "One or more GL Accounts do not exist or belong to another tenant"));
        }

        // 3. Create Document
        var je = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            DocumentNumber = string.IsNullOrWhiteSpace(request.DocumentNumber) ? $"JE-{DateTime.UtcNow.Ticks}" : request.DocumentNumber,
            PostingDate = request.PostingDate,
            DocumentDate = DateTime.UtcNow,
            Description = request.Description,
            Reference = request.Reference,
            Status = JournalEntryStatus.Posted, // Direct posting for now
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId
        };

        foreach (var line in request.Lines)
        {
            je.Lines.Add(new JournalEntryLine
            {
                Id = Guid.NewGuid(),
                GLAccountId = line.GLAccountId,
                Debit = line.Debit,
                Credit = line.Credit,
                Description = line.Description
            });
        }

        _dbContext.JournalEntries.Add(je);

        // 4. Outbox Event
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.fi.posted",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = je.Id,
                DocumentNumber = je.DocumentNumber,
                TotalAmount = totalDebit,
                AggregateType = "JournalEntry",
                AggregateId = je.Id.ToString()
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "FI", "post-je", new { Id = je.Id, DocumentNumber = je.DocumentNumber });
    }
}

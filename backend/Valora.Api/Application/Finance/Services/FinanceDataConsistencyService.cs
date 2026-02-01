using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Projections;
using System.Text.Json;

namespace Valora.Api.Application.Finance.Services;

public class FinanceDataConsistencyService
{
    private readonly PlatformDbContext _dbContext;
    private readonly ProjectionManager _projectionManager;
    private readonly ILogger<FinanceDataConsistencyService> _logger;

    public FinanceDataConsistencyService(
        PlatformDbContext dbContext, 
        ProjectionManager projectionManager,
        ILogger<FinanceDataConsistencyService> logger)
    {
        _dbContext = dbContext;
        _projectionManager = projectionManager;
        _logger = logger;
    }

    public async Task HandleGLAccountUpdatedAsync(Guid glAccountId)
    {
        _logger.LogInformation("Identifying dependents for GL Account {GLAccountId}...", glAccountId);

        // 1. Journal Entries
        await UpdateDependentJournalEntries(glAccountId);

        // 2. Future: Budgets
        // await UpdateDependentBudgets(glAccountId);

        // 3. Future: Recurring Entries
        // await UpdateDependentRecurringEntries(glAccountId);
    }

    private async Task UpdateDependentJournalEntries(Guid glAccountId)
    {
        // Find all Journal Entries that use this GL Account
        var journalEntryIds = await _dbContext.JournalEntryLines
            .Where(l => l.GLAccountId == glAccountId)
            .Select(l => l.JournalEntryId)
            .Distinct()
            .ToListAsync();

        if (!journalEntryIds.Any()) return;

        _logger.LogInformation("Found {Count} Journal Entries affected by GL Account update. Triggering reprojection...", journalEntryIds.Count);

        foreach (var jeId in journalEntryIds)
        {
            var payload = new 
            {
                AggregateType = "JournalEntry",
                AggregateId = jeId.ToString()
            };
            
            // We reuse the generic projection manager to "Refresh" the Read Model
            await _projectionManager.HandleEventAsync("valora.fi.propagation", jeId.ToString(), JsonSerializer.Serialize(payload));
        }
    }
}

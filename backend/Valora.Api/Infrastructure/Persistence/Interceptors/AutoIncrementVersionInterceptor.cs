using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Valora.Api.Domain.Common;

namespace Valora.Api.Infrastructure.Persistence.Interceptors;

public class AutoIncrementVersionInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        IncrementVersion(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        IncrementVersion(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void IncrementVersion(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Added)
            {
                // For Modified: Increment Version
                // For Added: Initialize (usually 0 or 1, but db default might handle it. Let's start at 1)
                
                if (entry.State == EntityState.Added)
                {
                    // entry.Entity.Version = 1; // Optional, can rely on default
                }
                else
                {
                    // EF Core Optimistic Concurrency check happens BEFORE this if we use [ConcurrencyCheck]
                    // Wait, [ConcurrencyCheck] compares the LOADED value with the DB value.
                    // If we change the value HERE, does it affect the WHERE clause?
                    // Answer: No, the WHERE clause uses 'OriginalValue'.
                    // So we update 'CurrentValue' here to be X + 1.
                    
                    entry.Entity.Version++;
                }
            }
        }
    }
}

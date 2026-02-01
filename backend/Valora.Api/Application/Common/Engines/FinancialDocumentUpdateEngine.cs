using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Common;
using Valora.Api.Infrastructure.Persistence;
using System.Text.Json;
using Valora.Api.Domain.Entities;

namespace Valora.Api.Application.Common.Engines;

public abstract class FinancialDocumentUpdateEngine<TDoc, TLine, TCommand> : IRequestHandler<TCommand, ApiResult>
    where TDoc : class, IAggregateRoot
    where TLine : class
    where TCommand : IRequest<ApiResult>
{
    protected readonly PlatformDbContext _dbContext;

    protected FinancialDocumentUpdateEngine(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        int maxRetries = 1;
        int attempts = 0;

        while (true)
        {
            // Explicit Transaction for Atomicity (Rule #7)
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Hook: Load Document (Rule #3 - Attempt 0)
                var doc = await LoadDocumentAsync(request, cancellationToken);
                
                if (doc == null)
                {
                    return BuildNotFoundResult(request);
                }

                // Hook: Validate (Rule #9)
                var validationResult = Validate(doc, request);
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                if (attempts == 0)
                {
                    // --- ATTEMPT 0: STANDARD EF CORE (Optimistic) ---
                    
                    // Hook: Apply Strict Concurrency (Rule #3)
                    ApplyStrictConcurrency(doc, request);

                    // Hook: Apply Tracked Update (Rule #3)
                    ApplyTrackedUpdate(doc, request);

                    // Hook: Replace Lines (Rule #3 - EF Core way for Attempt 0 is acceptable if bulk delete is used, 
                    // but Policy says "ReplaceLines" hook. We will follow Attempt 0 logic: "Apply changes using EF tracked entities")
                    // Actually, Policy Rule #3 says Attempt 0: "Apply changes using EF tracked entities".
                    // But Rule #5 says "NEVER rely on EF cascade delete". 
                    // So we should probably use ExecuteDeleteAsync even in Attempt 0 for lines to be safe/consistent?
                    // Policy says Attempt 0: "Apply changes using EF tracked entities". 
                    // Let's stick to standard EF for Attempt 0 as much as possible, OR consistency.
                    // However, Rule #3 Attempt 1 says "ExecuteDeleteAsync". 
                    // Let's implement a virtual method for replacing lines that can be overridden or used in both.
                    
                    // For Attempt 0, we'll assume the concrete class handles line changes via EF tracking 
                    // OR we can force the "Bulk Delete + Insert" pattern even in Attempt 0 if it's cleaner.
                    // The Policy Attempt 0 says "Apply changes using EF tracked entities". 
                    // This implies we modify the collection.
                    
                    await ReplaceLinesAsync(doc, request, attempt: 0, cancellationToken);

                    // Hook: Add Outbox (Rule #6)
                    AddOutbox(doc, request);

                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    // --- ATTEMPT 1: AUTHORITATIVE / LAST-WRITE-WINS (Rule #3) ---
                    
                    // 1. Authoritative Header Update (ExecuteUpdateAsync) (Rule #3, #4)
                    // This bypasses EF concurrency tokens and increments Version.
                    await ApplyAuthoritativeUpdateAsync(doc, request, cancellationToken);

                    // 2. Detach Header to prevent EF from interfering
                    _dbContext.Entry(doc).State = EntityState.Detached;

                    // 3. Replace Lines (ExecuteDeleteAsync + Inserts) (Rule #3, #5)
                    await ReplaceLinesAsync(doc, request, attempt: 1, cancellationToken);

                    // 4. Add Outbox (Rule #6)
                    // Since we detached the doc, we need to add the outbox message to the context
                    AddOutbox(doc, request);

                    // 5. Save Changes (for Lines and Outbox only)
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Rule #8: Reload if needed, or return built result
                return BuildSuccessResult(doc, request);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Rule #3: If DbUpdateConcurrencyException occurs, Clear ChangeTracker, Retry once
                await transaction.RollbackAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();

                attempts++;
                if (attempts > maxRetries)
                {
                    throw; // Should not happen if logic is correct, but fail safe
                }
                
                // Continue loop to Attempt 1
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    // Abstract Hooks
    protected abstract Task<TDoc?> LoadDocumentAsync(TCommand request, CancellationToken cancellationToken);
    protected abstract ApiResult BuildNotFoundResult(TCommand request);
    protected abstract ApiResult Validate(TDoc doc, TCommand request);
    protected abstract void ApplyStrictConcurrency(TDoc doc, TCommand request);
    protected abstract void ApplyTrackedUpdate(TDoc doc, TCommand request);
    protected abstract Task ApplyAuthoritativeUpdateAsync(TDoc doc, TCommand request, CancellationToken cancellationToken);
    protected abstract Task ReplaceLinesAsync(TDoc doc, TCommand request, int attempt, CancellationToken cancellationToken);
    protected abstract void AddOutbox(TDoc doc, TCommand request);
    protected abstract ApiResult BuildSuccessResult(TDoc doc, TCommand request);
}

using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Common;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Common.Commands;

public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}

public abstract class UpdateAggregateCommandHandler<TCommand, TAggregate> : IRequestHandler<TCommand, ApiResult>
    where TCommand : IRequest<ApiResult>, IIdempotentCommand
    where TAggregate : class, IAggregateRoot
{
    protected readonly PlatformDbContext _dbContext;

    protected UpdateAggregateCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        // 1. Idempotency Check (Placeholder)
        // Real implementation would check a dedicated Idempotency table.
        
        // 2. Retry Policy (Last Write Wins Strategy)
        // We attempt to update. If we hit a concurrency exception (meaning Header Version mismatch),
        // we assume "Last Write Wins" and retry the operation with the NEW version.
        // This effectively overwrites the previous user's changes.
        
        int maxRetries = 3;
        int attempts = 0;

        while (true)
        {
            // Explicit Transaction to ensure atomicity of Bulk Delete + Header Update
            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var result = await ExecuteUpdateAsync(request, cancellationToken, attempts);
                
                // If success, commit transaction
                await transaction.CommitAsync(cancellationToken);
                
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Rollback is automatic on dispose, but good to be explicit if logic requires
                await transaction.RollbackAsync(cancellationToken);

                attempts++;
                if (attempts > maxRetries)
                {
                    // If we exhausted retries, return failure
                    return HandleConcurrencyException(ex, request);
                }
                
                // If retry allowed, clear change tracker and try again
                _dbContext.ChangeTracker.Clear();
                
                // Add random jitter to avoid lock-step collisions (Exponential Backoff)
                int delay = new Random().Next(10, 50 * attempts);
                await Task.Delay(delay, cancellationToken);
                
                // Continue loop
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    protected abstract Task<ApiResult> ExecuteUpdateAsync(TCommand request, CancellationToken cancellationToken, int attempt);

    protected virtual ApiResult HandleConcurrencyException(DbUpdateConcurrencyException ex, TCommand request)
    {
        // Assuming ApiResult.Fail requires (TenantId, Module, Action, Error)
        // Since we don't have TenantId/Module easily accessible in the generic base without constraints or reflection,
        // we might need to abstract this or use a simpler Fail if available.
        // Let's assume we can get TenantId if TCommand has it (it doesn't enforce it yet).
        
        // Quick fix: Use a placeholder or reflection if needed, but better to enforce ITenantCommand.
        // For now, I'll pass empty strings or nulls if allowed, OR (better) I'll assume the command has TenantId
        // by making TCommand implement a tenant interface.
        
        // BUT, to fix the compilation quickly matching the previous code:
        // return ApiResult.Fail(request.TenantId, "FI", "update-je", ...);
        
        // I will use reflection to get TenantId safely or default to "Unknown".
        string tenantId = "Unknown";
        var prop = typeof(TCommand).GetProperty("TenantId");
        if (prop != null) tenantId = prop.GetValue(request)?.ToString() ?? "Unknown";

        return ApiResult.Fail(tenantId, "Generic", "Update", new ApiError("Concurrency", "The record has been modified by another user. Please refresh and try again."));
    }
}

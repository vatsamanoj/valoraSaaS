using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Infrastructure.Persistence;
using System.Text.Json;

namespace Valora.Api.Application.Finance.Commands.UpdateGLAccount;

public class UpdateGLAccountCommandHandler : IRequestHandler<UpdateGLAccountCommand, ApiResult>
{
    private readonly PlatformDbContext _context;

    public UpdateGLAccountCommandHandler(PlatformDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResult> Handle(UpdateGLAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _context.GLAccounts
            .FirstOrDefaultAsync(a => a.Id == request.Id && a.TenantId == request.TenantId, cancellationToken);

        if (account == null)
        {
            return ApiResult.Fail(request.TenantId, "FI", "update-gl", new ApiError("Not_Found", "GL Account not found", null));
        }

        // Parse Enum safely
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
        {
             return ApiResult.Fail(request.TenantId, "FI", "update-gl", new ApiError("Invalid_Type", $"Invalid Account Type: {request.Type}", null));
        }

        // Update fields
        account.Name = request.Name;
        account.Type = accountType;
        account.IsActive = request.IsActive;
        account.UpdatedBy = request.UpdatedBy;
        account.UpdatedAt = DateTime.UtcNow;

        // Add Outbox Message
        var eventPayload = new
        {
            AggregateId = account.Id.ToString(),
            AggregateType = "GLAccount",
            EventType = "GLAccountUpdated",
            TenantId = request.TenantId,
            Data = new
            {
                account.Id,
                account.AccountCode,
                account.Name,
                Type = account.Type.ToString(),
                account.IsActive
            }
        };

        _context.OutboxMessages.Add(new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.fi.masterdata",
            // Key property is missing in OutboxMessageEntity, so we rely on Payload or Topic convention
            // Or if Topic supports partitioning by Key, it might be in a different field. 
            // Checking entity definition: Key is NOT present. So we omit it.
            Payload = JsonSerializer.Serialize(eventPayload),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "FI", "update-gl", new { account.Id, account.AccountCode });
    }
}

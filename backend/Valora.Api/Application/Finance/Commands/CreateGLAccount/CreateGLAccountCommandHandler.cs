using Lab360.Application.Common.Results;
using MediatR;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Finance.Commands.CreateGLAccount;

public class CreateGLAccountCommandHandler : IRequestHandler<CreateGLAccountCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<CreateGLAccountCommandHandler> _logger;

    public CreateGLAccountCommandHandler(PlatformDbContext dbContext, ILogger<CreateGLAccountCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(CreateGLAccountCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
        {
            return ApiResult.Fail(request.TenantId, "FI", "create-gl", new ApiError("Validation", "Invalid Account Type"));
        }

        // Check uniqueness
        // Since we have Unique Index on (TenantId, AccountCode), EF would throw, but better to check manually for nice error
        var exists = _dbContext.GLAccounts.Any(x => x.TenantId == request.TenantId && x.AccountCode == request.AccountCode);
        if (exists)
        {
            return ApiResult.Fail(request.TenantId, "FI", "create-gl", new ApiError("Duplicate", "Account Code already exists"));
        }

        var account = new GLAccount
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            AccountCode = request.AccountCode,
            Name = request.Name,
            Type = accountType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId
        };

        _dbContext.GLAccounts.Add(account);

        // Outbox for Traceability
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.fi.gl.created",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Id = account.Id,
                AccountCode = account.AccountCode,
                AggregateType = "GLAccount",
                AggregateId = account.Id.ToString()
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "FI", "create-gl", new { Id = account.Id });
    }
}

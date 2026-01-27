using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;
using Valora.Api.Domain.Entities;

namespace Valora.Api.Application.Dynamic.Commands.DeleteEntity;

public class DeleteEntityCommandHandler : IRequestHandler<DeleteEntityCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<DeleteEntityCommandHandler> _logger;

    public DeleteEntityCommandHandler(
        PlatformDbContext dbContext,
        ILogger<DeleteEntityCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(DeleteEntityCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return ApiResult.Fail(request.TenantId, request.Module ?? "unknown", "delete", new ApiError("Validation", "Module is required."));
        }

        if (!Guid.TryParse(request.Id, out var id))
        {
             return ApiResult.Fail(request.TenantId, request.Module, "delete", new ApiError("Validation", "Invalid ID format"));
        }

        var entity = await _dbContext.ObjectRecords
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == request.TenantId, cancellationToken);

        if (entity == null)
        {
            return ApiResult.Fail(request.TenantId, request.Module, "delete", new ApiError("NotFound", "Entity not found or access denied"));
        }

        _dbContext.ObjectRecords.Remove(entity);
        // Attributes should cascade delete if DB is configured, but EF Core will also try to delete related entities if loaded.
        // Since we didn't load attributes, EF Core relies on DB Cascade Delete OR we need to load them.
        // EAV tables usually should have FK with ON DELETE CASCADE.
        // If migration created FKs properly, we are good.
        
        // Let's add Outbox Message for Deletion
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.data.changed",
            Payload = System.Text.Json.JsonSerializer.Serialize(new 
            { 
                Id = id, 
                ModuleCode = request.Module,
                AggregateType = request.Module, // Traceability
                AggregateId = id.ToString(),    // Traceability
                Type = "Delete" 
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, request.Module, "delete", new { Id = request.Id });
    }
}
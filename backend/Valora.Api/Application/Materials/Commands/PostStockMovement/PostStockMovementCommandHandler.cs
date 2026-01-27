using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Materials;
using Valora.Api.Domain.Events;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Materials.Commands.PostStockMovement;

public class PostStockMovementCommandHandler : IRequestHandler<PostStockMovementCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public PostStockMovementCommandHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(PostStockMovementCommand request, CancellationToken cancellationToken)
    {
        var material = await _dbContext.MaterialMasters
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId && m.TenantId == request.TenantId, cancellationToken);

        if (material == null)
        {
            return ApiResult.Fail(request.TenantId, "MM", "post-movement", new ApiError("NotFound", "Material not found."));
        }

        // Calculate Stock Impact
        decimal stockChange = request.Quantity;
        if (request.MovementType == MovementType.GoodsIssue)
        {
            stockChange = -request.Quantity;
        }

        // Validate Stock for Issues
        if (request.MovementType == MovementType.GoodsIssue && (material.CurrentStock + stockChange) < 0)
        {
            return ApiResult.Fail(request.TenantId, "MM", "post-movement", new ApiError("Stock", "Insufficient stock."));
        }

        // Create Movement Record
        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            MaterialId = request.MaterialId,
            MovementType = request.MovementType,
            Quantity = request.Quantity,
            MovementDate = request.MovementDate
        };

        // Update Material Stock
        material.CurrentStock += stockChange;

        _dbContext.StockMovements.Add(movement);
        _dbContext.MaterialMasters.Update(material);

        // Outbox Event for FI Posting (MM-FI Integration)
        var evt = new StockMovementPostedEvent
        {
            TenantId = request.TenantId,
            AggregateId = movement.Id.ToString(),
            MovementId = movement.Id,
            MaterialId = movement.MaterialId,
            MovementType = (int)movement.MovementType,
            Quantity = movement.Quantity,
            StockValue = movement.Quantity * material.StandardPrice,
            MovementDate = movement.MovementDate
        };

        _dbContext.OutboxMessages.Add(new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.mm.stock_moved",
            Payload = JsonSerializer.Serialize(evt),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "MM", "post-movement", movement.Id);
    }
}

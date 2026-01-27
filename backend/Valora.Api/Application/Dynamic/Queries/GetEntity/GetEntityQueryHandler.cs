using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Dynamic.Queries.GetEntity;

public class GetEntityQueryHandler : IRequestHandler<GetEntityQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public GetEntityQueryHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(GetEntityQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            return ApiResult.Fail(request.TenantId, request.Module, "get-by-id", new ApiError("Validation", "Invalid ID format"));
        }

        // 1. Get Object Definition
        var definition = await _dbContext.ObjectDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == request.TenantId && d.ObjectCode == request.Module, cancellationToken);

        if (definition == null)
        {
            // Fallback for backward compatibility or direct table query if needed?
            // For now, EAV requires definition.
            return ApiResult.Fail(request.TenantId, request.Module, "get-by-id", new ApiError("NotFound", "Module definition not found"));
        }

        // 2. Get Record
        var record = await _dbContext.ObjectRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == request.TenantId && r.ObjectDefinitionId == definition.Id, cancellationToken);

        if (record == null)
        {
            return ApiResult.Fail(request.TenantId, request.Module, "get-by-id", new ApiError("NotFound", $"Document with id {request.Id} not found"));
        }

        // 3. Get Attributes (Join with Fields)
        var attributes = await _dbContext.ObjectRecordAttributes
            .Include(a => a.Field)
            .Where(a => a.RecordId == id)
            .ToListAsync(cancellationToken);

        // 4. Pivot to Dictionary
        var dict = new Dictionary<string, object>
        {
            { "Id", record.Id },
            { "TenantId", record.TenantId },
            { "CreatedAt", record.CreatedAt },
            { "CreatedBy", record.CreatedBy },
            { "UpdatedAt", record.UpdatedAt },
            { "UpdatedBy", record.UpdatedBy }
        };

        foreach (var attr in attributes)
        {
            var fieldName = attr.Field.FieldName;
            object? value = null;

            if (attr.ValueText != null) value = attr.ValueText;
            else if (attr.ValueNumber != null) value = attr.ValueNumber;
            else if (attr.ValueDate != null) value = attr.ValueDate;
            else if (attr.ValueBoolean != null) value = attr.ValueBoolean;

            dict[fieldName] = value;
        }

        return ApiResult.Ok(request.TenantId, request.Module, "get-by-id", dict);
    }
}
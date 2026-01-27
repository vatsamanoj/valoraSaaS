using System.Text.Json;
using System.Text.Json.Nodes;
using Lab360.Application.Common.Results;
using Lab360.Application.Schemas;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Valora.Api.Application.Services;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;

namespace Valora.Api.Application.Dynamic.Commands.UpdateEntity;

public class UpdateEntityCommandHandler : IRequestHandler<UpdateEntityCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ISchemaProvider _schemaProvider;
    private readonly ILogger<UpdateEntityCommandHandler> _logger;

    public UpdateEntityCommandHandler(
        PlatformDbContext dbContext,
        ISchemaProvider schemaProvider,
        ILogger<UpdateEntityCommandHandler> logger)
    {
        _dbContext = dbContext;
        _schemaProvider = schemaProvider;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(UpdateEntityCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return ApiResult.Fail(request.TenantId, request.Module ?? "unknown", "update", new ApiError("Validation", "Module is required."));
        }

        if (!Guid.TryParse(request.Id, out var id))
        {
            return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("Validation", "Invalid ID format"));
        }

        // 1. Get Record
        var record = await _dbContext.ObjectRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == request.TenantId, cancellationToken);

        if (record == null)
        {
            return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("NotFound", "Entity not found"));
        }

        // 2. Get Definition to know fields
        var definition = await _dbContext.ObjectDefinitions
            .Include(d => d.ObjectFields) // Assuming navigation property exists or we query separately
            .FirstOrDefaultAsync(d => d.Id == record.ObjectDefinitionId, cancellationToken);

        if (definition == null) throw new Exception("Definition not found for record");

        // We need ObjectFields. If navigation property not added, query them.
        // I didn't add navigation property 'ObjectFields' to ObjectDefinition in the entity file explicitly? 
        // I added [ForeignKey] in ObjectField, so EF Core might have inferred collection if I added it.
        // Let's query them manually to be safe.
        var fields = await _dbContext.ObjectFields
            .Where(f => f.ObjectDefinitionId == definition.Id)
            .ToListAsync(cancellationToken);
        
        var fieldMap = fields.ToDictionary(f => f.FieldName);

        // 3. Parse Updates
        var updatesJson = JsonNode.Parse(request.Body.GetRawText())?.AsObject();
        if (updatesJson == null) return ApiResult.Ok(request.TenantId, request.Module, "update", new { });

        // Remove immutable
        updatesJson.Remove("Id");
        updatesJson.Remove("TenantId");
        updatesJson.Remove("CreatedAt");
        updatesJson.Remove("CreatedBy");

        // 4. Update Attributes
        foreach (var kvp in updatesJson)
        {
            if (fieldMap.TryGetValue(kvp.Key, out var fieldDef))
            {
                var attr = await _dbContext.ObjectRecordAttributes
                    .FirstOrDefaultAsync(a => a.RecordId == id && a.FieldId == fieldDef.Id, cancellationToken);

                if (attr == null)
                {
                    attr = new ObjectRecordAttribute
                    {
                        Id = Guid.NewGuid(),
                        RecordId = id,
                        FieldId = fieldDef.Id
                    };
                    _dbContext.ObjectRecordAttributes.Add(attr);
                }

                // Update Value based on Type
                var val = kvp.Value;
                // Reset all
                attr.ValueText = null;
                attr.ValueNumber = null;
                attr.ValueDate = null;
                attr.ValueBoolean = null;

                if (val != null)
                {
                    try 
                    {
                        if (fieldDef.DataType == "Number") attr.ValueNumber = val.GetValue<decimal>();
                        else if (fieldDef.DataType == "Boolean") attr.ValueBoolean = val.GetValue<bool>();
                        else if (fieldDef.DataType == "Date") attr.ValueDate = val.GetValue<DateTime>();
                        else attr.ValueText = val.ToString();
                    }
                    catch
                    {
                        // Fallback to text if conversion fails
                        attr.ValueText = val.ToString();
                    }
                }
            }
        }

        // 5. Update Audit
        record.UpdatedAt = DateTime.UtcNow;
        record.UpdatedBy = request.UserId;

        // 6. Outbox Message
        // For EAV, reconstructing full payload for outbox is expensive (fetching all attrs).
        // We can either send just the updates (delta) or full snapshot.
        // Let's send the updates for now + ID.
        var outboxPayload = new
        {
            Id = id,
            ModuleCode = request.Module,
            AggregateType = request.Module, // Traceability
            AggregateId = id.ToString(),    // Traceability
            Data = updatesJson, // Sending delta
            TenantId = request.TenantId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = request.UserId,
            Type = "Update"
        };

        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.data.changed",
            Payload = JsonSerializer.Serialize(outboxPayload),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Return the updates or full object? Usually full object is better but updates is faster.
        // Let's return updates.
        return ApiResult.Ok(request.TenantId, request.Module, "update", updatesJson);
    }
}
using System.Text.Json;
using System.Text.Json.Nodes;
using Lab360.Application.Common.Results;
using Valora.Api.Application.Schemas;
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
    private readonly ILogger<UpdateEntityCommandHandler> _logger;
    private readonly IServiceProvider _serviceProvider;

    public UpdateEntityCommandHandler(
        PlatformDbContext dbContext,
        ILogger<UpdateEntityCommandHandler> logger,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
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

        // --- HYBRID WRITE SUPPORT (SalesOrder) ---
        if (string.Equals(request.Module, "SalesOrder", StringComparison.OrdinalIgnoreCase))
        {
            // 1. Get SQL Record
            var so = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == request.TenantId, cancellationToken);

            if (so == null)
            {
                return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("NotFound", "SalesOrder not found"));
            }

            // 2. Parse Body
            var rawJsonSo = request.Body.GetRawText();
            var soJson = JsonNode.Parse(rawJsonSo)?.AsObject();
            if (soJson != null && soJson.ContainsKey("Data") && soJson["Data"] is JsonObject dataObjSo) soJson = dataObjSo;
            if (soJson == null) return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("Validation", "Body is required"));

            // Helper
            string? GetString(string key) {
                var match = soJson.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.OrdinalIgnoreCase));
                return match.Value?.ToString();
            }
            // Removed unused GetDecimal function

            // 3. Update SQL Fields
            var customerId = GetString("CustomerId");
            if (customerId != null) so.CustomerId = customerId;
            
            var currency = GetString("Currency");
            if (currency != null) so.Currency = currency;
            
            // Note: Items update is complex (diffing), for now we might skip or replace.
            // Let's assume for this fix we only update header fields or handle simple item replacement if provided.
            
            // 4. Update Audit
            so.UpdatedAt = DateTime.UtcNow;
            so.UpdatedBy = request.UserId;

            // 5. Save SQL
            // (EF Core tracks changes)
            
            // 6. EAV Update (for custom fields)
            // We reuse the generic logic below but targeting the same ID.
            // However, the generic logic expects an ObjectRecord to exist.
            // CreateEntityCommandHandler CREATES an ObjectRecord for SalesOrder too!
            // So we can let the flow continue to EAV update?
            
            // YES, continue to generic flow to update ObjectRecord attributes.
            // BUT, we must ensure we don't return early if ObjectRecord is missing? 
            // CreateEntity creates it, so it should be there.
            
            // If we just save changes here, we might have a race condition or double save.
            // Let's just save the SQL part here.
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 1. Get Record (EAV)
        var record = await _dbContext.ObjectRecords
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == request.TenantId, cancellationToken);

        if (record == null)
        {
            // Self-Healing: If SQL record exists (SalesOrder) but EAV record is missing, create it.
            if (string.Equals(request.Module, "SalesOrder", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Self-Healing: ObjectRecord missing for SalesOrder {Id}. Creating it.", id);
                
                // Need Definition
                var soDef = await _dbContext.ObjectDefinitions
                    .FirstOrDefaultAsync(d => d.TenantId == request.TenantId && d.ObjectCode == request.Module, cancellationToken);
                    
                if (soDef != null)
                {
                    record = new ObjectRecord
                    {
                        Id = id,
                        TenantId = request.TenantId,
                        ObjectDefinitionId = soDef.Id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = request.UserId,
                        UpdatedAt = DateTime.UtcNow,
                        UpdatedBy = request.UserId
                    };
                    _dbContext.ObjectRecords.Add(record);
                    // Continue to update attributes...
                }
                else
                {
                    // Attempt Auto-Sync of Definition
                    _logger.LogWarning("Self-Healing: ObjectDefinition missing for {Module}. Attempting to sync.", request.Module);
                    
                    var schemaProvider = _serviceProvider.GetRequiredService<Valora.Api.Application.Schemas.ISchemaProvider>();
                    // Trigger sync/seed
                    await schemaProvider.GetSchemaAsync(request.TenantId, request.Module, cancellationToken);
                    
                    // Retry fetch
                    soDef = await _dbContext.ObjectDefinitions
                        .FirstOrDefaultAsync(d => d.TenantId == request.TenantId && d.ObjectCode == request.Module, cancellationToken);

                    if (soDef != null)
                    {
                         record = new ObjectRecord
                         {
                             Id = id,
                             TenantId = request.TenantId,
                             ObjectDefinitionId = soDef.Id,
                             CreatedAt = DateTime.UtcNow,
                             CreatedBy = request.UserId,
                             UpdatedAt = DateTime.UtcNow,
                             UpdatedBy = request.UserId
                         };
                         _dbContext.ObjectRecords.Add(record);
                    }
                    else
                    {
                        return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("NotFound", "Entity not found (and Schema Definition missing)"));
                    }
                }
            }
            else
            {
                return ApiResult.Fail(request.TenantId, request.Module, "update", new ApiError("NotFound", "Entity not found"));
            }
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
        var rawJson = request.Body.GetRawText();
        var updatesJson = JsonNode.Parse(rawJson)?.AsObject();

        // Handle Action/Data wrapper (GenericObjectForm behavior)
        if (updatesJson != null && updatesJson.ContainsKey("Data") && updatesJson["Data"] is JsonObject dataObj)
        {
            updatesJson = dataObj;
        }

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
                        else if (fieldDef.DataType == "Date") 
                        {
                            // Robust date parsing
                            DateTime dt = DateTime.UtcNow;
                            try 
                            {
                                dt = val.GetValue<DateTime>();
                            }
                            catch
                            {
                                if (DateTime.TryParse(val.ToString(), out var parsed)) dt = parsed;
                            }
                            attr.ValueDate = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                        }
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
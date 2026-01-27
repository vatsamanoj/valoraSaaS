using System.Text.Json;
using System.Text.Json.Nodes;
using Lab360.Application.Common.Results;
using Lab360.Application.Schemas;
using MediatR;
using Valora.Api.Application.Services;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Valora.Api.Infrastructure.Services;

namespace Valora.Api.Application.Dynamic.Commands.CreateEntity;

public class CreateEntityCommandHandler : IRequestHandler<CreateEntityCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<CreateEntityCommandHandler> _logger;
    private readonly ISchemaProvider _schemaProvider;
    private readonly SchemaValidator _validator;

    public CreateEntityCommandHandler(
        PlatformDbContext dbContext,
        ILogger<CreateEntityCommandHandler> logger,
        ISchemaProvider schemaProvider,
        SchemaValidator validator)
    {
        _dbContext = dbContext;
        _logger = logger;
        _schemaProvider = schemaProvider;
        _validator = validator;
    }

    public async Task<ApiResult> Handle(CreateEntityCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return ApiResult.Fail(request.TenantId, request.Module ?? "unknown", "create", new ApiError("Validation", "Module is required."));
        }

        // 1. Fetch Schema from Mongo
        var schema = await _schemaProvider.GetSchemaAsync(request.TenantId, request.Module, cancellationToken);
        if (schema == null)
        {
            return ApiResult.Fail(request.TenantId, request.Module, "create", new ApiError("Validation", $"Schema not found for module '{request.Module}'"));
        }

        // 2. Validate
        var id = Guid.NewGuid();
        var errors = _validator.Validate(request.Body.GetRawText(), schema);

        if (errors.Any())
        {
            return ApiResult.Fail(request.TenantId, request.Module, "create", new ApiError("Validation", string.Join("; ", errors)));
        }

        // 3. Get Object Definition (EAV Metadata)
        var definition = await _dbContext.ObjectDefinitions
            .Include(d => d.ObjectFields)
            .FirstOrDefaultAsync(d => d.TenantId == request.TenantId && d.ObjectCode == request.Module, cancellationToken);

        if (definition == null)
        {
            return ApiResult.Fail(request.TenantId, request.Module, "create", new ApiError("Config", "Object definition not synced. Please publish the schema first."));
        }

        // 4. Create Record
        var record = new ObjectRecord
        {
            Id = id,
            TenantId = request.TenantId,
            ObjectDefinitionId = definition.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = request.UserId
        };
        _dbContext.ObjectRecords.Add(record);

        // 5. Create Attributes
        var jsonNode = JsonNode.Parse(request.Body.GetRawText())?.AsObject();
        if (jsonNode != null)
        {
            foreach (var field in schema.Fields)
            {
                var key = field.Key;
                if (!jsonNode.ContainsKey(key)) continue;

                var valNode = jsonNode[key];
                if (valNode == null) continue;

                var objectField = definition.ObjectFields.FirstOrDefault(f => f.FieldName == key);
                if (objectField == null) continue; // Field in schema but not in EAV metadata (sync lag?)

                var attr = new ObjectRecordAttribute
                {
                    Id = Guid.NewGuid(),
                    RecordId = id,
                    FieldId = objectField.Id
                };

                if (objectField.DataType == "Number") attr.ValueNumber = valNode.GetValue<decimal>();
                else if (objectField.DataType == "Boolean") attr.ValueBoolean = valNode.GetValue<bool>();
                else if (objectField.DataType == "Date") attr.ValueDate = valNode.GetValue<DateTime>();
                else attr.ValueText = valNode.ToString();

                _dbContext.ObjectRecordAttributes.Add(attr);
            }
        }

        // 6. Add Outbox Message
        var outboxPayload = new
        {
            Id = id,
            ModuleCode = request.Module,
            AggregateType = request.Module, // Traceability
            AggregateId = id.ToString(),    // Traceability
            Data = jsonNode,
            TenantId = request.TenantId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = request.UserId,
            Type = "Create"
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
        _logger.LogInformation("Entity created successfully: {Id}", id);
        return ApiResult.Ok(request.TenantId, request.Module, "create", outboxPayload);
    }
}
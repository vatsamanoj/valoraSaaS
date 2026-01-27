using Lab360.Application.Schemas;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Services;

public interface ISchemaSyncService
{
    Task SyncTableAsync(string tenantId, ModuleSchema schema);
}

public class SchemaSyncService : ISchemaSyncService
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<SchemaSyncService> _logger;

    public SchemaSyncService(PlatformDbContext dbContext, ILogger<SchemaSyncService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SyncTableAsync(string tenantId, ModuleSchema schema)
    {
        _logger.LogInformation("Syncing schema metadata for module {Module} (Tenant: {Tenant})", schema.Module, tenantId);

        // 1. Sync ObjectDefinition
        var definition = await _dbContext.ObjectDefinitions
            .FirstOrDefaultAsync(d => d.TenantId == tenantId && d.ObjectCode == schema.Module);

        if (definition == null)
        {
            definition = new ObjectDefinition
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ObjectCode = schema.Module,
                Version = schema.Version,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            _dbContext.ObjectDefinitions.Add(definition);
        }
        else
        {
            definition.Version = schema.Version;
            definition.UpdatedAt = DateTime.UtcNow;
            definition.UpdatedBy = "System";
        }

        await _dbContext.SaveChangesAsync();

        // 2. Sync ObjectFields
        var existingFields = await _dbContext.ObjectFields
            .Where(f => f.ObjectDefinitionId == definition.Id && f.TenantId == tenantId)
            .ToListAsync();

        foreach (var field in schema.Fields)
        {
            var rule = field.Value;
            var fieldName = field.Key;
            
            var existingField = existingFields.FirstOrDefault(f => f.FieldName == fieldName);

            if (existingField == null)
            {
                var newField = new ObjectField
                {
                    Id = Guid.NewGuid(),
                    ObjectDefinitionId = definition.Id,
                    TenantId = tenantId,
                    FieldName = fieldName,
                    DataType = MapUiTypeToDataType(rule.Ui?.Type ?? "text"),
                    IsRequired = rule.Required,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "System"
                };
                _dbContext.ObjectFields.Add(newField);
            }
            else
            {
                existingField.DataType = MapUiTypeToDataType(rule.Ui?.Type ?? "text");
                existingField.IsRequired = rule.Required;
                existingField.UpdatedAt = DateTime.UtcNow;
                existingField.UpdatedBy = "System";
            }
        }

        // Note: We are currently NOT deleting fields that are removed from schema to preserve data.
        // If strict sync is needed, we would delete fields not present in schema.Fields.

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Schema metadata sync completed for {Module}", schema.Module);
    }

    private string MapUiTypeToDataType(string uiType)
    {
        return uiType.ToLower() switch
        {
            "number" => "Number",
            "decimal" => "Number",
            "date" => "Date",
            "boolean" => "Boolean",
            _ => "Text"
        };
    }
}
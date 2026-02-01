using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Common;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Domain.Entities.Materials;
using Valora.Api.Domain.Entities.HumanCapital;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Projections;

public class ProjectionManager
{
    private readonly PlatformDbContext _dbContext;
    private readonly MongoProjectionRepository _mongoRepo;
    private readonly ILogger<ProjectionManager> _logger;
    
    // Cache for Type lookups
    private static readonly Dictionary<string, Type> _typeCache = new();

    public ProjectionManager(PlatformDbContext dbContext, MongoProjectionRepository mongoRepo, ILogger<ProjectionManager> logger)
    {
        _dbContext = dbContext;
        _mongoRepo = mongoRepo;
        _logger = logger;
    }

    public async Task HandleEventAsync(string topic, string key, string payload)
    {
        // We expect payload to have AggregateType and AggregateId
        // Example: { "AggregateType": "GLAccount", "AggregateId": "...", ... }
        
        try 
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (!root.TryGetProperty("AggregateType", out var typeProp) || 
                !root.TryGetProperty("AggregateId", out var idProp))
            {
                // Try camelCase fallback
                var hasType = root.TryGetProperty("AggregateType", out typeProp) || root.TryGetProperty("aggregateType", out typeProp);
                var hasId = root.TryGetProperty("AggregateId", out idProp) || root.TryGetProperty("aggregateId", out idProp);

                if (!hasType || !hasId)
                {
                    _logger.LogWarning("Event payload missing AggregateType or AggregateId. Payload: {Payload}", payload);
                    return;
                }
            }

            var aggregateType = typeProp.GetString();
            var aggregateId = idProp.GetString();

            if (string.IsNullOrEmpty(aggregateType) || string.IsNullOrEmpty(aggregateId)) return;

            // Resolve Type
            var entityType = GetEntityType(aggregateType);
            if (entityType == null)
            {
                _logger.LogWarning("Could not resolve Entity Type for {AggregateType}", aggregateType);
                return;
            }

            object? entity = null;
            var guidId = Guid.Parse(aggregateId);

            // Fetch from SQL with specific Includes for Aggregates
            if (entityType == typeof(JournalEntry))
            {
                entity = await _dbContext.JournalEntries
                    .AsNoTracking() // Ensure fresh read from DB, bypassing any stale cache
                    .Include(je => je.Lines)
                        .ThenInclude(l => l.GLAccount)
                    .FirstOrDefaultAsync(je => je.Id == guidId);
            }
            else if (entityType == typeof(EmployeePayroll))
            {
                entity = await _dbContext.EmployeePayrolls
                    .AsNoTracking()
                    .Include(ep => ep.Employee)
                    .FirstOrDefaultAsync(ep => ep.Id == guidId);
            }
            else if (entityType == typeof(SalesOrder))
            {
                entity = await _dbContext.SalesOrders
                    .AsNoTracking()
                    .Include(so => so.Items)
                    .FirstOrDefaultAsync(so => so.Id == guidId);
            }
            else if (entityType == typeof(StockMovement))
            {
                entity = await _dbContext.StockMovements
                    .AsNoTracking()
                    .Include(sm => sm.Material)
                    .FirstOrDefaultAsync(sm => sm.Id == guidId);
            }
            else
            {
                // Generic Fetch
                // Important: FindAsync might return a tracked entity that is stale if not reloaded?
                // But for a fresh scope it should be fine. 
                // However, we must ensure we are not getting a cached version if we are in a long-running process (like Kafka consumer).
                // OutboxProcessor runs in a scope? KafkaConsumer runs in a scope?
                // Let's force AsNoTracking logic by using Set<T>().
                
                // entity = await _dbContext.FindAsync(entityType, guidId);
                
                // Reflection-based AsNoTracking fetch
                var method = typeof(DbContext).GetMethods()
                    .First(m => m.Name == "Set" && m.IsGenericMethod && m.GetParameters().Length == 0)
                    .MakeGenericMethod(entityType);
                
                var dbSet = method.Invoke(_dbContext, null);
                // We can't easily chain AsNoTracking with reflection on object. 
                // Simplest fix: Just use FindAsync but reload?
                
                entity = await _dbContext.FindAsync(entityType, guidId);
                if (entity != null)
                {
                    await _dbContext.Entry(entity).ReloadAsync(); // Force reload from DB to get latest committed data
                }
            }
            
            if (entity == null)
            {
                _logger.LogWarning("Entity {AggregateType}:{AggregateId} not found in SQL. Skipping projection.", aggregateType, aggregateId);
                return;
            }

            // Get TenantId (Assume AuditableEntity or explicit property)
            var tenantId = GetTenantId(entity) ?? "UNKNOWN";

            // Serialize to JSON to detach from EF Core and handle cycles
            var jsonOptions = new JsonSerializerOptions 
            { 
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };
            var jsonString = JsonSerializer.Serialize(entity, entityType, jsonOptions);
            
            // Convert to BsonDocument
            // We use BsonDocument.Parse which handles standard JSON
            // But we might need to be careful with dates etc. System.Text.Json defaults to ISO 8601 which Mongo likes.
            var bsonDoc = MongoDB.Bson.BsonDocument.Parse(jsonString);

            // Project to Mongo
            await _mongoRepo.UpsertFullProjectionAsync(aggregateType, aggregateId, tenantId, bsonDoc);
            
            _logger.LogInformation("Projected {AggregateType}:{AggregateId} to Mongo.", aggregateType, aggregateId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ProjectionManager");
            throw; // Retry via Kafka
        }
    }

    // PropagateGLAccountUpdateAsync removed. 
    // Logic moved to Valora.Api.Application.Finance.Services.FinanceDataConsistencyService
    
    private Type? GetEntityType(string typeName)
    {
        if (_typeCache.TryGetValue(typeName, out var cachedType)) return cachedType;

        // Scan Domain Assembly
        // We assume entities are in the same assembly as AuditableEntity
        var assembly = typeof(AuditableEntity).Assembly;
        
        // Try exact match or with namespace
        var type = assembly.GetTypes()
            .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

        if (type != null)
        {
            _typeCache[typeName] = type;
        }

        return type;
    }

    private string? GetTenantId(object entity)
    {
        var prop = entity.GetType().GetProperty("TenantId");
        return prop?.GetValue(entity)?.ToString();
    }
}

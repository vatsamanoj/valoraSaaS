using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Dynamic.Queries.GetEntity;

public class GetEntityQueryHandler : IRequestHandler<GetEntityQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly MongoDbContext _mongoDb;

    public GetEntityQueryHandler(PlatformDbContext dbContext, MongoDbContext mongoDb)
    {
        _dbContext = dbContext;
        _mongoDb = mongoDb;
    }

    public async Task<ApiResult> Handle(GetEntityQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            return ApiResult.Fail(request.TenantId, request.Module, "get-by-id", new ApiError("Validation", "Invalid ID format"));
        }

        // --- SQL ERP MODULES SUPPORT (READ FROM MONGO READ-MODEL) ---
        if (string.Equals(request.Module, "SalesOrder", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "Material", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "CostCenter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "GLAccount", StringComparison.OrdinalIgnoreCase))
        {
            var collectionName = $"Entity_{request.Module}";
            var collection = _mongoDb.GetCollection<BsonDocument>(collectionName);
            var filter = Builders<BsonDocument>.Filter.Eq("_id", request.Id); // ID is stored as string in Mongo usually, but let's check projection
            
            // Try string first, then Guid if needed, but projection usually converts Guid to String
            var doc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

            if (doc == null)
            {
                 return ApiResult.Fail(request.TenantId, request.Module, "get-by-id", new ApiError("NotFound", $"{request.Module} not found (Read Model)"));
            }

            // Convert BsonDocument to Dictionary
            var resultDict = ConvertBsonDocument(doc);
            
            // Normalize ID
            if (resultDict.ContainsKey("_id"))
            {
                resultDict["Id"] = resultDict["_id"];
                resultDict.Remove("_id");
            }
            
            return ApiResult.Ok(request.TenantId, request.Module, "get-by-id", resultDict);
        }
        // --- SQL ERP MODULES END ---

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
        var dict = new Dictionary<string, object?>
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

    private Dictionary<string, object?> ConvertBsonDocument(BsonDocument doc)
    {
        return doc.ToDictionary(x => x.Name, x => ConvertBsonValue(x.Value));
    }

    private object? ConvertBsonValue(BsonValue value)
    {
        switch (value.BsonType)
        {
            case BsonType.Double: return value.AsDouble;
            case BsonType.String: return value.AsString;
            case BsonType.Document: return ConvertBsonDocument(value.AsBsonDocument);
            case BsonType.Array: return value.AsBsonArray.Select(ConvertBsonValue).ToList();
            case BsonType.Int32: return value.AsInt32;
            case BsonType.Int64: return value.AsInt64;
            case BsonType.Boolean: return value.AsBoolean;
            case BsonType.DateTime: return value.ToUniversalTime();
            case BsonType.Decimal128: return (decimal)value.AsDecimal128;
            case BsonType.ObjectId: return value.ToString();
            case BsonType.Null: return null;
            default: return value.ToString();
        }
    }
}
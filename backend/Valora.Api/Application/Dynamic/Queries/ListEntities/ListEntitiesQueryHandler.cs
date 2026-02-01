using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MongoDB.Driver;
using MongoDB.Bson;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;

namespace Valora.Api.Application.Dynamic.Queries.ListEntities;

public class ListEntitiesQueryHandler : IRequestHandler<ListEntitiesQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly MongoDbContext _mongoDb;

    public ListEntitiesQueryHandler(PlatformDbContext dbContext, MongoDbContext mongoDb)
    {
        _dbContext = dbContext;
        _mongoDb = mongoDb;
    }

    public async Task<ApiResult> Handle(ListEntitiesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return ApiResult.Fail(request.TenantId, "query", "execute", new ApiError("Validation", "Module is required."));
        }

        // --- SQL ERP MODULES SUPPORT (READ FROM MONGO READ-MODEL) ---
        if (string.Equals(request.Module, "SalesOrder", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "Material", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "CostCenter", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Module, "GLAccount", StringComparison.OrdinalIgnoreCase))
        {
            var collectionName = $"Entity_{request.Module}";
            var collection = _mongoDb.GetCollection<MongoDB.Bson.BsonDocument>(collectionName);

            // Build Filter
            var builder = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter;
            var filter = builder.Eq("TenantId", request.TenantId); 
            
            // Filter by ID if present
            if (request.Filters != null && request.Filters.TryGetValue("Id", out var idVal) && Guid.TryParse(idVal?.ToString(), out var id))
            {
                filter &= builder.Eq("_id", id.ToString()); // Projection stores ID as string usually
            }

            // Generic Filters
            if (request.Filters != null)
            {
                foreach (var kvp in request.Filters)
                {
                    if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null) continue;
                    if (string.Equals(kvp.Key, "Id", StringComparison.OrdinalIgnoreCase)) continue;

                    object? val = kvp.Value;
                    if (val is JsonElement je)
                    {
                        if (je.ValueKind == JsonValueKind.Number)
                        {
                            if (je.TryGetInt32(out var i)) val = i;
                            else if (je.TryGetInt64(out var l)) val = l;
                            else if (je.TryGetDouble(out var d)) val = d;
                        }
                        else if (je.ValueKind == JsonValueKind.String) val = je.GetString();
                        else if (je.ValueKind == JsonValueKind.True) val = true;
                        else if (je.ValueKind == JsonValueKind.False) val = false;
                    }
                    
                    // Case-sensitive match for Mongo fields?
                    // Mongo is case sensitive. "Type" vs "type".
                    // Schema uses "Type". Mongo uses "Type".
                    
                    if (val is string strVal)
                    {
                        // User Request: "search d/D/draft/DRAFT" (Case Insensitive StartsWith)
                        // User Request: "starts from M* and rest of words could be anything" (Wildcard)

                        string regexPattern;
                        if (strVal.Contains('*') || strVal.Contains('?'))
                        {
                            // Convert Glob to Regex
                            // Escape special regex chars except * and ?
                            // We need to be careful not to escape the * and ? we just checked for, 
                            // but we need to escape other regex chars like ., +, etc.
                            
                            // Strategy: Escape everything first, then unescape * and ? (complex)
                            // Better: Split by * and ?, escape parts, join with .* and .
                            
                            // Simple approach for now:
                            // 1. Escape the whole string to make it safe text
                            // 2. Replace the escaped versions of * and ? with regex equivalents
                            // Regex.Escape("*") -> "\*"
                            // Regex.Escape("?") -> "\?"
                            
                            var escaped = System.Text.RegularExpressions.Regex.Escape(strVal);
                            regexPattern = "^" + escaped.Replace("\\*", ".*").Replace("\\?", ".") + "$";
                        }
                        else
                        {
                            // Default: Case-Insensitive StartsWith
                            regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(strVal);
                        }

                        filter &= builder.Regex(kvp.Key, new MongoDB.Bson.BsonRegularExpression(regexPattern, "i"));
                    }
                    else
                    {
                        filter &= builder.Eq(kvp.Key, val);
                    }
                }
            }
            
            // Sorting
            var sort = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Sort.Descending("CreatedAt");
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                 if (request.SortDesc) sort = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Sort.Descending(request.SortBy);
                 else sort = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Sort.Ascending(request.SortBy);
            }

            var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            
            var docs = await collection.Find(filter)
                .Sort(sort)
                .Skip((request.Page - 1) * request.PageSize)
                .Limit(request.PageSize)
                .ToListAsync(cancellationToken);

            var data = docs.Select(d => 
            {
                var dict = ConvertBsonDocument(d);
                if (dict.ContainsKey("_id"))
                {
                    dict["Id"] = dict["_id"];
                    dict.Remove("_id");
                }
                return dict;
            });

            return ApiResult.Ok(request.TenantId, request.Module, "list", new { data, page = request.Page, pageSize = request.PageSize, totalCount = count });
        }
        // --- SQL ERP MODULES END ---

        // 1. Find Definition ID
        var definition = await _dbContext.ObjectDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == request.TenantId && d.ObjectCode == request.Module, cancellationToken);

        if (definition == null)
        {
            return ApiResult.Ok(request.TenantId, request.Module, "list", new
            {
                data = Array.Empty<object>(),
                page = request.Page,
                pageSize = request.PageSize,
                totalCount = 0
            });
        }

        // 2. Query Records
        var query = _dbContext.ObjectRecords.AsNoTracking()
            .Where(x => x.TenantId == request.TenantId && x.ObjectDefinitionId == definition.Id);

        // Filter by ID if present
        if (request.Filters != null)
        {
            foreach (var kvp in request.Filters)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key) || kvp.Value == null) continue;

                if (string.Equals(kvp.Key, "Id", StringComparison.OrdinalIgnoreCase) || 
                    string.Equals(kvp.Key, "_id", StringComparison.OrdinalIgnoreCase))
                {
                    if (Guid.TryParse(kvp.Value.ToString(), out var id))
                    {
                        query = query.Where(x => x.Id == id);
                    }
                }
                // Attribute filtering is complex, skipping for now (requires JOINs)
            }
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;

        // Sorting
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            if (request.SortBy.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
            {
                query = request.SortDesc ? query.OrderByDescending(x => x.CreatedAt) : query.OrderBy(x => x.CreatedAt);
            }
            else if (request.SortBy.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase))
            {
                query = request.SortDesc ? query.OrderByDescending(x => x.UpdatedAt) : query.OrderBy(x => x.UpdatedAt);
            }
            else
            {
                query = query.OrderBy(x => x.Id);
            }
        }
        else
        {
            query = query.OrderByDescending(x => x.CreatedAt);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var records = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (!records.Any())
        {
            return ApiResult.Ok(request.TenantId, request.Module, "list", new
            {
                data = Array.Empty<object>(),
                page,
                pageSize,
                totalCount
            });
        }

        // 3. Fetch Attributes for these records
        var recordIds = records.Select(r => r.Id).ToList();
        var attributes = await _dbContext.ObjectRecordAttributes
            .AsNoTracking()
            .Include(a => a.Field)
            .Where(a => recordIds.Contains(a.RecordId))
            .ToListAsync(cancellationToken);

        // 4. Pivot
        var result = records.Select(r =>
        {
            var dict = new Dictionary<string, object?>
            {
                { "Id", r.Id },
                { "TenantId", r.TenantId },
                { "CreatedAt", r.CreatedAt },
                { "CreatedBy", r.CreatedBy },
                { "UpdatedAt", r.UpdatedAt },
                { "UpdatedBy", r.UpdatedBy }
            };

            var recordAttrs = attributes.Where(a => a.RecordId == r.Id);
            foreach (var attr in recordAttrs)
            {
                var fieldName = attr.Field.FieldName;
                object? value = null;
                if (attr.ValueText != null) value = attr.ValueText;
                else if (attr.ValueNumber != null) value = attr.ValueNumber;
                else if (attr.ValueDate != null) value = attr.ValueDate;
                else if (attr.ValueBoolean != null) value = attr.ValueBoolean;

                dict[fieldName] = value;
            }
            return dict;
        });

        return ApiResult.Ok(request.TenantId, request.Module, "list", new
        {
            data = result,
            page,
            pageSize,
            totalCount
        });
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

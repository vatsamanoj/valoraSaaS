using System.Text.Json;
using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Infrastructure.Persistence;
using Valora.Api.Infrastructure.Services;

namespace Valora.Api.Application.Dynamic.Queries.ListEntities;

public class ListEntitiesQueryHandler : IRequestHandler<ListEntitiesQuery, ApiResult>
{
    private readonly PlatformDbContext _dbContext;

    public ListEntitiesQueryHandler(PlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ApiResult> Handle(ListEntitiesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Module))
        {
            return ApiResult.Fail(request.TenantId, "query", "execute", new ApiError("Validation", "Module is required."));
        }

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
            var dict = new Dictionary<string, object>
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
}

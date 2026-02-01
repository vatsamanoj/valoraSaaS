using Lab360.Application.Common.Results;
using MediatR;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Finance.Queries.GetJournalEntries;

public class GetJournalEntriesQueryHandler : IRequestHandler<GetJournalEntriesQuery, ApiResult>
{
    private readonly MongoDbContext _mongoDb;

    public GetJournalEntriesQueryHandler(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public async Task<ApiResult> Handle(GetJournalEntriesQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("full_JournalEntry");
        
        var builder = Builders<BsonDocument>.Filter;
        var filter = builder.Eq("TenantId", request.TenantId);

        if (!string.IsNullOrEmpty(request.DocumentNumber))
        {
            filter &= builder.Regex("DocumentNumber", new BsonRegularExpression(request.DocumentNumber, "i"));
        }

        if (request.StartDate.HasValue)
        {
            filter &= builder.Gte("PostingDate", request.StartDate.Value.ToUniversalTime());
        }

        if (request.EndDate.HasValue)
        {
            var endDate = request.EndDate.Value.Date.AddDays(1).AddTicks(-1).ToUniversalTime();
            filter &= builder.Lte("PostingDate", endDate);
        }

        var sort = Builders<BsonDocument>.Sort.Descending("DocumentDate");

        var totalCount = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        var documents = await collection.Find(filter)
            .Sort(sort)
            .Skip((request.Page - 1) * request.PageSize)
            .Limit(request.PageSize)
            .ToListAsync(cancellationToken);

        // Generate Debug Query Info
        var registry = BsonSerializer.SerializerRegistry;
        var serializer = registry.GetSerializer<BsonDocument>();
        
        var renderedFilter = filter.Render(serializer, registry);
        var renderedSort = sort.Render(serializer, registry);

        var debugParams = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(request.DocumentNumber)) 
            debugParams.Add("documentNumber", request.DocumentNumber);
        
        if (request.StartDate.HasValue) 
            debugParams.Add("startDate", request.StartDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));
            
        if (request.EndDate.HasValue) 
            debugParams.Add("endDate", request.EndDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"));

        var queryInfo = new
        {
            Collection = "full_JournalEntry",
            Filter = renderedFilter.ToJson(),
            Sort = renderedSort.ToJson(),
            TenantId = request.TenantId,
            Params = debugParams
        };

        var entries = documents.Select(doc => new
        {
            Id = doc["_id"].ToString(),
            DocumentNumber = doc["DocumentNumber"].AsString,
            PostingDate = ParseDate(doc["PostingDate"]),
            Description = doc.Contains("Description") && !doc["Description"].IsBsonNull ? doc["Description"].AsString : "",
            Status = doc["Status"].AsInt32,
            Version = doc.Contains("Version") && !doc["Version"].IsBsonNull 
                ? (doc["Version"].IsInt32 ? (uint)doc["Version"].AsInt32 : (uint)doc["Version"].AsInt64) 
                : 0, // Extract Version (xmin)
            Lines = doc["Lines"].AsBsonArray.Select(l => 
            {
                var line = l.AsBsonDocument;
                var glAccount = line.Contains("GLAccount") && !line["GLAccount"].IsBsonNull ? line["GLAccount"].AsBsonDocument : null;
                
                return new 
                {
                    Id = line["Id"].ToString(),
                    Description = line.Contains("Description") && !line["Description"].IsBsonNull ? line["Description"].AsString : "",
                    Debit = ToDecimalSafe(line["Debit"]),
                    Credit = ToDecimalSafe(line["Credit"]),
                    GLAccountName = glAccount != null && glAccount.Contains("Name") ? glAccount["Name"].AsString : "Unknown Account",
                    GLAccountCode = glAccount != null && glAccount.Contains("AccountCode") ? glAccount["AccountCode"].AsString : "N/A"
                };
            }).ToList()
        }).ToList();

        return ApiResult.Ok(request.TenantId, "FI", "list-je", new 
        { 
            Items = entries, 
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            DebugQuery = queryInfo // Pass query info to frontend
        });
    }

    private decimal ToDecimalSafe(BsonValue value)
    {
        if (value.IsDecimal128) return (decimal)value.AsDecimal;
        if (value.IsDouble) return (decimal)value.AsDouble;
        if (value.IsInt32) return (decimal)value.AsInt32;
        if (value.IsInt64) return (decimal)value.AsInt64;
        return 0m;
    }

    private DateTime ParseDate(BsonValue value)
    {
        if (value.IsBsonDateTime) return value.ToUniversalTime();
        if (value.IsString && DateTime.TryParse(value.AsString, out var dt)) return dt.ToUniversalTime();
        return DateTime.UtcNow;
    }
}

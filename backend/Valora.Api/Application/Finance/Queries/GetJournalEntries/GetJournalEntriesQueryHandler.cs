using Lab360.Application.Common.Results;
using MediatR;
using MongoDB.Bson;
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

        var entries = documents.Select(doc => new
        {
            Id = doc["_id"].ToString(),
            DocumentNumber = doc["DocumentNumber"].AsString,
            PostingDate = doc["PostingDate"].ToUniversalTime(),
            Description = doc.Contains("Description") && !doc["Description"].IsBsonNull ? doc["Description"].AsString : "",
            Status = doc["Status"].AsInt32,
            Lines = doc["Lines"].AsBsonArray.Select(l => 
            {
                var line = l.AsBsonDocument;
                var glAccount = line.Contains("GLAccount") && !line["GLAccount"].IsBsonNull ? line["GLAccount"].AsBsonDocument : null;
                
                return new 
                {
                    Id = line["Id"].ToString(),
                    Description = line.Contains("Description") && !line["Description"].IsBsonNull ? line["Description"].AsString : "",
                    Debit = line["Debit"].ToDecimal(),
                    Credit = line["Credit"].ToDecimal(),
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
            PageSize = request.PageSize
        });
    }
}

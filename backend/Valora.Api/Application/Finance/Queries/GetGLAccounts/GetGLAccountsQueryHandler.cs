using Lab360.Application.Common.Results;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.Finance.Queries.GetGLAccounts;

public class GetGLAccountsQueryHandler : IRequestHandler<GetGLAccountsQuery, ApiResult>
{
    private readonly MongoDbContext _mongoDb;

    public GetGLAccountsQueryHandler(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public async Task<ApiResult> Handle(GetGLAccountsQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("full_GLAccount");
        
        var filter = Builders<BsonDocument>.Filter.Eq("TenantId", request.TenantId);
        var sort = Builders<BsonDocument>.Sort.Ascending("AccountCode");

        var documents = await collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);

        var accounts = documents.Select(doc => new
        {
            Id = doc["_id"].ToString(),
            AccountCode = doc["AccountCode"].AsString,
            Name = doc["Name"].AsString,
            Type = ((AccountType)doc["Type"].AsInt32).ToString(),
            IsActive = doc["IsActive"].AsBoolean
        }).ToList();

        return ApiResult.Ok(request.TenantId, "FI", "list-gl", accounts);
    }
}

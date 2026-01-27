using Lab360.Application.Common.Results;
using Lab360.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/sequence")]
public class SequenceController : ControllerBase
{
    private readonly MongoDbContext _mongoDb;
    private readonly ILogger<SequenceController> _logger;

    public SequenceController(MongoDbContext mongoDb, ILogger<SequenceController> logger)
    {
        _mongoDb = mongoDb;
        _logger = logger;
    }

    [HttpGet("preview/{module}/{field}")]
    public async Task<IActionResult> PreviewSequence(string module, string field, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        
        var collection = _mongoDb.GetCollection<BsonDocument>("Sequences");
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("tenantId", tenantContext.TenantId),
            Builders<BsonDocument>.Filter.Eq("module", module),
            Builders<BsonDocument>.Filter.Eq("field", field)
        );

        var seqDoc = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        
        long nextVal = 1;
        string prefix = module.Length >= 3 ? module.Substring(0, 3).ToUpper() : module.ToUpper();
        
        if (seqDoc != null)
        {
            if (seqDoc.TryGetValue("currentValue", out var val))
            {
                nextVal = val.ToInt64() + 1;
            }
            if (seqDoc.TryGetValue("prefix", out var p))
            {
                prefix = p.AsString;
            }
        }

        var year = DateTime.UtcNow.Year;
        var value = $"{prefix}-{year}-{nextVal:D5}";

        return Ok(ApiResult.Ok(tenantContext.TenantId, module, "sequence-preview", new { value }));
    }
}

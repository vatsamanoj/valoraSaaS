using Lab360.Application.Common.Results;
using MediatR;
using MongoDB.Bson;
using MongoDB.Driver;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.HumanCapital.Queries.GetEmployeePayrolls;

public class GetEmployeePayrollsQueryHandler : IRequestHandler<GetEmployeePayrollsQuery, ApiResult>
{
    private readonly MongoDbContext _mongoDb;

    public GetEmployeePayrollsQueryHandler(MongoDbContext mongoDb)
    {
        _mongoDb = mongoDb;
    }

    public async Task<ApiResult> Handle(GetEmployeePayrollsQuery request, CancellationToken cancellationToken)
    {
        var collection = _mongoDb.GetCollection<BsonDocument>("full_EmployeePayroll");
        
        var filter = Builders<BsonDocument>.Filter.Eq("TenantId", request.TenantId);
        // Sort by Employee Name if possible, otherwise by CreatedAt
        var sort = Builders<BsonDocument>.Sort.Descending("CreatedAt");

        var documents = await collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);

        var payrolls = documents.Select(doc => 
        {
            var employee = doc.Contains("Employee") && !doc["Employee"].IsBsonNull ? doc["Employee"].AsBsonDocument : null;
            
            return new
            {
                Id = doc["_id"].ToString(),
                EmployeeName = employee != null 
                    ? $"{employee["FirstName"]} {employee["LastName"]}" 
                    : "Unknown Employee",
                EmployeeCode = employee != null && employee.Contains("EmployeeCode") ? employee["EmployeeCode"].AsString : "",
                BaseSalary = doc["BaseSalary"].ToDecimal(),
                Currency = doc["Currency"].AsString,
                EffectiveDate = doc["EffectiveDate"].ToUniversalTime(),
                BankName = doc.Contains("BankName") && !doc["BankName"].IsBsonNull ? doc["BankName"].AsString : "",
                AccountNumber = doc.Contains("BankAccountNumber") && !doc["BankAccountNumber"].IsBsonNull ? doc["BankAccountNumber"].AsString : ""
            };
        }).ToList();

        return ApiResult.Ok(request.TenantId, "HCM", "list-payroll", payrolls);
    }
}

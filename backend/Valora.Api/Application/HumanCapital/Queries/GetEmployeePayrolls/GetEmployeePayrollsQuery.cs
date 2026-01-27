using MediatR;
using Lab360.Application.Common.Results;

namespace Valora.Api.Application.HumanCapital.Queries.GetEmployeePayrolls;

public record GetEmployeePayrollsQuery(string TenantId) : IRequest<ApiResult>;

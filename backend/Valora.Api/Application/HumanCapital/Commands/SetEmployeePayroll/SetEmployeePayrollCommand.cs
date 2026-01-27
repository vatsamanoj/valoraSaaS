using Lab360.Application.Common.Results;
using MediatR;

namespace Valora.Api.Application.HumanCapital.Commands.SetEmployeePayroll;

public record SetEmployeePayrollCommand(
    string TenantId,
    Guid EmployeeId,
    decimal BaseSalary,
    string Currency,
    string? IBAN,
    string? BankCountry,
    string? BankKey,
    string? BankAccountNumber,
    string? BankName,
    string? TaxCode,
    DateTime EffectiveDate,
    string UserId
) : IRequest<ApiResult>;

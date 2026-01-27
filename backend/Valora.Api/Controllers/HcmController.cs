using Lab360.Application.Common.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Valora.Api.Application.HumanCapital.Commands.SetEmployeePayroll;
using Valora.Api.Application.HumanCapital.Queries.GetEmployeePayrolls;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/hcm")]
public class HcmController : ControllerBase
{
    private readonly IMediator _mediator;

    public HcmController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("payroll")]
    public async Task<IActionResult> GetPayrollList()
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var query = new GetEmployeePayrollsQuery(tenantContext.TenantId);
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("employees/{employeeId}/payroll")]
    public async Task<IActionResult> SetPayroll(Guid employeeId, [FromBody] SetPayrollRequest request)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        var userContext = UserContext.FromHttp(HttpContext);

        var command = new SetEmployeePayrollCommand(
            tenantContext.TenantId,
            employeeId,
            request.BaseSalary,
            request.Currency,
            request.IBAN,
            request.BankCountry,
            request.BankKey,
            request.BankAccountNumber,
            request.BankName,
            request.TaxCode,
            request.EffectiveDate,
            userContext.UserId
        );

        var result = await _mediator.Send(command);
        if (!result.Success) return BadRequest(result);
        return Ok(result);
    }
}

public record SetPayrollRequest(
    decimal BaseSalary,
    string Currency,
    string? IBAN,
    string? BankCountry,
    string? BankKey,
    string? BankAccountNumber,
    string? BankName,
    string? TaxCode,
    DateTime EffectiveDate
);

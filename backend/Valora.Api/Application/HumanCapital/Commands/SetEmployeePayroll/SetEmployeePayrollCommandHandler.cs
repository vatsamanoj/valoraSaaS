using Lab360.Application.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.HumanCapital;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Application.HumanCapital.Commands.SetEmployeePayroll;

public class SetEmployeePayrollCommandHandler : IRequestHandler<SetEmployeePayrollCommand, ApiResult>
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<SetEmployeePayrollCommandHandler> _logger;

    public SetEmployeePayrollCommandHandler(PlatformDbContext dbContext, ILogger<SetEmployeePayrollCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResult> Handle(SetEmployeePayrollCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Employee exists
        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId && e.TenantId == request.TenantId, cancellationToken);

        if (employee == null)
        {
            return ApiResult.Fail(request.TenantId, "HCM", "set-payroll", new ApiError("NotFound", "Employee not found"));
        }

        // 2. Get existing or create new Payroll record
        var payroll = await _dbContext.EmployeePayrolls
            .FirstOrDefaultAsync(p => p.EmployeeId == request.EmployeeId && p.TenantId == request.TenantId, cancellationToken);

        if (payroll == null)
        {
            payroll = new EmployeePayroll
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                EmployeeId = request.EmployeeId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = request.UserId
            };
            _dbContext.EmployeePayrolls.Add(payroll);
        }

        // 3. Update Sensitive Fields
        // In a real implementation, we would encrypt IBAN and TaxCode here before setting.
        // For now, we store as provided but mark the entity as containing sensitive data.
        
        payroll.BaseSalary = request.BaseSalary;
        payroll.Currency = request.Currency;
        payroll.IBAN = request.IBAN;
        payroll.BankCountry = request.BankCountry ?? "US";
        payroll.BankKey = request.BankKey;
        payroll.BankAccountNumber = request.BankAccountNumber;
        payroll.BankName = request.BankName;
        payroll.TaxCode = request.TaxCode;
        payroll.EffectiveDate = request.EffectiveDate;
        payroll.UpdatedAt = DateTime.UtcNow;
        payroll.UpdatedBy = request.UserId;

        // 4. Outbox Event (Masked Sensitive Data)
        // We do NOT emit IBAN/TaxCode in the event payload to avoid leaking PII to Kafka/Logs.
        
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            Topic = "valora.hcm.payroll.updated",
            Payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                EmployeeId = request.EmployeeId,
                AggregateType = "EmployeePayroll",
                AggregateId = payroll.Id.ToString(),
                BaseSalary = request.BaseSalary,
                Currency = request.Currency,
                EffectiveDate = request.EffectiveDate,
                MaskedIBAN = MaskString(request.IBAN),
                MaskedAccount = MaskString(request.BankAccountNumber),
                BankCountry = request.BankCountry
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ApiResult.Ok(request.TenantId, "HCM", "set-payroll", new { EmployeeId = request.EmployeeId, Message = "Payroll updated successfully" });
    }

    private string? MaskString(string? input)
    {
        if (string.IsNullOrEmpty(input) || input.Length < 4) return "****";
        return "****" + input.Substring(input.Length - 4);
    }
}

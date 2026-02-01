using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Domain.Events;
using Valora.Api.Infrastructure.Persistence;

namespace Valora.Api.Infrastructure.Services;

public class FiIntegrationService
{
    private readonly PlatformDbContext _dbContext;
    private readonly ILogger<FiIntegrationService> _logger;

    public FiIntegrationService(PlatformDbContext dbContext, ILogger<FiIntegrationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task HandleStockMovementAsync(string payload, CancellationToken cancellationToken)
    {
        var evt = JsonSerializer.Deserialize<StockMovementPostedEvent>(payload);
        if (evt == null) return;

        // Idempotency Check (Check if JE exists for this reference)
        var existingJe = await _dbContext.JournalEntries
            .FirstOrDefaultAsync(je => je.Reference == evt.MovementId.ToString(), cancellationToken);
        if (existingJe != null) return;

        // Get Accounts
        var inventoryAccount = await EnsureGLAccountAsync(evt.TenantId, "12000", "Inventory (Raw Materials)", cancellationToken);
        var grIrAccount = await EnsureGLAccountAsync(evt.TenantId, "50000", "GR/IR Clearing Account", cancellationToken);

        // Create JE
        // GR (101): Dr Inventory, Cr GR/IR
        // GI (201): Dr Expense (not handled yet), Cr Inventory
        
        var je = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = evt.TenantId,
            DocumentNumber = $"JE-MM-{evt.MovementId.ToString().Substring(0, 8)}",
            PostingDate = evt.MovementDate,
            DocumentDate = DateTime.UtcNow,
            Description = $"Stock Movement {evt.MovementType} - Material {evt.MaterialId}",
            Reference = evt.MovementId.ToString(),
            Status = JournalEntryStatus.Posted,
            Currency = "USD", // Simplified
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System-MM"
        };

        decimal amount = Math.Abs(evt.StockValue);

        if (evt.MovementType == 101) // GR
        {
            je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = inventoryAccount.Id, Debit = amount, Credit = 0, Description = "Inventory Increase" });
            je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = grIrAccount.Id, Debit = 0, Credit = amount, Description = "GR/IR Clearing" });
        }
        else // Issue
        {
             // For simplicity, just reverse for now. Ideally Dr Cost of Goods Sold.
             // We need COGS account.
             var cogsAccount = await EnsureGLAccountAsync(evt.TenantId, "51000", "Cost of Goods Sold", cancellationToken);
             
             je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = cogsAccount.Id, Debit = amount, Credit = 0, Description = "Cost of Goods Sold" });
             je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = inventoryAccount.Id, Debit = 0, Credit = amount, Description = "Inventory Decrease" });
        }

        _dbContext.JournalEntries.Add(je);
        AddOutboxMessage(je, amount);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Generated Journal Entry {DocumentNumber} for Stock Movement {MovementId}", je.DocumentNumber, evt.MovementId);
    }

    public async Task HandleSalesOrderBilledAsync(string payload, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[DEBUG] FiIntegration: Handling SalesOrderBilled. Payload len: {payload.Length}");
        var evt = JsonSerializer.Deserialize<SalesOrderBilledEvent>(payload);
        if (evt == null) 
        {
            Console.WriteLine("[DEBUG] FiIntegration: Failed to deserialize event.");
            return;
        }

        var existingJe = await _dbContext.JournalEntries
            .FirstOrDefaultAsync(je => je.Reference == evt.SalesOrderId.ToString(), cancellationToken);
        if (existingJe != null) return;

        // Debit: Customer Account (Receivable) - Try to resolve by Name (as passed in CustomerId)
        var arAccount = await _dbContext.GLAccounts
            .FirstOrDefaultAsync(a => a.TenantId == evt.TenantId && a.Name == evt.CustomerId, cancellationToken);
        
        // Fallback to default AR if not found
        if (arAccount == null)
        {
             arAccount = await EnsureGLAccountAsync(evt.TenantId, "11000", "Accounts Receivable", cancellationToken);
             _logger.LogWarning("Could not resolve GL Account for Customer '{Customer}'. Used default AR.", evt.CustomerId);
        }

        // Credit: Sales Revenue
        var revenueAccount = await EnsureGLAccountAsync(evt.TenantId, "40000", "Sales Revenue", cancellationToken);

        var je = new JournalEntry
        {
            Id = Guid.NewGuid(),
            TenantId = evt.TenantId,
            DocumentNumber = $"JE-SD-{evt.SalesOrderId.ToString().Substring(0, 8)}",
            PostingDate = evt.BillingDate,
            DocumentDate = DateTime.UtcNow,
            Description = $"Billing for Order {evt.OrderNumber}",
            Reference = evt.SalesOrderId.ToString(),
            Status = JournalEntryStatus.Posted,
            Currency = evt.Currency,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System-SD"
        };

        // Dr AR, Cr Revenue
        je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = arAccount.Id, Debit = evt.TotalAmount, Credit = 0, Description = $"Customer Invoice {evt.CustomerId}" });
        je.Lines.Add(new JournalEntryLine { Id = Guid.NewGuid(), GLAccountId = revenueAccount.Id, Debit = 0, Credit = evt.TotalAmount, Description = "Sales Revenue" });

        _dbContext.JournalEntries.Add(je);
        AddOutboxMessage(je, evt.TotalAmount);

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Generated Journal Entry {DocumentNumber} for Billing {SalesOrderId}", je.DocumentNumber, evt.SalesOrderId);
    }

    private void AddOutboxMessage(JournalEntry je, decimal totalAmount)
    {
        var outboxMessage = new OutboxMessageEntity
        {
            Id = Guid.NewGuid(),
            TenantId = je.TenantId,
            Topic = "valora.fi.posted",
            Payload = JsonSerializer.Serialize(new
            {
                Id = je.Id,
                DocumentNumber = je.DocumentNumber,
                TotalAmount = totalAmount,
                AggregateType = "JournalEntry",
                AggregateId = je.Id.ToString()
            }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.OutboxMessages.Add(outboxMessage);
    }

    private async Task<GLAccount> EnsureGLAccountAsync(string tenantId, string code, string name, CancellationToken cancellationToken)
    {
        var account = await _dbContext.GLAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.AccountCode == code, cancellationToken);

        if (account == null)
        {
            AccountType type = AccountType.Asset;
            if (code.StartsWith("4")) type = AccountType.Revenue;
            else if (code.StartsWith("5")) type = AccountType.Expense;
            else if (code.StartsWith("2") || code.StartsWith("50")) type = AccountType.Liability;

            account = new GLAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                AccountCode = code,
                Name = name,
                Type = type,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System"
            };
            _dbContext.GLAccounts.Add(account);
            
            // Emit Outbox for Projection
            _dbContext.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Topic = "valora.fi.gl_account_created",
                Payload = JsonSerializer.Serialize(new { AggregateType = "GLAccount", AggregateId = account.Id.ToString() }),
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }
        return account;
    }
}

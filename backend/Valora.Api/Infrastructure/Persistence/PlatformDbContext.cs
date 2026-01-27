using Microsoft.EntityFrameworkCore;
using Valora.Api.Domain.Entities;
using Valora.Api.Domain.Entities.Finance;
using Valora.Api.Domain.Entities.Controlling;
using Valora.Api.Domain.Entities.Sales;
using Valora.Api.Domain.Entities.Materials;
using Valora.Api.Domain.Entities.HumanCapital;

namespace Valora.Api.Infrastructure.Persistence;

public class PlatformDbContext : DbContext
{
    public PlatformDbContext(DbContextOptions<PlatformDbContext> options) : base(options) { }

    public DbSet<OutboxMessageEntity> OutboxMessages { get; set; }

    // EAV Tables
    public DbSet<ObjectDefinition> ObjectDefinitions { get; set; }
    public DbSet<ObjectField> ObjectFields { get; set; }
    public DbSet<ObjectRecord> ObjectRecords { get; set; }
    public DbSet<ObjectRecordAttribute> ObjectRecordAttributes { get; set; }

    // Finance (FI)
    public DbSet<GLAccount> GLAccounts { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalEntryLine> JournalEntryLines { get; set; }

    // Controlling (CO)
    public DbSet<CostCenter> CostCenters { get; set; }

    // Sales (SD)
    public DbSet<SalesOrder> SalesOrders { get; set; }
    public DbSet<SalesOrderItem> SalesOrderItems { get; set; }

    // Materials (MM)
    public DbSet<MaterialMaster> MaterialMasters { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }

    // Human Capital (HCM)
    public DbSet<Employee> Employees { get; set; }
    public DbSet<EmployeePayroll> EmployeePayrolls { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OutboxMessageEntity>()
            .HasIndex(x => new { x.Status, x.CreatedAt });

        // FI Constraints
        modelBuilder.Entity<GLAccount>()
            .HasIndex(x => new { x.TenantId, x.AccountCode })
            .IsUnique();

        // CO Constraints
        modelBuilder.Entity<CostCenter>()
            .HasIndex(x => new { x.TenantId, x.Code })
            .IsUnique();

        // SD Constraints
        modelBuilder.Entity<SalesOrder>()
            .HasIndex(x => new { x.TenantId, x.OrderNumber })
            .IsUnique();

        // MM Constraints
        modelBuilder.Entity<MaterialMaster>()
            .HasIndex(x => new { x.TenantId, x.MaterialCode })
            .IsUnique();

        // HCM Constraints
        modelBuilder.Entity<Employee>()
            .HasIndex(x => new { x.TenantId, x.EmployeeCode })
            .IsUnique();

        modelBuilder.Entity<EmployeePayroll>()
            .HasIndex(x => new { x.TenantId, x.EmployeeId })
            .IsUnique(); // One payroll record per employee
    }
}

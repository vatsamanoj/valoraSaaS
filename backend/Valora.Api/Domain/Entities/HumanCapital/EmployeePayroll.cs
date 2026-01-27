using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.HumanCapital;

[Table("EmployeePayroll", Schema = "secure")]
public class EmployeePayroll : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public Guid EmployeeId { get; set; }

    [ForeignKey("EmployeeId")]
    public Employee? Employee { get; set; }

    // Sensitive Data - In a real scenario, these columns should be encrypted at rest (e.g., Always Encrypted)
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal BaseSalary { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    [MaxLength(100)]
    public string? IBAN { get; set; } // Sensitive (Global/Europe)

    // Global Banking Fields
    [MaxLength(2)]
    public string BankCountry { get; set; } = "US"; // ISO 2 Char (e.g., IN, US, GB)

    [MaxLength(20)]
    public string? BankKey { get; set; } // IFSC (India), Routing (US), Sort Code (UK)

    [MaxLength(50)]
    public string? BankAccountNumber { get; set; } // Account Number (India/US/UK)

    [MaxLength(100)]
    public string? BankName { get; set; } // Optional: Bank Name for display

    [MaxLength(50)]
    public string? TaxCode { get; set; } // Sensitive

    public DateTime EffectiveDate { get; set; }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Finance;

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

[Table("GLAccount")]
public class GLAccount : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string AccountCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AccountType Type { get; set; }

    public bool IsActive { get; set; } = true;

    // Optional: Parent Account for hierarchy
    public Guid? ParentAccountId { get; set; }
}

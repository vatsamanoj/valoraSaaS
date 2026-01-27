using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Finance;

public enum JournalEntryStatus
{
    Draft,
    Posted,
    Reversed
}

[Table("JournalEntry")]
public class JournalEntry : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }

    [MaxLength(100)]
    public string? Reference { get; set; } // External Inv #

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public decimal ExchangeRate { get; set; } = 1.0m;

    public JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    // Navigation
    public ICollection<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
}

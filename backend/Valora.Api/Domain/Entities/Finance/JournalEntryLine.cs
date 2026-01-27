using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Finance;

[Table("JournalEntryLine")]
public class JournalEntryLine
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid JournalEntryId { get; set; }
    
    [ForeignKey("JournalEntryId")]
    public JournalEntry? JournalEntry { get; set; }

    [Required]
    public Guid GLAccountId { get; set; }

    [ForeignKey("GLAccountId")]
    public GLAccount? GLAccount { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    // For CO (Controlling) integration
    [MaxLength(50)]
    public string? CostCenter { get; set; }
}

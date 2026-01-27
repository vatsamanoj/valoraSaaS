using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Materials;

public enum MovementType
{
    GoodsReceipt = 101,
    GoodsIssue = 201,
    Transfer = 301
}

[Table("StockMovement")]
public class StockMovement : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    public Guid MaterialId { get; set; }
    
    [ForeignKey("MaterialId")]
    public MaterialMaster? Material { get; set; }

    [Required]
    public MovementType MovementType { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    public DateTime MovementDate { get; set; }

    // Reference to Financial Doc if applicable
    public Guid? JournalEntryId { get; set; }
}

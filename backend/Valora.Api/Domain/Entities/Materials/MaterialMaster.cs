using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Materials;

[Table("MaterialMaster")]
public class MaterialMaster : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string MaterialCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string BaseUnitOfMeasure { get; set; } = "EA";

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; }

    // Valuation Price (Standard Price)
    [Column(TypeName = "decimal(18,2)")]
    public decimal StandardPrice { get; set; }
}

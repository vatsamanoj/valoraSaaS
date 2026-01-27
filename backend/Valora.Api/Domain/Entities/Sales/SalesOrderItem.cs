using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Valora.Api.Domain.Entities.Sales;

[Table("SalesOrderItem")]
public class SalesOrderItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid SalesOrderId { get; set; }
    
    [ForeignKey("SalesOrderId")]
    public SalesOrder? SalesOrder { get; set; }

    [Required]
    [MaxLength(100)]
    public string MaterialCode { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; set; }
}

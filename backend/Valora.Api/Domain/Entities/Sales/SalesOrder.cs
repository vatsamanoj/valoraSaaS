using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Valora.Api.Domain.Common;

namespace Valora.Api.Domain.Entities.Sales;

public enum SalesOrderStatus
{
    Draft,
    Confirmed,
    Shipped,
    Invoiced,
    Cancelled
}

[Table("SalesOrder")]
public class SalesOrder : AuditableEntity
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public DateTime OrderDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string CustomerId { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
}

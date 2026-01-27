namespace Valora.Api.Domain.Events;

public class StockMovementPostedEvent
{
    public string TenantId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = "StockMovement";
    public string AggregateId { get; set; } = string.Empty;
    public Guid MovementId { get; set; }
    public Guid MaterialId { get; set; }
    public int MovementType { get; set; } // 101=GR, 201=GI
    public decimal Quantity { get; set; }
    public decimal StockValue { get; set; }
    public DateTime MovementDate { get; set; }
}

namespace Valora.Api.Domain.Events;

public class SalesOrderBilledEvent
{
    public string TenantId { get; set; } = string.Empty;
    public string AggregateType { get; set; } = "SalesOrder";
    public string AggregateId { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime BillingDate { get; set; }
}

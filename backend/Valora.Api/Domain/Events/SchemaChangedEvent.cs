namespace Valora.Api.Domain.Events;

public class SchemaChangedEvent
{
    public string TenantId { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string AggregateType { get; set; } = string.Empty; // Traceability
    public string AggregateId { get; set; } = string.Empty;   // Traceability
    public int Version { get; set; }
    public string SchemaJson { get; set; } = string.Empty;
}

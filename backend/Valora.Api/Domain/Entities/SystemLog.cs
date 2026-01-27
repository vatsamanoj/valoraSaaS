namespace Valora.Api.Domain.Entities;

public class FixStep
{
    public int Id { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SystemLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public bool IsFixed { get; set; }
    public string Data { get; set; } = string.Empty; // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime? FixedAt { get; set; }
    public string? FixedBy { get; set; }
    public List<FixStep> FixSteps { get; set; } = new();
}

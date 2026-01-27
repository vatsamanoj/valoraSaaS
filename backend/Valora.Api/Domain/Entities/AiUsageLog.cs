namespace Valora.Api.Domain.Entities;

public class AiUsageLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public string PromptSummary { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string TenantId { get; set; } = string.Empty;
}

public class AiStats
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
}

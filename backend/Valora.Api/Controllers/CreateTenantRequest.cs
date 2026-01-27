namespace Valora.Api.Controllers;

public class CreateTenantRequest
{
    public required string TenantId { get; set; }
    public required string Name { get; set; }
    public required string Environment { get; set; } = "prod";
    public string? AdminEmail { get; set; }
    public string SourceTenantId { get; set; } = string.Empty;
}

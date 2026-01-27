namespace Lab360.Application.Common.Security
{
    public sealed record TenantContext(
        string TenantId,
        string Environment,
        string Role);
}


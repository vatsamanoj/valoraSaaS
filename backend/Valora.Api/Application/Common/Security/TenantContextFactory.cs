using Microsoft.AspNetCore.Http;

namespace Lab360.Application.Common.Security
{
    public static class TenantContextFactory
    {
        public static TenantContext FromHttp(HttpContext httpContext)
        {
            var tenantId = httpContext.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? string.Empty;
            var environment = httpContext.Request.Headers["X-Environment"].FirstOrDefault() ?? "prod";
            var role = httpContext.Request.Headers["X-Role"].FirstOrDefault() ?? "TenantAdmin";
            return new TenantContext(tenantId, environment.ToLowerInvariant(), role);
        }
    }
}


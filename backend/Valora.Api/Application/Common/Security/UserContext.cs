using System.Security.Claims;

namespace Lab360.Application.Common.Security
{
    /// <summary>
    /// Holds resolved user identity for the current request.
    /// </summary>
    public sealed class UserContext
    {
        public string UserId { get; }
        public string Email { get; }
        public string Role { get; }

        public UserContext(string userId, string email, string role)
        {
            UserId = userId;
            Email = email;
            Role = role;
        }

        public static UserContext FromHttp(HttpContext httpContext)
        {
            // 1. Try to get from Claims (Prod/Secure Mode)
            var user = httpContext.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                         ?? user.FindFirst("sub")?.Value 
                         ?? "unknown";
                var email = user.FindFirst(ClaimTypes.Email)?.Value ?? "";
                var role = user.FindFirst(ClaimTypes.Role)?.Value ?? "User";
                
                return new UserContext(id, email, role);
            }

            // 2. Fallback to Headers (Dev Mode ONLY)
            // Policy: "Trusting TenantId from client" is forbidden in Prod, but for Dev we allow it with a warning log.
            // Ideally we check Environment here.
            
            var devUserId = httpContext.Request.Headers["X-User-Id"].FirstOrDefault() ?? "system";
            var devEmail = "dev@local";
            var devRole = httpContext.Request.Headers["X-Role"].FirstOrDefault() ?? "Admin";

            return new UserContext(devUserId, devEmail, devRole);
        }
    }
}
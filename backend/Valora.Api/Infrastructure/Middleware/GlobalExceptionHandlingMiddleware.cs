using System.Net;
using System.Text.Json;
using Lab360.Application.Common.Results;

namespace Valora.Api.Infrastructure.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Extract TenantId from context if available, or fallback
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault() ?? "unknown";
        var module = "system"; // Generic context

        // Create a standard ApiResult for error
        var message = exception.Message;
        if (exception.InnerException != null)
        {
            message += " Inner: " + exception.InnerException.Message;
        }
        var apiError = new ApiError("InternalServerError", message);
        
        // In production, we might want to hide the exception message details
        // but for now we pass it through for visibility as per previous code behavior.
        
        var response = ApiResult.Fail(tenantId, module, "error", apiError);

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

using Lab360.Application.Common.Security;
using Lab360.Application.Publishing;
using Lab360.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Valora.Api.Controllers;

[ApiController]
[Route("studio/screens")]
public class StudioScreensController : ControllerBase
{
    private readonly ScreenPublishService _publisher;

    public StudioScreensController(ScreenPublishService publisher)
    {
        _publisher = publisher;
    }

    public sealed record PublishRequest(
        string TenantId,
        string ObjectCode,
        string FromEnv,
        string ToEnv);

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishRequest request, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);

        var hasStudioAccess =
            string.Equals(tenantContext.Role, "PlatformAdmin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tenantContext.Role, "TenantAdmin", StringComparison.OrdinalIgnoreCase);

        if (!hasStudioAccess)
        {
            return Forbid();
        }

        try
        {
            var command = new ScreenPublishRequest(
                tenantContext.TenantId,
                request.ObjectCode,
                request.FromEnv,
                request.ToEnv);

            await _publisher.PublishAsync(command, cancellationToken);

            return Ok(ApiResult.Ok(
                tenantContext.TenantId,
                "studio",
                "publish",
                new { status = "ok" }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult.Fail(
                tenantContext.TenantId,
                "studio",
                "publish",
                new ApiError(ex.Message, "Invalid arguments provided")));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult.Fail(
                tenantContext.TenantId,
                "studio",
                "publish",
                new ApiError("publish.unexpectedError", ex.ToString())));
        }
    }
}

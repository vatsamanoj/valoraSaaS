using Lab360.Application.Common.Results;
using Lab360.Application.Common.Security;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Lab360.Application.Common.Results;

namespace Valora.Api.Controllers;

[ApiController]
[Route("api/calculation")]
public class CalculationController : ControllerBase
{
    private readonly ILogger<CalculationController> _logger;
    private readonly ISchemaProvider _schemaProvider;
    private readonly CalculationService _calculationService;

    public CalculationController(
        ILogger<CalculationController> logger,
        ISchemaProvider schemaProvider,
        CalculationService calculationService)
    {
        _logger = logger;
        _schemaProvider = schemaProvider;
        _calculationService = calculationService;
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteCalculation([FromBody] CalculationRequest request, CancellationToken cancellationToken)
    {
        var tenantContext = TenantContextFactory.FromHttp(HttpContext);
        _logger.LogInformation($"[CalculationController] ExecuteCalculation - Tenant: {tenantContext.TenantId}, Module: {request.Module}");

        try
        {
            // Get the schema for the module - assume objectType is the module for now
            var schema = await _schemaProvider.GetSchemaAsync(request.Module, request.Module, tenantContext.Environment, cancellationToken);
            if (schema == null)
            {
                return BadRequest(ApiResult.Fail(tenantContext.TenantId, request.Module, "calculation-failed", new ApiError("SCHEMA_NOT_FOUND", "Schema not found")));
            }

            // Execute calculations on the provided formData
            var calculatedData = await _calculationService.ExecuteCalculations(request.FormData, schema);

            // Structure the response as expected by frontend
            var response = new
            {
                calculatedValues = calculatedData, // All calculated data
                documentTotals = new Dictionary<string, object>(), // TODO: extract totals if needed
                tempValues = request.TempValues ?? new Dictionary<string, object>()
            };

            return Ok(ApiResult.Ok(tenantContext.TenantId, request.Module, "calculation-executed", response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing calculation for {request.Module}");
            return StatusCode(500, ApiResult.Fail(tenantContext.TenantId, request.Module, "calculation-failed", new ApiError("CALCULATION_ERROR", "Calculation execution failed")));
        }
    }
}

public class CalculationRequest
{
    public string Module { get; set; } = string.Empty;
    public string? ChangedField { get; set; }
    public Dictionary<string, object> FormData { get; set; } = new();
    public Dictionary<string, object>? TempValues { get; set; }
}
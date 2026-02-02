using Xunit;
using Valora.Api.Application.Services;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Valora.Tests;

public class CalculationServiceTests
{
    private readonly CalculationService _calculationService;

    public CalculationServiceTests()
    {
        _calculationService = new CalculationService();
    }

    [Fact]
    public async Task ExecuteCalculations_ShouldCalculateLineItemTotal_WhenGivenCorrectFormula()
    {
        // Arrange
        var schema = new ModuleSchema("test", "test", 1, "test", new Dictionary<string, FieldRule>(),
            CalculationRules: new CalculationRulesConfig
            {
                ServerSide = new ServerSideCalculations
                {
                    LineItemCalculations = new List<LineItemCalculation>
                    {
                        new()
                        {
                            TargetField = "LineTotal",
                            Formula = "{Quantity} * {UnitPrice}"
                        }
                    }
                }
            });

        var entityData = new Dictionary<string, object>
        {
            { "Items", new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "Quantity", 10 },
                        { "UnitPrice", 100 }
                    }
                }
            }
        };

        // Act
        var result = await _calculationService.ExecuteCalculations(entityData, schema);

        // Assert
        var items = result["Items"] as List<Dictionary<string, object>>;
        var lineTotal = items[0]["LineTotal"];
        Assert.Equal(1000, lineTotal);
    }

    [Fact]
    public async Task ExecuteCalculations_ShouldCalculateDocumentTotal_WhenGivenSumFormula()
    {
        // Arrange
        var schema = new ModuleSchema("test", "test", 1, "test", new Dictionary<string, FieldRule>(),
            CalculationRules: new CalculationRulesConfig
            {
                ServerSide = new ServerSideCalculations
                {
                    DocumentCalculations = new List<DocumentCalculation>
                    {
                        new()
                        {
                            TargetField = "SubTotal",
                            Formula = "SUM({Items.LineTotal})"
                        }
                    }
                }
            });

        var entityData = new Dictionary<string, object>
        {
            { "Items", new List<Dictionary<string, object>>
                {
                    new()
                    {
                        { "LineTotal", 1000 },
                    },
                    new()
                    {
                        { "LineTotal", 500 },
                    }
                }
            }
        };

        // Act
        var result = await _calculationService.ExecuteCalculations(entityData, schema);

        // Assert
        var subTotal = result["SubTotal"];
        Assert.Equal(1500m, subTotal);
    }


}

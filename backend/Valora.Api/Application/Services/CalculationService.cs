using DynamicExpresso;
using NCalc;
using System.Text.RegularExpressions;
using Valora.Api.Application.Schemas;
using Valora.Api.Application.Schemas.TemplateConfig;

namespace Valora.Api.Application.Services;

public class CalculationService
{
    public async Task<Dictionary<string, object>> ExecuteCalculations(Dictionary<string, object> entityData, ModuleSchema schema)
    {
        if (schema.CalculationRules?.ServerSide == null)
            return entityData;

        var serverSideRules = schema.CalculationRules.ServerSide;

        if (serverSideRules.LineItemCalculations.Any())
        {
            await ExecuteLineItemCalculations(entityData, serverSideRules.LineItemCalculations);
        }

        if (serverSideRules.DocumentCalculations.Any())
        {
            await ExecuteDocumentCalculations(entityData, serverSideRules.DocumentCalculations);
        }

        if (serverSideRules.ComplexCalculations.Any())
        {
            await ExecuteComplexCalculations(entityData, serverSideRules.ComplexCalculations);
        }

        return entityData;
    }

    private async Task ExecuteComplexCalculations(Dictionary<string, object> entityData, List<ComplexCalculation> complexCalculations)
    {
        foreach (var calculation in complexCalculations)
        {
            var interpreter = new Interpreter();
            var parameters = new List<Parameter>();

            foreach (var param in calculation.Parameters)
            {
                if (entityData.TryGetValue(param.Name, out var value))
                {
                    parameters.Add(new Parameter(param.Name, value.GetType(), value));
                }
            }

            var result = await Task.Run(() => interpreter.Eval(calculation.Expression, parameters.ToArray()));
            entityData[calculation.TargetField] = result;
        }
    }

    private async Task ExecuteDocumentCalculations(Dictionary<string, object> entityData, List<DocumentCalculation> documentCalculations)
    {
        foreach (var calculation in documentCalculations)
        {
            var formula = calculation.Formula;
            var targetField = calculation.TargetField;

            var evaluationResult = await EvaluateFormula(formula, entityData);
            if (evaluationResult != null)
            {
                entityData[targetField] = evaluationResult;
            }
        }
    }


    private async Task ExecuteLineItemCalculations(Dictionary<string, object> entityData, List<LineItemCalculation> lineItemCalculations)
    {
        if (!entityData.TryGetValue("Items", out var items) || items is not List<Dictionary<string, object>> lineItems)
            return;

        foreach (var lineItem in lineItems)
        {
            foreach (var calculation in lineItemCalculations)
            {
                var formula = calculation.Formula;
                var targetField = calculation.TargetField;

                var evaluationResult = await EvaluateFormula(formula, lineItem);
                if (evaluationResult != null)
                {
                    lineItem[targetField] = evaluationResult;
                }
            }
        }
    }

    private async Task<object?> EvaluateFormula(string formula, Dictionary<string, object> data)
    {
        try
        {
            var expression = new Expression(formula);
            var parameters = new Dictionary<string, object>();

            var matches = Regex.Matches(formula, @"{(\w+)}");
            foreach (Match match in matches)
            {
                var fieldName = match.Groups[1].Value;
                if (data.TryGetValue(fieldName, out var value))
                {
                    parameters[fieldName] = value;
                }
            }

            // Handle SUM aggregation
            if (formula.StartsWith("SUM("))
            {
                var match = Regex.Match(formula, @"SUM\({Items\.(\w+)}\)");
                if (match.Success)
                {
                    var fieldToSum = match.Groups[1].Value;
                    if (data.TryGetValue("Items", out var items) && items is List<Dictionary<string, object>> lineItems)
                    {
                        decimal sum = 0;
                        foreach (var lineItem in lineItems)
                        {
                            if (lineItem.TryGetValue(fieldToSum, out var value) && decimal.TryParse(value.ToString(), out var decimalValue))
                            {
                                sum += decimalValue;
                            }
                        }
                        return sum;
                    }
                }
            }

            expression.Parameters = parameters;
            return await Task.Run(() => expression.Evaluate());
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error evaluating formula: {formula}. Error: {ex.Message}");
            return null;
        }
    }
}

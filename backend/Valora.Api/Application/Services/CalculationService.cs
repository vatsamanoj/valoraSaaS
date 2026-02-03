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
            // Handle SUM aggregation first
            var sumMatch = Regex.Match(formula, @"SUM\({Items\.(\w+)}\)");
            if (sumMatch.Success)
            {
                var fieldToSum = sumMatch.Groups[1].Value;
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

            var ncalcFormula = Regex.Replace(formula, @"\{(\w+)\}", "$1");
            var expression = new NCalc.Expression(ncalcFormula);
            
            expression.EvaluateFunction += (name, args) =>
            {
                if (name.ToLower() == "if")
                {
    private object ConvertToNumeric(object value)
    {
        if (value is decimal || value is double || value is float || value is int || value is long)
        {
            return value;
        }
    
        var str = value?.ToString();
        if (string.IsNullOrEmpty(str))
        {
            return 0m; // Default to 0 for calculations
        }
    
        // Try to parse as decimal
        if (decimal.TryParse(str, out var decimalValue))
        {
            return decimalValue;
        }
    
        // Try to parse as double
        if (double.TryParse(str, out var doubleValue))
        {
            return doubleValue;
        }
    
        // If cannot parse, return 0
        return 0m;
    }
}
                    args.Result = (bool)args.Parameters[0].Evaluate() ? args.Parameters[1].Evaluate() : args.Parameters[2].Evaluate();
                }
            };
            
            // Let NCalc parse the expression to identify parameters
            expression.Evaluate();
            
            foreach (var paramName in expression.Parameters.Keys)
            {
                if (data.TryGetValue(paramName, out var value))
                {
                    // Convert value to numeric type if possible for NCalc
                    var convertedValue = ConvertToNumeric(value);
                    expression.Parameters[paramName] = convertedValue;
                }
            }

            Console.WriteLine($"Evaluating formula: {ncalcFormula}");
            foreach(var p in expression.Parameters)
            {
                Console.WriteLine($"Parameter: {p.Key}, Value: {p.Value}");
            }
            var result = await Task.Run(() => expression.Evaluate());
            Console.WriteLine($"Formula result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating formula: {formula}. Error: {ex.Message}");
            return null;
        }
    }
}

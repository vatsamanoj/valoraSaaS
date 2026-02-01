using System.Text.Json;
using System.Text.Json.Nodes;
using Valora.Api.Application.Schemas;
using Valora.Api.Domain.Common;

namespace Valora.Api.Application.Services;

public class SchemaValidator
{
    public List<string> Validate(string rawJson, ModuleSchema schema, bool isUpdate = false)
    {
        var errors = new List<string>();
        var jsonNode = JsonNode.Parse(rawJson)?.AsObject();

        if (jsonNode == null)
        {
            errors.Add("Invalid JSON body.");
            return errors;
        }

        foreach (var field in schema.Fields)
        {
            var key = field.Key;
            var rule = field.Value;
            
            // Check Required
            if (!jsonNode.ContainsKey(key))
            {
                if (rule.Required && !isUpdate)
                {
                    errors.Add($"Field '{key}' is required.");
                }
                continue;
            }

            var value = jsonNode[key];
            object? typedValue = GetTypedValue(value);

            // Validate MaxLength
            if (rule.MaxLength.HasValue && typedValue is string strVal && strVal.Length > rule.MaxLength.Value)
            {
                errors.Add($"Field '{key}' exceeds max length of {rule.MaxLength.Value}.");
            }
        }

        return errors;
    }

    private object? GetTypedValue(JsonNode? node)
    {
        if (node == null) return null;
        if (node is JsonValue val)
        {
            if (val.TryGetValue<string>(out var s)) return s;
            if (val.TryGetValue<int>(out var i)) return i;
            if (val.TryGetValue<bool>(out var b)) return b;
            if (val.TryGetValue<double>(out var d)) return d;
            return val.ToString();
        }
        return node.ToString();
    }
}

namespace Valora.Api.Application.Schemas.TemplateConfig;

public class CalculationRulesConfig
{
    public ServerSideCalculations ServerSide { get; set; } = new();
    public ClientSideCalculations ClientSide { get; set; } = new();
}

public class ServerSideCalculations
{
    public List<LineItemCalculation> LineItemCalculations { get; set; } = new();
    public List<DocumentCalculation> DocumentCalculations { get; set; } = new();
    public List<ComplexCalculation> ComplexCalculations { get; set; } = new();
}

public class LineItemCalculation
{
    public string TargetField { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Trigger { get; set; } = "onChange";
    public List<string> DependentFields { get; set; } = new();
    public string? Condition { get; set; }
}

public class DocumentCalculation
{
    public string TargetField { get; set; } = string.Empty;
    public string Formula { get; set; } = string.Empty;
    public string Trigger { get; set; } = "onLineChange";
}

public class ComplexCalculation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    [System.Text.Json.Serialization.JsonConverter(typeof(JsonCalculationScopeConverter))]
    public CalculationScope Scope { get; set; } = CalculationScope.LineItem;
    public string Trigger { get; set; } = "onChange";
    public string Expression { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public List<CalculationParameter> Parameters { get; set; } = new();
    public List<ExternalDataSource> ExternalDataSources { get; set; } = new();
    public List<string> AssemblyReferences { get; set; } = new();
}

public enum CalculationScope
{
    LineItem,
    Document
}

// JSON Converter for case-insensitive enum parsing
public class JsonCalculationScopeConverter : System.Text.Json.Serialization.JsonConverter<CalculationScope>
{
    public override CalculationScope Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.ToLowerInvariant() switch
        {
            "lineitem" => CalculationScope.LineItem,
            "document" => CalculationScope.Document,
            _ => Enum.TryParse<CalculationScope>(value, true, out var result) ? result : CalculationScope.LineItem
        };
    }

    public override void Write(System.Text.Json.Utf8JsonWriter writer, CalculationScope value, System.Text.Json.JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class CalculationParameter
{
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? DataType { get; set; }
    public bool IsRequired { get; set; } = true;
}

public class ExternalDataSource
{
    public string Name { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string? QueryOrEndpoint { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public class ClientSideCalculations
{
    public string? OnLoad { get; set; }
    public string? OnBeforeSave { get; set; }
    public string? OnLineItemAdd { get; set; }
    public string? OnLineItemRemove { get; set; }
    public Dictionary<string, string> CustomFunctions { get; set; } = new();
}

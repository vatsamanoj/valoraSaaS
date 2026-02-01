namespace Valora.Api.Application.Schemas.TemplateConfig;

public class DocumentTotalsConfig
{
    public Dictionary<string, TotalFieldConfig> Fields { get; set; } = new();
    public TotalsDisplayConfig DisplayConfig { get; set; } = new();
}

public class TotalFieldConfig
{
    public string Source { get; set; } = string.Empty;
    public string? Formula { get; set; }
    public string Label { get; set; } = string.Empty;
    public string DisplayPosition { get; set; } = "footer";
    public int DecimalPlaces { get; set; } = 2;
    public bool Editable { get; set; } = false;
    public bool IsReadOnly { get; set; } = true;
    public bool Highlight { get; set; } = false;
    public decimal? DefaultValue { get; set; }
}

public class TotalsDisplayConfig
{
    public string Layout { get; set; } = "stacked";
    public string Position { get; set; } = "bottom";
    public string? CurrencySymbol { get; set; }
    public bool ShowSeparator { get; set; } = true;
}

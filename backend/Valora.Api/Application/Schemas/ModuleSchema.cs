using System.Text.Json.Serialization;
using Valora.Api.Application.Schemas.TemplateConfig;

namespace Valora.Api.Application.Schemas
{
    public sealed record ModuleSchema(
    string TenantId,
    string Module,
    int Version,
    string ObjectType, // Master or Transaction
    Dictionary<string, FieldRule> Fields,
    List<string[]>? UniqueConstraints = null, // 🔥 NEW
    ModuleUi? Ui = null,
    bool ShouldPost = false, // 🔥 NEW: Auto-Posting Flag
    
    // ===== NEW: Template Configuration Extensions =====
    CalculationRulesConfig? CalculationRules = null,
    DocumentTotalsConfig? DocumentTotals = null,
    AttachmentConfig? AttachmentConfig = null,
    CloudStorageConfig? CloudStorage = null
    );

    public record ModuleUi(
        string? Title = null,
        string? Icon = null,
        object[]? Layout = null,
        bool EnterKeyNavigation = false,
        string[]? ListFields = null,
        string[]? FilterFields = null,
        [property: JsonPropertyName("totals")] TotalsConfig? Totals = null
    );

    public record TotalsConfig(
        string? TotalAmountField = null,
        string? DiscountAmountField = null,
        string? TaxAmountField = null,
        string? NetAmountField = null
    );

    public record FieldRule(
        bool Required,
        bool Unique = false,
        int? MaxLength = null,
        bool IsSensitive = false,
        bool AutoGenerate = false,
        string? Pattern = null, // e.g. "BILL-{YYYY}-{SEQ:6}"
        string Storage = "Core", // "Core" or "Extension" - using string to match JSON serialization easier
        UiHint? Ui = null
    );

    public static class FieldStorage 
    {
        public const string Core = "Core";
        public const string Extension = "Extension";
        public const string ExtraData = "Extension"; // Backward compat
    }
    public record UiHint(
    string Type,           // text, number, date, select, textarea
    string? Label = null,
    string? Mask = null,
    string[]? Options = null,
    string? Section = null,
    object? GridConfig = null,
    // Lookup Properties
    string? Lookup = null,
    string? LookupField = null,
    string? DisplayField = null,
    Dictionary<string, string>? Mapping = null,
    int? DecimalPlaces = null // 🔥 NEW
    );
}

// using Lab360.Application.Infrastructure.Metadata.Entities; // Removed missing reference
using System.Text.Json;
using System.Text.Json.Serialization;
using Valora.Api.Application.Schemas.TemplateConfig;

namespace Valora.Api.Application.Schemas
{
    public static class ModuleSchemaJson
    {
        private sealed record ModuleSchemaDto(
            [property: JsonPropertyName("objectType")] string? ObjectType,
            [property: JsonPropertyName("fields")] Dictionary<string, object>? Fields = null,
            [property: JsonPropertyName("uniqueConstraints")] List<string[]>? UniqueConstraints = null,
            [property: JsonPropertyName("ui")] ModuleUi? Ui = null,
            [property: JsonPropertyName("shouldPost")] bool ShouldPost = false, // 🔥 NEW

            // ===== NEW: Template Configuration Extensions =====
            [property: JsonPropertyName("calculationRules")] CalculationRulesConfig? CalculationRules = null,
            [property: JsonPropertyName("documentTotals")] DocumentTotalsConfig? DocumentTotals = null,
            [property: JsonPropertyName("attachmentConfig")] AttachmentConfig? AttachmentConfig = null,
            [property: JsonPropertyName("cloudStorage")] CloudStorageConfig? CloudStorage = null
        );

        public static ModuleSchema FromRawJson(
            string tenantId,
            string module,
            int version,
            string schemaJson)
        {
            var options = new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            ModuleSchemaDto? body;
            try
            {
                body = JsonSerializer.Deserialize<ModuleSchemaDto>(schemaJson, options);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to deserialize schema JSON for {module} v{version}: {ex.Message}. JSON: {schemaJson[..Math.Min(500, schemaJson.Length)]}", ex);
            }

            if (body == null)
                throw new Exception("Invalid SchemaJson - deserialization returned null");

            if (body.Fields == null)
                throw new Exception("Schema must contain a 'fields' property");

            var flattenedFields = new Dictionary<string, FieldRule>();
            FlattenFields(body.Fields, flattenedFields, options);

            return new ModuleSchema(
                TenantId: tenantId,
                Module: module,
                Version: version,
                ObjectType: body.ObjectType ?? "Master",
                Fields: flattenedFields,
                UniqueConstraints: body.UniqueConstraints,
                Ui: body.Ui,
                ShouldPost: body.ShouldPost,
                CalculationRules: body.CalculationRules,
                DocumentTotals: body.DocumentTotals,
                AttachmentConfig: body.AttachmentConfig,
                CloudStorage: body.CloudStorage
            );
        }

        private static void FlattenFields(Dictionary<string, object> fields, Dictionary<string, FieldRule> result, JsonSerializerOptions options, string prefix = "")
        {
            if (fields == null) return;

            foreach (var property in fields)
            {
                var value = property.Value;
                if (value is Dictionary<string, object> nestedDict)
                {
                    string fullKey = string.IsNullOrEmpty(prefix) ? property.Key : $"{prefix}.{property.Key}";

                    // Check if it's a FieldRule (has 'type' or 'ui') or a Nested Section
                    // Note: We use case-insensitive check for 'type', 'required', 'ui'
                    bool isField = nestedDict.ContainsKey("type") ||
                                   nestedDict.ContainsKey("required") ||
                                   nestedDict.ContainsKey("ui") ||
                                   nestedDict.ContainsKey("unique") ||
                                   nestedDict.ContainsKey("maxLength") ||
                                   nestedDict.ContainsKey("pattern") ||
                                   nestedDict.ContainsKey("autoGenerate") ||
                                   nestedDict.ContainsKey("isSensitive") ||
                                   nestedDict.ContainsKey("default") ||
                                   nestedDict.ContainsKey("defaultValue") ||
                                   nestedDict.ContainsKey("options") ||
                                   nestedDict.ContainsKey("readOnly");

                    if (isField)
                    {
                        try
                        {
                            var json = JsonSerializer.Serialize(nestedDict, options);
                            var fieldRule = JsonSerializer.Deserialize<FieldRule>(json, options);
                            if (fieldRule != null)
                            {
                                result[fullKey] = fieldRule;
                            }
                        }
                        catch
                        {
                            // Fallback or ignore invalid field
                        }
                    }
                    else
                    {
                        // Treat as Section (Recurse)
                        FlattenFields(nestedDict, result, options, fullKey);
                    }
                }
            }
        }


        /*
        public static ModuleSchema FromJson(ModuleSchemaEntity entity)
        {
            try 
            {
                var body = JsonSerializer.Deserialize<ModuleSchemaDto>(
                    entity.SchemaJson,
                    // JsonOptionsProvider.Default
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                ) ?? throw new Exception("Invalid SchemaJson");

                return new ModuleSchema(
                    TenantId: entity.TenantId,
                    Module: entity.Module,
                    Version: entity.Version,
                    ObjectType: body.ObjectType ?? "Master",
                    Fields: body.Fields,
                    UniqueConstraints: body.UniqueConstraints,
                    Ui: body.Ui
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ModuleSchemaJson] Error deserializing {entity.Module}: {ex.Message}");
                Console.WriteLine($"[ModuleSchemaJson] JSON: {entity.SchemaJson}");
                throw;
            }
        }
        */
        public static string ToJson(ModuleSchema schema)
        {
            // For Serialization, we have to create a new DTO that uses Dictionary
            // OR we rely on the fact that ModuleSchema.Fields is ALREADY flat.
            // If we want to preserve nesting on output, that's much harder (requires reversing normalization).
            // For now, we serialize the FLAT structure, which is valid JSON (just not nested).
            
            // To make it compatible with our modified DTO (which expects JsonElement), 
            // we can't use the DTO for serialization easily if DTO properties are JsonElement.
            // We should use an anonymous object or a separate WriteDto.

            var dto = new
            {
                objectType = schema.ObjectType,
                fields = schema.Fields,
                uniqueConstraints = schema.UniqueConstraints,
                ui = schema.Ui,
                shouldPost = schema.ShouldPost,
                calculationRules = schema.CalculationRules,
                documentTotals = schema.DocumentTotals,
                attachmentConfig = schema.AttachmentConfig,
                cloudStorage = schema.CloudStorage
            };

            return JsonSerializer.Serialize(
                dto,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                ) ?? throw new Exception("Invalid SchemaJson");
        }
    }
}

using Valora.Api.Application.Schemas;
// using Lab360.Application.Subscription; // Removed missing reference
using System.Text.Json;
namespace Lab360.Application.Common
{
    public static class FieldMasker
    {
        public static object Apply(
            object data,
            string module,
            ModuleSchema schema
            // SubscriptionContext subscription // Removed missing reference
            )
        {
            var json = JsonSerializer.Serialize(data);
            var dict = JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json)!;

            /*
            foreach (var row in dict)
            {
                foreach (var field in schema.Fields)
                {
                    if (!field.Value.IsSensitive)
                        continue;

                    var key = $"{module}.{field.Key}";

                    if (!subscription.AllowedSensitiveFields.Contains(key))
                    {
                        row[field.Key] = "***";
                    }
                }
            }
            */

            return dict;
        }
    }
}
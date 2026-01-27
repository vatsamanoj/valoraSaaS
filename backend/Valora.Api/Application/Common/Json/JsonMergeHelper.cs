using System.Text.Json;
namespace Lab360.Application.Common.Json
{
    public static class JsonMergeHelper
    {
        public static Dictionary<string, object?> Merge(
            string? existingJson,
            Dictionary<string, object?> incoming)
        {
            var result = new Dictionary<string, object?>();

            // 1️⃣ Load existing JSON
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                var existing =
                    JsonSerializer.Deserialize<Dictionary<string, object?>>(
                        existingJson,
                        // JsonOptionsProvider.Default
                        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                        );

                if (existing != null)
                {
                    foreach (var kv in existing)
                        result[kv.Key] = kv.Value;
                }
            }

            // 2️⃣ Apply incoming changes (overwrite / add)
            foreach (var (key, value) in incoming)
            {
                // Optional: allow explicit null to remove field
                if (value == null)
                {
                    result.Remove(key);
                }
                else
                {
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
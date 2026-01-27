using System.Text.Json;

namespace Lab360.Application.Common.Json
{
    public static class JsonValueConverter
    {
        public static object? Normalize(object? value)
        {
            if (value == null)
                return null;

            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => GetNumber(element),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,

                    // Objects / Arrays → store as JSON text
                    JsonValueKind.Object or JsonValueKind.Array
                        => element.GetRawText(),

                    _ => element.GetRawText()
                };
            }

            return value;
        }

        private static object GetNumber(JsonElement element)
        {
            if (element.TryGetInt32(out var i)) return i;
            if (element.TryGetInt64(out var l)) return l;
            if (element.TryGetDecimal(out var d)) return d;
            if (element.TryGetDouble(out var dbl)) return dbl;

            return element.GetRawText();
        }
    }
}

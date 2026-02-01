using MongoDB.Bson;

namespace Valora.Api.Extensions;

public static class BsonExtensions
{
    public static Dictionary<string, object?> ToDictionaryNative(this BsonDocument doc)
    {
        return doc.ToDictionary(x => x.Name, x => ConvertBsonValue(x.Value));
    }

    private static object? ConvertBsonValue(BsonValue value)
    {
        switch (value.BsonType)
        {
            case BsonType.Double: return value.AsDouble;
            case BsonType.String: return value.AsString;
            case BsonType.Document: return value.AsBsonDocument.ToDictionaryNative();
            case BsonType.Array: return value.AsBsonArray.Select(ConvertBsonValue).ToList();
            case BsonType.Int32: return value.AsInt32;
            case BsonType.Int64: return value.AsInt64;
            case BsonType.Boolean: return value.AsBoolean;
            case BsonType.DateTime: return value.ToUniversalTime();
            case BsonType.Decimal128: return (decimal)value.AsDecimal128;
            case BsonType.ObjectId: return value.ToString();
            case BsonType.Null: return null;
            default: return value.ToString();
        }
    }
}

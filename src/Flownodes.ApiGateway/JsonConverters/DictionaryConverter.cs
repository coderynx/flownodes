using System.Text.Json;
using System.Text.Json.Serialization;

namespace Flownodes.ApiGateway.JsonConverters;

public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object?>>
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(Dictionary<string, object>)
               || typeToConvert == typeof(Dictionary<string, object?>);
    }

    public override Dictionary<string, object?> Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");

        var dictionary = new Dictionary<string, object?>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return dictionary;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("JsonTokenType was not PropertyName");

            var propertyName = reader.GetString();

            if (string.IsNullOrWhiteSpace(propertyName)) throw new JsonException("Failed to get property name");

            reader.Read();

            dictionary.Add(propertyName!, ExtractValue(ref reader, options));
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, object?> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (IDictionary<string, object?>)value, options);
    }

    private object? ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                if (reader.TryGetDateTime(out var date)) return date;

                return reader.GetString();
            case JsonTokenType.False:
                return false;
            case JsonTokenType.True:
                return true;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.TryGetInt64(out var result) ? result : reader.GetDecimal();
            case JsonTokenType.StartObject:
                return Read(ref reader, null!, options);
            case JsonTokenType.StartArray:
                var list = new List<object?>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    list.Add(ExtractValue(ref reader, options));

                return list;
            case JsonTokenType.None:
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            default:
                throw new JsonException($"'{reader.TokenType}' is not supported");
        }
    }
}
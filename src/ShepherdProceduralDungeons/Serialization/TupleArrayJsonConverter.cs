using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for arrays of value tuples (Type, int).
/// </summary>
public sealed class TupleArrayJsonConverter<TEnum> : JsonConverter<IReadOnlyList<(TEnum Type, int Count)>> where TEnum : Enum
{
    public override IReadOnlyList<(TEnum Type, int Count)> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array for tuple array.");
        }

        var list = new List<(TEnum Type, int Count)>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;
                
                var typeStr = root.GetProperty("type").GetString();
                if (!Enum.TryParse(typeof(TEnum), typeStr, true, out var typeObj) || typeObj is not TEnum type)
                {
                    throw new JsonException($"Invalid enum value: {typeStr}");
                }
                
                var count = root.GetProperty("count").GetInt32();
                list.Add((type, count));
            }
        }

        return list.AsReadOnly();
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlyList<(TEnum Type, int Count)> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("type");
            JsonSerializer.Serialize(writer, item.Type, options);
            writer.WriteNumber("count", item.Count);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
}

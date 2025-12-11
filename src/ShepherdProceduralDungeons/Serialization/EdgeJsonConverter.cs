using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for Edge flags enum.
/// </summary>
public sealed class EdgeJsonConverter : JsonConverter<Edge>
{
    public override Edge Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<Edge>(value, true, out var result))
            {
                return result;
            }
            throw new JsonException($"Invalid Edge value: {value}");
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            Edge result = Edge.None;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    var edgeValue = reader.GetString();
                    if (Enum.TryParse<Edge>(edgeValue, true, out var edge))
                    {
                        result |= edge;
                    }
                    else
                    {
                        throw new JsonException($"Invalid Edge value: {edgeValue}");
                    }
                }
            }
            return result;
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return (Edge)reader.GetInt32();
        }

        throw new JsonException($"Unexpected token type for Edge: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Edge value, JsonSerializerOptions options)
    {
        // Write as array of flag names
        writer.WriteStartArray();
        if (value.HasFlag(Edge.North)) writer.WriteStringValue("North");
        if (value.HasFlag(Edge.South)) writer.WriteStringValue("South");
        if (value.HasFlag(Edge.East)) writer.WriteStringValue("East");
        if (value.HasFlag(Edge.West)) writer.WriteStringValue("West");
        writer.WriteEndArray();
    }
}

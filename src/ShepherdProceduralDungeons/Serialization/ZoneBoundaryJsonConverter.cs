using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Configuration;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for ZoneBoundary polymorphic types.
/// </summary>
public sealed class ZoneBoundaryJsonConverter : JsonConverter<ZoneBoundary>
{
    public override ZoneBoundary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for ZoneBoundary.");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var typeElement))
        {
            throw new JsonException("ZoneBoundary must have a 'type' property.");
        }

        var type = typeElement.GetString();
        return type switch
        {
            "DistanceBased" => JsonSerializer.Deserialize<ZoneBoundary.DistanceBased>(root.GetRawText(), options) 
                ?? throw new JsonException("Failed to deserialize DistanceBased boundary."),
            "CriticalPathBased" => JsonSerializer.Deserialize<ZoneBoundary.CriticalPathBased>(root.GetRawText(), options) 
                ?? throw new JsonException("Failed to deserialize CriticalPathBased boundary."),
            _ => throw new JsonException($"Unknown ZoneBoundary type: {type}")
        };
    }

    public override void Write(Utf8JsonWriter writer, ZoneBoundary value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case ZoneBoundary.DistanceBased distanceBased:
                writer.WriteString("type", "DistanceBased");
                writer.WriteNumber("minDistance", distanceBased.MinDistance);
                writer.WriteNumber("maxDistance", distanceBased.MaxDistance);
                break;

            case ZoneBoundary.CriticalPathBased criticalPathBased:
                writer.WriteString("type", "CriticalPathBased");
                writer.WriteNumber("startPercent", criticalPathBased.StartPercent);
                writer.WriteNumber("endPercent", criticalPathBased.EndPercent);
                break;

            default:
                throw new JsonException($"Unknown ZoneBoundary type: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for Cell struct.
/// </summary>
public sealed class CellJsonConverter : JsonConverter<Cell>
{
    public override Cell Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object for Cell.");
        }

        int x = 0, y = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read();

                switch (propertyName?.ToLowerInvariant())
                {
                    case "x":
                        x = reader.GetInt32();
                        break;
                    case "y":
                        y = reader.GetInt32();
                        break;
                }
            }
        }

        return new Cell(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Cell value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}

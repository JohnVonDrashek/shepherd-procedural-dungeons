using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Exceptions;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for generic enum types with validation.
/// </summary>
public sealed class EnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (Enum.TryParse<T>(value, true, out var result))
            {
                return result;
            }
            throw new InvalidConfigurationException($"Invalid enum value '{value}' for type {typeof(T).Name}.");
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            if (Enum.IsDefined(typeof(T), value))
            {
                return (T)(object)value;
            }
            throw new InvalidConfigurationException($"Invalid enum value '{value}' for type {typeof(T).Name}.");
        }

        throw new JsonException($"Unexpected token type for enum: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

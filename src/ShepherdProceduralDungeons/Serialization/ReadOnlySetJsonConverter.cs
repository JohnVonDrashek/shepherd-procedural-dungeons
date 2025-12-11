using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for IReadOnlySet collections.
/// </summary>
public sealed class ReadOnlySetJsonConverter<T> : JsonConverter<IReadOnlySet<T>>
{
    public override IReadOnlySet<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = JsonSerializer.Deserialize<List<T>>(ref reader, options) ?? new List<T>();
        return new HashSet<T>(list);
    }

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<T> value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.ToList(), options);
    }
}

using System.Text.Json;
using ShepherdProceduralDungeons.Configuration;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// Extension methods for convenient configuration serialization.
/// </summary>
public static class ConfigurationSerializationExtensions
{
    /// <summary>
    /// Serializes a FloorConfig to JSON string.
    /// </summary>
    public static string ToJson<TRoomType>(this FloorConfig<TRoomType> config) where TRoomType : Enum
    {
        var serializer = new ConfigurationSerializer<TRoomType>();
        return serializer.SerializeToJson(config, prettyPrint: true);
    }

    /// <summary>
    /// Deserializes a JSON string to FloorConfig.
    /// </summary>
    public static FloorConfig<TRoomType> FromJson<TRoomType>(string json) where TRoomType : Enum
    {
        var serializer = new ConfigurationSerializer<TRoomType>();
        return serializer.DeserializeFromJson(json);
    }

    /// <summary>
    /// Saves a FloorConfig to a file.
    /// </summary>
    public static void SaveToFile<TRoomType>(this FloorConfig<TRoomType> config, string filePath) where TRoomType : Enum
    {
        var json = config.ToJson();
        File.WriteAllText(filePath, json);
    }

    /// <summary>
    /// Loads a FloorConfig from a file.
    /// </summary>
    public static FloorConfig<TRoomType> LoadFromFile<TRoomType>(string filePath) where TRoomType : Enum
    {
        var json = File.ReadAllText(filePath);
        return FromJson<TRoomType>(json);
    }
}

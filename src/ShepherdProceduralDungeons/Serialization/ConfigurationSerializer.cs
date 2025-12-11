using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// Serializes and deserializes dungeon configurations to/from JSON.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class ConfigurationSerializer<TRoomType> where TRoomType : Enum
{
    private readonly JsonSerializerOptions _defaultOptions;

    /// <summary>
    /// Creates a new configuration serializer with default options.
    /// </summary>
    public ConfigurationSerializer()
    {
        _defaultOptions = CreateDefaultOptions();
    }

    /// <summary>
    /// Serializes a FloorConfig to JSON string.
    /// </summary>
    /// <param name="config">The configuration to serialize.</param>
    /// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the configuration.</returns>
    public string SerializeToJson(FloorConfig<TRoomType> config, bool prettyPrint = true)
    {
        var options = prettyPrint ? CreatePrettyPrintOptions() : _defaultOptions;
        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Serializes a FloorConfig to JSON string with custom options.
    /// </summary>
    /// <param name="config">The configuration to serialize.</param>
    /// <param name="options">Custom JSON serializer options.</param>
    /// <returns>A JSON string representation of the configuration.</returns>
    public string SerializeToJson(FloorConfig<TRoomType> config, JsonSerializerOptions options)
    {
        // Merge with our converters
        var mergedOptions = MergeOptions(options);
        return JsonSerializer.Serialize(config, mergedOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to FloorConfig.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A FloorConfig instance.</returns>
    /// <exception cref="InvalidConfigurationException">Thrown when the JSON is invalid or missing required fields.</exception>
    public FloorConfig<TRoomType> DeserializeFromJson(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<FloorConfig<TRoomType>>(json, _defaultOptions);
            if (config == null)
            {
                throw new InvalidConfigurationException("Deserialized configuration is null. Check that all required fields are present.");
            }
            ValidateConfig(config);
            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidConfigurationException($"Invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidConfigurationException($"Deserialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Deserializes a JSON string to FloorConfig with custom options.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="options">Custom JSON serializer options.</param>
    /// <returns>A FloorConfig instance.</returns>
    /// <exception cref="InvalidConfigurationException">Thrown when the JSON is invalid or missing required fields.</exception>
    public FloorConfig<TRoomType> DeserializeFromJson(string json, JsonSerializerOptions options)
    {
        try
        {
            var mergedOptions = MergeOptions(options);
            var config = JsonSerializer.Deserialize<FloorConfig<TRoomType>>(json, mergedOptions);
            if (config == null)
            {
                throw new InvalidConfigurationException("Deserialized configuration is null. Check that all required fields are present.");
            }
            ValidateConfig(config);
            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidConfigurationException($"Invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidConfigurationException($"Deserialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Serializes a MultiFloorConfig to JSON string.
    /// </summary>
    /// <param name="config">The configuration to serialize.</param>
    /// <param name="prettyPrint">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the configuration.</returns>
    public string SerializeToJson(MultiFloorConfig<TRoomType> config, bool prettyPrint = true)
    {
        var options = prettyPrint ? CreatePrettyPrintOptions() : _defaultOptions;
        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes a JSON string to MultiFloorConfig.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A MultiFloorConfig instance.</returns>
    /// <exception cref="InvalidConfigurationException">Thrown when the JSON is invalid or missing required fields.</exception>
    public MultiFloorConfig<TRoomType> DeserializeMultiFloorConfigFromJson(string json)
    {
        try
        {
            var config = JsonSerializer.Deserialize<MultiFloorConfig<TRoomType>>(json, _defaultOptions);
            if (config == null)
            {
                throw new InvalidConfigurationException("Deserialized configuration is null. Check that all required fields are present.");
            }
            return config;
        }
        catch (JsonException ex)
        {
            throw new InvalidConfigurationException($"Invalid JSON: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidConfigurationException($"Deserialization failed: {ex.Message}");
        }
    }


    private JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        AddConverters(options);
        return options;
    }

    private JsonSerializerOptions CreatePrettyPrintOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        AddConverters(options);
        return options;
    }

    private void AddConverters(JsonSerializerOptions options)
    {
        options.Converters.Add(new CellJsonConverter());
        options.Converters.Add(new EdgeJsonConverter());
        options.Converters.Add(new ZoneBoundaryJsonConverter());
        options.Converters.Add(new RoomTemplateJsonConverter<TRoomType>());
        options.Converters.Add(new ConstraintJsonConverter<TRoomType>());
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new TupleArrayJsonConverter<TRoomType>());
        options.Converters.Add(new ReadOnlySetJsonConverter<TRoomType>());
    }

    private JsonSerializerOptions MergeOptions(JsonSerializerOptions customOptions)
    {
        // Create a copy of custom options and add our converters
        var merged = new JsonSerializerOptions(customOptions);
        AddConverters(merged);
        return merged;
    }

    private void ValidateConfig(FloorConfig<TRoomType> config)
    {
        // Basic validation - templates can be empty for deserialization
        // The generator will validate when actually generating
    }
}

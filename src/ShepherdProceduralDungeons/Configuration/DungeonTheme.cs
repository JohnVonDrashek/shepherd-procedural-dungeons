using ShepherdProceduralDungeons.Exceptions;

namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Represents a complete dungeon theme with all generation parameters.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class DungeonTheme<TRoomType> where TRoomType : Enum
{
    /// <summary>Unique identifier for this theme.</summary>
    public required string Id { get; init; }
    
    /// <summary>Display name for this theme.</summary>
    public required string Name { get; init; }
    
    /// <summary>Description of this theme's characteristics.</summary>
    public string? Description { get; init; }
    
    /// <summary>Base floor configuration for this theme.</summary>
    public required FloorConfig<TRoomType> BaseConfig { get; init; }
    
    /// <summary>Optional zone configurations for this theme.</summary>
    public IReadOnlyList<Zone<TRoomType>>? Zones { get; init; }
    
    /// <summary>Tags for categorizing themes (e.g., "underground", "structured", "organic").</summary>
    public IReadOnlySet<string> Tags { get; init; } = new HashSet<string>();
    
    /// <summary>
    /// Creates a FloorConfig from this theme with optional overrides.
    /// </summary>
    public FloorConfig<TRoomType> ToFloorConfig(ThemeOverrides? overrides = null)
    {
        // Validate theme properties
        if (string.IsNullOrWhiteSpace(Id))
            throw new InvalidConfigurationException("Theme Id cannot be empty");
        
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidConfigurationException("Theme Name cannot be empty");

        // Apply overrides to base config
        var seed = overrides?.Seed ?? BaseConfig.Seed;
        var roomCount = overrides?.RoomCount ?? BaseConfig.RoomCount;
        var branchingFactor = overrides?.BranchingFactor ?? BaseConfig.BranchingFactor;
        var hallwayMode = overrides?.HallwayMode ?? BaseConfig.HallwayMode;
        var graphAlgorithm = overrides?.GraphAlgorithm ?? BaseConfig.GraphAlgorithm;

        // Create new config with overrides applied
        var config = new FloorConfig<TRoomType>
        {
            Seed = seed,
            RoomCount = roomCount,
            SpawnRoomType = BaseConfig.SpawnRoomType,
            BossRoomType = BaseConfig.BossRoomType,
            DefaultRoomType = BaseConfig.DefaultRoomType,
            RoomRequirements = BaseConfig.RoomRequirements,
            Constraints = BaseConfig.Constraints,
            Templates = BaseConfig.Templates,
            BranchingFactor = branchingFactor,
            HallwayMode = hallwayMode,
            Zones = Zones ?? BaseConfig.Zones,
            SecretPassageConfig = BaseConfig.SecretPassageConfig,
            GraphAlgorithm = graphAlgorithm,
            GridBasedConfig = BaseConfig.GridBasedConfig,
            CellularAutomataConfig = BaseConfig.CellularAutomataConfig,
            MazeBasedConfig = BaseConfig.MazeBasedConfig,
            HubAndSpokeConfig = BaseConfig.HubAndSpokeConfig,
            DifficultyConfig = BaseConfig.DifficultyConfig
        };

        // Validate the config by attempting to create a generator
        // This will throw InvalidConfigurationException if invalid
        try
        {
            var generator = new FloorGenerator<TRoomType>();
            // Use reflection to call the private ValidateConfig method
            var validateMethod = typeof(FloorGenerator<TRoomType>).GetMethod("ValidateConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (validateMethod != null)
            {
                validateMethod.Invoke(generator, new object[] { config });
            }
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is InvalidConfigurationException)
        {
            throw ex.InnerException;
        }

        return config;
    }
    
    /// <summary>
    /// Creates a new theme by combining this theme with another (other takes precedence).
    /// </summary>
    public DungeonTheme<TRoomType> Combine(DungeonTheme<TRoomType> other)
    {
        // Merge zones - combine both lists
        var combinedZones = new List<Zone<TRoomType>>();
        if (Zones != null)
            combinedZones.AddRange(Zones);
        if (other.Zones != null)
            combinedZones.AddRange(other.Zones);

        // Merge tags
        var combinedTags = new HashSet<string>(Tags);
        foreach (var tag in other.Tags)
            combinedTags.Add(tag);

        return new DungeonTheme<TRoomType>
        {
            Id = other.Id, // Other takes precedence
            Name = other.Name,
            Description = other.Description ?? Description,
            BaseConfig = other.BaseConfig, // Other's config takes precedence
            Zones = combinedZones.Count > 0 ? combinedZones : null,
            Tags = combinedTags
        };
    }
}

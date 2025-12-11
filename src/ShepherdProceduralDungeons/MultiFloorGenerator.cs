using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;

namespace ShepherdProceduralDungeons;

/// <summary>
/// Main entry point for generating multi-floor procedural dungeons.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MultiFloorGenerator<TRoomType> where TRoomType : Enum
{
    private readonly ISpatialSolver<TRoomType>? _spatialSolver;

    /// <summary>
    /// Creates a new multi-floor generator with the default spatial solver.
    /// </summary>
    public MultiFloorGenerator()
    {
    }

    /// <summary>
    /// Creates a new multi-floor generator with a custom spatial solver.
    /// </summary>
    public MultiFloorGenerator(ISpatialSolver<TRoomType> spatialSolver)
    {
        _spatialSolver = spatialSolver;
    }

    /// <summary>
    /// Generates a multi-floor dungeon layout from the given configuration.
    /// </summary>
    /// <param name="config">Multi-floor generation configuration.</param>
    /// <returns>The generated multi-floor dungeon layout.</returns>
    /// <exception cref="InvalidConfigurationException">Config is invalid.</exception>
    /// <exception cref="ConstraintViolationException">Constraints cannot be satisfied.</exception>
    /// <exception cref="SpatialPlacementException">Rooms cannot be placed.</exception>
    public MultiFloorLayout<TRoomType> Generate(MultiFloorConfig<TRoomType> config)
    {
        // Validate configuration
        ValidateConfig(config);

        // Generate each floor independently
        var floorGenerator = _spatialSolver != null 
            ? new FloorGenerator<TRoomType>(_spatialSolver)
            : new FloorGenerator<TRoomType>();

        var floors = new List<FloorLayout<TRoomType>>();
        for (int i = 0; i < config.Floors.Count; i++)
        {
            var floorConfig = config.Floors[i];
            
            // Generate floor with floor index for floor-aware constraints
            // Use reflection to call the internal Generate method, or make it public/internal
            // Actually, let's use a different approach - we'll set floor indices before calling Generate
            SetFloorIndexOnConstraints(floorConfig.Constraints, i);
            
            var floor = floorGenerator.Generate(floorConfig, i);
            floors.Add(floor);
        }

        // Validate connections
        ValidateConnections(config, floors);

        // Build output
        return new MultiFloorLayout<TRoomType>
        {
            Floors = floors,
            Connections = config.Connections,
            Seed = config.Seed,
            TotalFloorCount = config.Floors.Count
        };
    }

    private void SetFloorIndexOnConstraints(IReadOnlyList<Constraints.IConstraint<TRoomType>> constraints, int floorIndex)
    {
        foreach (var constraint in constraints)
        {
            if (constraint is Constraints.IFloorAwareConstraint<TRoomType> floorAware)
            {
                floorAware.SetFloorIndex(floorIndex);
            }
        }
    }

    private void ValidateConfig(MultiFloorConfig<TRoomType> config)
    {
        if (config.Floors == null || config.Floors.Count == 0)
            throw new InvalidConfigurationException("At least one floor must be specified");

        if (config.Connections == null)
            throw new InvalidConfigurationException("Connections cannot be null (use empty array for no connections)");

        // Validate floor indices in connections
        foreach (var connection in config.Connections)
        {
            if (connection.FromFloorIndex < 0 || connection.FromFloorIndex >= config.Floors.Count)
                throw new InvalidConfigurationException($"Invalid FromFloorIndex {connection.FromFloorIndex} (must be 0-{config.Floors.Count - 1})");

            if (connection.ToFloorIndex < 0 || connection.ToFloorIndex >= config.Floors.Count)
                throw new InvalidConfigurationException($"Invalid ToFloorIndex {connection.ToFloorIndex} (must be 0-{config.Floors.Count - 1})");

            if (connection.FromFloorIndex == connection.ToFloorIndex)
                throw new InvalidConfigurationException("Floor connections must connect different floors");
        }
    }

    private void ValidateConnections(MultiFloorConfig<TRoomType> config, IReadOnlyList<FloorLayout<TRoomType>> floors)
    {
        foreach (var connection in config.Connections)
        {
            var fromFloor = floors[connection.FromFloorIndex];
            var toFloor = floors[connection.ToFloorIndex];

            // Check if rooms exist
            var fromRoom = fromFloor.GetRoom(connection.FromRoomNodeId);
            if (fromRoom == null)
                throw new InvalidConfigurationException($"Connection references non-existent room {connection.FromRoomNodeId} on floor {connection.FromFloorIndex}");

            var toRoom = toFloor.GetRoom(connection.ToRoomNodeId);
            if (toRoom == null)
                throw new InvalidConfigurationException($"Connection references non-existent room {connection.ToRoomNodeId} on floor {connection.ToFloorIndex}");
        }
    }
}

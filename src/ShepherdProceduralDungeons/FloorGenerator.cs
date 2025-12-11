using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons;

/// <summary>
/// Main entry point for generating procedural dungeon floors.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class FloorGenerator<TRoomType> where TRoomType : Enum
{
    private readonly ISpatialSolver<TRoomType>? _spatialSolver;

    /// <summary>
    /// Creates a new floor generator with the default spatial solver.
    /// </summary>
    public FloorGenerator()
    {
    }

    /// <summary>
    /// Creates a new floor generator with a custom spatial solver.
    /// </summary>
    public FloorGenerator(ISpatialSolver<TRoomType> spatialSolver)
    {
        _spatialSolver = spatialSolver;
    }

    /// <summary>
    /// Generates a floor layout from the given configuration.
    /// </summary>
    /// <param name="config">Generation configuration.</param>
    /// <returns>The generated floor layout.</returns>
    /// <exception cref="InvalidConfigurationException">Config is invalid.</exception>
    /// <exception cref="ConstraintViolationException">Constraints cannot be satisfied.</exception>
    /// <exception cref="SpatialPlacementException">Rooms cannot be placed.</exception>
    public FloorLayout<TRoomType> Generate(FloorConfig<TRoomType> config)
    {
        return Generate(config, -1);
    }

    /// <summary>
    /// Generates a floor layout from the given configuration with optional floor index for floor-aware constraints.
    /// </summary>
    /// <param name="config">Generation configuration.</param>
    /// <param name="floorIndex">Floor index for floor-aware constraints. Use -1 for single-floor generation.</param>
    /// <returns>The generated floor layout.</returns>
    /// <exception cref="InvalidConfigurationException">Config is invalid.</exception>
    /// <exception cref="ConstraintViolationException">Constraints cannot be satisfied.</exception>
    /// <exception cref="SpatialPlacementException">Rooms cannot be placed.</exception>
    internal FloorLayout<TRoomType> Generate(FloorConfig<TRoomType> config, int floorIndex)
    {
        // Validate configuration
        ValidateConfig(config);

        // Master RNG from seed
        var masterRng = new Random(config.Seed);

        // Derive child seeds for each phase (ensures phase order doesn't affect other phases)
        int graphSeed = masterRng.Next();
        int typeSeed = masterRng.Next();
        int templateSeed = masterRng.Next();
        int spatialSeed = masterRng.Next();
        int hallwaySeed = masterRng.Next();

        // Each phase gets its own RNG
        var graphRng = new Random(graphSeed);
        var typeRng = new Random(typeSeed);
        var templateRng = new Random(templateSeed);
        var spatialRng = new Random(spatialSeed);
        var hallwayRng = new Random(hallwaySeed);

        // 1. Generate graph
        var graphGenerator = new GraphGenerator();
        var graph = graphGenerator.Generate(config.RoomCount, config.BranchingFactor, graphRng);

        // 2. Assign room types (pass floor index for floor-aware constraints)
        var typeAssigner = new RoomTypeAssigner<TRoomType>();
        typeAssigner.AssignTypes(
            graph,
            config.SpawnRoomType,
            config.BossRoomType,
            config.DefaultRoomType,
            config.RoomRequirements,
            config.Constraints,
            typeRng,
            out var assignments,
            floorIndex);

        // 3. Organize templates by room type
        var templatesByType = new Dictionary<TRoomType, List<RoomTemplate<TRoomType>>>();
        foreach (var template in config.Templates)
        {
            foreach (var roomType in template.ValidRoomTypes)
            {
                if (!templatesByType.TryGetValue(roomType, out var list))
                {
                    list = new List<RoomTemplate<TRoomType>>();
                    templatesByType[roomType] = list;
                }
                if (!list.Contains(template))
                {
                    list.Add(template);
                }
            }
        }

        var templatesByTypeReadOnly = templatesByType.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<RoomTemplate<TRoomType>>)kvp.Value);

        // 4. Spatial placement
        var spatialSolver = _spatialSolver ?? new IncrementalSolver<TRoomType>();
        var placedRooms = spatialSolver.Solve(graph, assignments, templatesByTypeReadOnly, config.HallwayMode, spatialRng);

        // 5. Generate hallways
        var occupiedCells = new HashSet<Cell>(placedRooms.SelectMany(r => r.GetWorldCells()));
        var hallwayGenerator = new HallwayGenerator<TRoomType>();
        var hallways = hallwayGenerator.Generate(placedRooms, graph, occupiedCells, hallwayRng);

        // 6. Place doors
        var doors = PlaceDoors(placedRooms, hallways, graph);

        // 7. Build output
        return new FloorLayout<TRoomType>
        {
            Rooms = placedRooms,
            Hallways = hallways,
            Doors = doors,
            Seed = config.Seed,
            CriticalPath = graph.CriticalPath,
            SpawnRoomId = graph.StartNodeId,
            BossRoomId = graph.BossNodeId
        };
    }

    private void ValidateConfig(FloorConfig<TRoomType> config)
    {
        if (config.RoomCount < 2)
            throw new InvalidConfigurationException("RoomCount must be at least 2 (spawn + boss)");

        if (config.RoomCount < 1 + 1 + config.RoomRequirements.Sum(r => r.Count))
            throw new InvalidConfigurationException("RoomCount is too small for spawn + boss + all required rooms");

        // Check templates exist for all required types
        var requiredTypes = new HashSet<TRoomType> { config.SpawnRoomType, config.BossRoomType, config.DefaultRoomType };
        foreach (var req in config.RoomRequirements)
            requiredTypes.Add(req.Type);

        var availableTypes = config.Templates.SelectMany(t => t.ValidRoomTypes).ToHashSet();

        foreach (var required in requiredTypes)
        {
            if (!availableTypes.Contains(required))
                throw new InvalidConfigurationException($"No template available for room type {required}");
        }

        if (config.BranchingFactor < 0 || config.BranchingFactor > 1)
            throw new InvalidConfigurationException("BranchingFactor must be between 0.0 and 1.0");
    }

    private IReadOnlyList<Door> PlaceDoors(
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        IReadOnlyList<Hallway> hallways,
        FloorGraph graph)
    {
        var doors = new List<Door>();

        // Add hallway doors
        foreach (var hallway in hallways)
        {
            doors.Add(hallway.DoorA);
            doors.Add(hallway.DoorB);
        }

        // Add direct room-to-room doors (connections without hallways)
        foreach (var conn in graph.Connections.Where(c => !c.RequiresHallway))
        {
            var roomA = rooms.First(r => r.NodeId == conn.NodeAId);
            var roomB = rooms.First(r => r.NodeId == conn.NodeBId);

            // Find compatible door edges
            var doorPair = FindDirectDoorPair(roomA, roomB);
            if (doorPair.HasValue)
            {
                var (doorA, doorB) = doorPair.Value;
                doors.Add(new Door
                {
                    Position = doorA.WorldCell,
                    Edge = doorA.Edge,
                    ConnectsToRoomId = roomB.NodeId
                });
                doors.Add(new Door
                {
                    Position = doorB.WorldCell,
                    Edge = doorB.Edge,
                    ConnectsToRoomId = roomA.NodeId
                });
            }
        }

        return doors;
    }

    private ((Cell WorldCell, Edge Edge) DoorA, (Cell WorldCell, Edge Edge) DoorB)? FindDirectDoorPair(
        PlacedRoom<TRoomType> roomA,
        PlacedRoom<TRoomType> roomB)
    {
        var edgesA = roomA.GetExteriorEdgesWorld()
            .Where(e => roomA.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .ToList();

        var edgesB = roomB.GetExteriorEdgesWorld()
            .Where(e => roomB.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .ToList();

        // Find adjacent edges (touching cells with opposite edges)
        foreach (var edgeA in edgesA)
        {
            foreach (var edgeB in edgesB)
            {
                if (edgeA.Edge.Opposite() == edgeB.Edge)
                {
                    var adjacentCellA = GetAdjacentCell(edgeA.WorldCell, edgeA.Edge);
                    if (adjacentCellA == edgeB.WorldCell)
                    {
                        return ((edgeA.WorldCell, edgeA.Edge), (edgeB.WorldCell, edgeB.Edge));
                    }
                }
            }
        }

        return null;
    }

    private Cell GetAdjacentCell(Cell cell, Edge edge)
    {
        return edge switch
        {
            Edge.North => cell.North,
            Edge.South => cell.South,
            Edge.East => cell.East,
            Edge.West => cell.West,
            _ => throw new ArgumentException($"Invalid edge: {edge}")
        };
    }
}


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

        // 2. Prepare zone data structures (but don't assign zones yet - need critical path first)
        Dictionary<int, string>? zoneAssignments = null;
        Dictionary<string, IReadOnlyList<(TRoomType type, int count)>>? zoneRoomRequirements = null;
        Dictionary<string, IReadOnlyList<RoomTemplate<TRoomType>>>? zoneTemplates = null;

        if (config.Zones != null && config.Zones.Count > 0)
        {
            // Build zone-specific room requirements and templates
            zoneRoomRequirements = new Dictionary<string, IReadOnlyList<(TRoomType type, int count)>>();
            zoneTemplates = new Dictionary<string, IReadOnlyList<RoomTemplate<TRoomType>>>();
            
            foreach (var zone in config.Zones)
            {
                if (zone.RoomRequirements != null && zone.RoomRequirements.Count > 0)
                {
                    zoneRoomRequirements[zone.Id] = zone.RoomRequirements;
                }
                if (zone.Templates != null && zone.Templates.Count > 0)
                {
                    zoneTemplates[zone.Id] = zone.Templates;
                }
            }

            // For zone-specific room requirements, we need preliminary zone assignments
            // Assign distance-based zones temporarily (critical path zones will be assigned later)
            var distanceBasedZones = config.Zones.Where(z => z.Boundary is ZoneBoundary.DistanceBased).ToList();
            if (distanceBasedZones.Count > 0)
            {
                var zoneAssigner = new ZoneAssigner<TRoomType>();
                // Set a temporary critical path for zone assignment (just start node)
                graph.CriticalPath = new[] { graph.StartNodeId };
                graph.Nodes.First(n => n.Id == graph.StartNodeId).IsOnCriticalPath = true;
                zoneAssignments = zoneAssigner.AssignZones(graph, distanceBasedZones);
            }
        }

        // 3. Assign room types (pass floor index and zone info for zone-aware constraints)
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
            floorIndex,
            zoneAssignments,
            zoneRoomRequirements);

        // 4. Assign all zones now that critical path is available (in config order, first match wins)
        if (config.Zones != null && config.Zones.Count > 0)
        {
            var zoneAssigner = new ZoneAssigner<TRoomType>();
            zoneAssignments = zoneAssigner.AssignZones(graph, config.Zones);
        }

        // 5. Organize templates by room type
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

        // 6. Spatial placement (with zone-aware template selection)
        var spatialSolver = _spatialSolver ?? new IncrementalSolver<TRoomType>();
        if (spatialSolver is IncrementalSolver<TRoomType> incrementalSolver)
        {
            incrementalSolver.SetZoneInfo(zoneAssignments, zoneTemplates);
        }
        var placedRooms = spatialSolver.Solve(graph, assignments, templatesByTypeReadOnly, config.HallwayMode, spatialRng);

        // 7. Generate hallways
        var occupiedCells = new HashSet<Cell>(placedRooms.SelectMany(r => r.GetWorldCells()));
        var hallwayGenerator = new HallwayGenerator<TRoomType>();
        var hallways = hallwayGenerator.Generate(placedRooms, graph, occupiedCells, hallwayRng);

        // 8. Place doors
        var doors = PlaceDoors(placedRooms, hallways, graph);

        // 9. Identify transition rooms (rooms connecting different zones)
        var transitionRooms = new List<PlacedRoom<TRoomType>>();
        if (zoneAssignments != null && zoneAssignments.Count > 0)
        {
            foreach (var room in placedRooms)
            {
                if (!zoneAssignments.TryGetValue(room.NodeId, out var roomZone))
                    continue;

                // Check if this room connects to rooms in different zones
                var roomNode = graph.Nodes.First(n => n.Id == room.NodeId);
                var hasDifferentZoneConnection = roomNode.Connections.Any(conn =>
                {
                    var otherNodeId = conn.GetOtherNodeId(room.NodeId);
                    return zoneAssignments.TryGetValue(otherNodeId, out var otherZone) && otherZone != roomZone;
                });

                if (hasDifferentZoneConnection)
                {
                    transitionRooms.Add(room);
                }
            }
        }

        // 10. Build output
        return new FloorLayout<TRoomType>
        {
            Rooms = placedRooms,
            Hallways = hallways,
            Doors = doors,
            Seed = config.Seed,
            CriticalPath = graph.CriticalPath,
            SpawnRoomId = graph.StartNodeId,
            BossRoomId = graph.BossNodeId,
            ZoneAssignments = zoneAssignments,
            TransitionRooms = transitionRooms
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

        // Include zone-specific room requirements
        if (config.Zones != null)
        {
            foreach (var zone in config.Zones)
            {
                if (zone.RoomRequirements != null)
                {
                    foreach (var req in zone.RoomRequirements)
                    {
                        requiredTypes.Add(req.Type);
                    }
                }
            }
        }

        // Collect all available templates (global + zone-specific)
        var availableTypes = config.Templates.SelectMany(t => t.ValidRoomTypes).ToHashSet();
        if (config.Zones != null)
        {
            foreach (var zone in config.Zones)
            {
                if (zone.Templates != null)
                {
                    foreach (var template in zone.Templates)
                    {
                        foreach (var roomType in template.ValidRoomTypes)
                        {
                            availableTypes.Add(roomType);
                        }
                    }
                }
            }
        }

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


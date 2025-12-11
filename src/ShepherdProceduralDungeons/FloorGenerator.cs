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
        var graphGenerator = CreateGraphGenerator(config);
        FloorGraph graph;
        
        if (config.GraphAlgorithm == GraphAlgorithm.GridBased && config.GridBasedConfig != null)
        {
            graph = ((GridBasedGraphGenerator)graphGenerator).Generate(config.RoomCount, config.BranchingFactor, graphRng, config.GridBasedConfig);
        }
        else if (config.GraphAlgorithm == GraphAlgorithm.CellularAutomata && config.CellularAutomataConfig != null)
        {
            graph = ((CellularAutomataGraphGenerator)graphGenerator).Generate(config.RoomCount, config.BranchingFactor, graphRng, config.CellularAutomataConfig);
        }
        else if (config.GraphAlgorithm == GraphAlgorithm.MazeBased && config.MazeBasedConfig != null)
        {
            graph = ((MazeBasedGraphGenerator)graphGenerator).Generate(config.RoomCount, config.BranchingFactor, graphRng, config.MazeBasedConfig);
        }
        else if (config.GraphAlgorithm == GraphAlgorithm.HubAndSpoke && config.HubAndSpokeConfig != null)
        {
            graph = ((HubAndSpokeGraphGenerator)graphGenerator).Generate(config.RoomCount, config.BranchingFactor, graphRng, config.HubAndSpokeConfig);
        }
        else
        {
            // Default: SpanningTree or fallback
            graph = graphGenerator.Generate(config.RoomCount, config.BranchingFactor, graphRng);
        }

        // 2. Calculate difficulty for all nodes if difficulty config is provided
        if (config.DifficultyConfig != null)
        {
            CalculateDifficulties(graph, config.DifficultyConfig);
        }
        else
        {
            // Set default difficulty to 0 if no config provided
            foreach (var node in graph.Nodes)
            {
                node.Difficulty = 0.0;
            }
        }

        // 3. Prepare zone data structures (but don't assign zones yet - need critical path first)
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

        // 4. Assign room types (pass floor index and zone info for zone-aware constraints)
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

        // 5. Assign all zones now that critical path is available (in config order, first match wins)
        if (config.Zones != null && config.Zones.Count > 0)
        {
            var zoneAssigner = new ZoneAssigner<TRoomType>();
            zoneAssignments = zoneAssigner.AssignZones(graph, config.Zones);
        }

        // 6. Organize templates by room type
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

        // 7. Spatial placement (with zone-aware template selection and difficulty-aware template selection)
        var spatialSolver = _spatialSolver ?? new IncrementalSolver<TRoomType>();
        if (spatialSolver is IncrementalSolver<TRoomType> incrementalSolver)
        {
            incrementalSolver.SetZoneInfo(zoneAssignments, zoneTemplates);
        }
        var placedRooms = spatialSolver.Solve(graph, assignments, templatesByTypeReadOnly, config.HallwayMode, spatialRng);

        // 8. Generate hallways
        var occupiedCells = new HashSet<Cell>(placedRooms.SelectMany(r => r.GetWorldCells()));
        var hallwayGenerator = new HallwayGenerator<TRoomType>();
        var hallways = hallwayGenerator.Generate(placedRooms, graph, occupiedCells, hallwayRng);

        // 9. Place doors
        var doors = PlaceDoors(placedRooms, hallways, graph);

        // 9.5. Generate secret passages (after hallways, before final output)
        int secretPassageSeed = masterRng.Next();
        var secretPassageRng = new Random(secretPassageSeed);
        var secretPassages = GenerateSecretPassages(
            config.SecretPassageConfig,
            placedRooms,
            graph,
            occupiedCells,
            secretPassageRng);

        // 10. Identify transition rooms (rooms connecting different zones)
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

        // 11. Detect clusters (if enabled)
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomCluster<TRoomType>>> clusters = 
            new Dictionary<TRoomType, IReadOnlyList<RoomCluster<TRoomType>>>();
        
        if (config.ClusterConfig != null && config.ClusterConfig.Enabled)
        {
            // Apply max cluster size constraints to cluster config
            var clusterConfig = ApplyClusterConstraints(config.ClusterConfig, config.Constraints);
            
            var clusterDetector = new ClusterDetector<TRoomType>();
            clusters = clusterDetector.DetectClusters(placedRooms, clusterConfig);
        }

        // 12. Build output
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
            TransitionRooms = transitionRooms,
            SecretPassages = secretPassages,
            Clusters = clusters
        };
    }

    private void CalculateDifficulties(FloorGraph graph, DifficultyConfig difficultyConfig)
    {
        foreach (var node in graph.Nodes)
        {
            double difficulty = CalculateDifficulty(node.DistanceFromStart, difficultyConfig);
            node.Difficulty = Math.Min(difficulty, difficultyConfig.MaxDifficulty);
        }
    }

    private double CalculateDifficulty(int distance, DifficultyConfig config)
    {
        double difficulty = config.Function switch
        {
            DifficultyScalingFunction.Linear => config.BaseDifficulty + (distance * config.ScalingFactor),
            DifficultyScalingFunction.Exponential => distance == 0 
                ? config.BaseDifficulty 
                : config.BaseDifficulty + (Math.Pow(config.ScalingFactor, distance) - 1.0),
            DifficultyScalingFunction.Custom => config.CustomFunction?.Invoke(distance) ?? config.BaseDifficulty,
            _ => config.BaseDifficulty
        };

        return Math.Min(difficulty, config.MaxDifficulty);
    }

    private IGraphGenerator CreateGraphGenerator(FloorConfig<TRoomType> config)
    {
        return config.GraphAlgorithm switch
        {
            GraphAlgorithm.SpanningTree => new SpanningTreeGraphGenerator(),
            GraphAlgorithm.GridBased => new GridBasedGraphGenerator(),
            GraphAlgorithm.CellularAutomata => new CellularAutomataGraphGenerator(),
            GraphAlgorithm.MazeBased => new MazeBasedGraphGenerator(),
            GraphAlgorithm.HubAndSpoke => new HubAndSpokeGraphGenerator(),
            _ => new SpanningTreeGraphGenerator() // Default fallback
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

    private IReadOnlyList<SecretPassage> GenerateSecretPassages(
        SecretPassageConfig<TRoomType>? config,
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        FloorGraph graph,
        HashSet<Cell> occupiedCells,
        Random rng)
    {
        var secretPassages = new List<SecretPassage>();

        // If no config or count is 0, return empty list
        if (config == null || config.Count <= 0)
        {
            return secretPassages;
        }

        // Build set of graph-connected room pairs
        var graphConnectedPairs = new HashSet<(int, int)>();
        foreach (var conn in graph.Connections)
        {
            var pair = (Math.Min(conn.NodeAId, conn.NodeBId), Math.Max(conn.NodeAId, conn.NodeBId));
            graphConnectedPairs.Add(pair);
        }

        // Build set of critical path room IDs
        var criticalPathSet = graph.CriticalPath.ToHashSet();

        // Find all candidate room pairs
        var candidates = new List<(PlacedRoom<TRoomType> RoomA, PlacedRoom<TRoomType> RoomB, int Distance)>();
        
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var roomA = rooms[i];
                var roomB = rooms[j];

                // Check room type constraints
                if (config.AllowedRoomTypes.Count > 0)
                {
                    if (!config.AllowedRoomTypes.Contains(roomA.RoomType) ||
                        !config.AllowedRoomTypes.Contains(roomB.RoomType))
                    {
                        continue;
                    }
                }

                if (config.ForbiddenRoomTypes.Contains(roomA.RoomType) ||
                    config.ForbiddenRoomTypes.Contains(roomB.RoomType))
                {
                    continue;
                }

                // Check critical path constraint
                if (!config.AllowCriticalPathConnections)
                {
                    if (criticalPathSet.Contains(roomA.NodeId) || criticalPathSet.Contains(roomB.NodeId))
                    {
                        continue;
                    }
                }

                // Check graph connection constraint
                if (!config.AllowGraphConnectedRooms)
                {
                    var pair = (Math.Min(roomA.NodeId, roomB.NodeId), Math.Max(roomA.NodeId, roomB.NodeId));
                    if (graphConnectedPairs.Contains(pair))
                    {
                        continue;
                    }
                }

                // Calculate spatial distance (Manhattan distance between room centers)
                var centerA = GetRoomCenter(roomA);
                var centerB = GetRoomCenter(roomB);
                int distance = Math.Abs(centerA.X - centerB.X) + Math.Abs(centerA.Y - centerB.Y);

                if (distance <= config.MaxSpatialDistance)
                {
                    candidates.Add((roomA, roomB, distance));
                }
            }
        }

        // Sort by distance (prefer shorter distances)
        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));

        // Select up to Count secret passages
        int selectedCount = Math.Min(config.Count, candidates.Count);
        var selectedCandidates = new List<(PlacedRoom<TRoomType> RoomA, PlacedRoom<TRoomType> RoomB)>();
        
        // Shuffle candidates to add randomness, then take first N
        var shuffledCandidates = candidates.OrderBy(_ => rng.Next()).Take(selectedCount).ToList();
        
        foreach (var candidate in shuffledCandidates)
        {
            selectedCandidates.Add((candidate.RoomA, candidate.RoomB));
        }

        // Generate secret passages for selected candidates
        var hallwayGenerator = new HallwayGenerator<TRoomType>();
        int secretPassageId = 0;

        foreach (var (roomA, roomB) in selectedCandidates)
        {
            try
            {
                var secretPassage = CreateSecretPassage(
                    roomA,
                    roomB,
                    secretPassageId++,
                    occupiedCells,
                    hallwayGenerator,
                    rng);
                
                if (secretPassage != null)
                {
                    secretPassages.Add(secretPassage);
                    
                    // Mark hallway cells as occupied if hallway was created
                    if (secretPassage.Hallway != null)
                    {
                        foreach (var segment in secretPassage.Hallway.Segments)
                        {
                            foreach (var cell in segment.GetCells())
                            {
                                occupiedCells.Add(cell);
                            }
                        }
                    }
                }
            }
            catch
            {
                // If we can't create a secret passage for this pair, skip it
                // This can happen if rooms can't be connected spatially
                continue;
            }
        }

        return secretPassages;
    }

    private Cell GetRoomCenter(PlacedRoom<TRoomType> room)
    {
        var cells = room.GetWorldCells().ToList();
        if (cells.Count == 0)
            return room.Position;

        int sumX = 0;
        int sumY = 0;
        foreach (var cell in cells)
        {
            sumX += cell.X;
            sumY += cell.Y;
        }

        return new Cell(sumX / cells.Count, sumY / cells.Count);
    }

    private SecretPassage? CreateSecretPassage(
        PlacedRoom<TRoomType> roomA,
        PlacedRoom<TRoomType> roomB,
        int secretPassageId,
        HashSet<Cell> occupiedCells,
        HallwayGenerator<TRoomType> hallwayGenerator,
        Random rng)
    {
        // Get all possible door positions on each room
        var doorsA = roomA.GetExteriorEdgesWorld()
            .Where(e => roomA.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
            .ToList();
        
        var doorsB = roomB.GetExteriorEdgesWorld()
            .Where(e => roomB.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
            .ToList();

        if (doorsA.Count == 0 || doorsB.Count == 0)
        {
            // Fallback: use any exterior edge
            var fallbackA = roomA.GetExteriorEdgesWorld().First();
            var fallbackB = roomB.GetExteriorEdgesWorld().First();
            doorsA = new List<(Cell WorldCell, Edge Edge)> { (fallbackA.WorldCell, fallbackA.Edge) };
            doorsB = new List<(Cell WorldCell, Edge Edge)> { (fallbackB.WorldCell, fallbackB.Edge) };
        }

        // Shuffle for randomness
        Shuffle(doorsA, rng);
        Shuffle(doorsB, rng);

        // Try door pairs until one works
        IReadOnlyList<Cell>? path = null;
        (Cell WorldCell, Edge Edge) doorA = default;
        (Cell WorldCell, Edge Edge) doorB = default;
        
        foreach (var doorAOption in doorsA)
        {
            foreach (var doorBOption in doorsB)
            {
                try
                {
                    doorA = doorAOption;
                    doorB = doorBOption;
                    
                    // Check if rooms are adjacent (no hallway needed)
                    var adjacentCellA = GetAdjacentCell(doorA.WorldCell, doorA.Edge);
                    var adjacentCellB = GetAdjacentCell(doorB.WorldCell, doorB.Edge);
                    
                    if (adjacentCellA == doorB.WorldCell && doorA.Edge.Opposite() == doorB.Edge)
                    {
                        // Rooms are adjacent, no hallway needed
                        path = Array.Empty<Cell>();
                        break;
                    }
                    
                    // Try to find a hallway path
                    path = FindHallwayPathForSecretPassage(
                        doorA.WorldCell,
                        doorA.Edge,
                        doorB.WorldCell,
                        doorB.Edge,
                        occupiedCells);
                    break; // Success!
                }
                catch
                {
                    // Try next door pair
                    continue;
                }
            }
            if (path != null) break;
        }

        if (path == null)
        {
            return null; // Couldn't find a valid connection
        }

        // Create doors
        var doorAObj = new Door
        {
            Position = doorA.WorldCell,
            Edge = doorA.Edge,
            ConnectsToRoomId = roomB.NodeId
        };

        var doorBObj = new Door
        {
            Position = doorB.WorldCell,
            Edge = doorB.Edge,
            ConnectsToRoomId = roomA.NodeId
        };

        // Create hallway if needed
        Hallway? hallway = null;
        if (path.Count > 0)
        {
            var segments = PathToSegments(path);
            hallway = new Hallway
            {
                Id = secretPassageId,
                Segments = segments,
                DoorA = doorAObj,
                DoorB = doorBObj
            };
        }

        return new SecretPassage
        {
            RoomAId = roomA.NodeId,
            RoomBId = roomB.NodeId,
            DoorA = doorAObj,
            DoorB = doorBObj,
            Hallway = hallway
        };
    }

    private IReadOnlyList<Cell> FindHallwayPathForSecretPassage(
        Cell startCell,
        Edge startEdge,
        Cell endCell,
        Edge endEdge,
        HashSet<Cell> occupied)
    {
        // Get the cell outside each door
        Cell start = GetAdjacentCell(startCell, startEdge);
        Cell end = GetAdjacentCell(endCell, endEdge);

        // Calculate adaptive search radius
        int manhattanDist = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
        int adaptiveSearchRadius = Math.Max(5, Math.Min(manhattanDist / 2, 15));

        // If the start cell is occupied, find an alternative
        if (occupied.Contains(start))
        {
            var alternativeStart = FindNearestUnoccupiedCell(start, occupied, adaptiveSearchRadius);
            if (alternativeStart.HasValue)
            {
                start = alternativeStart.Value;
            }
        }

        // If the end cell is occupied, find an alternative
        if (occupied.Contains(end))
        {
            var alternativeEnd = FindNearestUnoccupiedCell(end, occupied, adaptiveSearchRadius);
            if (alternativeEnd.HasValue)
            {
                end = alternativeEnd.Value;
            }
        }

        // A* pathfinding
        var path = AStarForSecretPassage(start, end, occupied);

        if (path == null)
            throw new SpatialPlacementException($"Cannot find secret passage path from {start} to {end}");

        return path;
    }

    private Cell? FindNearestUnoccupiedCell(Cell target, HashSet<Cell> occupied, int maxSearchRadius)
    {
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        queue.Enqueue(target);
        visited.Add(target);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            int distance = Math.Abs(current.X - target.X) + Math.Abs(current.Y - target.Y);
            if (distance > maxSearchRadius)
                break;

            if (!occupied.Contains(current))
                return current;

            foreach (var neighbor in GetNeighborsForSecretPassage(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return null;
    }

    private IReadOnlyList<Cell>? AStarForSecretPassage(Cell start, Cell end, HashSet<Cell> occupied)
    {
        var openSet = new PriorityQueue<Cell, int>();
        var closedSet = new HashSet<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var gScore = new Dictionary<Cell, int> { [start] = 0 };

        openSet.Enqueue(start, ManhattanDistance(start, end));

        const int maxNodesExplored = 10000;
        int nodesExplored = 0;

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (closedSet.Contains(current))
                continue;

            closedSet.Add(current);
            nodesExplored++;

            if (nodesExplored >= maxNodesExplored)
                return null;

            if (current == end)
            {
                var pathList = new List<Cell>();
                
                if (current == start)
                {
                    pathList.Add(start);
                    return pathList;
                }
                
                var node = end;
                pathList.Add(end);
                
                while (cameFrom.TryGetValue(node, out var prev))
                {
                    pathList.Add(prev);
                    node = prev;
                    if (node == start)
                        break;
                }
                
                if (pathList[pathList.Count - 1] != start)
                {
                    pathList.Add(start);
                }
                
                pathList.Reverse();
                return pathList;
            }

            foreach (var neighbor in GetNeighborsForSecretPassage(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                if (occupied.Contains(neighbor) && neighbor != end)
                    continue;

                int tentativeG = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, end);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        return null;
    }

    private IEnumerable<Cell> GetNeighborsForSecretPassage(Cell cell)
    {
        yield return cell.North;
        yield return cell.South;
        yield return cell.East;
        yield return cell.West;
    }

    private int ManhattanDistance(Cell a, Cell b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private IReadOnlyList<HallwaySegment> PathToSegments(IReadOnlyList<Cell> path)
    {
        var segments = new List<HallwaySegment>();

        if (path.Count < 2) return segments;

        for (int i = 1; i < path.Count; i++)
        {
            var prev = path[i - 1];
            var current = path[i];
            int dx = Math.Abs(current.X - prev.X);
            int dy = Math.Abs(current.Y - prev.Y);
            int distance = dx + dy;
            
            if (distance != 1)
            {
                throw new InvalidOperationException(
                    $"Invalid path: non-adjacent cells at index {i}. " +
                    $"Cell {prev} -> {current} (distance: {distance}). " +
                    $"Path length: {path.Count}");
            }
        }

        Cell segmentStart = path[0];
        Cell? lastDir = null;

        for (int i = 1; i < path.Count; i++)
        {
            Cell current = path[i];
            Cell prev = path[i - 1];
            Cell dir = new Cell(current.X - prev.X, current.Y - prev.Y);

            if (lastDir.HasValue && dir != lastDir.Value)
            {
                segments.Add(new HallwaySegment
                {
                    Start = segmentStart,
                    End = prev
                });
                segmentStart = prev;
            }

            lastDir = dir;
        }

        segments.Add(new HallwaySegment
        {
            Start = segmentStart,
            End = path[^1]
        });

        return segments;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    private ClusterConfig<TRoomType> ApplyClusterConstraints(
        ClusterConfig<TRoomType> baseConfig,
        IReadOnlyList<Constraints.IConstraint<TRoomType>> constraints)
    {
        // Check for MaxClusterSizeConstraint and apply to config
        int? maxClusterSize = baseConfig.MaxClusterSize;
        
        foreach (var constraint in constraints)
        {
            if (constraint is Constraints.MaxClusterSizeConstraint<TRoomType> maxSizeConstraint)
            {
                // If multiple constraints exist, use the minimum max size
                if (!maxClusterSize.HasValue || maxSizeConstraint.MaxSize < maxClusterSize.Value)
                {
                    maxClusterSize = maxSizeConstraint.MaxSize;
                }
            }
        }

        // Return new config with updated max cluster size if changed
        if (maxClusterSize != baseConfig.MaxClusterSize)
        {
            return new ClusterConfig<TRoomType>
            {
                Enabled = baseConfig.Enabled,
                Epsilon = baseConfig.Epsilon,
                MinClusterSize = baseConfig.MinClusterSize,
                MaxClusterSize = maxClusterSize,
                RoomTypesToCluster = baseConfig.RoomTypesToCluster
            };
        }

        return baseConfig;
    }
}


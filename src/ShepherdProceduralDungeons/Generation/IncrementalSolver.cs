using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

    /// <summary>
    /// Default spatial solver that places rooms incrementally via BFS from the start room.
    /// </summary>
    /// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class IncrementalSolver<TRoomType> : ISpatialSolver<TRoomType> where TRoomType : Enum
{
    private IReadOnlyDictionary<int, string>? _zoneAssignments;
    private IReadOnlyDictionary<string, IReadOnlyList<RoomTemplate<TRoomType>>>? _zoneTemplates;
    private FloorGraph? _graph;

    /// <summary>
    /// Sets zone information for zone-aware template selection.
    /// </summary>
    internal void SetZoneInfo(
        IReadOnlyDictionary<int, string>? zoneAssignments,
        IReadOnlyDictionary<string, IReadOnlyList<RoomTemplate<TRoomType>>>? zoneTemplates)
    {
        _zoneAssignments = zoneAssignments;
        _zoneTemplates = zoneTemplates;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng)
    {
        _graph = graph;
        var placedRooms = new Dictionary<int, PlacedRoom<TRoomType>>();
        var occupiedCells = new HashSet<Cell>();

        // Track which connections need hallways
        var hallwayConnections = new HashSet<(int, int)>();

        // 1. Place start room at origin
        var startNode = graph.GetNode(graph.StartNodeId);
        var startTemplate = SelectTemplateWithZone(assignments[startNode.Id], templates, rng, startNode.Id);
        var startRoom = PlaceRoom(startNode.Id, startTemplate, new Cell(0, 0), assignments[startNode.Id], startNode.Difficulty);
        placedRooms[startNode.Id] = startRoom;
        AddOccupiedCells(startRoom, occupiedCells);
        
#if DEBUG
        DungeonDebugVisualizer.PrintRoomPlacement(
            startNode.Id, 
            assignments[startNode.Id], 
            startRoom, 
            placedRooms.Count);
        DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, "  Placement: START ROOM at origin");
#endif

        // 2. BFS from start, placing connected rooms
        var queue = new Queue<int>();
        var visited = new HashSet<int> { graph.StartNodeId };
        queue.Enqueue(graph.StartNodeId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            var currentRoom = placedRooms[currentId];
            var currentNode = graph.GetNode(currentId);

            foreach (var conn in currentNode.Connections)
            {
                int neighborId = conn.GetOtherNodeId(currentId);
                if (visited.Contains(neighborId)) continue;
                visited.Add(neighborId);

                var neighborTemplate = SelectTemplateWithZone(assignments[neighborId], templates, rng, neighborId);

                // Try to place adjacent to current room
                var placement = TryPlaceAdjacent(currentRoom, neighborTemplate, occupiedCells, rng);

                if (placement.HasValue)
                {
                    var neighborNode = graph.GetNode(neighborId);
                    var neighborRoom = PlaceRoom(neighborId, neighborTemplate, placement.Value, assignments[neighborId], neighborNode.Difficulty);
                    placedRooms[neighborId] = neighborRoom;
                    AddOccupiedCells(neighborRoom, occupiedCells);
                    
#if DEBUG
                    DungeonDebugVisualizer.PrintRoomPlacement(
                        neighborId, 
                        assignments[neighborId], 
                        neighborRoom, 
                        placedRooms.Count);
                    DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, $"  Placement: ADJACENT to room {currentId}");
#endif
                }
                else if (hallwayMode != HallwayMode.None)
                {
                    // Place nearby with gap for hallway
                    var nearbyPlacement = PlaceNearby(currentRoom, neighborTemplate, occupiedCells, rng);
                    var neighborNode = graph.GetNode(neighborId);
                    var neighborRoom = PlaceRoom(neighborId, neighborTemplate, nearbyPlacement, assignments[neighborId], neighborNode.Difficulty);
                    placedRooms[neighborId] = neighborRoom;
                    AddOccupiedCells(neighborRoom, occupiedCells);

                    // Mark connection for hallway generation
                    conn.RequiresHallway = true;
                    hallwayConnections.Add((Math.Min(currentId, neighborId), Math.Max(currentId, neighborId)));
                    
#if DEBUG
                    DungeonDebugVisualizer.PrintRoomPlacement(
                        neighborId, 
                        assignments[neighborId], 
                        neighborRoom, 
                        placedRooms.Count);
                    DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, $"  Placement: NEARBY (hallway required) to room {currentId}");
                    DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, $"  Distance from room {currentId}: anchor={nearbyPlacement}, current room anchor={currentRoom.Position}");
#endif
                }
                else
                {
                    throw new SpatialPlacementException($"Cannot place room {neighborId} adjacent to room {currentId} and hallways are disabled");
                }

                queue.Enqueue(neighborId);
            }
        }

        // Force hallways for all connections if mode is Always
        if (hallwayMode == HallwayMode.Always)
        {
            foreach (var conn in graph.Connections)
            {
                conn.RequiresHallway = true;
            }
        }

#if DEBUG
        DungeonDebugVisualizer.PrintSpatialLayout(placedRooms.Values.ToList(), "After Room Placement");
        var asciiMap = DungeonDebugVisualizer.CreateAsciiMap(placedRooms.Values.ToList(), occupiedCells);
        DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, asciiMap);
#endif

        return placedRooms.Values.ToList();
    }

    private Cell? TryPlaceAdjacent(PlacedRoom<TRoomType> existingRoom, RoomTemplate<TRoomType> template, HashSet<Cell> occupied, Random rng)
    {
        // Get all exterior edges of existing room
        var existingExterior = existingRoom.GetExteriorEdgesWorld().ToList();

        // Get all door edges of template
        var templateDoorEdges = template.DoorEdges.SelectMany(kvp =>
            Enum.GetValues<Edge>()
                .Where(e => e != Edge.None && e != Edge.All && kvp.Value.HasFlag(e))
                .Select(e => (Cell: kvp.Key, Edge: e)))
            .ToList();

        // Shuffle for randomness
        Shuffle(templateDoorEdges, rng);

        foreach (var (templateCell, templateEdge) in templateDoorEdges)
        {
            Edge requiredExistingEdge = templateEdge.Opposite();

            // Find compatible edges on existing room
            var compatibleEdges = existingExterior
                .Where(e => e.Edge == requiredExistingEdge && existingRoom.Template.CanPlaceDoor(e.LocalCell, e.Edge))
                .ToList();

            Shuffle(compatibleEdges, rng);

            foreach (var existingEdge in compatibleEdges)
            {
                // Calculate template position to align these edges
                Cell templateAnchor = CalculateAnchorForEdgeAlignment(
                    existingEdge.WorldCell, existingEdge.Edge,
                    templateCell, templateEdge);

                // Check if template fits without overlap
                if (TemplateFits(template, templateAnchor, occupied))
                {
                    return templateAnchor;
                }
            }
        }

        return null; // No valid adjacent placement found
    }

    private Cell CalculateAnchorForEdgeAlignment(Cell existingCell, Edge existingEdge, Cell templateCell, Edge templateEdge)
    {
        // Calculate the offset needed to align the template's door edge with the existing room's door edge
        // The template's door edge should be adjacent to the existing room's door edge

        Cell offset = existingEdge switch
        {
            Edge.North => existingCell.North.Offset(-templateCell.X, -templateCell.Y + 1),
            Edge.South => existingCell.South.Offset(-templateCell.X, -templateCell.Y - 1),
            Edge.East => existingCell.East.Offset(-templateCell.X - 1, -templateCell.Y),
            Edge.West => existingCell.West.Offset(-templateCell.X + 1, -templateCell.Y),
            _ => throw new InvalidOperationException($"Invalid edge: {existingEdge}")
        };

        return offset;
    }

    private Cell PlaceNearby(PlacedRoom<TRoomType> existingRoom, RoomTemplate<TRoomType> template, HashSet<Cell> occupied, Random rng)
    {
        // Search in expanding radius for valid placement
        // Leave gap of 1-3 cells for hallway
        int maxRadius = 20;

        // Calculate bounding box ONCE before the loop (optimization: OPT-004)
        var existingCells = existingRoom.GetWorldCells().ToList();
        int minX = existingCells.Min(c => c.X);
        int maxX = existingCells.Max(c => c.X);
        int minY = existingCells.Min(c => c.Y);
        int maxY = existingCells.Max(c => c.Y);

        for (int radius = 2; radius <= maxRadius; radius++)
        {
            var candidates = new List<Cell>();

            // Search around the bounding box
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) < radius && Math.Abs(dy) < radius) continue; // Only check perimeter

                    Cell anchor = new Cell(minX + dx, minY + dy);
                    if (TemplateFits(template, anchor, occupied))
                    {
                        candidates.Add(anchor);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[rng.Next(candidates.Count)];
            }
        }

        throw new SpatialPlacementException("Could not find any valid placement for room");
    }

    private bool TemplateFits(RoomTemplate<TRoomType> template, Cell anchor, HashSet<Cell> occupied)
    {
        foreach (var cell in template.Cells)
        {
            Cell world = new Cell(anchor.X + cell.X, anchor.Y + cell.Y);
            if (occupied.Contains(world))
                return false;
        }
        return true;
    }

    private RoomTemplate<TRoomType> SelectTemplate(
        TRoomType roomType, 
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates, 
        Random rng)
    {
        return SelectTemplate(roomType, templates, rng, null, _zoneAssignments, _zoneTemplates);
    }

    private RoomTemplate<TRoomType> SelectTemplate(
        TRoomType roomType, 
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates, 
        Random rng,
        int? nodeId,
        IReadOnlyDictionary<int, string>? zoneAssignments,
        IReadOnlyDictionary<string, IReadOnlyList<RoomTemplate<TRoomType>>>? zoneTemplates)
    {
        // Get node difficulty if available
        double? nodeDifficulty = null;
        if (nodeId.HasValue && _graph != null)
        {
            try
            {
                var node = _graph.GetNode(nodeId.Value);
                nodeDifficulty = node.Difficulty;
            }
            catch (KeyNotFoundException)
            {
                // Node not found, leave difficulty as null
            }
        }

        // Try zone-specific templates first if available
        List<RoomTemplate<TRoomType>>? zoneSpecificTemplates = null;
        if (nodeId.HasValue && zoneAssignments != null && zoneTemplates != null)
        {
            if (zoneAssignments.TryGetValue(nodeId.Value, out var zoneId) &&
                zoneTemplates.TryGetValue(zoneId, out var zoneTemplatesList))
            {
                zoneSpecificTemplates = zoneTemplatesList
                    .Where(t => t.ValidRoomTypes.Contains(roomType))
                    .Where(t => MatchesDifficultyBounds(t, nodeDifficulty))
                    .ToList();
            }
        }

        // Get available templates (zone-specific preferred, fallback to global)
        IReadOnlyList<RoomTemplate<TRoomType>> available;
        if (zoneSpecificTemplates != null && zoneSpecificTemplates.Count > 0)
        {
            // Prefer zone-specific templates, but include global templates as fallback
            var allTemplates = new List<RoomTemplate<TRoomType>>(zoneSpecificTemplates);
            if (templates.TryGetValue(roomType, out var globalTemplates))
            {
                // Add global templates that aren't already in zone templates and match difficulty
                foreach (var global in globalTemplates)
                {
                    if (!allTemplates.Any(t => t.Id == global.Id) && MatchesDifficultyBounds(global, nodeDifficulty))
                    {
                        allTemplates.Add(global);
                    }
                }
            }
            available = allTemplates;
        }
        else
        {
            // Use global templates
            if (!templates.TryGetValue(roomType, out var global) || global.Count == 0)
                throw new InvalidConfigurationException($"No templates registered for room type {roomType}");
            available = global.Where(t => MatchesDifficultyBounds(t, nodeDifficulty)).ToList();
        }

        if (available.Count == 0)
            throw new InvalidConfigurationException($"No templates registered for room type {roomType} matching difficulty bounds");

        // Filter out zero-weight templates (they are disabled)
        available = available.Where(t => t.Weight > 0).ToList();

        if (available.Count == 0)
            throw new InvalidConfigurationException($"No templates with positive weight found for room type {roomType}. All templates have zero weight.");

        // Calculate total weight
        double totalWeight = available.Sum(t => t.Weight);

        // Weighted random selection (zone templates have higher effective weight)
        double randomValue = rng.NextDouble() * totalWeight;
        double cumulative = 0;
        
        foreach (var template in available)
        {
            cumulative += template.Weight;
            if (randomValue < cumulative)
                return template;
        }
        
        // Fallback (shouldn't happen, but for safety)
        return available[available.Count - 1];
    }

    private RoomTemplate<TRoomType> SelectTemplateWithZone(
        TRoomType roomType,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        Random rng,
        int nodeId)
    {
        return SelectTemplate(roomType, templates, rng, nodeId, _zoneAssignments, _zoneTemplates);
    }

    private bool MatchesDifficultyBounds(RoomTemplate<TRoomType> template, double? difficulty)
    {
        if (difficulty == null)
            return true; // If no difficulty is set, don't filter

        if (template.MinDifficulty.HasValue && difficulty < template.MinDifficulty.Value)
            return false;

        if (template.MaxDifficulty.HasValue && difficulty > template.MaxDifficulty.Value)
            return false;

        return true;
    }

    private PlacedRoom<TRoomType> PlaceRoom(int nodeId, RoomTemplate<TRoomType> template, Cell position, TRoomType roomType, double difficulty)
    {
        return new PlacedRoom<TRoomType>
        {
            NodeId = nodeId,
            Template = template,
            Position = position,
            RoomType = roomType,
            Difficulty = difficulty
        };
    }

    private void AddOccupiedCells(PlacedRoom<TRoomType> room, HashSet<Cell> occupied)
    {
        foreach (var cell in room.GetWorldCells())
        {
            occupied.Add(cell);
        }
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
}


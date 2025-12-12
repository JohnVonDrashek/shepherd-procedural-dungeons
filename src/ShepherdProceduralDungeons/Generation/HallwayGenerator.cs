using System.Linq;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates hallways between rooms that cannot be placed adjacent.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class HallwayGenerator<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Generates hallways for all connections that require them.
    /// </summary>
    public IReadOnlyList<Hallway> Generate(
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        FloorGraph graph,
        HashSet<Cell> occupiedCells,
        Random rng)
    {
        var hallways = new List<Hallway>();
        int hallwayId = 0;

        // Create room lookup dictionary for O(1) lookups
        var roomLookup = rooms.ToDictionary(r => r.NodeId, r => r);

        // Cache exterior edges for all rooms (optimization: OPT-006)
        var edgeCache = new Dictionary<int, IReadOnlyList<(Cell LocalCell, Cell WorldCell, Edge Edge)>>();
        foreach (var room in rooms)
        {
            edgeCache[room.NodeId] = room.GetExteriorEdgesWorld().ToList();
        }

        foreach (var conn in graph.Connections.Where(c => c.RequiresHallway))
        {
            var roomA = roomLookup[conn.NodeAId];
            var roomB = roomLookup[conn.NodeBId];

            // Get all possible door positions on each room (using cached edges)
            var edgesA = edgeCache[roomA.NodeId];
            var doorsA = edgesA
                .Where(e => roomA.Template.CanPlaceDoor(e.LocalCell, e.Edge))
                .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
                .ToList();
            
            var edgesB = edgeCache[roomB.NodeId];
            var doorsB = edgesB
                .Where(e => roomB.Template.CanPlaceDoor(e.LocalCell, e.Edge))
                .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
                .ToList();

            if (doorsA.Count == 0 || doorsB.Count == 0)
            {
                // Fallback: use any exterior edge (using cached edges)
                var fallbackA = edgesA.First();
                var fallbackB = edgesB.First();
                doorsA = new List<(Cell WorldCell, Edge Edge)> { (fallbackA.WorldCell, fallbackA.Edge) };
                doorsB = new List<(Cell WorldCell, Edge Edge)> { (fallbackB.WorldCell, fallbackB.Edge) };
            }

            // Shuffle for randomness
            CollectionUtilities.Shuffle(doorsA, rng);
            CollectionUtilities.Shuffle(doorsB, rng);

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
                        
#if DEBUG
                        var start = GetAdjacentCell(doorA.WorldCell, doorA.Edge);
                        var end = GetAdjacentCell(doorB.WorldCell, doorB.Edge);
                        int manhattanDist = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
                        DungeonDebugVisualizer.PrintHallwayAttempt(
                            roomA.NodeId, 
                            roomB.NodeId, 
                            doorA.WorldCell, 
                            doorB.WorldCell, 
                            start, 
                            end, 
                            manhattanDist);
#endif
                        path = FindHallwayPath(doorA.WorldCell, doorA.Edge, doorB.WorldCell, doorB.Edge, occupiedCells);
                        break; // Success!
                    }
                    catch (SpatialPlacementException)
                    {
                        // Try next door pair
                        continue;
                    }
                }
                if (path != null) break;
            }

            if (path == null)
            {
                throw new SpatialPlacementException(
                    $"Cannot find hallway path between room {roomA.NodeId} and room {roomB.NodeId} " +
                    $"after trying {doorsA.Count * doorsB.Count} door pair combinations");
            }

            // Convert path to hallway segments
            var segments = HallwayUtilities.PathToSegments(path);
            
#if DEBUG
            DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, $"✓ Hallway generated: {path.Count} cells");
#endif

            var hallway = new Hallway
            {
                Id = hallwayId++,
                Segments = segments,
                DoorA = new Door
                {
                    Position = doorA.WorldCell,
                    Edge = doorA.Edge,
                    ConnectsToRoomId = roomA.NodeId
                },
                DoorB = new Door
                {
                    Position = doorB.WorldCell,
                    Edge = doorB.Edge,
                    ConnectsToRoomId = roomB.NodeId
                }
            };

            hallways.Add(hallway);

            // Mark hallway cells as occupied
            foreach (var segment in segments)
            {
                foreach (var cell in segment.GetCells())
                {
                    occupiedCells.Add(cell);
                }
            }
        }

        return hallways;
    }

    private ((Cell WorldCell, Edge Edge) DoorA, (Cell WorldCell, Edge Edge) DoorB) FindBestDoorPair(
        PlacedRoom<TRoomType> roomA,
        PlacedRoom<TRoomType> roomB,
        IReadOnlyDictionary<int, IReadOnlyList<(Cell LocalCell, Cell WorldCell, Edge Edge)>> edgeCache,
        Random rng)
    {
        // Get all possible door positions from both rooms (using cached edges)
        var edgesA = edgeCache[roomA.NodeId];
        var doorsA = edgesA
            .Where(e => roomA.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
            .ToList();

        var edgesB = edgeCache[roomB.NodeId];
        var doorsB = edgesB
            .Where(e => roomB.Template.CanPlaceDoor(e.LocalCell, e.Edge))
            .Select(e => (WorldCell: e.WorldCell, Edge: e.Edge))
            .ToList();

        if (doorsA.Count == 0 || doorsB.Count == 0)
        {
            // Fallback: use any exterior edge (using cached edges)
            var fallbackA = edgesA.First();
            var fallbackB = edgesB.First();
            return ((fallbackA.WorldCell, fallbackA.Edge), (fallbackB.WorldCell, fallbackB.Edge));
        }

        // Shuffle for randomness, then pick the first pair (could be enhanced to find closest pair)
        CollectionUtilities.Shuffle(doorsA, rng);
        CollectionUtilities.Shuffle(doorsB, rng);

        return (doorsA[0], doorsB[0]);
    }

    private IReadOnlyList<Cell> FindHallwayPath(Cell startCell, Edge startEdge, Cell endCell, Edge endEdge, HashSet<Cell> occupied)
    {
        // Get the cell outside each door
        Cell start = GetAdjacentCell(startCell, startEdge);
        Cell end = GetAdjacentCell(endCell, endEdge);

        // Calculate adaptive search radius based on distance between rooms
        // For large dungeons, we need a larger search radius to find paths around obstacles
        int manhattanDist = Math.Abs(start.X - end.X) + Math.Abs(start.Y - end.Y);
        // Increase max radius from 15 to 30 for large dungeons, and scale better with distance
        int adaptiveSearchRadius = Math.Max(5, Math.Min(manhattanDist / 2, 30)); // Between 5 and 30

        // If the start cell is occupied (inside another room), find an alternative
        // Try with increasing radius if first attempt fails
        if (occupied.Contains(start))
        {
            var alternativeStart = PathfindingUtilities.FindNearestUnoccupiedCell(start, occupied, maxSearchRadius: adaptiveSearchRadius, PathfindingUtilities.GetNeighbors);
            if (!alternativeStart.HasValue)
            {
                // Try with progressively larger radii up to 50 cells
                for (int radius = adaptiveSearchRadius + 5; radius <= 50; radius += 5)
                {
                    alternativeStart = PathfindingUtilities.FindNearestUnoccupiedCell(start, occupied, maxSearchRadius: radius, PathfindingUtilities.GetNeighbors);
                    if (alternativeStart.HasValue)
                        break;
                }
            }
            if (alternativeStart.HasValue)
            {
#if DEBUG
                DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, $"Start cell {start} is occupied, using alternative start: {alternativeStart.Value}");
#endif
                start = alternativeStart.Value;
            }
        }

        // If the end cell is occupied (inside another room), try to find an alternative end point
        // Try with increasing radius if first attempt fails
        if (occupied.Contains(end))
        {
            var alternativeEnd = PathfindingUtilities.FindNearestUnoccupiedCell(end, occupied, maxSearchRadius: adaptiveSearchRadius, PathfindingUtilities.GetNeighbors);
            if (!alternativeEnd.HasValue)
            {
                // Try with progressively larger radii up to 50 cells
                for (int radius = adaptiveSearchRadius + 5; radius <= 50; radius += 5)
                {
                    alternativeEnd = PathfindingUtilities.FindNearestUnoccupiedCell(end, occupied, maxSearchRadius: radius, PathfindingUtilities.GetNeighbors);
                    if (alternativeEnd.HasValue)
                        break;
                }
            }
            if (alternativeEnd.HasValue)
            {
#if DEBUG
                DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, $"End cell {end} is occupied, using alternative end: {alternativeEnd.Value}");
#endif
                end = alternativeEnd.Value;
            }
        }

        // A* pathfinding avoiding occupied cells
        var path = PathfindingUtilities.AStar(start, end, occupied, new AStarOptions { EnableDebugLogging = true, UseObstaclePenalty = true });

        if (path == null)
            throw new SpatialPlacementException($"Cannot find hallway path from {start} to {end}");

        return path;
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


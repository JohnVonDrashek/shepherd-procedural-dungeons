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

        foreach (var conn in graph.Connections.Where(c => c.RequiresHallway))
        {
            var roomA = roomLookup[conn.NodeAId];
            var roomB = roomLookup[conn.NodeBId];

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
            var segments = PathToSegments(path);
            
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
        Random rng)
    {
        // Get all possible door positions from both rooms
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
            return ((fallbackA.WorldCell, fallbackA.Edge), (fallbackB.WorldCell, fallbackB.Edge));
        }

        // Shuffle for randomness, then pick the first pair (could be enhanced to find closest pair)
        Shuffle(doorsA, rng);
        Shuffle(doorsB, rng);

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
            var alternativeStart = FindNearestUnoccupiedCell(start, occupied, maxSearchRadius: adaptiveSearchRadius);
            if (!alternativeStart.HasValue)
            {
                // Try with progressively larger radii up to 50 cells
                for (int radius = adaptiveSearchRadius + 5; radius <= 50; radius += 5)
                {
                    alternativeStart = FindNearestUnoccupiedCell(start, occupied, maxSearchRadius: radius);
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
            var alternativeEnd = FindNearestUnoccupiedCell(end, occupied, maxSearchRadius: adaptiveSearchRadius);
            if (!alternativeEnd.HasValue)
            {
                // Try with progressively larger radii up to 50 cells
                for (int radius = adaptiveSearchRadius + 5; radius <= 50; radius += 5)
                {
                    alternativeEnd = FindNearestUnoccupiedCell(end, occupied, maxSearchRadius: radius);
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
        var path = AStar(start, end, occupied);

        if (path == null)
            throw new SpatialPlacementException($"Cannot find hallway path from {start} to {end}");

        return path;
    }

    private Cell? FindNearestUnoccupiedCell(Cell target, HashSet<Cell> occupied, int maxSearchRadius)
    {
        // Breadth-first search for nearest unoccupied cell
        var queue = new Queue<Cell>();
        var visited = new HashSet<Cell>();
        queue.Enqueue(target);
        visited.Add(target);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            // Check if we've gone too far
            int distance = Math.Abs(current.X - target.X) + Math.Abs(current.Y - target.Y);
            if (distance > maxSearchRadius)
                continue; // Skip this cell but continue searching neighbors that are still within radius

            // If this cell is unoccupied, use it
            if (!occupied.Contains(current))
                return current;

            // Check neighbors
            foreach (var neighbor in GetNeighbors(current))
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    // Only enqueue if within radius
                    int neighborDistance = Math.Abs(neighbor.X - target.X) + Math.Abs(neighbor.Y - target.Y);
                    if (neighborDistance <= maxSearchRadius)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return null; // No unoccupied cell found within radius
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

    private IReadOnlyList<Cell>? AStar(Cell start, Cell end, HashSet<Cell> occupied)
    {
#if DEBUG
        DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* called: start={start}, end={end}, startOccupied={occupied.Contains(start)}, endOccupied={occupied.Contains(end)}");
#endif
        var openSet = new PriorityQueue<Cell, int>();
        var closedSet = new HashSet<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var gScore = new Dictionary<Cell, int> { [start] = 0 };

        openSet.Enqueue(start, ManhattanDistance(start, end));
        
        int nodesExplored = 0;
#if DEBUG
        int lastReport = 0;
        DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* initialized: openSet.Count={openSet.Count}, startPriority={ManhattanDistance(start, end)}");
#endif

        // Limit exploration to prevent infinite loops
        // For large dungeons, increase the limit based on distance
        int manhattanDist = ManhattanDistance(start, end);
        // Base limit of 10,000, but increase for large distances (up to 50,000 for very large dungeons)
        int maxNodesExplored = Math.Min(10000 + (manhattanDist * 100), 50000);

        while (openSet.Count > 0)
        {
#if DEBUG
            if (nodesExplored == 0)
                DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* entering while loop, openSet.Count={openSet.Count}");
#endif
            var current = openSet.Dequeue();

            if (closedSet.Contains(current))
            {
#if DEBUG
                if (nodesExplored < 10)
                    DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* skipping already-closed cell: {current}");
#endif
                continue;
            }

            closedSet.Add(current);
            
#if DEBUG
            nodesExplored++;
            DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* exploring node {nodesExplored}: {current}, openSet={openSet.Count}, closedSet={closedSet.Count}");
            if (nodesExplored - lastReport >= 500 || nodesExplored == 1) // Report every 500 or first
            {
                lastReport = nodesExplored;
                DungeonDebugVisualizer.PrintAStarProgress(
                    nodesExplored, 
                    openSet.Count, 
                    closedSet.Count, 
                    current, 
                    end);
            }
#else
            nodesExplored++;
#endif

            // Safety limit to prevent infinite exploration
            if (nodesExplored >= maxNodesExplored)
            {
#if DEBUG
                DebugLogger.LogWarn(DebugLogger.Component.AStar, $"A* reached max exploration limit ({maxNodesExplored} nodes), aborting");
                DungeonDebugVisualizer.PrintAStarComplete(false, nodesExplored, 0, start, end);
#endif
                return null;
            }

            if (current == end)
            {
                // Reconstruct path
                var pathList = new List<Cell>();
                
                // Handle edge case where start == end
                if (current == start)
                {
                    pathList.Add(start);
#if DEBUG
                    DungeonDebugVisualizer.PrintAStarComplete(true, nodesExplored, pathList.Count, start, end);
#endif
                    return pathList;
                }
                
                // Build path back to start
                var node = end;
                pathList.Add(end); // Start with end
                
                // Follow cameFrom chain back to start
                while (cameFrom.TryGetValue(node, out var prev))
                {
                    pathList.Add(prev);
                    node = prev;
                    if (node == start)
                        break;
                }
                
                // Verify we reached start - if not, this indicates a bug in A* implementation
                if (pathList[pathList.Count - 1] != start)
                {
                    // In normal A*, end should always have cameFrom pointing back to start
                    // If we didn't reach start, it means end wasn't properly linked
                    // This should only happen if start == end (already handled above)
                    // But if it happens otherwise, we still try to create a valid path
                    pathList.Add(start);
                    
#if DEBUG
                    DebugLogger.LogWarn(DebugLogger.Component.AStar, $"WARNING: A* path reconstruction didn't reach start via cameFrom chain. " +
                        $"Start: {start}, End: {end}, Path length before adding start: {pathList.Count - 1}");
#endif
                }
                
                pathList.Reverse(); // Now path goes from start to end
                
#if DEBUG
                DungeonDebugVisualizer.PrintAStarComplete(true, nodesExplored, pathList.Count, start, end);
#endif
                return pathList;
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                // Skip occupied cells unless it's the end
                if (occupied.Contains(neighbor) && neighbor != end)
                    continue;

                // Calculate cost: base cost of 1, but add penalty if adjacent to occupied cells
                // This helps find paths around obstacles in dense dungeons
                int baseCost = 1;
                int penalty = 0;
                
                // Check if this neighbor is adjacent to any occupied cells (except the end)
                foreach (var adjacent in GetNeighbors(neighbor))
                {
                    if (occupied.Contains(adjacent) && adjacent != end)
                    {
                        penalty += 2; // Small penalty for being near obstacles
                        break; // Only count once per neighbor
                    }
                }

                int tentativeG = gScore[current] + baseCost + penalty;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, end);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

#if DEBUG
        DungeonDebugVisualizer.PrintAStarComplete(false, nodesExplored, 0, start, end);
#endif
        return null; // No path found
    }

    private IEnumerable<Cell> GetNeighbors(Cell cell)
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
        // Combine consecutive cells going same direction into segments
        var segments = new List<HallwaySegment>();

        if (path.Count < 2) return segments;

        // Validate that all cells in the path are adjacent (defensive check)
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
                // Direction changed, end current segment
                segments.Add(new HallwaySegment
                {
                    Start = segmentStart,
                    End = prev
                });
                segmentStart = prev;
            }

            lastDir = dir;
        }

        // Add final segment
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
}


using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Options for configuring A* pathfinding behavior.
/// </summary>
public class AStarOptions
{
    /// <summary>
    /// Enables debug logging (only effective in DEBUG builds).
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Maximum number of nodes to explore. If null, uses dynamic calculation based on distance.
    /// </summary>
    public int? MaxNodesExplored { get; set; } = null;

    /// <summary>
    /// Enables obstacle penalty logic (adds penalty for cells adjacent to occupied cells).
    /// </summary>
    public bool UseObstaclePenalty { get; set; } = false;
}

/// <summary>
/// Utility methods for pathfinding operations.
/// </summary>
public static class PathfindingUtilities
{
    /// <summary>
    /// Calculates the Manhattan distance between two cells.
    /// </summary>
    /// <param name="a">The first cell.</param>
    /// <param name="b">The second cell.</param>
    /// <returns>The Manhattan distance between the two cells.</returns>
    public static int ManhattanDistance(Cell a, Cell b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    /// <summary>
    /// Gets the four cardinal neighbors of a cell (North, South, East, West).
    /// </summary>
    /// <param name="cell">The cell to get neighbors for.</param>
    /// <returns>An enumerable of the four cardinal neighbors.</returns>
    public static IEnumerable<Cell> GetNeighbors(Cell cell)
    {
        yield return cell.North;
        yield return cell.South;
        yield return cell.East;
        yield return cell.West;
    }

    /// <summary>
    /// Finds the nearest unoccupied cell to the target cell using breadth-first search.
    /// </summary>
    /// <param name="target">The target cell to find an alternative for.</param>
    /// <param name="occupied">The set of occupied cells to avoid.</param>
    /// <param name="maxSearchRadius">The maximum search radius.</param>
    /// <param name="getNeighbors">A function to get neighbors of a cell.</param>
    /// <returns>The nearest unoccupied cell, or null if none found within the radius.</returns>
    public static Cell? FindNearestUnoccupiedCell(Cell target, HashSet<Cell> occupied, int maxSearchRadius, Func<Cell, IEnumerable<Cell>> getNeighbors)
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
            foreach (var neighbor in getNeighbors(current))
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

    /// <summary>
    /// Finds a path from start to end using the A* pathfinding algorithm.
    /// </summary>
    /// <param name="start">The starting cell.</param>
    /// <param name="end">The target cell.</param>
    /// <param name="occupied">The set of occupied cells to avoid.</param>
    /// <param name="options">Optional configuration for A* behavior.</param>
    /// <returns>A path from start to end, or null if no path exists.</returns>
    public static IReadOnlyList<Cell>? AStar(Cell start, Cell end, HashSet<Cell> occupied, AStarOptions? options = null)
    {
        options ??= new AStarOptions();

#if DEBUG
        if (options.EnableDebugLogging)
        {
            DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* called: start={start}, end={end}, startOccupied={occupied.Contains(start)}, endOccupied={occupied.Contains(end)}");
        }
#endif
        var openSet = new PriorityQueue<Cell, int>();
        var closedSet = new HashSet<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var gScore = new Dictionary<Cell, int> { [start] = 0 };

        openSet.Enqueue(start, ManhattanDistance(start, end));
        
        int nodesExplored = 0;
#if DEBUG
        int lastReport = 0;
        if (options.EnableDebugLogging)
        {
            DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* initialized: openSet.Count={openSet.Count}, startPriority={ManhattanDistance(start, end)}");
        }
#endif

        // Limit exploration to prevent infinite loops
        int maxNodesExplored;
        if (options.MaxNodesExplored.HasValue)
        {
            maxNodesExplored = options.MaxNodesExplored.Value;
        }
        else
        {
            // For large dungeons, increase the limit based on distance
            int manhattanDist = ManhattanDistance(start, end);
            // Base limit of 10,000, but increase for large distances (up to 50,000 for very large dungeons)
            maxNodesExplored = Math.Min(10000 + (manhattanDist * 100), 50000);
        }

        while (openSet.Count > 0)
        {
#if DEBUG
            if (options.EnableDebugLogging && nodesExplored == 0)
            {
                DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* entering while loop, openSet.Count={openSet.Count}");
            }
#endif
            var current = openSet.Dequeue();

            if (closedSet.Contains(current))
            {
#if DEBUG
                if (options.EnableDebugLogging && nodesExplored < 10)
                {
                    DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"A* skipping already-closed cell: {current}");
                }
#endif
                continue;
            }

            closedSet.Add(current);
            
#if DEBUG
            if (options.EnableDebugLogging)
            {
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
            }
            else
            {
                nodesExplored++;
            }
#else
            nodesExplored++;
#endif

            // Safety limit to prevent infinite exploration
            if (nodesExplored >= maxNodesExplored)
            {
#if DEBUG
                if (options.EnableDebugLogging)
                {
                    DebugLogger.LogWarn(DebugLogger.Component.AStar, $"A* reached max exploration limit ({maxNodesExplored} nodes), aborting");
                    DungeonDebugVisualizer.PrintAStarComplete(false, nodesExplored, 0, start, end);
                }
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
                    if (options.EnableDebugLogging)
                    {
                        DungeonDebugVisualizer.PrintAStarComplete(true, nodesExplored, pathList.Count, start, end);
                    }
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
                    if (options.EnableDebugLogging)
                    {
                        DebugLogger.LogWarn(DebugLogger.Component.AStar, $"WARNING: A* path reconstruction didn't reach start via cameFrom chain. " +
                            $"Start: {start}, End: {end}, Path length before adding start: {pathList.Count - 1}");
                    }
#endif
                }
                
                pathList.Reverse(); // Now path goes from start to end
                
#if DEBUG
                if (options.EnableDebugLogging)
                {
                    DungeonDebugVisualizer.PrintAStarComplete(true, nodesExplored, pathList.Count, start, end);
                }
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
                int baseCost = 1;
                int penalty = 0;
                
                if (options.UseObstaclePenalty)
                {
                    // Check if this neighbor is adjacent to any occupied cells (except the end)
                    foreach (var adjacent in GetNeighbors(neighbor))
                    {
                        if (occupied.Contains(adjacent) && adjacent != end)
                        {
                            penalty += 2; // Small penalty for being near obstacles
                            break; // Only count once per neighbor
                        }
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
        if (options.EnableDebugLogging)
        {
            DungeonDebugVisualizer.PrintAStarComplete(false, nodesExplored, 0, start, end);
        }
#endif
        return null; // No path found
    }
}

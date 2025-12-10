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

        foreach (var conn in graph.Connections.Where(c => c.RequiresHallway))
        {
            var roomA = rooms.First(r => r.NodeId == conn.NodeAId);
            var roomB = rooms.First(r => r.NodeId == conn.NodeBId);

            // Find best door positions on each room
            var (doorA, doorB) = FindBestDoorPair(roomA, roomB, rng);

            // Pathfind between doors
            var path = FindHallwayPath(doorA.WorldCell, doorA.Edge, doorB.WorldCell, doorB.Edge, occupiedCells);

            // Convert path to hallway segments
            var segments = PathToSegments(path);

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

        // A* pathfinding avoiding occupied cells
        var path = AStar(start, end, occupied);

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

    private IReadOnlyList<Cell>? AStar(Cell start, Cell end, HashSet<Cell> occupied)
    {
        var openSet = new PriorityQueue<Cell, int>();
        var closedSet = new HashSet<Cell>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var gScore = new Dictionary<Cell, int> { [start] = 0 };

        openSet.Enqueue(start, ManhattanDistance(start, end));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (closedSet.Contains(current))
                continue;

            closedSet.Add(current);

            if (current == end)
            {
                // Reconstruct path
                var path = new List<Cell>();
                var node = end;
                while (cameFrom.TryGetValue(node, out var prev) && node != start)
                {
                    path.Add(node);
                    node = prev;
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            foreach (var neighbor in GetNeighbors(current))
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


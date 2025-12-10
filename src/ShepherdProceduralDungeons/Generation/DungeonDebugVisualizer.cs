#if DEBUG
using System.Text;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Debug helper to visualize dungeon layouts during generation.
/// </summary>
internal static class DungeonDebugVisualizer
{
    public static void PrintRoomPlacement<TRoomType>(
        int roomId, 
        TRoomType roomType, 
        PlacedRoom<TRoomType> room, 
        int totalRooms) where TRoomType : Enum
    {
        var bounds = GetBounds(room.GetWorldCells());
        Console.WriteLine($"[DEBUG] Placed Room {roomId} ({roomType}) at anchor {room.Position}");
        Console.WriteLine($"[DEBUG]   Bounds: {bounds.min} to {bounds.max}");
        Console.WriteLine($"[DEBUG]   Cells: {room.GetWorldCells().Count()}");
        Console.WriteLine($"[DEBUG]   Progress: {totalRooms} rooms placed");
    }

    public static void PrintSpatialLayout<TRoomType>(
        IReadOnlyList<PlacedRoom<TRoomType>> rooms, 
        string stage) where TRoomType : Enum
    {
        if (rooms.Count == 0) return;

        Console.WriteLine($"\n[DEBUG] === {stage} ===");
        Console.WriteLine($"[DEBUG] Total rooms placed: {rooms.Count}");

        var allCells = rooms.SelectMany(r => r.GetWorldCells()).ToList();
        var bounds = GetBounds(allCells);
        int width = bounds.max.X - bounds.min.X + 1;
        int height = bounds.max.Y - bounds.min.Y + 1;

        Console.WriteLine($"[DEBUG] Overall bounds: {bounds.min} to {bounds.max} (size: {width}x{height})");

        // Print each room's position
        foreach (var room in rooms.OrderBy(r => r.NodeId))
        {
            var roomBounds = GetBounds(room.GetWorldCells());
            int roomWidth = roomBounds.max.X - roomBounds.min.X + 1;
            int roomHeight = roomBounds.max.Y - roomBounds.min.Y + 1;
            Console.WriteLine($"[DEBUG]   Room {room.NodeId} ({room.RoomType}): anchor={room.Position}, size={roomWidth}x{roomHeight}, bounds={roomBounds.min} to {roomBounds.max}");
        }
    }

    public static void PrintHallwayAttempt(
        int roomAId, 
        int roomBId, 
        Cell doorA, 
        Cell doorB, 
        Cell start, 
        Cell end,
        int manhattanDistance)
    {
        Console.WriteLine($"\n[DEBUG] === Hallway Generation: Room {roomAId} -> Room {roomBId} ===");
        Console.WriteLine($"[DEBUG] Door A at: {doorA} (adjacent start: {start})");
        Console.WriteLine($"[DEBUG] Door B at: {doorB} (adjacent end: {end})");
        Console.WriteLine($"[DEBUG] Manhattan distance: {manhattanDistance} cells");
        Console.WriteLine($"[DEBUG] Starting A* pathfinding...");
    }

    public static void PrintAStarProgress(
        int nodesExplored, 
        int openSetSize, 
        int closedSetSize,
        Cell currentCell,
        Cell targetCell)
    {
        if (nodesExplored % 1000 == 0) // Print every 1000 nodes to avoid spam
        {
            int remaining = Math.Abs(currentCell.X - targetCell.X) + Math.Abs(currentCell.Y - targetCell.Y);
            Console.WriteLine($"[DEBUG]   A* Progress: explored={nodesExplored}, open={openSetSize}, closed={closedSetSize}, current={currentCell}, remaining={remaining}");
        }
    }

    public static void PrintAStarComplete(
        bool success, 
        int nodesExplored, 
        int pathLength,
        Cell start,
        Cell end)
    {
        if (success)
        {
            Console.WriteLine($"[DEBUG] ✓ A* found path: {pathLength} cells, explored {nodesExplored} nodes");
        }
        else
        {
            Console.WriteLine($"[DEBUG] ✗ A* failed: explored {nodesExplored} nodes, no path from {start} to {end}");
        }
    }

    public static string CreateAsciiMap<TRoomType>(
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        HashSet<Cell>? occupiedCells = null) where TRoomType : Enum
    {
        if (rooms.Count == 0) return "[empty]";

        var allCells = rooms.SelectMany(r => r.GetWorldCells()).ToList();
        if (occupiedCells != null)
        {
            allCells.AddRange(occupiedCells);
        }

        var bounds = GetBounds(allCells);
        int width = bounds.max.X - bounds.min.X + 1;
        int height = bounds.max.Y - bounds.min.Y + 1;

        // Limit size for readability
        if (width > 60 || height > 30)
        {
            return $"[Map too large: {width}x{height}, bounds: {bounds.min} to {bounds.max}]";
        }

        var map = new char[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = ' ';
            }
        }

        // Mark rooms with their ID (single digit)
        foreach (var room in rooms)
        {
            foreach (var cell in room.GetWorldCells())
            {
                int x = cell.X - bounds.min.X;
                int y = cell.Y - bounds.min.Y;
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    char c = room.NodeId.ToString()[0];
                    map[x, y] = c;
                }
            }
        }

        // Mark other occupied cells
        if (occupiedCells != null)
        {
            foreach (var cell in occupiedCells)
            {
                int x = cell.X - bounds.min.X;
                int y = cell.Y - bounds.min.Y;
                if (x >= 0 && x < width && y >= 0 && y < height && map[x, y] == ' ')
                {
                    map[x, y] = '#';
                }
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[DEBUG] ASCII Map ({width}x{height}):");
        for (int y = 0; y < height; y++)
        {
            sb.Append("[DEBUG] ");
            for (int x = 0; x < width; x++)
            {
                sb.Append(map[x, y]);
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static (Cell min, Cell max) GetBounds(IEnumerable<Cell> cells)
    {
        var cellList = cells.ToList();
        if (cellList.Count == 0)
            return (new Cell(0, 0), new Cell(0, 0));

        int minX = cellList.Min(c => c.X);
        int maxX = cellList.Max(c => c.X);
        int minY = cellList.Min(c => c.Y);
        int maxY = cellList.Max(c => c.Y);

        return (new Cell(minX, minY), new Cell(maxX, maxY));
    }
}
#endif


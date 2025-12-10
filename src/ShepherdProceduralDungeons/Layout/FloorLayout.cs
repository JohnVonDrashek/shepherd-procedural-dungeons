using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// The final output of dungeon generation, containing all placed rooms, hallways, and doors.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class FloorLayout<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// All placed rooms with positions.
    /// </summary>
    public required IReadOnlyList<PlacedRoom<TRoomType>> Rooms { get; init; }

    /// <summary>
    /// Generated hallways between rooms.
    /// </summary>
    public required IReadOnlyList<Hallway> Hallways { get; init; }

    /// <summary>
    /// All doors in the floor.
    /// </summary>
    public required IReadOnlyList<Door> Doors { get; init; }

    /// <summary>
    /// The seed used to generate this floor.
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Node IDs forming the critical path from spawn to boss.
    /// </summary>
    public required IReadOnlyList<int> CriticalPath { get; init; }

    /// <summary>
    /// ID of the spawn room (node ID).
    /// </summary>
    public required int SpawnRoomId { get; init; }

    /// <summary>
    /// ID of the boss room (node ID).
    /// </summary>
    public required int BossRoomId { get; init; }

    /// <summary>
    /// Gets a room by its node ID.
    /// </summary>
    public PlacedRoom<TRoomType>? GetRoom(int nodeId) => Rooms.FirstOrDefault(r => r.NodeId == nodeId);

    /// <summary>
    /// Gets all cells occupied by rooms (not hallways).
    /// </summary>
    public IEnumerable<Cell> GetAllRoomCells() => Rooms.SelectMany(r => r.GetWorldCells());

    /// <summary>
    /// Gets all cells occupied by hallways.
    /// </summary>
    public IEnumerable<Cell> GetAllHallwayCells() => Hallways.SelectMany(h => h.Segments.SelectMany(s => s.GetCells()));

    /// <summary>
    /// Gets the bounding box of the entire floor.
    /// Returns (Min, Max) cells that contain all rooms and hallways.
    /// </summary>
    public (Cell Min, Cell Max) GetBounds()
    {
        var allCells = GetAllRoomCells().Concat(GetAllHallwayCells()).ToList();
        
        if (allCells.Count == 0)
            return (new Cell(0, 0), new Cell(0, 0));

        int minX = allCells.Min(c => c.X);
        int maxX = allCells.Max(c => c.X);
        int minY = allCells.Min(c => c.Y);
        int maxY = allCells.Max(c => c.Y);

        return (new Cell(minX, minY), new Cell(maxX, maxY));
    }
}


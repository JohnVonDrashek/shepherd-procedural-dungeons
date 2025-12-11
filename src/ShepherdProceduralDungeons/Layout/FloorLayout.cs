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
    /// Zone assignments for rooms (node ID -> zone ID). Null if no zones configured.
    /// </summary>
    public IReadOnlyDictionary<int, string>? ZoneAssignments { get; init; }

    /// <summary>
    /// Rooms that connect different zones (transition rooms). Empty if no zones configured.
    /// </summary>
    public IReadOnlyList<PlacedRoom<TRoomType>> TransitionRooms { get; init; } = Array.Empty<PlacedRoom<TRoomType>>();

    /// <summary>
    /// All secret passages in this floor.
    /// </summary>
    public required IReadOnlyList<SecretPassage> SecretPassages { get; init; }

    /// <summary>
    /// Gets a room by its node ID.
    /// </summary>
    public PlacedRoom<TRoomType>? GetRoom(int nodeId) => Rooms.FirstOrDefault(r => r.NodeId == nodeId);

    /// <summary>
    /// Gets all secret passages connected to a specific room.
    /// </summary>
    public IEnumerable<SecretPassage> GetSecretPassagesForRoom(int roomId)
    {
        return SecretPassages.Where(sp => sp.RoomAId == roomId || sp.RoomBId == roomId);
    }

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

    /// <summary>
    /// Gets all interior features from all rooms in world coordinates.
    /// </summary>
    public IEnumerable<(Cell WorldCell, InteriorFeature Feature)> InteriorFeatures =>
        Rooms.SelectMany(r => r.GetInteriorFeatures());

    /// <summary>
    /// Gets a dictionary mapping node IDs to their difficulty levels.
    /// </summary>
    public IReadOnlyDictionary<int, double> GetDifficultyByNodeId()
    {
        return Rooms.ToDictionary(r => r.NodeId, r => r.Difficulty);
    }

    /// <summary>
    /// All detected room clusters, grouped by room type.
    /// </summary>
    public IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomCluster<TRoomType>>> Clusters { get; init; } = 
        new Dictionary<TRoomType, IReadOnlyList<RoomCluster<TRoomType>>>();

    /// <summary>
    /// Gets all clusters for a specific room type.
    /// </summary>
    public IReadOnlyList<RoomCluster<TRoomType>> GetClustersForRoomType(TRoomType roomType)
    {
        return Clusters.TryGetValue(roomType, out var clusters) ? clusters : Array.Empty<RoomCluster<TRoomType>>();
    }

    /// <summary>
    /// Gets the largest cluster for a specific room type, or null if no clusters exist.
    /// </summary>
    public RoomCluster<TRoomType>? GetLargestCluster(TRoomType roomType)
    {
        var clusters = GetClustersForRoomType(roomType);
        if (clusters.Count == 0)
            return null;

        return clusters.OrderByDescending(c => c.GetSize()).First();
    }
}


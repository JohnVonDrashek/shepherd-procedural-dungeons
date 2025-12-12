using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires a room to be at least a minimum distance from the spawn room (spatial distance, not graph distance).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MinSpatialDistanceFromStartConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Minimum distance in cells (Manhattan distance).
    /// </summary>
    public int MinDistance { get; }

    /// <summary>
    /// Creates a new minimum distance from start constraint.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="minDistance">Minimum distance in cells (Manhattan distance).</param>
    public MinSpatialDistanceFromStartConstraint(TRoomType targetRoomType, int minDistance)
    {
        TargetRoomType = targetRoomType;
        MinDistance = minDistance;
    }

    /// <inheritdoc/>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Graph-based validation always passes for spatial constraints
        return true;
    }

    /// <inheritdoc/>
    public bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // Find spawn room
        var spawnRoom = placedRooms.FirstOrDefault(r => r.NodeId == graph.StartNodeId);
        if (spawnRoom == null)
        {
            // No spawn room placed yet, allow placement
            return true;
        }

        // Calculate minimum Manhattan distance from any cell of this room to any cell of spawn room
        var proposedCells = roomTemplate.Cells.Select(c => new Cell(proposedPosition.X + c.X, proposedPosition.Y + c.Y)).ToList();
        var spawnCells = spawnRoom.GetWorldCells().ToList();

        var minDistance = proposedCells
            .SelectMany(pc => spawnCells.Select(sc => Math.Abs(pc.X - sc.X) + Math.Abs(pc.Y - sc.Y)))
            .Min();

        return minDistance >= MinDistance;
    }
}

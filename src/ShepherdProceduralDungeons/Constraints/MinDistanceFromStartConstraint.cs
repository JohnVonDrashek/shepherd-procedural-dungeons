using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to be at least N steps from the start node.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MinDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Minimum distance required from the start node.
    /// </summary>
    public int MinDistance { get; }

    /// <summary>
    /// Creates a new minimum distance constraint.
    /// </summary>
    public MinDistanceFromStartConstraint(TRoomType roomType, int minDistance)
    {
        TargetRoomType = roomType;
        MinDistance = minDistance;
    }

    /// <summary>
    /// Checks if the node is at least MinDistance steps from start.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.DistanceFromStart >= MinDistance;
}

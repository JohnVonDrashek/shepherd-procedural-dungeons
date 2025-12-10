using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to be at most N steps from the start node.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum distance allowed from the start node.
    /// </summary>
    public int MaxDistance { get; }

    /// <summary>
    /// Creates a new maximum distance constraint.
    /// </summary>
    public MaxDistanceFromStartConstraint(TRoomType roomType, int maxDistance)
    {
        TargetRoomType = roomType;
        MaxDistance = maxDistance;
    }

    /// <summary>
    /// Checks if the node is at most MaxDistance steps from start.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.DistanceFromStart <= MaxDistance;
}

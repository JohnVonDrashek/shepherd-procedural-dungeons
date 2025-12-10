using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to NOT be on the critical path (start to boss).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class NotOnCriticalPathConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Creates a new not-on-critical-path constraint.
    /// </summary>
    public NotOnCriticalPathConstraint(TRoomType roomType)
    {
        TargetRoomType = roomType;
    }

    /// <summary>
    /// Checks if the node is NOT on the critical path.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => !node.IsOnCriticalPath;
}

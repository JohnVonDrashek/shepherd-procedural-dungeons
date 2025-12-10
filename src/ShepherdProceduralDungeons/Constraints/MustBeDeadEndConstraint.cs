using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to be a dead end (exactly one connection).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MustBeDeadEndConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Creates a new must-be-dead-end constraint.
    /// </summary>
    public MustBeDeadEndConstraint(TRoomType roomType)
    {
        TargetRoomType = roomType;
    }

    /// <summary>
    /// Checks if the node has exactly one connection.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.ConnectionCount == 1;
}


using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Custom callback-based constraint for advanced placement logic.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class CustomConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    private readonly Func<RoomNode, FloorGraph, IReadOnlyDictionary<int, TRoomType>, bool> _predicate;

    /// <summary>
    /// Creates a new custom constraint with a predicate function.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="predicate">Function that returns true if the node is valid for this room type.</param>
    public CustomConstraint(TRoomType roomType, Func<RoomNode, FloorGraph, IReadOnlyDictionary<int, TRoomType>, bool> predicate)
    {
        TargetRoomType = roomType;
        _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    }

    /// <summary>
    /// Evaluates the custom predicate function.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => _predicate(node, graph, currentAssignments);
}


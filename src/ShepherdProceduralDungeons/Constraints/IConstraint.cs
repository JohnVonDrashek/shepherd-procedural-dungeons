using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Interface for room placement constraints.
/// Constraints determine which nodes are valid locations for specific room types.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public interface IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    TRoomType TargetRoomType { get; }

    /// <summary>
    /// Checks if a node is valid for the target room type.
    /// </summary>
    /// <param name="node">The node being evaluated.</param>
    /// <param name="graph">The full graph for context.</param>
    /// <param name="currentAssignments">Room types already assigned to other nodes.</param>
    /// <returns>True if this node can be assigned the target room type.</returns>
    bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments);
}

using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring the target room type to form at least one cluster of the specified minimum size.
/// Note: This constraint cannot be fully validated during room type assignment since clustering
/// happens after spatial placement. It serves as a requirement that will be validated post-generation.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MinClusterSizeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The minimum cluster size required.
    /// </summary>
    public int MinSize { get; }

    /// <summary>
    /// Creates a constraint requiring the target room type to form at least one cluster of the specified minimum size.
    /// </summary>
    public MinClusterSizeConstraint(TRoomType targetRoomType, int minSize)
    {
        if (minSize < 1)
            throw new ArgumentException("Minimum cluster size must be at least 1.", nameof(minSize));

        TargetRoomType = targetRoomType;
        MinSize = minSize;
    }

    /// <summary>
    /// Always returns true during room type assignment since clustering happens after spatial placement.
    /// This constraint is validated post-generation by checking that clusters meet the minimum size requirement.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Cannot validate clustering during room type assignment
        // This will be validated post-generation
        return true;
    }
}

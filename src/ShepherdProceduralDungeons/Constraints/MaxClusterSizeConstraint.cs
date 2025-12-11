using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring the target room type clusters to not exceed the specified maximum size.
/// Note: This constraint cannot be fully validated during room type assignment since clustering
/// happens after spatial placement. It serves as a requirement that will be validated post-generation.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxClusterSizeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The maximum cluster size allowed.
    /// </summary>
    public int MaxSize { get; }

    /// <summary>
    /// Creates a constraint requiring the target room type clusters to not exceed the specified maximum size.
    /// </summary>
    public MaxClusterSizeConstraint(TRoomType targetRoomType, int maxSize)
    {
        if (maxSize < 1)
            throw new ArgumentException("Maximum cluster size must be at least 1.", nameof(maxSize));

        TargetRoomType = targetRoomType;
        MaxSize = maxSize;
    }

    /// <summary>
    /// Always returns true during room type assignment since clustering happens after spatial placement.
    /// This constraint is validated post-generation by checking that clusters do not exceed the maximum size.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Cannot validate clustering during room type assignment
        // This will be validated post-generation
        return true;
    }
}

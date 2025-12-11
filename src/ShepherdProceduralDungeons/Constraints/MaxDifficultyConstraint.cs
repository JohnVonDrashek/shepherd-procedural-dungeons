using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to have at most a maximum difficulty level.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MaxDifficultyConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum difficulty allowed.
    /// </summary>
    public double MaxDifficulty { get; }

    /// <summary>
    /// Creates a new maximum difficulty constraint.
    /// </summary>
    public MaxDifficultyConstraint(TRoomType roomType, double maxDifficulty)
    {
        TargetRoomType = roomType;
        MaxDifficulty = maxDifficulty;
    }

    /// <summary>
    /// Checks if the node has at most MaxDifficulty.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.Difficulty <= MaxDifficulty;
}

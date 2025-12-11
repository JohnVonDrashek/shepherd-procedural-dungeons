using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to have at least a minimum difficulty level.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MinDifficultyConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Minimum difficulty required.
    /// </summary>
    public double MinDifficulty { get; }

    /// <summary>
    /// Creates a new minimum difficulty constraint.
    /// </summary>
    public MinDifficultyConstraint(TRoomType roomType, double minDifficulty)
    {
        TargetRoomType = roomType;
        MinDifficulty = minDifficulty;
    }

    /// <summary>
    /// Checks if the node has at least MinDifficulty.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.Difficulty >= MinDifficulty;
}

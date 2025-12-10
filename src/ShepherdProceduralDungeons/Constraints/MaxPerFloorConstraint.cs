using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint limiting the maximum number of rooms of a specific type on the floor.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxPerFloorConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum number of rooms of this type allowed on the floor.
    /// </summary>
    public int MaxCount { get; }

    /// <summary>
    /// Creates a new max-per-floor constraint.
    /// </summary>
    public MaxPerFloorConstraint(TRoomType roomType, int maxCount)
    {
        TargetRoomType = roomType;
        MaxCount = maxCount;
    }

    /// <summary>
    /// Checks if adding this room type would not exceed the maximum count.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        int currentCount = currentAssignments.Values.Count(t => t.Equals(TargetRoomType));
        return currentCount < MaxCount;
    }
}


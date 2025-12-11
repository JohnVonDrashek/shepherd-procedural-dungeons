using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room type to be placed on a floor at or below the maximum floor index.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum floor index (0-based) where this room type can be placed.
    /// </summary>
    public int MaxFloor { get; }

    private int _currentFloorIndex = -1;

    /// <summary>
    /// Creates a new max-floor constraint.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="maxFloor">Maximum floor index (0-based) where this room type can be placed.</param>
    public MaxFloorConstraint(TRoomType roomType, int maxFloor)
    {
        TargetRoomType = roomType;
        MaxFloor = maxFloor;
    }

    /// <summary>
    /// Sets the current floor index for constraint evaluation.
    /// </summary>
    public void SetFloorIndex(int floorIndex)
    {
        _currentFloorIndex = floorIndex;
    }

    /// <summary>
    /// Checks if the current floor is at or below the maximum floor.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        if (_currentFloorIndex < 0)
            return true; // Floor index not set, allow (for backward compatibility)

        return _currentFloorIndex <= MaxFloor;
    }
}

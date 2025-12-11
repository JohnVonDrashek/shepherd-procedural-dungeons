using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room type to NOT be placed on specific floors.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class NotOnFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Floor indices where this room type cannot be placed.
    /// </summary>
    public IReadOnlyList<int> ExcludedFloors { get; }

    private int _currentFloorIndex = -1;

    /// <summary>
    /// Creates a new not-on-floor constraint.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="excludedFloors">Floor indices where this room type cannot be placed.</param>
    public NotOnFloorConstraint(TRoomType roomType, IReadOnlyList<int> excludedFloors)
    {
        TargetRoomType = roomType;
        ExcludedFloors = excludedFloors ?? throw new ArgumentNullException(nameof(excludedFloors));
    }

    /// <summary>
    /// Sets the current floor index for constraint evaluation.
    /// </summary>
    public void SetFloorIndex(int floorIndex)
    {
        _currentFloorIndex = floorIndex;
    }

    /// <summary>
    /// Checks if the current floor is not in the excluded floors list.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        if (_currentFloorIndex < 0)
            return true; // Floor index not set, allow (for backward compatibility)

        return !ExcludedFloors.Contains(_currentFloorIndex);
    }
}

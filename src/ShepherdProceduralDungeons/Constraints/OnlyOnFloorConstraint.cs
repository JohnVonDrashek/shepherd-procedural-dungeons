using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room type to ONLY be placed on specific floors.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class OnlyOnFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Floor indices where this room type can be placed.
    /// </summary>
    public IReadOnlyList<int> AllowedFloors { get; }

    private int _currentFloorIndex = -1;

    /// <summary>
    /// Creates a new only-on-floor constraint.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="allowedFloors">Floor indices where this room type can be placed.</param>
    public OnlyOnFloorConstraint(TRoomType roomType, IReadOnlyList<int> allowedFloors)
    {
        TargetRoomType = roomType;
        AllowedFloors = allowedFloors ?? throw new ArgumentNullException(nameof(allowedFloors));
    }

    /// <summary>
    /// Sets the current floor index for constraint evaluation.
    /// </summary>
    public void SetFloorIndex(int floorIndex)
    {
        _currentFloorIndex = floorIndex;
    }

    /// <summary>
    /// Checks if the current floor is in the allowed floors list.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        if (_currentFloorIndex < 0)
            return true; // Floor index not set, allow (for backward compatibility)

        return AllowedFloors.Contains(_currentFloorIndex);
    }
}

using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires a room type to only be placed in a specific zone.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class OnlyInZoneConstraint<TRoomType> : IConstraint<TRoomType>, IZoneAwareConstraint<TRoomType> 
    where TRoomType : Enum
{
    private IReadOnlyDictionary<int, string>? _zoneAssignments;

    /// <summary>
    /// Creates a constraint that requires the target room type to only be placed in the specified zone.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="zoneId">The zone ID where this room type must be placed.</param>
    public OnlyInZoneConstraint(TRoomType targetRoomType, string zoneId)
    {
        TargetRoomType = targetRoomType;
        ZoneId = zoneId;
    }

    /// <inheritdoc/>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The zone ID where this room type must be placed.
    /// </summary>
    public string ZoneId { get; }

    /// <inheritdoc/>
    public void SetZoneAssignments(IReadOnlyDictionary<int, string> zoneAssignments)
    {
        _zoneAssignments = zoneAssignments;
    }

    /// <inheritdoc/>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        if (_zoneAssignments == null)
            return true; // If zones not assigned yet, allow (will be validated later)

        // Check if node is assigned to the required zone
        return _zoneAssignments.TryGetValue(node.Id, out var assignedZone) && assignedZone == ZoneId;
    }
}

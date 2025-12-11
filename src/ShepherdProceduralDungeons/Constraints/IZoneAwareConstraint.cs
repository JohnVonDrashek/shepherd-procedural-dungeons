namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Interface for constraints that need to check zone assignments.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public interface IZoneAwareConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Sets the zone assignments for zone-aware constraint validation.
    /// </summary>
    /// <param name="zoneAssignments">Dictionary mapping node IDs to zone IDs.</param>
    void SetZoneAssignments(IReadOnlyDictionary<int, string> zoneAssignments);
}

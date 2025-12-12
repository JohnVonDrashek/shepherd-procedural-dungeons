using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Interface for spatial constraints that validate room placement based on 2D spatial positions.
/// Spatial constraints are evaluated during the spatial placement phase, not during type assignment.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public interface ISpatialConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Checks if a room placement is valid spatially.
    /// Called during spatial placement phase, not type assignment phase.
    /// </summary>
    /// <param name="proposedPosition">The proposed anchor position for the room.</param>
    /// <param name="roomTemplate">The template being placed.</param>
    /// <param name="placedRooms">All rooms already placed in the dungeon.</param>
    /// <param name="graph">The floor graph for context.</param>
    /// <param name="assignments">Room type assignments.</param>
    /// <returns>True if this spatial position is valid for the target room type.</returns>
    bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments);
}

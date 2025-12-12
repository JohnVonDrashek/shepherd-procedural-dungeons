using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires a room to be within a maximum distance from rooms of specified type(s) (spatial distance, not graph distance).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MaxSpatialDistanceFromRoomTypeConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The reference room types to check distance from.
    /// </summary>
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }

    /// <summary>
    /// Maximum distance in cells (Manhattan distance).
    /// </summary>
    public int MaxDistance { get; }

    /// <summary>
    /// Creates a new maximum distance from room type constraint.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="referenceRoomType">The reference room type to check distance from.</param>
    /// <param name="maxDistance">Maximum distance in cells (Manhattan distance).</param>
    public MaxSpatialDistanceFromRoomTypeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType, int maxDistance)
    {
        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType> { referenceRoomType };
        MaxDistance = maxDistance;
    }

    /// <summary>
    /// Creates a new maximum distance from room type constraint with multiple reference types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="maxDistance">Maximum distance in cells (Manhattan distance).</param>
    /// <param name="referenceRoomTypes">The reference room types to check distance from.</param>
    public MaxSpatialDistanceFromRoomTypeConstraint(TRoomType targetRoomType, int maxDistance, params TRoomType[] referenceRoomTypes)
    {
        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType>(referenceRoomTypes);
        MaxDistance = maxDistance;
    }

    /// <inheritdoc/>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Graph-based validation always passes for spatial constraints
        return true;
    }

    /// <inheritdoc/>
    public bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // Find all placed rooms of reference types
        var referenceRooms = placedRooms
            .Where(r => assignments.TryGetValue(r.NodeId, out var assignedType) && ReferenceRoomTypes.Contains(assignedType))
            .ToList();

        if (referenceRooms.Count == 0)
        {
            // No reference rooms placed yet, allow placement
            return true;
        }

        // Calculate minimum Manhattan distance from any cell of this room to any cell of reference rooms
        var proposedCells = roomTemplate.Cells.Select(c => new Cell(proposedPosition.X + c.X, proposedPosition.Y + c.Y)).ToList();

        foreach (var referenceRoom in referenceRooms)
        {
            var referenceCells = referenceRoom.GetWorldCells().ToList();
            var minDistance = proposedCells
                .SelectMany(pc => referenceCells.Select(rc => Math.Abs(pc.X - rc.X) + Math.Abs(pc.Y - rc.Y)))
                .Min();

            if (minDistance <= MaxDistance)
            {
                // At least one reference room is within range
                return true;
            }
        }

        // No reference rooms are within range
        return false;
    }
}

using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires a room to be placed in a specific quadrant of the dungeon.
/// Quadrants are determined relative to the dungeon's bounding box.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MustBeInQuadrantConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The allowed quadrants (can be multiple using flags).
    /// </summary>
    public Quadrant AllowedQuadrants { get; }

    /// <summary>
    /// Creates a new quadrant constraint.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="allowedQuadrants">The allowed quadrants (can be multiple using flags).</param>
    public MustBeInQuadrantConstraint(TRoomType targetRoomType, Quadrant allowedQuadrants)
    {
        TargetRoomType = targetRoomType;
        AllowedQuadrants = allowedQuadrants;
    }

    /// <inheritdoc/>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Graph-based validation always passes for spatial constraints
        // The actual validation happens in IsValidSpatially
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
        if (placedRooms.Count == 0)
        {
            // First room can be placed anywhere
            return true;
        }

        // Calculate dungeon bounds
        var allCells = placedRooms.SelectMany(r => r.GetWorldCells()).ToList();
        if (allCells.Count == 0)
        {
            return true;
        }

        var minX = allCells.Min(c => c.X);
        var maxX = allCells.Max(c => c.X);
        var minY = allCells.Min(c => c.Y);
        var maxY = allCells.Max(c => c.Y);

        // Calculate center
        var centerX = (minX + maxX) / 2.0;
        var centerY = (minY + maxY) / 2.0;

        // Calculate room center (using template cells)
        var templateCells = roomTemplate.Cells.Select(c => new Cell(proposedPosition.X + c.X, proposedPosition.Y + c.Y)).ToList();
        var roomCenterX = templateCells.Average(c => c.X);
        var roomCenterY = templateCells.Average(c => c.Y);

        // Determine quadrant
        var quadrant = DetermineQuadrant(roomCenterX, roomCenterY, centerX, centerY);

        // Check if quadrant is allowed
        return (AllowedQuadrants & quadrant) != 0;
    }

    private Quadrant DetermineQuadrant(double x, double y, double centerX, double centerY)
    {
        // Check for center first (within 5 cells of center)
        var dx = Math.Abs(x - centerX);
        var dy = Math.Abs(y - centerY);
        if (dx <= 5 && dy <= 5)
        {
            return Quadrant.Center;
        }

        // Determine quadrant based on position relative to center
        bool isRight = x > centerX;
        bool isBottom = y > centerY;

        if (!isRight && !isBottom)
            return Quadrant.TopLeft;
        if (isRight && !isBottom)
            return Quadrant.TopRight;
        if (!isRight && isBottom)
            return Quadrant.BottomLeft;
        return Quadrant.BottomRight;
    }
}

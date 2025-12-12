using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires a room to be placed within a rectangular region.
/// The entire room must fit within the region bounds.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MustBeInRegionConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Minimum X coordinate (inclusive).
    /// </summary>
    public int MinX { get; }

    /// <summary>
    /// Maximum X coordinate (inclusive).
    /// </summary>
    public int MaxX { get; }

    /// <summary>
    /// Minimum Y coordinate (inclusive).
    /// </summary>
    public int MinY { get; }

    /// <summary>
    /// Maximum Y coordinate (inclusive).
    /// </summary>
    public int MaxY { get; }

    /// <summary>
    /// Creates a new region constraint.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="minX">Minimum X coordinate (inclusive).</param>
    /// <param name="maxX">Maximum X coordinate (inclusive).</param>
    /// <param name="minY">Minimum Y coordinate (inclusive).</param>
    /// <param name="maxY">Maximum Y coordinate (inclusive).</param>
    public MustBeInRegionConstraint(TRoomType targetRoomType, int minX, int maxX, int minY, int maxY)
    {
        TargetRoomType = targetRoomType;
        MinX = minX;
        MaxX = maxX;
        MinY = minY;
        MaxY = maxY;
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
        // Check that all cells of the room fit within the region
        foreach (var cell in roomTemplate.Cells)
        {
            var worldCell = new Cell(proposedPosition.X + cell.X, proposedPosition.Y + cell.Y);
            if (worldCell.X < MinX || worldCell.X > MaxX || worldCell.Y < MinY || worldCell.Y > MaxY)
            {
                return false;
            }
        }

        return true;
    }
}

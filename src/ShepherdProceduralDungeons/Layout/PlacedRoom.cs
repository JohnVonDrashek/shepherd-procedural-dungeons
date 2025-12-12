using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a room that has been placed in the dungeon layout.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class PlacedRoom<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The node ID from the floor graph.
    /// </summary>
    public required int NodeId { get; init; }

    /// <summary>
    /// The room type assigned to this room.
    /// </summary>
    public required TRoomType RoomType { get; init; }

    /// <summary>
    /// The template used for this room's shape.
    /// </summary>
    public required RoomTemplate<TRoomType> Template { get; init; }

    /// <summary>
    /// Anchor position of this room in world coordinates.
    /// This is the position where cell (0,0) of the template is placed.
    /// </summary>
    public required Cell Position { get; init; }

    /// <summary>
    /// Difficulty level of this room, calculated based on distance from spawn.
    /// </summary>
    public required double Difficulty { get; init; }

    // Cached world cells (lazy-initialized) - optimization: OPT-005
    private IReadOnlyList<Cell>? _cachedWorldCells;

    /// <summary>
    /// Gets all cells this room occupies in world coordinates.
    /// </summary>
    public IEnumerable<Cell> GetWorldCells()
    {
        if (_cachedWorldCells == null)
        {
            _cachedWorldCells = Template.Cells
                .Select(c => new Cell(Position.X + c.X, Position.Y + c.Y))
                .ToList();
        }
        
        return _cachedWorldCells;
    }

    /// <summary>
    /// Gets all exterior edges in world coordinates.
    /// Returns tuples of (local cell, world cell, edge direction).
    /// </summary>
    public IEnumerable<(Cell LocalCell, Cell WorldCell, Edge Edge)> GetExteriorEdgesWorld()
    {
        foreach (var (localCell, edge) in Template.GetExteriorEdges())
        {
            Cell worldCell = new Cell(Position.X + localCell.X, Position.Y + localCell.Y);
            yield return (localCell, worldCell, edge);
        }
    }

    /// <summary>
    /// Gets all interior features in world coordinates.
    /// Returns tuples of (world cell, feature type).
    /// </summary>
    public IEnumerable<(Cell WorldCell, InteriorFeature Feature)> GetInteriorFeatures()
    {
        foreach (var (localCell, feature) in Template.InteriorFeatures)
        {
            Cell worldCell = new Cell(Position.X + localCell.X, Position.Y + localCell.Y);
            yield return (worldCell, feature);
        }
    }
}


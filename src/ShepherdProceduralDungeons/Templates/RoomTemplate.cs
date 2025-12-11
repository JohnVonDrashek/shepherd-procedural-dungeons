namespace ShepherdProceduralDungeons.Templates;

/// <summary>
/// Defines a room's shape and valid door positions.
/// Room templates are generic over the room type enum defined by the consuming application.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class RoomTemplate<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Unique identifier for this template.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Room types this template can be used for.
    /// </summary>
    public required IReadOnlySet<TRoomType> ValidRoomTypes { get; init; }

    /// <summary>
    /// Cells this room occupies, relative to anchor (0,0).
    /// </summary>
    public required IReadOnlySet<Cell> Cells { get; init; }

    /// <summary>
    /// Cell edges where doors can be placed. Key is cell, value is valid edges.
    /// </summary>
    public required IReadOnlyDictionary<Cell, Edge> DoorEdges { get; init; }

    /// <summary>
    /// Weight for template selection. Higher weights increase selection probability.
    /// Default is 1.0 (uniform distribution when all templates have default weight).
    /// </summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>
    /// Gets the bounding box width of this template.
    /// </summary>
    public int Width => Cells.Count > 0 ? Cells.Max(c => c.X) - Cells.Min(c => c.X) + 1 : 0;

    /// <summary>
    /// Gets the bounding box height of this template.
    /// </summary>
    public int Height => Cells.Count > 0 ? Cells.Max(c => c.Y) - Cells.Min(c => c.Y) + 1 : 0;

    /// <summary>
    /// Gets all exterior edges of the room (edges not shared with another cell in the template).
    /// </summary>
    /// <returns>A collection of tuples containing the cell and its exterior edge.</returns>
    public IEnumerable<(Cell Cell, Edge Edge)> GetExteriorEdges()
    {
        foreach (var cell in Cells)
        {
            // Check each cardinal direction
            if (!Cells.Contains(cell.North))
                yield return (cell, Edge.North);
            if (!Cells.Contains(cell.South))
                yield return (cell, Edge.South);
            if (!Cells.Contains(cell.East))
                yield return (cell, Edge.East);
            if (!Cells.Contains(cell.West))
                yield return (cell, Edge.West);
        }
    }

    /// <summary>
    /// Checks if a door can be placed at the given cell edge.
    /// </summary>
    /// <param name="cell">The cell to check.</param>
    /// <param name="edge">The edge to check.</param>
    /// <returns>True if a door can be placed at this location.</returns>
    public bool CanPlaceDoor(Cell cell, Edge edge)
    {
        if (!Cells.Contains(cell))
            return false;

        if (!DoorEdges.TryGetValue(cell, out var allowedEdges))
            return false;

        return allowedEdges.HasFlag(edge);
    }
}

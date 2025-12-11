using ShepherdProceduralDungeons.Exceptions;

namespace ShepherdProceduralDungeons.Templates;

/// <summary>
/// Fluent builder for constructing room templates.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class RoomTemplateBuilder<TRoomType> where TRoomType : Enum
{
    private string? _id;
    private HashSet<TRoomType> _validTypes = new();
    private HashSet<Cell> _cells = new();
    private Dictionary<Cell, Edge> _doorEdges = new();
    private double _weight = 1.0;
    private Dictionary<Cell, InteriorFeature> _interiorFeatures = new();

    /// <summary>
    /// Sets the template ID.
    /// </summary>
    public RoomTemplateBuilder<TRoomType> WithId(string id)
    {
        _id = id;
        return this;
    }

    /// <summary>
    /// Adds room types this template can be used for.
    /// </summary>
    public RoomTemplateBuilder<TRoomType> ForRoomTypes(params TRoomType[] types)
    {
        foreach (var type in types)
        {
            _validTypes.Add(type);
        }
        return this;
    }

    /// <summary>
    /// Adds a single cell to the template.
    /// </summary>
    public RoomTemplateBuilder<TRoomType> AddCell(int x, int y)
    {
        _cells.Add(new Cell(x, y));
        return this;
    }

    /// <summary>
    /// Adds a rectangular region of cells.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="height">Height of the rectangle.</param>
    public RoomTemplateBuilder<TRoomType> AddRectangle(int x, int y, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                _cells.Add(new Cell(x + dx, y + dy));
            }
        }
        return this;
    }

    /// <summary>
    /// Creates a simple rectangle template (most common case).
    /// </summary>
    public static RoomTemplateBuilder<TRoomType> Rectangle(int width, int height)
    {
        var builder = new RoomTemplateBuilder<TRoomType>();
        builder.AddRectangle(0, 0, width, height);
        return builder;
    }

    /// <summary>
    /// Creates an L-shaped template.
    /// </summary>
    /// <param name="width">Total width of the L-shape.</param>
    /// <param name="height">Total height of the L-shape.</param>
    /// <param name="cutoutWidth">Width of the cut-out section.</param>
    /// <param name="cutoutHeight">Height of the cut-out section.</param>
    /// <param name="cutoutCorner">Which corner to cut out.</param>
    public static RoomTemplateBuilder<TRoomType> LShape(int width, int height, int cutoutWidth, int cutoutHeight, Corner cutoutCorner)
    {
        var builder = new RoomTemplateBuilder<TRoomType>();

        // Start with full rectangle
        builder.AddRectangle(0, 0, width, height);

        // Remove the cutout corner
        var cellsToRemove = new List<Cell>();

        switch (cutoutCorner)
        {
            case Corner.TopLeft:
                for (int dy = 0; dy < cutoutHeight; dy++)
                {
                    for (int dx = 0; dx < cutoutWidth; dx++)
                    {
                        cellsToRemove.Add(new Cell(dx, dy));
                    }
                }
                break;

            case Corner.TopRight:
                for (int dy = 0; dy < cutoutHeight; dy++)
                {
                    for (int dx = 0; dx < cutoutWidth; dx++)
                    {
                        cellsToRemove.Add(new Cell(width - cutoutWidth + dx, dy));
                    }
                }
                break;

            case Corner.BottomLeft:
                for (int dy = 0; dy < cutoutHeight; dy++)
                {
                    for (int dx = 0; dx < cutoutWidth; dx++)
                    {
                        cellsToRemove.Add(new Cell(dx, height - cutoutHeight + dy));
                    }
                }
                break;

            case Corner.BottomRight:
                for (int dy = 0; dy < cutoutHeight; dy++)
                {
                    for (int dx = 0; dx < cutoutWidth; dx++)
                    {
                        cellsToRemove.Add(new Cell(width - cutoutWidth + dx, height - cutoutHeight + dy));
                    }
                }
                break;
        }

        // Remove cutout cells
        foreach (var cell in cellsToRemove)
        {
            builder._cells.Remove(cell);
        }

        return builder;
    }

    /// <summary>
    /// Sets door edges for a specific cell. Only exterior edges are valid.
    /// </summary>
    public RoomTemplateBuilder<TRoomType> WithDoorEdges(int x, int y, Edge edges)
    {
        var cell = new Cell(x, y);
        if (_doorEdges.ContainsKey(cell))
        {
            _doorEdges[cell] = edges;
        }
        else
        {
            _doorEdges.Add(cell, edges);
        }
        return this;
    }

    /// <summary>
    /// Allows doors on all exterior edges of all cells.
    /// </summary>
    public RoomTemplateBuilder<TRoomType> WithDoorsOnAllExteriorEdges()
    {
        foreach (var cell in _cells)
        {
            var exteriorEdges = Edge.None;

            if (!_cells.Contains(cell.North))
                exteriorEdges |= Edge.North;
            if (!_cells.Contains(cell.South))
                exteriorEdges |= Edge.South;
            if (!_cells.Contains(cell.East))
                exteriorEdges |= Edge.East;
            if (!_cells.Contains(cell.West))
                exteriorEdges |= Edge.West;

            if (exteriorEdges != Edge.None)
            {
                _doorEdges[cell] = exteriorEdges;
            }
        }
        return this;
    }

    /// <summary>
    /// Sets the selection weight for this template.
    /// </summary>
    /// <param name="weight">Weight value (must be > 0). Default is 1.0.</param>
    /// <returns>This builder for method chaining.</returns>
    public RoomTemplateBuilder<TRoomType> WithWeight(double weight)
    {
        _weight = weight;
        return this;
    }

    /// <summary>
    /// Adds an interior feature at the specified cell position.
    /// </summary>
    /// <param name="x">X coordinate of the cell (template-local).</param>
    /// <param name="y">Y coordinate of the cell (template-local).</param>
    /// <param name="feature">The interior feature to place.</param>
    /// <returns>This builder for method chaining.</returns>
    public RoomTemplateBuilder<TRoomType> AddInteriorFeature(int x, int y, InteriorFeature feature)
    {
        var cell = new Cell(x, y);
        _interiorFeatures[cell] = feature;
        return this;
    }

    /// <summary>
    /// Allows doors only on specific sides of the bounding box.
    /// </summary>
    /// <param name="sides">Which sides (North, South, East, West) to allow doors on.</param>
    public RoomTemplateBuilder<TRoomType> WithDoorsOnSides(Edge sides)
    {
        if (_cells.Count == 0)
            return this;

        int minX = _cells.Min(c => c.X);
        int maxX = _cells.Max(c => c.X);
        int minY = _cells.Min(c => c.Y);
        int maxY = _cells.Max(c => c.Y);

        foreach (var cell in _cells)
        {
            var allowedEdges = Edge.None;

            // Check if cell is on a bounding box edge and that side is allowed
            if (cell.Y == minY && sides.HasFlag(Edge.North) && !_cells.Contains(cell.North))
                allowedEdges |= Edge.North;

            if (cell.Y == maxY && sides.HasFlag(Edge.South) && !_cells.Contains(cell.South))
                allowedEdges |= Edge.South;

            if (cell.X == maxX && sides.HasFlag(Edge.East) && !_cells.Contains(cell.East))
                allowedEdges |= Edge.East;

            if (cell.X == minX && sides.HasFlag(Edge.West) && !_cells.Contains(cell.West))
                allowedEdges |= Edge.West;

            if (allowedEdges != Edge.None)
            {
                _doorEdges[cell] = allowedEdges;
            }
        }

        return this;
    }

    /// <summary>
    /// Builds the template. Throws if invalid.
    /// </summary>
    public RoomTemplate<TRoomType> Build()
    {
        // Validate
        if (string.IsNullOrWhiteSpace(_id))
            throw new InvalidConfigurationException("Template must have an ID");

        if (_validTypes.Count == 0)
            throw new InvalidConfigurationException($"Template '{_id}' must specify at least one valid room type");

        if (_cells.Count == 0)
            throw new InvalidConfigurationException($"Template '{_id}' must have at least one cell");

        if (_doorEdges.Count == 0)
            throw new InvalidConfigurationException($"Template '{_id}' must have at least one door edge");

        // Validate weight
        if (_weight <= 0)
            throw new InvalidConfigurationException($"Template '{_id}' weight must be greater than 0, but was {_weight}");

        // Validate door edges are on exterior edges
        foreach (var (cell, edges) in _doorEdges)
        {
            if (!_cells.Contains(cell))
                throw new InvalidConfigurationException($"Template '{_id}' has door edge for cell {cell} which is not part of the template");

            // Check each flagged edge is actually exterior
            if (edges.HasFlag(Edge.North) && _cells.Contains(cell.North))
                throw new InvalidConfigurationException($"Template '{_id}' has door on North edge of cell {cell}, but that edge is interior");

            if (edges.HasFlag(Edge.South) && _cells.Contains(cell.South))
                throw new InvalidConfigurationException($"Template '{_id}' has door on South edge of cell {cell}, but that edge is interior");

            if (edges.HasFlag(Edge.East) && _cells.Contains(cell.East))
                throw new InvalidConfigurationException($"Template '{_id}' has door on East edge of cell {cell}, but that edge is interior");

            if (edges.HasFlag(Edge.West) && _cells.Contains(cell.West))
                throw new InvalidConfigurationException($"Template '{_id}' has door on West edge of cell {cell}, but that edge is interior");
        }

        // Validate interior features
        if (_cells.Count > 0)
        {
            int minX = _cells.Min(c => c.X);
            int maxX = _cells.Max(c => c.X);
            int minY = _cells.Min(c => c.Y);
            int maxY = _cells.Max(c => c.Y);

            foreach (var (cell, feature) in _interiorFeatures)
            {
                // Check if feature is within template bounds
                if (!_cells.Contains(cell))
                    throw new InvalidConfigurationException($"Template '{_id}' has interior feature at cell {cell} which is not part of the template");

                // Check if feature is on an exterior edge (not allowed)
                if (cell.X == minX || cell.X == maxX || cell.Y == minY || cell.Y == maxY)
                    throw new InvalidConfigurationException($"Template '{_id}' has interior feature at cell {cell} which is on an exterior edge. Interior features must be placed in interior cells only.");
            }
        }

        return new RoomTemplate<TRoomType>
        {
            Id = _id,
            ValidRoomTypes = _validTypes,
            Cells = _cells,
            DoorEdges = _doorEdges,
            Weight = _weight,
            InteriorFeatures = _interiorFeatures
        };
    }
}

namespace ShepherdProceduralDungeons.Templates;

/// <summary>
/// Represents a single grid position in 2D space.
/// Uses top-left origin with Y+ going down (screen coordinates).
/// </summary>
public readonly record struct Cell(int X, int Y)
{
    /// <summary>
    /// Creates a new cell offset by the specified delta.
    /// </summary>
    public Cell Offset(int dx, int dy) => new(X + dx, Y + dy);

    /// <summary>Gets the cell directly north (Y-1) of this cell.</summary>
    public Cell North => new(X, Y - 1);

    /// <summary>Gets the cell directly south (Y+1) of this cell.</summary>
    public Cell South => new(X, Y + 1);

    /// <summary>Gets the cell directly east (X+1) of this cell.</summary>
    public Cell East => new(X + 1, Y);

    /// <summary>Gets the cell directly west (X-1) of this cell.</summary>
    public Cell West => new(X - 1, Y);
}

using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a single straight segment of a hallway.
/// </summary>
public sealed class HallwaySegment
{
    /// <summary>
    /// Starting cell of this segment.
    /// </summary>
    public required Cell Start { get; init; }

    /// <summary>
    /// Ending cell of this segment.
    /// </summary>
    public required Cell End { get; init; }

    /// <summary>
    /// Whether this segment is horizontal (same Y coordinate).
    /// </summary>
    public bool IsHorizontal => Start.Y == End.Y;

    /// <summary>
    /// Whether this segment is vertical (same X coordinate).
    /// </summary>
    public bool IsVertical => Start.X == End.X;

    /// <summary>
    /// Gets all cells occupied by this segment, including start and end.
    /// </summary>
    public IEnumerable<Cell> GetCells()
    {
        int dx = Math.Sign(End.X - Start.X);
        int dy = Math.Sign(End.Y - Start.Y);

        Cell current = Start;
        while (current != End)
        {
            yield return current;
            current = new Cell(current.X + dx, current.Y + dy);
        }
        yield return End;
    }
}


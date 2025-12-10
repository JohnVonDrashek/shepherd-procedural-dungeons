namespace ShepherdProceduralDungeons.Templates;

/// <summary>
/// Cardinal directions for door placement on cell edges.
/// Can be combined using flags for multiple edges.
/// </summary>
[Flags]
public enum Edge
{
    /// <summary>No edge.</summary>
    None = 0,

    /// <summary>North edge (top).</summary>
    North = 1,

    /// <summary>South edge (bottom).</summary>
    South = 2,

    /// <summary>East edge (right).</summary>
    East = 4,

    /// <summary>West edge (left).</summary>
    West = 8,

    /// <summary>All four edges.</summary>
    All = North | South | East | West
}

/// <summary>
/// Extension methods for the Edge enum.
/// </summary>
public static class EdgeExtensions
{
    /// <summary>
    /// Returns the opposite edge.
    /// North->South, East->West, etc.
    /// </summary>
    public static Edge Opposite(this Edge edge) => edge switch
    {
        Edge.North => Edge.South,
        Edge.South => Edge.North,
        Edge.East => Edge.West,
        Edge.West => Edge.East,
        _ => Edge.None
    };
}

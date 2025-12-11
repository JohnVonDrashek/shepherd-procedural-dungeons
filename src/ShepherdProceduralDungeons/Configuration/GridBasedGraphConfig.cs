namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for grid-based graph generation.
/// </summary>
public sealed class GridBasedGraphConfig
{
    /// <summary>
    /// Width of the grid.
    /// </summary>
    public required int GridWidth { get; init; }

    /// <summary>
    /// Height of the grid.
    /// </summary>
    public required int GridHeight { get; init; }

    /// <summary>
    /// Connectivity pattern for grid connections.
    /// </summary>
    public ConnectivityPattern ConnectivityPattern { get; init; } = ConnectivityPattern.FourWay;
}

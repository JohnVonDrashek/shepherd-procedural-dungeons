namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Connectivity pattern for grid-based graph generation.
/// </summary>
public enum ConnectivityPattern
{
    /// <summary>
    /// Four-way connectivity (north, south, east, west).
    /// </summary>
    FourWay,

    /// <summary>
    /// Eight-way connectivity (includes diagonals).
    /// </summary>
    EightWay
}

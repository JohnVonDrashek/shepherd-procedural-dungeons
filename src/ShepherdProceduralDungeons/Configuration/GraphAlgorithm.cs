namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Algorithm used for generating the dungeon floor graph topology.
/// </summary>
public enum GraphAlgorithm
{
    /// <summary>
    /// Default spanning tree algorithm with optional extra connections (backward compatible).
    /// </summary>
    SpanningTree,

    /// <summary>
    /// Grid-based algorithm that arranges rooms in a 2D grid pattern.
    /// </summary>
    GridBased,

    /// <summary>
    /// Cellular automata algorithm that produces organic, cave-like structures.
    /// </summary>
    CellularAutomata,

    /// <summary>
    /// Maze-based algorithm that creates complex, winding path structures.
    /// </summary>
    MazeBased,

    /// <summary>
    /// Hub-and-spoke algorithm that creates central hub rooms with branching spokes.
    /// </summary>
    HubAndSpoke
}

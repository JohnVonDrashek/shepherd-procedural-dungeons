namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for maze-based graph generation.
/// </summary>
public sealed class MazeBasedGraphConfig
{
    /// <summary>
    /// Type of maze to generate.
    /// </summary>
    public MazeType MazeType { get; init; } = MazeType.Perfect;

    /// <summary>
    /// Algorithm to use for maze generation.
    /// </summary>
    public MazeAlgorithm Algorithm { get; init; } = MazeAlgorithm.Prims;
}

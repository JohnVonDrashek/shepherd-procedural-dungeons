namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Type of maze to generate.
/// </summary>
public enum MazeType
{
    /// <summary>
    /// Perfect maze (no loops, tree structure).
    /// </summary>
    Perfect,

    /// <summary>
    /// Imperfect maze (may contain loops).
    /// </summary>
    Imperfect
}

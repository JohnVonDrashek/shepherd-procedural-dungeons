namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for cellular automata graph generation.
/// </summary>
public sealed class CellularAutomataGraphConfig
{
    /// <summary>
    /// Birth threshold for cellular automata rules.
    /// </summary>
    public int BirthThreshold { get; init; } = 4;

    /// <summary>
    /// Survival threshold for cellular automata rules.
    /// </summary>
    public int SurvivalThreshold { get; init; } = 3;

    /// <summary>
    /// Number of iterations to run the cellular automata.
    /// </summary>
    public int Iterations { get; init; } = 5;
}

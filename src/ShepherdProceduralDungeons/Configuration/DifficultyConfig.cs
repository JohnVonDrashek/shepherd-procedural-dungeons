namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for room difficulty scaling based on distance from spawn.
/// </summary>
public sealed class DifficultyConfig
{
    /// <summary>
    /// Base difficulty for the spawn room (distance 0).
    /// </summary>
    public double BaseDifficulty { get; init; } = 1.0;

    /// <summary>
    /// Scaling factor used by the scaling function.
    /// </summary>
    public double ScalingFactor { get; init; } = 1.0;

    /// <summary>
    /// The scaling function to use.
    /// </summary>
    public DifficultyScalingFunction Function { get; init; } = DifficultyScalingFunction.Linear;

    /// <summary>
    /// Custom function for difficulty calculation. Only used when Function is Custom.
    /// Takes distance as input and returns difficulty.
    /// </summary>
    public Func<int, double>? CustomFunction { get; init; }

    /// <summary>
    /// Maximum difficulty cap. All calculated difficulties will be clamped to this value.
    /// </summary>
    public double MaxDifficulty { get; init; } = 10.0;
}

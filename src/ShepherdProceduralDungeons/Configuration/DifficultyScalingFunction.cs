namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Enumeration of difficulty scaling functions.
/// </summary>
public enum DifficultyScalingFunction
{
    /// <summary>
    /// Linear scaling: difficulty = baseDifficulty + (distance * scalingFactor)
    /// </summary>
    Linear,

    /// <summary>
    /// Exponential scaling: difficulty = baseDifficulty + (scalingFactor ^ distance)
    /// </summary>
    Exponential,

    /// <summary>
    /// Custom function provided by the user.
    /// </summary>
    Custom
}

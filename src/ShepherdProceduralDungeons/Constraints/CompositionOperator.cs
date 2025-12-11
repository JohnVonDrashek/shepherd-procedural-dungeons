namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Operator for composing multiple constraints together.
/// </summary>
public enum CompositionOperator
{
    /// <summary>
    /// All constraints must pass (logical AND).
    /// </summary>
    And,

    /// <summary>
    /// At least one constraint must pass (logical OR).
    /// </summary>
    Or,

    /// <summary>
    /// The wrapped constraint must fail (logical NOT).
    /// </summary>
    Not
}

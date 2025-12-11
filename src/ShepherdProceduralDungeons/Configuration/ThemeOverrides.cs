namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Allows overriding specific aspects of a theme when converting to FloorConfig.
/// </summary>
public sealed class ThemeOverrides
{
    /// <summary>
    /// Override the seed value.
    /// </summary>
    public int? Seed { get; init; }

    /// <summary>
    /// Override the room count.
    /// </summary>
    public int? RoomCount { get; init; }

    /// <summary>
    /// Override the branching factor.
    /// </summary>
    public float? BranchingFactor { get; init; }

    /// <summary>
    /// Override the hallway mode.
    /// </summary>
    public HallwayMode? HallwayMode { get; init; }

    /// <summary>
    /// Override the graph algorithm.
    /// </summary>
    public GraphAlgorithm? GraphAlgorithm { get; init; }
}

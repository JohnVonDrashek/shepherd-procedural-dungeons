namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for hub-and-spoke graph generation.
/// </summary>
public sealed class HubAndSpokeGraphConfig
{
    /// <summary>
    /// Number of hub rooms to create.
    /// </summary>
    public required int HubCount { get; init; }

    /// <summary>
    /// Maximum length of spokes from hubs.
    /// </summary>
    public required int MaxSpokeLength { get; init; }
}

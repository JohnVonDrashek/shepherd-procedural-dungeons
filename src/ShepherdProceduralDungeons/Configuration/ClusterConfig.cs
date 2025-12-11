namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for room clustering detection.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class ClusterConfig<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Whether clustering is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Maximum spatial distance (in cells) between rooms in the same cluster.
    /// </summary>
    public double Epsilon { get; init; } = 20.0;

    /// <summary>
    /// Minimum number of rooms required to form a cluster.
    /// </summary>
    public int MinClusterSize { get; init; } = 2;

    /// <summary>
    /// Maximum number of rooms allowed in a cluster. If null, no maximum limit.
    /// </summary>
    public int? MaxClusterSize { get; init; }

    /// <summary>
    /// Optional filter for which room types to cluster. If null, all room types are clustered.
    /// </summary>
    public IReadOnlySet<TRoomType>? RoomTypesToCluster { get; init; }
}

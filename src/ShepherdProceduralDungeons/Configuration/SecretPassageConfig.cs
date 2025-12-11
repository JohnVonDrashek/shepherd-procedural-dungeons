namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for generating secret passages in a dungeon floor.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class SecretPassageConfig<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Number of secret passages to generate. Default: 0 (disabled).
    /// </summary>
    public int Count { get; init; } = 0;
    
    /// <summary>
    /// Maximum spatial distance (in cells) between rooms for secret passage eligibility.
    /// Rooms further apart won't be connected. Default: 5.
    /// </summary>
    public int MaxSpatialDistance { get; init; } = 5;
    
    /// <summary>
    /// Room types that can have secret passages. If empty, all room types are eligible.
    /// </summary>
    public IReadOnlySet<TRoomType> AllowedRoomTypes { get; init; } = new HashSet<TRoomType>();
    
    /// <summary>
    /// Room types that cannot have secret passages.
    /// </summary>
    public IReadOnlySet<TRoomType> ForbiddenRoomTypes { get; init; } = new HashSet<TRoomType>();
    
    /// <summary>
    /// Whether secret passages can connect rooms on the critical path.
    /// Default: true.
    /// </summary>
    public bool AllowCriticalPathConnections { get; init; } = true;
    
    /// <summary>
    /// Whether secret passages can connect rooms that are already graph-connected.
    /// Default: false (secret passages should provide alternative routes).
    /// </summary>
    public bool AllowGraphConnectedRooms { get; init; } = false;
}

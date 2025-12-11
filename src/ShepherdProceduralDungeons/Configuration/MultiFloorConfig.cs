namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for generating a multi-floor dungeon.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MultiFloorConfig<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Seed for deterministic generation.
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Configuration for each floor.
    /// </summary>
    public required IReadOnlyList<FloorConfig<TRoomType>> Floors { get; init; }

    /// <summary>
    /// Connections between floors.
    /// </summary>
    public required IReadOnlyList<FloorConnection> Connections { get; init; }
}

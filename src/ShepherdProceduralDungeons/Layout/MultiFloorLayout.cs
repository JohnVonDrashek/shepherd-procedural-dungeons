namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// The final output of multi-floor dungeon generation, containing all floors and their connections.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MultiFloorLayout<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// All generated floors.
    /// </summary>
    public required IReadOnlyList<FloorLayout<TRoomType>> Floors { get; init; }

    /// <summary>
    /// Connections between floors.
    /// </summary>
    public required IReadOnlyList<Configuration.FloorConnection> Connections { get; init; }

    /// <summary>
    /// The seed used to generate this dungeon.
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Total number of floors.
    /// </summary>
    public required int TotalFloorCount { get; init; }
}

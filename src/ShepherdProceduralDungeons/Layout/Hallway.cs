namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a hallway connecting two rooms.
/// </summary>
public sealed class Hallway
{
    /// <summary>
    /// Unique identifier for this hallway.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// All segments that make up this hallway.
    /// </summary>
    public required IReadOnlyList<HallwaySegment> Segments { get; init; }

    /// <summary>
    /// Door connecting this hallway to the first room.
    /// </summary>
    public required Door DoorA { get; init; }

    /// <summary>
    /// Door connecting this hallway to the second room.
    /// </summary>
    public required Door DoorB { get; init; }
}


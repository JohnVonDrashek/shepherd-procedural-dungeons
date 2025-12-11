namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Represents a connection between two floors.
/// </summary>
public sealed class FloorConnection
{
    /// <summary>
    /// Index of the source floor (0-based).
    /// </summary>
    public required int FromFloorIndex { get; init; }

    /// <summary>
    /// Node ID of the room on the source floor.
    /// </summary>
    public required int FromRoomNodeId { get; init; }

    /// <summary>
    /// Index of the destination floor (0-based).
    /// </summary>
    public required int ToFloorIndex { get; init; }

    /// <summary>
    /// Node ID of the room on the destination floor.
    /// </summary>
    public required int ToRoomNodeId { get; init; }

    /// <summary>
    /// Type of connection (stairs, teleporter, etc.).
    /// </summary>
    public required ConnectionType Type { get; init; }
}

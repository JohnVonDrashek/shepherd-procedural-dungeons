using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a door connecting rooms or hallways.
/// </summary>
public sealed class Door
{
    /// <summary>
    /// The cell position where the door is located.
    /// </summary>
    public required Cell Position { get; init; }

    /// <summary>
    /// The edge (North/South/East/West) where the door is located on the cell.
    /// </summary>
    public required Edge Edge { get; init; }

    /// <summary>
    /// ID of the room this door connects to, if applicable.
    /// </summary>
    public int? ConnectsToRoomId { get; init; }

    /// <summary>
    /// ID of the hallway this door connects to, if applicable.
    /// </summary>
    public int? ConnectsToHallwayId { get; init; }
}


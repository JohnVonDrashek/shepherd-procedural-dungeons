namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a secret passage connecting two rooms.
/// Secret passages are hidden connections not part of the main graph topology.
/// </summary>
public sealed class SecretPassage
{
    /// <summary>
    /// ID of the first room connected by this secret passage.
    /// </summary>
    public required int RoomAId { get; init; }
    
    /// <summary>
    /// ID of the second room connected by this secret passage.
    /// </summary>
    public required int RoomBId { get; init; }
    
    /// <summary>
    /// Door placement for room A side of the secret passage.
    /// </summary>
    public required Door DoorA { get; init; }
    
    /// <summary>
    /// Door placement for room B side of the secret passage.
    /// </summary>
    public required Door DoorB { get; init; }
    
    /// <summary>
    /// Optional hallway if rooms are not adjacent (similar to regular connections).
    /// </summary>
    public Hallway? Hallway { get; init; }
    
    /// <summary>
    /// Whether this secret passage requires a hallway.
    /// </summary>
    public bool RequiresHallway => Hallway != null;
}

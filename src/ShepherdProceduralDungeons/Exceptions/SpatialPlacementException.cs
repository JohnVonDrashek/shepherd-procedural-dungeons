namespace ShepherdProceduralDungeons.Exceptions;

/// <summary>
/// Exception thrown when rooms cannot be placed in 2D space during spatial solving.
/// </summary>
public class SpatialPlacementException : GenerationException
{
    /// <summary>
    /// Gets the ID of the room that could not be placed, if available.
    /// </summary>
    public int? RoomId { get; init; }

    /// <summary>
    /// Initializes a new instance of the SpatialPlacementException class with a specified error message.
    /// </summary>
    public SpatialPlacementException(string message) : base(message) { }
}

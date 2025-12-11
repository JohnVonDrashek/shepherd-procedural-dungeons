namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Type of connection between floors.
/// </summary>
public enum ConnectionType
{
    /// <summary>
    /// Stairs going up to a higher floor.
    /// </summary>
    StairsUp,

    /// <summary>
    /// Stairs going down to a lower floor.
    /// </summary>
    StairsDown,

    /// <summary>
    /// Teleporter pad/portal connecting floors.
    /// </summary>
    Teleporter
}

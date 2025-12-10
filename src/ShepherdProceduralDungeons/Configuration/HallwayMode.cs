namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Specifies how hallways should be generated between rooms.
/// </summary>
public enum HallwayMode
{
    /// <summary>
    /// Rooms must share a wall directly. Throws an exception if rooms cannot be placed adjacent.
    /// </summary>
    None,

    /// <summary>
    /// Generate hallways only when rooms cannot touch directly.
    /// This is the recommended default mode.
    /// </summary>
    AsNeeded,

    /// <summary>
    /// Always generate hallways between all connected rooms, even if they share a wall.
    /// </summary>
    Always
}

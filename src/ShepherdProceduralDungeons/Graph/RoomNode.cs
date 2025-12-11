namespace ShepherdProceduralDungeons.Graph;

/// <summary>
/// Represents a single room node in the floor graph.
/// </summary>
public sealed class RoomNode
{
    /// <summary>
    /// Unique identifier for this node.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Distance (in edges) from the start node. Set by BFS traversal.
    /// </summary>
    public int DistanceFromStart { get; internal set; }

    /// <summary>
    /// Whether this node is on the critical path from spawn to boss.
    /// </summary>
    public bool IsOnCriticalPath { get; internal set; }

    /// <summary>
    /// Number of connections this node has.
    /// </summary>
    public int ConnectionCount => Connections.Count;

    /// <summary>
    /// Difficulty level of this room, calculated based on distance from spawn.
    /// </summary>
    public double Difficulty { get; internal set; }

    /// <summary>
    /// All connections this node participates in.
    /// </summary>
    internal List<RoomConnection> Connections { get; } = new();
}

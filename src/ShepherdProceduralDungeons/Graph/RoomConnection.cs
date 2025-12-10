namespace ShepherdProceduralDungeons.Graph;

/// <summary>
/// Represents a connection (edge) between two room nodes.
/// </summary>
public sealed class RoomConnection
{
    /// <summary>
    /// ID of the first connected node.
    /// </summary>
    public required int NodeAId { get; init; }

    /// <summary>
    /// ID of the second connected node.
    /// </summary>
    public required int NodeBId { get; init; }

    /// <summary>
    /// Whether this connection requires a hallway because rooms couldn't be placed adjacent.
    /// Set by the spatial solver.
    /// </summary>
    public bool RequiresHallway { get; internal set; }

    /// <summary>
    /// Gets the ID of the other node in this connection.
    /// </summary>
    public int GetOtherNodeId(int nodeId)
    {
        if (nodeId == NodeAId) return NodeBId;
        if (nodeId == NodeBId) return NodeAId;
        throw new ArgumentException($"Node {nodeId} is not part of this connection", nameof(nodeId));
    }
}

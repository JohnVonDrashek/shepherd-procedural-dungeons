using System.Linq;

namespace ShepherdProceduralDungeons.Graph;

/// <summary>
/// Represents the topology of a dungeon floor as a graph of connected rooms.
/// </summary>
public sealed class FloorGraph
{
    private Dictionary<int, RoomNode>? _nodeLookup;

    /// <summary>
    /// All room nodes in the graph.
    /// </summary>
    public required IReadOnlyList<RoomNode> Nodes { get; init; }

    /// <summary>
    /// All connections between rooms.
    /// </summary>
    public required IReadOnlyList<RoomConnection> Connections { get; init; }

    /// <summary>
    /// ID of the start node (always 0).
    /// </summary>
    public required int StartNodeId { get; init; }

    /// <summary>
    /// ID of the boss room node. Set by RoomTypeAssigner.
    /// </summary>
    public int BossNodeId { get; internal set; }

    /// <summary>
    /// Sequence of node IDs forming the critical path from spawn to boss.
    /// Set by RoomTypeAssigner.
    /// </summary>
    public IReadOnlyList<int> CriticalPath { get; internal set; } = Array.Empty<int>();

    /// <summary>
    /// Gets a node by its ID using O(1) dictionary lookup.
    /// </summary>
    public RoomNode GetNode(int id)
    {
        // Lazy initialization: populate dictionary on first access
        if (_nodeLookup == null)
        {
            _nodeLookup = Nodes.ToDictionary(n => n.Id, n => n);
        }
        return _nodeLookup[id];
    }
}

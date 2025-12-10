using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates the topology of a dungeon floor as a connected graph.
/// </summary>
public sealed class GraphGenerator
{
    /// <summary>
    /// Generates a connected graph with the specified number of nodes.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <returns>A connected floor graph.</returns>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        // 1. Create all nodes
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // 2. Build spanning tree (guarantees connectivity)
        // Use randomized approach: for each node after first, connect to a random existing node
        var connectedNodes = new List<int> { 0 };
        for (int i = 1; i < roomCount; i++)
        {
            int parentIndex = rng.Next(connectedNodes.Count);
            int parentId = connectedNodes[parentIndex];

            connections.Add(new RoomConnection { NodeAId = parentId, NodeBId = i });
            connectedNodes.Add(i);
        }

        // 3. Add extra edges for loops based on branchingFactor
        // branchingFactor 0.0 = tree only, 1.0 = many extra connections
        int maxExtraEdges = (int)(roomCount * branchingFactor);
        int extraEdges = rng.Next(0, maxExtraEdges + 1);

        for (int i = 0; i < extraEdges; i++)
        {
            int a = rng.Next(roomCount);
            int b = rng.Next(roomCount);
            if (a != b && !ConnectionExists(connections, a, b))
            {
                connections.Add(new RoomConnection { NodeAId = a, NodeBId = b });
            }
        }

        // 4. Wire up node connections
        foreach (var conn in connections)
        {
            nodes[conn.NodeAId].Connections.Add(conn);
            nodes[conn.NodeBId].Connections.Add(conn);
        }

        // 5. Calculate distances from start via BFS
        CalculateDistances(nodes, startId: 0);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private bool ConnectionExists(List<RoomConnection> connections, int a, int b)
    {
        return connections.Any(c =>
            (c.NodeAId == a && c.NodeBId == b) ||
            (c.NodeAId == b && c.NodeBId == a));
    }

    private void CalculateDistances(List<RoomNode> nodes, int startId)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<(int nodeId, int distance)>();
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            var (nodeId, distance) = queue.Dequeue();
            if (visited.Contains(nodeId)) continue;
            visited.Add(nodeId);

            nodes[nodeId].DistanceFromStart = distance;

            foreach (var conn in nodes[nodeId].Connections)
            {
                int neighborId = conn.GetOtherNodeId(nodeId);
                if (!visited.Contains(neighborId))
                {
                    queue.Enqueue((neighborId, distance + 1));
                }
            }
        }
    }
}

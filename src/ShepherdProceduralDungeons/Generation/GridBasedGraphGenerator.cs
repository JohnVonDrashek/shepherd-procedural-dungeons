using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates graph topologies using a grid-based layout.
/// </summary>
public sealed class GridBasedGraphGenerator : IGraphGenerator
{
    /// <summary>
    /// Generates a connected graph using grid-based layout.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <param name="config">Grid-based configuration.</param>
    /// <returns>A connected floor graph.</returns>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng, GridBasedGraphConfig config)
    {
        if (config.GridWidth * config.GridHeight < roomCount)
            throw new ArgumentException($"Grid size ({config.GridWidth}x{config.GridHeight}) must be at least {roomCount} cells");

        // Create nodes
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // Map node IDs to grid positions
        var nodeToGrid = new Dictionary<int, (int x, int y)>();
        var gridToNode = new Dictionary<(int x, int y), int>();

        int nodeIndex = 0;
        for (int y = 0; y < config.GridHeight && nodeIndex < roomCount; y++)
        {
            for (int x = 0; x < config.GridWidth && nodeIndex < roomCount; x++)
            {
                nodeToGrid[nodeIndex] = (x, y);
                gridToNode[(x, y)] = nodeIndex;
                nodeIndex++;
            }
        }

        // Build connections based on connectivity pattern
        var neighbors = new List<(int dx, int dy)>();
        if (config.ConnectivityPattern == ConnectivityPattern.FourWay)
        {
            neighbors.AddRange(new[] { (0, -1), (0, 1), (-1, 0), (1, 0) });
        }
        else // EightWay
        {
            neighbors.AddRange(new[] { (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (-1, 1), (1, -1), (1, 1) });
        }

        // Create all possible grid connections
        var possibleConnections = new List<(int a, int b)>();
        foreach (var (nodeId, (x, y)) in nodeToGrid)
        {
            foreach (var (dx, dy) in neighbors)
            {
                int nx = x + dx;
                int ny = y + dy;
                if (gridToNode.TryGetValue((nx, ny), out int neighborId))
                {
                    if (nodeId < neighborId) // Avoid duplicates
                    {
                        possibleConnections.Add((nodeId, neighborId));
                    }
                }
            }
        }

        // Build spanning tree first (guarantees connectivity)
        var connectedNodes = new HashSet<int> { 0 };
        var spanningTreeConnections = new List<(int a, int b)>();
        var availableConnections = possibleConnections.ToList();

        while (connectedNodes.Count < roomCount)
        {
            // Find connections that connect a connected node to an unconnected node
            var candidateConnections = availableConnections
                .Where(c => connectedNodes.Contains(c.a) != connectedNodes.Contains(c.b))
                .ToList();

            if (candidateConnections.Count == 0)
                break; // Should not happen if grid is large enough

            var selected = candidateConnections[rng.Next(candidateConnections.Count)];
            spanningTreeConnections.Add(selected);
            connectedNodes.Add(selected.a);
            connectedNodes.Add(selected.b);
        }

        // Add spanning tree connections
        foreach (var (a, b) in spanningTreeConnections)
        {
            connections.Add(new RoomConnection { NodeAId = a, NodeBId = b });
        }

        // Add extra connections based on branching factor
        var remainingConnections = possibleConnections
            .Except(spanningTreeConnections)
            .ToList();

        int maxExtraEdges = (int)(remainingConnections.Count * branchingFactor);
        int extraEdges = rng.Next(0, maxExtraEdges + 1);

        // Shuffle and take random connections
        var shuffled = remainingConnections.OrderBy(_ => rng.Next()).Take(extraEdges).ToList();
        foreach (var (a, b) in shuffled)
        {
            connections.Add(new RoomConnection { NodeAId = a, NodeBId = b });
        }

        // Wire up node connections
        foreach (var conn in connections)
        {
            nodes[conn.NodeAId].Connections.Add(conn);
            nodes[conn.NodeBId].Connections.Add(conn);
        }

        // Calculate distances from start
        CalculateDistances(nodes, startId: 0);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    /// <summary>
    /// Generates a connected graph using grid-based layout with default configuration.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        // Calculate grid dimensions (prefer square-ish)
        int gridSize = (int)Math.Ceiling(Math.Sqrt(roomCount));
        var config = new GridBasedGraphConfig
        {
            GridWidth = gridSize,
            GridHeight = gridSize,
            ConnectivityPattern = ConnectivityPattern.FourWay
        };
        return Generate(roomCount, branchingFactor, rng, config);
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

using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates graph topologies using maze generation algorithms.
/// </summary>
public sealed class MazeBasedGraphGenerator : IGraphGenerator
{
    /// <summary>
    /// Generates a connected graph using maze generation.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <param name="config">Maze-based configuration.</param>
    /// <returns>A connected floor graph.</returns>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng, MazeBasedGraphConfig config)
    {
        // Calculate grid dimensions
        int gridSize = (int)Math.Ceiling(Math.Sqrt(roomCount));

        // Create nodes arranged in a grid
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // Map node IDs to grid positions
        var nodeToGrid = new Dictionary<int, (int x, int y)>();
        var gridToNode = new Dictionary<(int x, int y), int>();

        int nodeIndex = 0;
        for (int y = 0; y < gridSize && nodeIndex < roomCount; y++)
        {
            for (int x = 0; x < gridSize && nodeIndex < roomCount; x++)
            {
                nodeToGrid[nodeIndex] = (x, y);
                gridToNode[(x, y)] = nodeIndex;
                nodeIndex++;
            }
        }

        // Get all possible grid connections (4-way)
        var possibleConnections = new List<(int a, int b)>();
        var neighbors = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

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

        // Generate maze using selected algorithm
        List<(int a, int b)> mazeConnections;
        if (config.Algorithm == MazeAlgorithm.Prims)
        {
            mazeConnections = GeneratePrimsMaze(roomCount, possibleConnections, rng);
        }
        else // Kruskal's
        {
            mazeConnections = GenerateKruskalsMaze(roomCount, possibleConnections, rng);
        }

        // For perfect maze, use only maze connections (tree structure)
        // For imperfect maze, add extra connections based on branching factor
        if (config.MazeType == MazeType.Perfect)
        {
            // Perfect maze = tree (no loops)
            connections.AddRange(mazeConnections.Select(c => new RoomConnection { NodeAId = c.a, NodeBId = c.b }));
        }
        else // Imperfect
        {
            // Start with maze connections
            connections.AddRange(mazeConnections.Select(c => new RoomConnection { NodeAId = c.a, NodeBId = c.b }));

            // Add extra connections for loops
            var remainingConnections = possibleConnections
                .Except(mazeConnections)
                .ToList();

            int maxExtraEdges = (int)(remainingConnections.Count * branchingFactor);
            int extraEdges = rng.Next(0, maxExtraEdges + 1);

            var shuffled = remainingConnections.OrderBy(_ => rng.Next()).Take(extraEdges).ToList();
            foreach (var (a, b) in shuffled)
            {
                connections.Add(new RoomConnection { NodeAId = a, NodeBId = b });
            }
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
    /// Generates a connected graph using maze generation with default configuration.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        var config = new MazeBasedGraphConfig
        {
            MazeType = MazeType.Perfect,
            Algorithm = MazeAlgorithm.Prims
        };
        return Generate(roomCount, branchingFactor, rng, config);
    }

    private List<(int a, int b)> GeneratePrimsMaze(int roomCount, List<(int a, int b)> possibleConnections, Random rng)
    {
        // Prim's algorithm: start with a random node, grow tree by adding random edges
        var mazeConnections = new List<(int a, int b)>();
        var inMaze = new HashSet<int> { 0 }; // Start with node 0

        while (inMaze.Count < roomCount)
        {
            // Find all edges connecting a node in the maze to a node outside
            var candidateEdges = possibleConnections
                .Where(e => inMaze.Contains(e.a) != inMaze.Contains(e.b))
                .ToList();

            if (candidateEdges.Count == 0)
                break; // Should not happen if graph is connected

            // Pick a random candidate edge
            var selected = candidateEdges[rng.Next(candidateEdges.Count)];
            mazeConnections.Add(selected);
            inMaze.Add(selected.a);
            inMaze.Add(selected.b);
        }

        return mazeConnections;
    }

    private List<(int a, int b)> GenerateKruskalsMaze(int roomCount, List<(int a, int b)> possibleConnections, Random rng)
    {
        // Kruskal's algorithm: union-find to build minimum spanning tree
        var mazeConnections = new List<(int a, int b)>();
        var parent = new Dictionary<int, int>();

        // Initialize union-find
        for (int i = 0; i < roomCount; i++)
        {
            parent[i] = i;
        }

        int Find(int x)
        {
            if (parent[x] != x)
                parent[x] = Find(parent[x]);
            return parent[x];
        }

        void Union(int x, int y)
        {
            int rootX = Find(x);
            int rootY = Find(y);
            if (rootX != rootY)
                parent[rootX] = rootY;
        }

        // Shuffle edges
        var shuffledEdges = possibleConnections.OrderBy(_ => rng.Next()).ToList();

        // Add edges that don't create cycles
        foreach (var edge in shuffledEdges)
        {
            if (Find(edge.a) != Find(edge.b))
            {
                mazeConnections.Add(edge);
                Union(edge.a, edge.b);
            }
        }

        return mazeConnections;
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

using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates graph topologies using cellular automata rules to create organic, cave-like structures.
/// </summary>
public sealed class CellularAutomataGraphGenerator : IGraphGenerator
{
    /// <summary>
    /// Generates a connected graph using cellular automata.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <param name="config">Cellular automata configuration.</param>
    /// <returns>A connected floor graph.</returns>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng, CellularAutomataGraphConfig config)
    {
        // Create a grid large enough to hold the rooms
        int gridSize = (int)Math.Ceiling(Math.Sqrt(roomCount * 2)); // Extra space for CA to work with

        // Initialize grid with random cells (seed points)
        bool[,] grid = new bool[gridSize, gridSize];
        int placedRooms = 0;

        // Place initial seed points randomly
        while (placedRooms < roomCount)
        {
            int x = rng.Next(gridSize);
            int y = rng.Next(gridSize);
            if (!grid[x, y])
            {
                grid[x, y] = true;
                placedRooms++;
            }
        }

        // Run cellular automata iterations
        for (int iteration = 0; iteration < config.Iterations; iteration++)
        {
            bool[,] newGrid = new bool[gridSize, gridSize];

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    int neighbors = CountNeighbors(grid, x, y, gridSize);

                    if (grid[x, y])
                    {
                        // Survival rule
                        newGrid[x, y] = neighbors >= config.SurvivalThreshold;
                    }
                    else
                    {
                        // Birth rule
                        newGrid[x, y] = neighbors >= config.BirthThreshold;
                    }
                }
            }

            grid = newGrid;
        }

        // Extract room positions from final grid
        var roomPositions = new List<(int x, int y)>();
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                if (grid[x, y])
                {
                    roomPositions.Add((x, y));
                }
            }
        }

        // Limit to requested room count (take first N)
        if (roomPositions.Count > roomCount)
        {
            roomPositions = roomPositions.Take(roomCount).ToList();
        }
        else if (roomPositions.Count < roomCount)
        {
            // Add more rooms if CA didn't produce enough
            while (roomPositions.Count < roomCount)
            {
                int x = rng.Next(gridSize);
                int y = rng.Next(gridSize);
                if (!roomPositions.Contains((x, y)))
                {
                    roomPositions.Add((x, y));
                }
            }
        }

        // Create nodes
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // Map positions to node IDs
        var positionToNode = new Dictionary<(int x, int y), int>();
        for (int i = 0; i < roomPositions.Count; i++)
        {
            positionToNode[roomPositions[i]] = i;
        }

        // Build connections between adjacent rooms
        var possibleConnections = new List<(int a, int b)>();
        var neighborOffsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0) }; // 4-way connectivity

        foreach (var (pos, nodeId) in positionToNode)
        {
            foreach (var (dx, dy) in neighborOffsets)
            {
                var neighborPos = (pos.x + dx, pos.y + dy);
                if (positionToNode.TryGetValue(neighborPos, out int neighborId))
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
            var candidateConnections = availableConnections
                .Where(c => connectedNodes.Contains(c.a) != connectedNodes.Contains(c.b))
                .ToList();

            if (candidateConnections.Count == 0)
            {
                // If no direct connections available, connect to nearest node
                var unconnected = Enumerable.Range(0, roomCount).Where(id => !connectedNodes.Contains(id)).ToList();
                if (unconnected.Count > 0)
                {
                    int unconnectedId = unconnected[rng.Next(unconnected.Count)];
                    int connectedId = connectedNodes.OrderBy(id =>
                    {
                        var posA = roomPositions[id];
                        var posB = roomPositions[unconnectedId];
                        return Math.Abs(posA.x - posB.x) + Math.Abs(posA.y - posB.y);
                    }).First();

                    spanningTreeConnections.Add((Math.Min(connectedId, unconnectedId), Math.Max(connectedId, unconnectedId)));
                    connectedNodes.Add(unconnectedId);
                }
                else
                {
                    break;
                }
            }
            else
            {
                var selected = candidateConnections[rng.Next(candidateConnections.Count)];
                spanningTreeConnections.Add(selected);
                connectedNodes.Add(selected.a);
                connectedNodes.Add(selected.b);
            }
        }

        // Add spanning tree connections
        foreach (var (a, b) in spanningTreeConnections)
        {
            if (!connections.Any(c => (c.NodeAId == a && c.NodeBId == b) || (c.NodeAId == b && c.NodeBId == a)))
            {
                connections.Add(new RoomConnection { NodeAId = a, NodeBId = b });
            }
        }

        // Add extra connections based on branching factor
        var remainingConnections = possibleConnections
            .Except(spanningTreeConnections)
            .ToList();

        int maxExtraEdges = (int)(remainingConnections.Count * branchingFactor);
        int extraEdges = rng.Next(0, maxExtraEdges + 1);

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
    /// Generates a connected graph using cellular automata with default configuration.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        var config = new CellularAutomataGraphConfig
        {
            BirthThreshold = 4,
            SurvivalThreshold = 3,
            Iterations = 5
        };
        return Generate(roomCount, branchingFactor, rng, config);
    }

    private int CountNeighbors(bool[,] grid, int x, int y, int gridSize)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < gridSize && ny >= 0 && ny < gridSize && grid[nx, ny])
                {
                    count++;
                }
            }
        }
        return count;
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

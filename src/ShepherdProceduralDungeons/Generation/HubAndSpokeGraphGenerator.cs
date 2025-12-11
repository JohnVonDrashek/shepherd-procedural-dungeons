using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Generates graph topologies using a hub-and-spoke pattern with central hub rooms and branching spokes.
/// </summary>
public sealed class HubAndSpokeGraphGenerator : IGraphGenerator
{
    /// <summary>
    /// Generates a connected graph using hub-and-spoke pattern.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <param name="config">Hub-and-spoke configuration.</param>
    /// <returns>A connected floor graph.</returns>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng, HubAndSpokeGraphConfig config)
    {
        if (config.HubCount <= 0)
            throw new ArgumentException("HubCount must be at least 1", nameof(config));
        if (config.HubCount >= roomCount)
            throw new ArgumentException("HubCount must be less than roomCount", nameof(config));

        // Create nodes
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // Select hub nodes (first N nodes, with node 0 always being a hub)
        var hubIds = Enumerable.Range(0, config.HubCount).ToList();
        var spokeIds = Enumerable.Range(config.HubCount, roomCount - config.HubCount).ToList();

        // Connect hubs to each other (create a connected hub network)
        // This ensures hubs have at least some connections
        if (hubIds.Count > 1)
        {
            // Build spanning tree of hubs
            var connectedHubs = new List<int> { hubIds[0] };
            for (int i = 1; i < hubIds.Count; i++)
            {
                int hubToConnect = hubIds[i];
                int connectedHub = connectedHubs[rng.Next(connectedHubs.Count)];
                connections.Add(new RoomConnection { NodeAId = connectedHub, NodeBId = hubToConnect });
                connectedHubs.Add(hubToConnect);
            }
        }
        
        // Ensure each hub connects to at least one other hub or will get spokes
        // (This is already handled above for multi-hub cases)

        // Assign spokes to hubs and create spoke chains
        var spokesPerHub = spokeIds.Count / config.HubCount;
        var extraSpokes = spokeIds.Count % config.HubCount;

        int spokeIndex = 0;
        for (int hubIndex = 0; hubIndex < config.HubCount; hubIndex++)
        {
            int hubId = hubIds[hubIndex];
            int spokesForThisHub = spokesPerHub + (hubIndex < extraSpokes ? 1 : 0);

            // Create spoke chains from this hub
            var currentSpokes = spokeIds.Skip(spokeIndex).Take(spokesForThisHub).ToList();
            spokeIndex += spokesForThisHub;

            if (currentSpokes.Count > 0)
            {
                // Connect first spoke to hub
                connections.Add(new RoomConnection { NodeAId = hubId, NodeBId = currentSpokes[0] });

                // Create chains of spokes (each spoke connects to next, up to MaxSpokeLength)
                for (int i = 0; i < currentSpokes.Count - 1; i++)
                {
                    int currentSpoke = currentSpokes[i];
                    int nextSpoke = currentSpokes[i + 1];
                    
                    // Only connect if within max spoke length from hub
                    int distanceFromHub = i + 1;
                    if (distanceFromHub < config.MaxSpokeLength)
                    {
                        connections.Add(new RoomConnection { NodeAId = currentSpoke, NodeBId = nextSpoke });
                    }
                    else
                    {
                        // Connect back to hub to maintain connectivity (don't extend chain beyond max length)
                        connections.Add(new RoomConnection { NodeAId = currentSpoke, NodeBId = hubId });
                        // Also connect the next spoke to hub to ensure connectivity
                        if (!connections.Any(c => (c.NodeAId == nextSpoke && c.NodeBId == hubId) || (c.NodeAId == hubId && c.NodeBId == nextSpoke)))
                        {
                            connections.Add(new RoomConnection { NodeAId = nextSpoke, NodeBId = hubId });
                        }
                    }
                }
                
                // Ensure last spoke in chain is connected (if chain was cut short by MaxSpokeLength)
                if (currentSpokes.Count > 0)
                {
                    int lastSpoke = currentSpokes[currentSpokes.Count - 1];
                    // Check if last spoke is connected
                    bool lastSpokeConnected = connections.Any(c => 
                        (c.NodeAId == lastSpoke || c.NodeBId == lastSpoke));
                    if (!lastSpokeConnected)
                    {
                        // Connect last spoke back to hub
                        connections.Add(new RoomConnection { NodeAId = lastSpoke, NodeBId = hubId });
                    }
                }
            }
        }

        // Add extra connections based on branching factor
        // These can connect spokes to other spokes, spokes to other hubs, etc.
        var allPossibleConnections = new List<(int a, int b)>();
        
        // Spoke-to-spoke connections (within same hub's spokes)
        for (int hubIndex = 0; hubIndex < config.HubCount; hubIndex++)
        {
            int hubId = hubIds[hubIndex];
            int spokesForThisHub = spokesPerHub + (hubIndex < extraSpokes ? 1 : 0);
            var hubSpokes = spokeIds.Skip(hubIndex * spokesPerHub + Math.Min(hubIndex, extraSpokes)).Take(spokesForThisHub).ToList();
            
            for (int i = 0; i < hubSpokes.Count; i++)
            {
                for (int j = i + 1; j < hubSpokes.Count; j++)
                {
                    allPossibleConnections.Add((hubSpokes[i], hubSpokes[j]));
                }
            }
        }

        // Cross-hub connections (spokes from different hubs)
        for (int i = 0; i < spokeIds.Count; i++)
        {
            for (int j = i + 1; j < spokeIds.Count; j++)
            {
                allPossibleConnections.Add((spokeIds[i], spokeIds[j]));
            }
        }

        // Filter out existing connections
        var existingConnectionsSet = connections.Select(c => 
            (Math.Min(c.NodeAId, c.NodeBId), Math.Max(c.NodeAId, c.NodeBId))).ToHashSet();
        
        var availableConnections = allPossibleConnections
            .Where(c => !existingConnectionsSet.Contains((c.a, c.b)))
            .ToList();

        int maxExtraEdges = (int)(availableConnections.Count * branchingFactor);
        int extraEdges = rng.Next(0, maxExtraEdges + 1);

        var shuffled = availableConnections.OrderBy(_ => rng.Next()).Take(extraEdges).ToList();
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
    /// Generates a connected graph using hub-and-spoke pattern with default configuration.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        int hubCount = Math.Max(1, roomCount / 5); // Default: ~20% hubs
        var config = new HubAndSpokeGraphConfig
        {
            HubCount = hubCount,
            MaxSpokeLength = 5
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

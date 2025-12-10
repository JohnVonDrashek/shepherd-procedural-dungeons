using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Assigns room types to graph nodes based on constraints and requirements.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class RoomTypeAssigner<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Assigns room types to all nodes in the graph.
    /// </summary>
    /// <param name="graph">The floor graph to assign types to.</param>
    /// <param name="spawnType">Room type for the spawn room.</param>
    /// <param name="bossType">Room type for the boss room.</param>
    /// <param name="defaultType">Default room type for unassigned nodes.</param>
    /// <param name="roomRequirements">Required room types and their counts.</param>
    /// <param name="constraints">Constraints for room type placement.</param>
    /// <param name="rng">Random number generator.</param>
    /// <param name="assignments">Output dictionary mapping node IDs to room types.</param>
    public void AssignTypes(
        FloorGraph graph,
        TRoomType spawnType,
        TRoomType bossType,
        TRoomType defaultType,
        IReadOnlyList<(TRoomType type, int count)> roomRequirements,
        IReadOnlyList<IConstraint<TRoomType>> constraints,
        Random rng,
        out Dictionary<int, TRoomType> assignments)
    {
        var localAssignments = new Dictionary<int, TRoomType>();

        // 1. Assign spawn to start node
        localAssignments[graph.StartNodeId] = spawnType;

        // 2. Find boss location: farthest node that satisfies boss constraints
        var bossConstraints = constraints.Where(c => c.TargetRoomType.Equals(bossType)).ToList();
        var validBossNodes = graph.Nodes
            .Where(n => n.Id != graph.StartNodeId)
            .Where(n => bossConstraints.All(c => c.IsValid(n, graph, localAssignments)))
            .OrderByDescending(n => n.DistanceFromStart)
            .ToList();

        if (validBossNodes.Count == 0)
            throw new ConstraintViolationException($"No valid location for {bossType}");

        var bossNode = validBossNodes[rng.Next(validBossNodes.Count)];
        localAssignments[bossNode.Id] = bossType;
        graph.BossNodeId = bossNode.Id;

        // 3. Calculate critical path via BFS from start to boss
        graph.CriticalPath = FindPath(graph, graph.StartNodeId, bossNode.Id);
        foreach (int nodeId in graph.CriticalPath)
        {
            graph.Nodes.First(n => n.Id == nodeId).IsOnCriticalPath = true;
        }

        // 4. Assign required room types based on constraints
        foreach (var (roomType, count) in roomRequirements.OrderByDescending(r => GetConstraintPriority(r.type, constraints)))
        {
            if (roomType.Equals(spawnType) || roomType.Equals(bossType))
                continue; // Already handled

            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            int assigned = 0;

            var candidates = graph.Nodes
                .Where(n => !localAssignments.ContainsKey(n.Id))
                .Where(n => typeConstraints.All(c => c.IsValid(n, graph, localAssignments)))
                .ToList();

            // Shuffle candidates for randomness
            Shuffle(candidates, rng);

            foreach (var node in candidates)
            {
                if (assigned >= count) break;
                localAssignments[node.Id] = roomType;
                assigned++;
            }

            if (assigned < count)
                throw new ConstraintViolationException($"Could only place {assigned}/{count} rooms of type {roomType}");
        }

        // 5. Fill remaining with default type
        foreach (var node in graph.Nodes)
        {
            if (!localAssignments.ContainsKey(node.Id))
            {
                localAssignments[node.Id] = defaultType;
            }
        }

        // Assign to out parameter
        assignments = localAssignments;
    }

    private IReadOnlyList<int> FindPath(FloorGraph graph, int fromId, int toId)
    {
        // BFS to find shortest path
        var visited = new Dictionary<int, int>(); // nodeId -> previousNodeId
        var queue = new Queue<int>();
        queue.Enqueue(fromId);
        visited[fromId] = -1;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == toId)
            {
                // Reconstruct path
                var path = new List<int>();
                int node = toId;
                while (node != -1)
                {
                    path.Add(node);
                    node = visited[node];
                }
                path.Reverse();
                return path;
            }

            var currentNode = graph.Nodes.First(n => n.Id == current);
            foreach (var conn in currentNode.Connections)
            {
                int neighborId = conn.GetOtherNodeId(current);
                if (!visited.ContainsKey(neighborId))
                {
                    visited[neighborId] = current;
                    queue.Enqueue(neighborId);
                }
            }
        }

        throw new InvalidOperationException("No path found - graph is disconnected");
    }

    private int GetConstraintPriority(TRoomType roomType, IReadOnlyList<IConstraint<TRoomType>> constraints)
    {
        var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
        
        // Higher priority = more specific constraints
        // Count constraint types to determine priority
        int priority = 0;
        foreach (var constraint in typeConstraints)
        {
            priority += constraint.GetType().Name switch
            {
                nameof(MustBeDeadEndConstraint<TRoomType>) => 10,
                nameof(OnlyOnCriticalPathConstraint<TRoomType>) => 8,
                nameof(NotOnCriticalPathConstraint<TRoomType>) => 7,
                nameof(MaxPerFloorConstraint<TRoomType>) => 5,
                nameof(MinDistanceFromStartConstraint<TRoomType>) => 3,
                nameof(MaxDistanceFromStartConstraint<TRoomType>) => 3,
                nameof(CustomConstraint<TRoomType>) => 1,
                _ => 1
            };
        }
        
        return priority;
    }

    private static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}


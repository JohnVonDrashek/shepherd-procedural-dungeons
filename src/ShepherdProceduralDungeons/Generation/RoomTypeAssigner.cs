using ShepherdProceduralDungeons.Configuration;
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
    /// <param name="floorIndex">Optional floor index for floor-aware constraints. Use -1 for single-floor generation.</param>
    /// <param name="zoneAssignments">Optional zone assignments for zone-aware constraints.</param>
    /// <param name="zoneRoomRequirements">Optional zone-specific room requirements.</param>
    public void AssignTypes(
        FloorGraph graph,
        TRoomType spawnType,
        TRoomType bossType,
        TRoomType defaultType,
        IReadOnlyList<(TRoomType type, int count)> roomRequirements,
        IReadOnlyList<IConstraint<TRoomType>> constraints,
        Random rng,
        out Dictionary<int, TRoomType> assignments,
        int floorIndex = -1,
        IReadOnlyDictionary<int, string>? zoneAssignments = null,
        IReadOnlyDictionary<string, IReadOnlyList<(TRoomType type, int count)>>? zoneRoomRequirements = null)
    {
        // Set floor index on floor-aware constraints
        if (floorIndex >= 0)
        {
            foreach (var constraint in constraints)
            {
                if (constraint is IFloorAwareConstraint<TRoomType> floorAware)
                {
                    floorAware.SetFloorIndex(floorIndex);
                }
            }
        }

        // Set zone assignments on zone-aware constraints
        if (zoneAssignments != null)
        {
            foreach (var constraint in constraints)
            {
                if (constraint is IZoneAwareConstraint<TRoomType> zoneAware)
                {
                    zoneAware.SetZoneAssignments(zoneAssignments);
                }
            }
        }

        // Pre-group constraints by room type to eliminate repeated filtering
        var constraintsByType = constraints
            .GroupBy(c => c.TargetRoomType)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<IConstraint<TRoomType>>)g.ToList());

        var localAssignments = new Dictionary<int, TRoomType>();

        // 1. Assign spawn to start node
        localAssignments[graph.StartNodeId] = spawnType;

        // 2. Find boss location: farthest node that satisfies boss constraints
        var bossConstraints = constraintsByType.TryGetValue(bossType, out var boss)
            ? boss
            : Array.Empty<IConstraint<TRoomType>>();
        var validBossNodes = graph.Nodes
            .Where(n => n.Id != graph.StartNodeId)
            .Where(n => bossConstraints.All(c => c.IsValid(n, graph, localAssignments)))
            .OrderByDescending(n => n.DistanceFromStart)
            .ToList();

        if (validBossNodes.Count == 0)
        {
            // Check if this is due to floor-aware constraints (multi-floor scenario)
            // If all boss constraints are floor-aware and prevent placement, skip boss placement
            var floorAwareBossConstraints = bossConstraints.OfType<IFloorAwareConstraint<TRoomType>>().ToList();
            if (floorAwareBossConstraints.Count > 0 && floorAwareBossConstraints.Count == bossConstraints.Count)
            {
                // All constraints are floor-aware and prevent placement - skip boss for this floor
                graph.BossNodeId = -1; // Mark as no boss
            }
            else
            {
                throw new ConstraintViolationException($"No valid location for {bossType}");
            }
        }
        else
        {
            var bossNode = validBossNodes[rng.Next(validBossNodes.Count)];
            localAssignments[bossNode.Id] = bossType;
            graph.BossNodeId = bossNode.Id;
        }

        // 3. Calculate critical path via BFS from start to boss (if boss exists)
        if (graph.BossNodeId >= 0)
        {
            graph.CriticalPath = FindPath(graph, graph.StartNodeId, graph.BossNodeId);
            foreach (int nodeId in graph.CriticalPath)
            {
                graph.GetNode(nodeId).IsOnCriticalPath = true;
            }
        }
        else
        {
            // No boss room - critical path is just the start node
            graph.CriticalPath = new[] { graph.StartNodeId };
            graph.GetNode(graph.StartNodeId).IsOnCriticalPath = true;
        }

        // 4. Assign required room types based on constraints
        // Process global requirements first
        foreach (var (roomType, count) in roomRequirements.OrderByDescending(r => GetConstraintPriority(r.type, constraintsByType)))
        {
            if (roomType.Equals(spawnType) || roomType.Equals(bossType))
                continue; // Already handled

            var typeConstraints = constraintsByType.TryGetValue(roomType, out var type)
                ? type
                : Array.Empty<IConstraint<TRoomType>>();
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

        // 5. Assign zone-specific room requirements
        if (zoneRoomRequirements != null && zoneAssignments != null)
        {
            // Collect all zone requirements with their zone IDs
            var zoneReqsWithZones = new List<(TRoomType type, int count, string zoneId)>();
            foreach (var (zoneId, zoneReqs) in zoneRoomRequirements)
            {
                foreach (var (roomType, count) in zoneReqs)
                {
                    if (roomType.Equals(spawnType) || roomType.Equals(bossType))
                        continue; // Already handled
                    zoneReqsWithZones.Add((roomType, count, zoneId));
                }
            }

            // Process zone requirements ordered by constraint priority
            foreach (var (roomType, count, zoneId) in zoneReqsWithZones.OrderByDescending(r => GetConstraintPriority(r.type, constraintsByType)))
            {
                var typeConstraints = constraintsByType.TryGetValue(roomType, out var type)
                    ? type
                    : Array.Empty<IConstraint<TRoomType>>();
                int assigned = 0;

                // Only consider nodes in the specific zone
                var candidates = graph.Nodes
                    .Where(n => !localAssignments.ContainsKey(n.Id))
                    .Where(n => zoneAssignments.TryGetValue(n.Id, out var nodeZoneId) && nodeZoneId == zoneId)
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
                    throw new ConstraintViolationException($"Could only place {assigned}/{count} rooms of type {roomType} in zone {zoneId}");
            }
        }

        // 6. Fill remaining with default type
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

            var currentNode = graph.GetNode(current);
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

    private int GetConstraintPriority(TRoomType roomType, IReadOnlyDictionary<TRoomType, IReadOnlyList<IConstraint<TRoomType>>> constraintsByType)
    {
        var typeConstraints = constraintsByType.TryGetValue(roomType, out var constraints)
            ? constraints
            : Array.Empty<IConstraint<TRoomType>>();
        
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
                nameof(OnlyOnFloorConstraint<TRoomType>) => 9,
                nameof(NotOnFloorConstraint<TRoomType>) => 9,
                nameof(MinFloorConstraint<TRoomType>) => 6,
                nameof(MaxFloorConstraint<TRoomType>) => 6,
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


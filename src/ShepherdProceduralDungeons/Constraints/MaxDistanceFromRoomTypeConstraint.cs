using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to be at most N steps from rooms of specified type(s).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxDistanceFromRoomTypeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The reference room types to check distance from.
    /// </summary>
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }

    /// <summary>
    /// Maximum distance allowed from any reference room type.
    /// </summary>
    public int MaxDistance { get; }

    /// <summary>
    /// Creates a new maximum distance from room type constraint with a single reference type.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="referenceRoomType">The reference room type to check distance from.</param>
    /// <param name="maxDistance">Maximum distance allowed. Must be non-negative.</param>
    public MaxDistanceFromRoomTypeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType, int maxDistance)
    {
        if (maxDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "Maximum distance must be non-negative.");

        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType> { referenceRoomType };
        MaxDistance = maxDistance;
    }

    /// <summary>
    /// Creates a new maximum distance from room type constraint with multiple reference types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="maxDistance">Maximum distance allowed. Must be non-negative.</param>
    /// <param name="referenceRoomTypes">The reference room types to check distance from.</param>
    public MaxDistanceFromRoomTypeConstraint(TRoomType targetRoomType, int maxDistance, params TRoomType[] referenceRoomTypes)
    {
        if (maxDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "Maximum distance must be non-negative.");
        if (referenceRoomTypes == null || referenceRoomTypes.Length == 0)
            throw new ArgumentException("At least one reference room type must be provided.", nameof(referenceRoomTypes));

        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType>(referenceRoomTypes);
        MaxDistance = maxDistance;
    }

    /// <summary>
    /// Checks if the node is at most MaxDistance steps from any node with a reference room type.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Find all nodes with reference room types
        var referenceNodeIds = graph.Nodes
            .Where(n => currentAssignments.TryGetValue(n.Id, out var assignedType) && ReferenceRoomTypes.Contains(assignedType))
            .Select(n => n.Id)
            .ToList();

        // If no reference nodes exist:
        // - If assignments dictionary is empty: return true (permissive, allows assignment order flexibility)
        // - If assignments exist but none match reference types: return false (can't satisfy max distance)
        if (referenceNodeIds.Count == 0)
        {
            // Check if any assignments exist at all
            if (currentAssignments.Count == 0)
                return true; // Permissive when no assignments yet
            else
                return false; // Can't satisfy max distance if reference rooms don't exist
        }

        // Calculate shortest distance to nearest reference node
        int shortestDistance = int.MaxValue;
        foreach (var referenceNodeId in referenceNodeIds)
        {
            int distance = CalculateShortestDistance(graph, node.Id, referenceNodeId);
            if (distance < shortestDistance)
                shortestDistance = distance;
        }

        // If no path exists (disconnected graph), shortestDistance remains int.MaxValue
        // For max distance constraint, no path means infinite distance > maxDistance, so invalid
        return shortestDistance <= MaxDistance;
    }

    /// <summary>
    /// Calculates the shortest path distance between two nodes using BFS.
    /// Returns int.MaxValue if no path exists.
    /// </summary>
    private static int CalculateShortestDistance(FloorGraph graph, int fromId, int toId)
    {
        if (fromId == toId)
            return 0;

        var visited = new HashSet<int>();
        var queue = new Queue<(int nodeId, int distance)>();
        queue.Enqueue((fromId, 0));
        visited.Add(fromId);

        while (queue.Count > 0)
        {
            var (currentId, distance) = queue.Dequeue();

            if (currentId == toId)
                return distance;

            var currentNode = graph.GetNode(currentId);
            foreach (var conn in currentNode.Connections)
            {
                int neighborId = conn.GetOtherNodeId(currentId);
                if (!visited.Contains(neighborId))
                {
                    visited.Add(neighborId);
                    queue.Enqueue((neighborId, distance + 1));
                }
            }
        }

        // No path exists
        return int.MaxValue;
    }
}

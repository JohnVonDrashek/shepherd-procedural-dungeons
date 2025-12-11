using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to be at least N steps from rooms of specified type(s).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MinDistanceFromRoomTypeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
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
    /// Minimum distance required from any reference room type.
    /// </summary>
    public int MinDistance { get; }

    /// <summary>
    /// Creates a new minimum distance from room type constraint with a single reference type.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="referenceRoomType">The reference room type to check distance from.</param>
    /// <param name="minDistance">Minimum distance required. Must be non-negative.</param>
    public MinDistanceFromRoomTypeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType, int minDistance)
    {
        if (minDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(minDistance), minDistance, "Minimum distance must be non-negative.");

        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType> { referenceRoomType };
        MinDistance = minDistance;
    }

    /// <summary>
    /// Creates a new minimum distance from room type constraint with multiple reference types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="minDistance">Minimum distance required. Must be non-negative.</param>
    /// <param name="referenceRoomTypes">The reference room types to check distance from.</param>
    public MinDistanceFromRoomTypeConstraint(TRoomType targetRoomType, int minDistance, params TRoomType[] referenceRoomTypes)
    {
        if (minDistance < 0)
            throw new ArgumentOutOfRangeException(nameof(minDistance), minDistance, "Minimum distance must be non-negative.");
        if (referenceRoomTypes == null || referenceRoomTypes.Length == 0)
            throw new ArgumentException("At least one reference room type must be provided.", nameof(referenceRoomTypes));

        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType>(referenceRoomTypes);
        MinDistance = minDistance;
    }

    /// <summary>
    /// Checks if the node is at least MinDistance steps from any node with a reference room type.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Find all nodes with reference room types
        var referenceNodeIds = graph.Nodes
            .Where(n => currentAssignments.TryGetValue(n.Id, out var assignedType) && ReferenceRoomTypes.Contains(assignedType))
            .Select(n => n.Id)
            .ToList();

        // If no reference nodes exist, constraint is permissive (allows assignment order flexibility)
        if (referenceNodeIds.Count == 0)
            return true;

        // Calculate shortest distance to nearest reference node
        int shortestDistance = int.MaxValue;
        foreach (var referenceNodeId in referenceNodeIds)
        {
            int distance = CalculateShortestDistance(graph, node.Id, referenceNodeId);
            if (distance < shortestDistance)
                shortestDistance = distance;
        }

        // If no path exists (disconnected graph), shortestDistance remains int.MaxValue
        // For min distance constraint, no path means infinite distance >= minDistance, so valid
        return shortestDistance >= MinDistance;
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

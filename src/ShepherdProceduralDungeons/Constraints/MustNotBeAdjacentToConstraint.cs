using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint preventing the target room type from being adjacent to any of the specified room types.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MustNotBeAdjacentToConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }
    
    /// <summary>
    /// Room types that must NOT be adjacent (none of these should be neighbors).
    /// </summary>
    public IReadOnlySet<TRoomType> ForbiddenAdjacentTypes { get; }

    /// <summary>
    /// Creates a constraint preventing the target room type from being adjacent to the specified room type.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="forbiddenAdjacentType">The room type that must not be adjacent.</param>
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, TRoomType forbiddenAdjacentType)
    {
        TargetRoomType = targetRoomType;
        ForbiddenAdjacentTypes = new HashSet<TRoomType> { forbiddenAdjacentType };
    }
    
    /// <summary>
    /// Creates a constraint preventing the target room type from being adjacent to any of the specified room types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="forbiddenAdjacentTypes">The room types that must not be adjacent (none of these should be neighbors).</param>
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, params TRoomType[] forbiddenAdjacentTypes)
    {
        if (forbiddenAdjacentTypes == null || forbiddenAdjacentTypes.Length == 0)
            throw new ArgumentException("At least one forbidden adjacent type must be specified.", nameof(forbiddenAdjacentTypes));
        
        TargetRoomType = targetRoomType;
        ForbiddenAdjacentTypes = new HashSet<TRoomType>(forbiddenAdjacentTypes);
    }
    
    /// <summary>
    /// Creates a constraint preventing the target room type from being adjacent to any of the specified room types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="forbiddenAdjacentTypes">The room types that must not be adjacent (none of these should be neighbors).</param>
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, IEnumerable<TRoomType> forbiddenAdjacentTypes)
    {
        if (forbiddenAdjacentTypes == null)
            throw new ArgumentNullException(nameof(forbiddenAdjacentTypes));
        
        var typesList = forbiddenAdjacentTypes.ToList();
        if (typesList.Count == 0)
            throw new ArgumentException("At least one forbidden adjacent type must be specified.", nameof(forbiddenAdjacentTypes));
        
        TargetRoomType = targetRoomType;
        ForbiddenAdjacentTypes = new HashSet<TRoomType>(typesList);
    }

    /// <summary>
    /// Checks if the node has any neighbors with forbidden adjacent room types.
    /// </summary>
    /// <param name="node">The node being evaluated.</param>
    /// <param name="graph">The full graph for context.</param>
    /// <param name="currentAssignments">Room types already assigned to other nodes.</param>
    /// <returns>True if the node has no neighbors with forbidden adjacent room types, false otherwise.</returns>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // If node has no connections, it cannot violate adjacency constraint
        if (node.ConnectionCount == 0)
            return true;

        // Check each connection to see if the neighbor has a forbidden type
        foreach (var connection in node.Connections)
        {
            var neighborId = connection.GetOtherNodeId(node.Id);
            
            // Check if this neighbor has been assigned a room type
            if (currentAssignments.TryGetValue(neighborId, out var neighborRoomType))
            {
                // Check if the neighbor's room type is one of the forbidden adjacent types
                if (ForbiddenAdjacentTypes.Contains(neighborRoomType))
                {
                    // Found a neighbor with a forbidden type - invalid placement
                    return false;
                }
            }
            // If neighbor is not assigned yet, it doesn't violate the constraint
        }

        // No neighbor has a forbidden adjacent type (or neighbors not yet assigned)
        return true;
    }
}

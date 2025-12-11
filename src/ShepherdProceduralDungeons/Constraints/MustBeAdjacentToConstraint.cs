using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring the target room type to be adjacent to at least one of the specified room types.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MustBeAdjacentToConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }
    
    /// <summary>
    /// Room types that must be adjacent (at least one).
    /// </summary>
    public IReadOnlySet<TRoomType> RequiredAdjacentTypes { get; }

    /// <summary>
    /// Creates a constraint requiring the target room type to be adjacent to the specified room type.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="requiredAdjacentType">The room type that must be adjacent.</param>
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, TRoomType requiredAdjacentType)
    {
        TargetRoomType = targetRoomType;
        RequiredAdjacentTypes = new HashSet<TRoomType> { requiredAdjacentType };
    }
    
    /// <summary>
    /// Creates a constraint requiring the target room type to be adjacent to at least one of the specified room types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="requiredAdjacentTypes">The room types that must be adjacent (at least one).</param>
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, params TRoomType[] requiredAdjacentTypes)
    {
        if (requiredAdjacentTypes == null || requiredAdjacentTypes.Length == 0)
            throw new ArgumentException("At least one required adjacent type must be specified.", nameof(requiredAdjacentTypes));
        
        TargetRoomType = targetRoomType;
        RequiredAdjacentTypes = new HashSet<TRoomType>(requiredAdjacentTypes);
    }
    
    /// <summary>
    /// Creates a constraint requiring the target room type to be adjacent to at least one of the specified room types.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="requiredAdjacentTypes">The room types that must be adjacent (at least one).</param>
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, IEnumerable<TRoomType> requiredAdjacentTypes)
    {
        if (requiredAdjacentTypes == null)
            throw new ArgumentNullException(nameof(requiredAdjacentTypes));
        
        var typesList = requiredAdjacentTypes.ToList();
        if (typesList.Count == 0)
            throw new ArgumentException("At least one required adjacent type must be specified.", nameof(requiredAdjacentTypes));
        
        TargetRoomType = targetRoomType;
        RequiredAdjacentTypes = new HashSet<TRoomType>(typesList);
    }

    /// <summary>
    /// Checks if the node has at least one neighbor with one of the required adjacent room types.
    /// </summary>
    /// <param name="node">The node being evaluated.</param>
    /// <param name="graph">The full graph for context.</param>
    /// <param name="currentAssignments">Room types already assigned to other nodes.</param>
    /// <returns>True if the node has at least one neighbor with a required adjacent room type.</returns>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // If node has no connections, it cannot be adjacent to any required type
        if (node.ConnectionCount == 0)
            return false;

        // Check each connection to see if the neighbor has one of the required types
        foreach (var connection in node.Connections)
        {
            var neighborId = connection.GetOtherNodeId(node.Id);
            
            // Check if this neighbor has been assigned a room type
            if (currentAssignments.TryGetValue(neighborId, out var neighborRoomType))
            {
                // Check if the neighbor's room type is one of the required adjacent types
                if (RequiredAdjacentTypes.Contains(neighborRoomType))
                {
                    return true;
                }
            }
        }

        // No neighbor has a required adjacent type
        return false;
    }
}

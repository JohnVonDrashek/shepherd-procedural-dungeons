using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room type to appear before another room type (or types) on the critical path.
/// This ensures ordering constraints, such as requiring a mini-boss before the final boss.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MustComeBeforeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The reference room types that the target must come before.
    /// The target must come before at least one of these types on the critical path.
    /// </summary>
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }

    /// <summary>
    /// Creates a new must-come-before constraint with a single reference type.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="referenceRoomType">The reference room type that the target must come before.</param>
    public MustComeBeforeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType)
    {
        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType> { referenceRoomType };
    }

    /// <summary>
    /// Creates a new must-come-before constraint with multiple reference types.
    /// The target must come before at least one of the reference types on the critical path.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="referenceRoomTypes">The reference room types that the target must come before (at least one).</param>
    public MustComeBeforeConstraint(TRoomType targetRoomType, params TRoomType[] referenceRoomTypes)
    {
        if (referenceRoomTypes == null || referenceRoomTypes.Length == 0)
            throw new ArgumentException("At least one reference room type must be provided.", nameof(referenceRoomTypes));

        TargetRoomType = targetRoomType;
        ReferenceRoomTypes = new HashSet<TRoomType>(referenceRoomTypes);
    }

    /// <summary>
    /// Checks if the node comes before the reference room type(s) on the critical path.
    /// </summary>
    /// <param name="node">The node being evaluated.</param>
    /// <param name="graph">The full graph for context.</param>
    /// <param name="currentAssignments">Room types already assigned to other nodes.</param>
    /// <returns>True if this node can be assigned the target room type (comes before at least one reference type).</returns>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // If critical path is empty or not set, be permissive
        if (graph.CriticalPath == null || graph.CriticalPath.Count == 0)
            return true;

        // If candidate node is not on critical path, be permissive
        if (!node.IsOnCriticalPath)
            return true;

        // Find all reference room types that are assigned and on the critical path
        var assignedReferenceNodes = new List<int>();
        foreach (var assignment in currentAssignments)
        {
            if (ReferenceRoomTypes.Contains(assignment.Value))
            {
                var assignedNode = graph.GetNode(assignment.Key);
                if (assignedNode.IsOnCriticalPath)
                {
                    assignedReferenceNodes.Add(assignment.Key);
                }
            }
        }

        // If no reference room types are assigned yet, be permissive (allows assignment order flexibility)
        if (assignedReferenceNodes.Count == 0)
            return true;

        // Find candidate node's index on critical path
        int candidateIndex = -1;
        for (int i = 0; i < graph.CriticalPath.Count; i++)
        {
            if (graph.CriticalPath[i] == node.Id)
            {
                candidateIndex = i;
                break;
            }
        }

        // If candidate is not found on critical path (shouldn't happen if IsOnCriticalPath is true, but handle gracefully)
        if (candidateIndex == -1)
            return true;

        // Check if candidate comes before at least one reference room type
        // The constraint is satisfied if candidate comes before ANY of the reference types
        foreach (var referenceNodeId in assignedReferenceNodes)
        {
            int referenceIndex = -1;
            for (int i = 0; i < graph.CriticalPath.Count; i++)
            {
                if (graph.CriticalPath[i] == referenceNodeId)
                {
                    referenceIndex = i;
                    break;
                }
            }

            // If reference is found on critical path
            if (referenceIndex != -1)
            {
                // If candidate comes before this reference, constraint is satisfied
                if (candidateIndex < referenceIndex)
                    return true;
            }
        }

        // Candidate does not come before any of the assigned reference types
        return false;
    }
}

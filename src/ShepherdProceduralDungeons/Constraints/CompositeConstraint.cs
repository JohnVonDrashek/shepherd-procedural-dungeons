using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Composes multiple constraints using AND, OR, or NOT logic.
/// Enables complex constraint patterns like "constraint1 OR constraint2" or "NOT constraint".
/// Supports both graph-based and spatial constraints.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class CompositeConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// The composition operator (AND, OR, or NOT).
    /// </summary>
    public CompositionOperator Operator { get; }

    /// <summary>
    /// The constraints being composed.
    /// </summary>
    public IReadOnlyList<IConstraint<TRoomType>> Constraints { get; }

    /// <summary>
    /// Creates a new composite constraint.
    /// </summary>
    private CompositeConstraint(CompositionOperator op, TRoomType targetRoomType, IReadOnlyList<IConstraint<TRoomType>> constraints)
    {
        Operator = op;
        TargetRoomType = targetRoomType;
        Constraints = constraints;
    }

    /// <summary>
    /// Creates an AND composition: all constraints must pass.
    /// </summary>
    /// <param name="constraints">The constraints to combine with AND logic.</param>
    /// <returns>A composite constraint requiring all constraints to pass.</returns>
    public static CompositeConstraint<TRoomType> And(params IConstraint<TRoomType>[] constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        if (constraints.Length == 0)
        {
            // Empty AND should always pass - use default target room type
            // Note: TargetRoomType won't be meaningful for empty compositions
            var defaultTargetRoomType = (TRoomType)Enum.GetValues(typeof(TRoomType)).GetValue(0)!;
            return new CompositeConstraint<TRoomType>(
                CompositionOperator.And,
                defaultTargetRoomType,
                Array.Empty<IConstraint<TRoomType>>().ToList().AsReadOnly()
            );
        }

        // Validate all constraints target the same room type
        var targetRoomType = constraints[0].TargetRoomType;
        ValidateSameTargetRoomType(constraints, targetRoomType);

        return new CompositeConstraint<TRoomType>(
            CompositionOperator.And,
            targetRoomType,
            constraints.ToList().AsReadOnly()
        );
    }

    /// <summary>
    /// Creates an OR composition: at least one constraint must pass.
    /// </summary>
    /// <param name="constraints">The constraints to combine with OR logic.</param>
    /// <returns>A composite constraint requiring at least one constraint to pass.</returns>
    public static CompositeConstraint<TRoomType> Or(params IConstraint<TRoomType>[] constraints)
    {
        if (constraints == null)
        {
            throw new ArgumentNullException(nameof(constraints));
        }

        if (constraints.Length == 0)
        {
            // Empty OR should always fail - use default target room type
            // Note: TargetRoomType won't be meaningful for empty compositions
            var defaultTargetRoomType = (TRoomType)Enum.GetValues(typeof(TRoomType)).GetValue(0)!;
            return new CompositeConstraint<TRoomType>(
                CompositionOperator.Or,
                defaultTargetRoomType,
                Array.Empty<IConstraint<TRoomType>>().ToList().AsReadOnly()
            );
        }

        // For OR compositions, allow different target room types (e.g., "Shop OR Treasure")
        // Use the first constraint's target room type for the composite
        var targetRoomType = constraints[0].TargetRoomType;
        
        // Validate constraints are not null, but allow different target room types for OR
        foreach (var constraint in constraints)
        {
            if (constraint == null)
            {
                throw new ArgumentException("Constraints cannot be null.", nameof(constraints));
            }
        }

        return new CompositeConstraint<TRoomType>(
            CompositionOperator.Or,
            targetRoomType,
            constraints.ToList().AsReadOnly()
        );
    }

    /// <summary>
    /// Creates a NOT composition: the wrapped constraint must fail.
    /// </summary>
    /// <param name="constraint">The constraint to negate.</param>
    /// <returns>A composite constraint requiring the constraint to fail.</returns>
    public static CompositeConstraint<TRoomType> Not(IConstraint<TRoomType> constraint)
    {
        if (constraint == null)
        {
            throw new ArgumentNullException(nameof(constraint));
        }

        return new CompositeConstraint<TRoomType>(
            CompositionOperator.Not,
            constraint.TargetRoomType,
            new[] { constraint }.ToList().AsReadOnly()
        );
    }

    /// <summary>
    /// Checks if a node is valid for the target room type based on the composition logic.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        return Operator switch
        {
            CompositionOperator.And => EvaluateAnd(node, graph, currentAssignments),
            CompositionOperator.Or => EvaluateOr(node, graph, currentAssignments),
            CompositionOperator.Not => EvaluateNot(node, graph, currentAssignments),
            _ => throw new InvalidOperationException($"Unknown composition operator: {Operator}")
        };
    }

    private bool EvaluateAnd(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Empty AND should always pass (but we validate in factory method)
        if (Constraints.Count == 0)
        {
            return true;
        }

        // All constraints must pass (short-circuit on first failure)
        foreach (var constraint in Constraints)
        {
            if (!constraint.IsValid(node, graph, currentAssignments))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateOr(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Empty OR should always fail (but we validate in factory method)
        if (Constraints.Count == 0)
        {
            return false;
        }

        // At least one constraint must pass (short-circuit on first success)
        foreach (var constraint in Constraints)
        {
            if (constraint.IsValid(node, graph, currentAssignments))
            {
                return true;
            }
        }

        return false;
    }

    private bool EvaluateNot(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // NOT should have exactly one constraint
        if (Constraints.Count != 1)
        {
            throw new InvalidOperationException("NOT composition must contain exactly one constraint.");
        }

        // Return the negation of the constraint's result
        return !Constraints[0].IsValid(node, graph, currentAssignments);
    }

    /// <summary>
    /// Checks if a room placement is valid spatially based on the composition logic.
    /// </summary>
    public bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        return Operator switch
        {
            CompositionOperator.And => EvaluateAndSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments),
            CompositionOperator.Or => EvaluateOrSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments),
            CompositionOperator.Not => EvaluateNotSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments),
            _ => throw new InvalidOperationException($"Unknown composition operator: {Operator}")
        };
    }

    private bool EvaluateAndSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // Empty AND should always pass
        if (Constraints.Count == 0)
        {
            return true;
        }

        // All constraints must pass (short-circuit on first failure)
        foreach (var constraint in Constraints)
        {
            // Check if constraint is spatial
            if (constraint is ISpatialConstraint<TRoomType> spatialConstraint)
            {
                if (!spatialConstraint.IsValidSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments))
                {
                    return false;
                }
            }
            // For non-spatial constraints, spatial validation always passes
            // (they are validated in the graph phase)
        }

        return true;
    }

    private bool EvaluateOrSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // Empty OR should always fail
        if (Constraints.Count == 0)
        {
            return false;
        }

        // At least one constraint must pass (short-circuit on first success)
        foreach (var constraint in Constraints)
        {
            // Check if constraint is spatial
            if (constraint is ISpatialConstraint<TRoomType> spatialConstraint)
            {
                if (spatialConstraint.IsValidSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments))
                {
                    return true;
                }
            }
            // For non-spatial constraints, spatial validation always passes
            // (they are validated in the graph phase)
        }

        return false;
    }

    private bool EvaluateNotSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // NOT should have exactly one constraint
        if (Constraints.Count != 1)
        {
            throw new InvalidOperationException("NOT composition must contain exactly one constraint.");
        }

        var constraint = Constraints[0];
        
        // Check if constraint is spatial
        if (constraint is ISpatialConstraint<TRoomType> spatialConstraint)
        {
            return !spatialConstraint.IsValidSpatially(proposedPosition, roomTemplate, placedRooms, graph, assignments);
        }
        
        // For non-spatial constraints, NOT always fails in spatial phase
        // (they are validated in the graph phase)
        return false;
    }

    private static void ValidateSameTargetRoomType(IConstraint<TRoomType>[] constraints, TRoomType expectedTargetRoomType)
    {
        foreach (var constraint in constraints)
        {
            if (constraint == null)
            {
                throw new ArgumentException("Constraints cannot be null.", nameof(constraints));
            }

            if (!constraint.TargetRoomType.Equals(expectedTargetRoomType))
            {
                throw new ArgumentException(
                    $"All constraints in composition must target the same room type. " +
                    $"Expected {expectedTargetRoomType}, but found {constraint.TargetRoomType}.",
                    nameof(constraints));
            }
        }
    }
}

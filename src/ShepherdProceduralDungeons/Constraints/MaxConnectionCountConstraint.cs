using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to have at most N connections.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MaxConnectionCountConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum number of connections allowed.
    /// </summary>
    public int MaxConnections { get; }

    /// <summary>
    /// Creates a new maximum connection count constraint.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="maxConnections">Maximum number of connections allowed. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when maxConnections is negative.</exception>
    public MaxConnectionCountConstraint(TRoomType roomType, int maxConnections)
    {
        if (maxConnections < 0)
            throw new ArgumentOutOfRangeException(nameof(maxConnections), maxConnections, "Maximum connections must be non-negative.");

        TargetRoomType = roomType;
        MaxConnections = maxConnections;
    }

    /// <summary>
    /// Checks if the node has at most MaxConnections connections.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.ConnectionCount <= MaxConnections;
}

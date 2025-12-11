using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint requiring a room to have at least N connections.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public class MinConnectionCountConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Minimum number of connections required.
    /// </summary>
    public int MinConnections { get; }

    /// <summary>
    /// Creates a new minimum connection count constraint.
    /// </summary>
    /// <param name="roomType">The room type this constraint applies to.</param>
    /// <param name="minConnections">Minimum number of connections required. Must be non-negative.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when minConnections is negative.</exception>
    public MinConnectionCountConstraint(TRoomType roomType, int minConnections)
    {
        if (minConnections < 0)
            throw new ArgumentOutOfRangeException(nameof(minConnections), minConnections, "Minimum connections must be non-negative.");

        TargetRoomType = roomType;
        MinConnections = minConnections;
    }

    /// <summary>
    /// Checks if the node has at least MinConnections connections.
    /// </summary>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.ConnectionCount >= MinConnections;
}

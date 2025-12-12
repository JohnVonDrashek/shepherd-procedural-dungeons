using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Constraint that requires rooms of a type to form a spatial cluster (all within clusterRadius of each other).
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class MustFormSpatialClusterConstraint<TRoomType> : ISpatialConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// The room type this constraint applies to.
    /// </summary>
    public TRoomType TargetRoomType { get; }

    /// <summary>
    /// Maximum distance between rooms in the cluster (Manhattan distance).
    /// </summary>
    public int ClusterRadius { get; }

    /// <summary>
    /// Minimum number of rooms required in the cluster.
    /// </summary>
    public int MinClusterSize { get; }

    /// <summary>
    /// Creates a new spatial cluster constraint.
    /// </summary>
    /// <param name="targetRoomType">The room type this constraint applies to.</param>
    /// <param name="clusterRadius">Maximum distance between rooms in the cluster (Manhattan distance).</param>
    /// <param name="minClusterSize">Minimum number of rooms required in the cluster.</param>
    public MustFormSpatialClusterConstraint(TRoomType targetRoomType, int clusterRadius, int minClusterSize)
    {
        TargetRoomType = targetRoomType;
        ClusterRadius = clusterRadius;
        MinClusterSize = minClusterSize;
    }

    /// <inheritdoc/>
    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        // Graph-based validation always passes for spatial constraints
        return true;
    }

    /// <inheritdoc/>
    public bool IsValidSpatially(
        Cell proposedPosition,
        RoomTemplate<TRoomType> roomTemplate,
        IReadOnlyList<PlacedRoom<TRoomType>> placedRooms,
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments)
    {
        // Find all existing rooms of the target type
        var existingRooms = placedRooms
            .Where(r => assignments.TryGetValue(r.NodeId, out var assignedType) && assignedType.Equals(TargetRoomType))
            .ToList();

        // If this is the first room of this type, always allow it
        if (existingRooms.Count == 0)
        {
            return true;
        }

        // Check if the proposed room is within clusterRadius of at least one existing room
        var proposedCells = roomTemplate.Cells.Select(c => new Cell(proposedPosition.X + c.X, proposedPosition.Y + c.Y)).ToList();

        foreach (var existingRoom in existingRooms)
        {
            var existingCells = existingRoom.GetWorldCells().ToList();
            var minDistance = proposedCells
                .SelectMany(pc => existingCells.Select(ec => Math.Abs(pc.X - ec.X) + Math.Abs(pc.Y - ec.Y)))
                .Min();

            if (minDistance <= ClusterRadius)
            {
                // This room is within cluster radius of at least one existing room
                // Check if adding this room maintains cluster connectivity
                return IsClusterConnected(existingRooms, proposedPosition, roomTemplate, ClusterRadius);
            }
        }

        // Proposed room is not within cluster radius of any existing room
        return false;
    }

    private bool IsClusterConnected(
        IReadOnlyList<PlacedRoom<TRoomType>> existingRooms,
        Cell proposedPosition,
        RoomTemplate<TRoomType> proposedTemplate,
        int clusterRadius)
    {
        // Create a graph of rooms where edges represent rooms within clusterRadius
        var allRooms = existingRooms.Concat(new[]
        {
            new PlacedRoom<TRoomType>
            {
                NodeId = -1, // Temporary ID
                RoomType = TargetRoomType,
                Template = proposedTemplate,
                Position = proposedPosition,
                Difficulty = 0.0
            }
        }).ToList();

        // Check if all rooms form a connected cluster
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(0); // Start with first room
        visited.Add(0);

        while (queue.Count > 0)
        {
            var currentIndex = queue.Dequeue();
            var currentRoom = allRooms[currentIndex];
            var currentCells = currentRoom.GetWorldCells().ToList();

            for (int i = 0; i < allRooms.Count; i++)
            {
                if (visited.Contains(i))
                    continue;

                var otherRoom = allRooms[i];
                var otherCells = otherRoom.GetWorldCells().ToList();

                var minDistance = currentCells
                    .SelectMany(cc => otherCells.Select(oc => Math.Abs(cc.X - oc.X) + Math.Abs(cc.Y - oc.Y)))
                    .Min();

                if (minDistance <= clusterRadius)
                {
                    visited.Add(i);
                    queue.Enqueue(i);
                }
            }
        }

        // All rooms must be reachable
        return visited.Count == allRooms.Count;
    }
}

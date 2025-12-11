using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Layout;

/// <summary>
/// Represents a cluster of spatially adjacent rooms of the same type.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class RoomCluster<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Unique identifier for this cluster.
    /// </summary>
    public int ClusterId { get; }

    /// <summary>
    /// The room type of all rooms in this cluster.
    /// </summary>
    public TRoomType RoomType { get; }

    /// <summary>
    /// All rooms belonging to this cluster.
    /// </summary>
    public IReadOnlyList<PlacedRoom<TRoomType>> Rooms { get; }

    /// <summary>
    /// The spatial centroid (center point) of this cluster.
    /// </summary>
    public Cell Centroid { get; }

    /// <summary>
    /// The bounding box of this cluster (Min, Max).
    /// </summary>
    public (Cell Min, Cell Max) BoundingBox { get; }

    /// <summary>
    /// Creates a new room cluster.
    /// </summary>
    public RoomCluster(
        int clusterId,
        TRoomType roomType,
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        Cell centroid,
        (Cell Min, Cell Max) boundingBox)
    {
        ClusterId = clusterId;
        RoomType = roomType;
        Rooms = rooms;
        Centroid = centroid;
        BoundingBox = boundingBox;
    }

    /// <summary>
    /// Gets the number of rooms in this cluster.
    /// </summary>
    public int GetSize() => Rooms.Count;

    /// <summary>
    /// Checks if a room with the given node ID is in this cluster.
    /// </summary>
    public bool ContainsRoom(int nodeId) => Rooms.Any(r => r.NodeId == nodeId);

    /// <summary>
    /// Calculates the average distance between rooms in this cluster.
    /// </summary>
    public double GetAverageDistance()
    {
        if (Rooms.Count < 2)
            return 0.0;

        var roomsList = Rooms.ToList();
        double totalDistance = 0.0;
        int pairCount = 0;

        for (int i = 0; i < roomsList.Count; i++)
        {
            for (int j = i + 1; j < roomsList.Count; j++)
            {
                var distance = CalculateCentroidDistance(roomsList[i], roomsList[j]);
                totalDistance += distance;
                pairCount++;
            }
        }

        return pairCount > 0 ? totalDistance / pairCount : 0.0;
    }

    private double CalculateCentroidDistance(PlacedRoom<TRoomType> room1, PlacedRoom<TRoomType> room2)
    {
        var cells1 = room1.GetWorldCells().ToList();
        var cells2 = room2.GetWorldCells().ToList();

        var centroid1X = cells1.Average(c => c.X);
        var centroid1Y = cells1.Average(c => c.Y);
        var centroid2X = cells2.Average(c => c.X);
        var centroid2Y = cells2.Average(c => c.Y);

        var dx = centroid2X - centroid1X;
        var dy = centroid2Y - centroid1Y;

        return Math.Sqrt(dx * dx + dy * dy);
    }
}

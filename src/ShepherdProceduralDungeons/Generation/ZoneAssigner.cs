using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Assigns rooms to zones based on zone boundaries.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
internal sealed class ZoneAssigner<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Assigns rooms to zones based on their boundaries.
    /// </summary>
    /// <param name="graph">The floor graph.</param>
    /// <param name="zones">The zones to assign rooms to.</param>
    /// <returns>Dictionary mapping node IDs to zone IDs.</returns>
    public Dictionary<int, string> AssignZones(FloorGraph graph, IReadOnlyList<Zone<TRoomType>> zones)
    {
        var assignments = new Dictionary<int, string>();

        if (zones == null || zones.Count == 0)
            return assignments;

        // Assign each node to a zone (first match wins for overlapping zones)
        foreach (var node in graph.Nodes)
        {
            foreach (var zone in zones)
            {
                if (IsNodeInZone(node, zone, graph))
                {
                    assignments[node.Id] = zone.Id;
                    break; // First match wins
                }
            }
        }

        return assignments;
    }

    private bool IsNodeInZone(RoomNode node, Zone<TRoomType> zone, FloorGraph graph)
    {
        return zone.Boundary switch
        {
            ZoneBoundary.DistanceBased distanceBased => 
                node.DistanceFromStart >= distanceBased.MinDistance && 
                node.DistanceFromStart <= distanceBased.MaxDistance,

            ZoneBoundary.CriticalPathBased criticalPathBased =>
                IsNodeOnCriticalPathInRange(node, criticalPathBased, graph),

            _ => false
        };
    }

    private bool IsNodeOnCriticalPathInRange(RoomNode node, ZoneBoundary.CriticalPathBased boundary, FloorGraph graph)
    {
        if (graph.CriticalPath.Count == 0)
            return false;

        int criticalPathIndex = -1;
        for (int i = 0; i < graph.CriticalPath.Count; i++)
        {
            if (graph.CriticalPath[i] == node.Id)
            {
                criticalPathIndex = i;
                break;
            }
        }

        if (criticalPathIndex < 0)
            return false; // Node not on critical path

        // Calculate position along critical path (0.0 to 1.0)
        float position;
        if (graph.CriticalPath.Count == 1)
        {
            position = 0.0f; // Only one node, it's at the start
        }
        else
        {
            position = (float)criticalPathIndex / (graph.CriticalPath.Count - 1);
        }
        
        return position >= boundary.StartPercent && position <= boundary.EndPercent;
    }
}

using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Utility methods for hallway operations.
/// </summary>
public static class HallwayUtilities
{
    /// <summary>
    /// Converts a path of cells into hallway segments by combining consecutive cells going in the same direction.
    /// </summary>
    /// <param name="path">The path of cells to convert.</param>
    /// <returns>A list of hallway segments.</returns>
    public static IReadOnlyList<HallwaySegment> PathToSegments(IReadOnlyList<Cell> path)
    {
        // Combine consecutive cells going same direction into segments
        var segments = new List<HallwaySegment>();

        if (path.Count < 2) return segments;

        // Validate that all cells in the path are adjacent (defensive check)
        for (int i = 1; i < path.Count; i++)
        {
            var prev = path[i - 1];
            var current = path[i];
            int dx = Math.Abs(current.X - prev.X);
            int dy = Math.Abs(current.Y - prev.Y);
            int distance = dx + dy;
            
            if (distance != 1)
            {
                throw new InvalidOperationException(
                    $"Invalid path: non-adjacent cells at index {i}. " +
                    $"Cell {prev} -> {current} (distance: {distance}). " +
                    $"Path length: {path.Count}");
            }
        }

        Cell segmentStart = path[0];
        Cell? lastDir = null;

        for (int i = 1; i < path.Count; i++)
        {
            Cell current = path[i];
            Cell prev = path[i - 1];
            Cell dir = new Cell(current.X - prev.X, current.Y - prev.Y);

            if (lastDir.HasValue && dir != lastDir.Value)
            {
                // Direction changed, end current segment
                segments.Add(new HallwaySegment
                {
                    Start = segmentStart,
                    End = prev
                });
                segmentStart = prev;
            }

            lastDir = dir;
        }

        // Add final segment
        segments.Add(new HallwaySegment
        {
            Start = segmentStart,
            End = path[^1]
        });

        return segments;
    }
}

using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

public class HallwayValidationTests
{
    [Fact]
    public void GeneratedHallways_HaveAdjacentCellsInSegments()
    {
        // This test verifies that PathToSegments creates valid segments
        // where all cells in each segment are adjacent (valid path)
        // Arrange
        var seeds = new[] { 54321, 99999, 12345 };
        FloorLayout<TestHelpers.RoomType>? layout = null;
        
        foreach (var seed in seeds)
        {
            try
            {
                var config = TestHelpers.CreateSimpleConfig(seed: seed, roomCount: 5);
                var generator = new FloorGenerator<TestHelpers.RoomType>();
                layout = generator.Generate(config);
                break;
            }
            catch (SpatialPlacementException)
            {
                continue;
            }
        }
        
        Assert.NotNull(layout);
        
        // Act & Assert - Verify all hallway segments have adjacent cells
        foreach (var hallway in layout!.Hallways)
        {
            foreach (var segment in hallway.Segments)
            {
                var cells = segment.GetCells().ToList();
                
                // Verify segment has at least start and end
                Assert.NotEmpty(cells);
                Assert.Equal(segment.Start, cells.First());
                Assert.Equal(segment.End, cells.Last());
                
                // Verify all consecutive cells are adjacent
                for (int i = 1; i < cells.Count; i++)
                {
                    var prev = cells[i - 1];
                    var current = cells[i];
                    
                    int dx = Math.Abs(current.X - prev.X);
                    int dy = Math.Abs(current.Y - prev.Y);
                    int distance = dx + dy;
                    
                    Assert.True(
                        distance == 1,
                        $"Segment has non-adjacent cells: {prev} -> {current} (distance: {distance})");
                }
                
                // Verify segment is either horizontal or vertical (straight line)
                Assert.True(
                    segment.IsHorizontal || segment.IsVertical,
                    $"Segment is not straight: Start={segment.Start}, End={segment.End}");
            }
        }
    }

    [Fact]
    public void GeneratedHallways_PathsAreContinuous()
    {
        // This test verifies that A* path reconstruction creates continuous paths
        // where each step moves to an adjacent cell
        // Arrange
        var seeds = new[] { 54321, 99999, 12345 };
        FloorLayout<TestHelpers.RoomType>? layout = null;
        
        foreach (var seed in seeds)
        {
            try
            {
                var config = TestHelpers.CreateSimpleConfig(seed: seed, roomCount: 5);
                var generator = new FloorGenerator<TestHelpers.RoomType>();
                layout = generator.Generate(config);
                break;
            }
            catch (SpatialPlacementException)
            {
                continue;
            }
        }
        
        Assert.NotNull(layout);
        
        // Act & Assert - Reconstruct paths from segments and verify continuity
        foreach (var hallway in layout!.Hallways)
        {
            // Reconstruct the full path from segments
            var path = new List<Cell>();
            foreach (var segment in hallway.Segments)
            {
                var segmentCells = segment.GetCells().ToList();
                if (path.Count == 0)
                {
                    path.AddRange(segmentCells);
                }
                else
                {
                    // Next segment should start where previous ended (or be adjacent)
                    var lastCell = path[path.Count - 1];
                    var firstSegmentCell = segmentCells[0];
                    
                    int dx = Math.Abs(firstSegmentCell.X - lastCell.X);
                    int dy = Math.Abs(firstSegmentCell.Y - lastCell.Y);
                    int distance = dx + dy;
                    
                    // Segments should be connected (same cell or adjacent)
                    Assert.True(
                        distance <= 1,
                        $"Hallway segments not connected: {lastCell} -> {firstSegmentCell} (distance: {distance})");
                    
                    // Add cells from segment, skipping first if it's the same as last
                    if (firstSegmentCell == lastCell && segmentCells.Count > 1)
                    {
                        path.AddRange(segmentCells.Skip(1));
                    }
                    else if (firstSegmentCell != lastCell)
                    {
                        path.AddRange(segmentCells);
                    }
                }
            }
            
            // Verify the full path has adjacent cells
            Assert.True(path.Count >= 2, "Hallway path should have at least 2 cells");
            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var current = path[i];
                
                int dx = Math.Abs(current.X - prev.X);
                int dy = Math.Abs(current.Y - prev.Y);
                int distance = dx + dy;
                
                Assert.True(
                    distance == 1,
                    $"Hallway path has non-adjacent cells: {prev} -> {current} (distance: {distance})");
            }
        }
    }
}


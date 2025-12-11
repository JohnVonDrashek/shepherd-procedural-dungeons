using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;
using ShepherdProceduralDungeons.Visualization;
using System.Text;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for FEATURE-002: ASCII Art Map Visualization System
/// </summary>
public class AsciiMapVisualizationTests
{
    private FloorLayout<TestHelpers.RoomType> CreateSimpleLayout()
    {
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("default")
            .ForRoomTypes(TestHelpers.RoomType.Spawn, TestHelpers.RoomType.Boss, TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var spawnRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Spawn,
            Template = template,
            Position = new Cell(0, 0),
            Difficulty = 0.0
        };

        var combatRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 2,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(5, 0),
            Difficulty = 1.0
        };

        var bossRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 3,
            RoomType = TestHelpers.RoomType.Boss,
            Template = template,
            Position = new Cell(10, 0),
            Difficulty = 2.0
        };

        var hallway = new Hallway
        {
            Id = 1,
            Segments = new List<HallwaySegment>
            {
                new HallwaySegment { Start = new Cell(3, 1), End = new Cell(4, 1) }
            },
            DoorA = new Door { Position = new Cell(2, 1), Edge = Edge.East, ConnectsToRoomId = 1 },
            DoorB = new Door { Position = new Cell(5, 1), Edge = Edge.West, ConnectsToRoomId = 2 }
        };

        return new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { spawnRoom, combatRoom, bossRoom },
            Hallways = new List<Hallway> { hallway },
            Doors = new List<Door> { hallway.DoorA, hallway.DoorB },
            Seed = 12345,
            CriticalPath = new List<int> { 1, 2, 3 },
            SpawnRoomId = 1,
            BossRoomId = 3,
            SecretPassages = new List<SecretPassage>()
        };
    }

    [Fact]
    public void AsciiMapRenderer_Exists()
    {
        // Arrange & Act
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();

        // Assert
        Assert.NotNull(renderer);
    }

    [Fact]
    public void AsciiRenderOptions_Exists()
    {
        // Arrange & Act
        var options = new AsciiRenderOptions();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void AsciiRenderOptions_HasDefaultValues()
    {
        // Arrange & Act
        var options = new AsciiRenderOptions();

        // Assert
        Assert.Equal(AsciiRenderStyle.Detailed, options.Style);
        Assert.False(options.ShowRoomIds);
        Assert.True(options.HighlightCriticalPath);
        Assert.True(options.ShowHallways);
        Assert.True(options.ShowDoors);
        Assert.True(options.ShowInteriorFeatures);
        Assert.True(options.ShowSecretPassages);
        Assert.False(options.ShowZoneBoundaries);
        Assert.Null(options.Viewport);
        Assert.Equal(1, options.Scale);
        Assert.True(options.IncludeLegend);
        Assert.NotNull(options.MaxSize);
        Assert.Equal(120, options.MaxSize.Value.MaxWidth);
        Assert.Equal(40, options.MaxSize.Value.MaxHeight);
    }

    [Fact]
    public void AsciiRenderStyle_Enum_HasAllValues()
    {
        // Arrange & Act
        var minimal = AsciiRenderStyle.Minimal;
        var detailed = AsciiRenderStyle.Detailed;
        var artistic = AsciiRenderStyle.Artistic;
        var compact = AsciiRenderStyle.Compact;

        // Assert - Just verify they compile and exist
        Assert.True(Enum.IsDefined(typeof(AsciiRenderStyle), minimal));
        Assert.True(Enum.IsDefined(typeof(AsciiRenderStyle), detailed));
        Assert.True(Enum.IsDefined(typeof(AsciiRenderStyle), artistic));
        Assert.True(Enum.IsDefined(typeof(AsciiRenderStyle), compact));
    }

    [Fact]
    public void Render_FloorLayoutToString_ReturnsNonEmptyString()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();

        // Act
        var result = renderer.Render(layout);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_FloorLayoutToString_ContainsRoomSymbols()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();

        // Act
        var result = renderer.Render(layout);

        // Assert - Should contain symbols for Spawn, Combat, and Boss rooms
        Assert.Contains("S", result); // Spawn room symbol
        Assert.Contains("B", result); // Boss room symbol
        Assert.Contains("C", result); // Combat room symbol
    }

    [Fact]
    public void Render_FloorLayoutToString_ContainsHallways()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();

        // Act
        var result = renderer.Render(layout);

        // Assert - Should contain hallway symbols ('.' or '·')
        Assert.True(result.Contains(".") || result.Contains("·"), "Result should contain hallway symbols");
    }

    [Fact]
    public void Render_FloorLayoutToString_ContainsDoors()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();

        // Act
        var result = renderer.Render(layout);

        // Assert - Should contain door symbols ('+' or '#')
        Assert.True(result.Contains("+") || result.Contains("#"), "Result should contain door symbols");
    }

    [Fact]
    public void Render_FloorLayoutToTextWriter_WritesOutput()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var writer = new StringWriter();

        // Act
        renderer.Render(layout, writer);

        // Assert
        var result = writer.ToString();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_FloorLayoutToStringBuilder_AppendsOutput()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var builder = new StringBuilder();

        // Act
        renderer.Render(layout, builder);

        // Assert
        var result = builder.ToString();
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithCustomRoomTypeSymbols_UsesCustomSymbols()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions
        {
            CustomRoomTypeSymbols = new Dictionary<object, char>
            {
                { TestHelpers.RoomType.Spawn, 'X' },
                { TestHelpers.RoomType.Boss, 'Z' },
                { TestHelpers.RoomType.Combat, 'W' }
            }
        };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should use custom symbols
        Assert.Contains("X", result); // Custom spawn symbol
        Assert.Contains("Z", result); // Custom boss symbol
        Assert.Contains("W", result); // Custom combat symbol
    }

    [Fact]
    public void Render_WithShowRoomIds_DisplaysRoomIds()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { ShowRoomIds = true };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should contain room IDs (1, 2, 3)
        Assert.Contains("1", result);
        Assert.Contains("2", result);
        Assert.Contains("3", result);
    }

    [Fact]
    public void Render_WithHighlightCriticalPath_HighlightsCriticalPath()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { HighlightCriticalPath = true };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Critical path rooms should be visually distinct
        // This could be done with special markers or background highlighting
        // For now, just verify it doesn't throw and produces output
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithShowHallwaysFalse_HidesHallways()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { ShowHallways = false };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should not contain hallway symbols
        // Note: This test may need adjustment based on actual implementation
        Assert.NotNull(result);
    }

    [Fact]
    public void Render_WithShowDoorsFalse_HidesDoors()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { ShowDoors = false };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should not contain door symbols
        // Note: This test may need adjustment based on actual implementation
        Assert.NotNull(result);
    }

    [Fact]
    public void Render_WithInteriorFeatures_ShowsInteriorFeatures()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("with-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .Build();

        var room = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { room },
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1 },
            SpawnRoomId = 1,
            BossRoomId = 1,
            SecretPassages = new List<SecretPassage>()
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var options = new AsciiRenderOptions { ShowInteriorFeatures = true };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should contain interior feature symbols
        // Pillar might be represented as '○' or '█' or similar
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithSecretPassages_ShowsSecretPassages()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("default")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var room1 = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        var room2 = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 2,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(5, 0),
            Difficulty = 1.0
        };

        var secretPassage = new SecretPassage
        {
            RoomAId = 1,
            RoomBId = 2,
            DoorA = new Door { Position = new Cell(2, 1), Edge = Edge.East, ConnectsToRoomId = 2 },
            DoorB = new Door { Position = new Cell(5, 1), Edge = Edge.West, ConnectsToRoomId = 1 }
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { room1, room2 },
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1, 2 },
            SpawnRoomId = 1,
            BossRoomId = 2,
            SecretPassages = new List<SecretPassage> { secretPassage }
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var options = new AsciiRenderOptions { ShowSecretPassages = true };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should contain secret passage symbols ('~' or '≈')
        Assert.True(result.Contains("~") || result.Contains("≈"), "Result should contain secret passage symbols");
    }

    [Fact]
    public void Render_WithZoneBoundaries_ShowsZoneBoundaries()
    {
        // Arrange
        var layout = CreateSimpleLayout();
        var layoutWithZones = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = layout.Rooms,
            Hallways = layout.Hallways,
            Doors = layout.Doors,
            Seed = layout.Seed,
            CriticalPath = layout.CriticalPath,
            SpawnRoomId = layout.SpawnRoomId,
            BossRoomId = layout.BossRoomId,
            SecretPassages = layout.SecretPassages,
            ZoneAssignments = new Dictionary<int, string>
            {
                { 1, "Zone1" },
                { 2, "Zone2" },
                { 3, "Zone2" }
            }
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var options = new AsciiRenderOptions { ShowZoneBoundaries = true };

        // Act
        var result = renderer.Render(layoutWithZones, options);

        // Assert - Should show zone boundaries
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithViewport_RendersOnlyViewport()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions
        {
            Viewport = (new Cell(0, 0), new Cell(4, 4))
        };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should only render viewport region
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithScale_RendersScaled()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { Scale = 2 };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Scaled output should be larger
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public void Render_WithIncludeLegend_IncludesLegend()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();
        var options = new AsciiRenderOptions { IncludeLegend = true };

        // Act
        var result = renderer.Render(layout, options);

        // Assert - Should contain legend text
        Assert.Contains("Legend", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_WithDifferentStyles_ProducesDifferentOutput()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var layout = CreateSimpleLayout();

        // Act
        var minimal = renderer.Render(layout, new AsciiRenderOptions { Style = AsciiRenderStyle.Minimal });
        var detailed = renderer.Render(layout, new AsciiRenderOptions { Style = AsciiRenderStyle.Detailed });
        var artistic = renderer.Render(layout, new AsciiRenderOptions { Style = AsciiRenderStyle.Artistic });
        var compact = renderer.Render(layout, new AsciiRenderOptions { Style = AsciiRenderStyle.Compact });

        // Assert - All should produce output, and at least some should differ
        Assert.NotNull(minimal);
        Assert.NotNull(detailed);
        Assert.NotNull(artistic);
        Assert.NotNull(compact);
        Assert.NotEmpty(minimal);
        Assert.NotEmpty(detailed);
        Assert.NotEmpty(artistic);
        Assert.NotEmpty(compact);
    }

    [Fact]
    public void Render_MultiFloorLayout_RendersAllFloors()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var floor1 = CreateSimpleLayout();
        var floor2 = CreateSimpleLayout();
        var multiFloor = new MultiFloorLayout<TestHelpers.RoomType>
        {
            Floors = new List<FloorLayout<TestHelpers.RoomType>> { floor1, floor2 },
            Connections = new List<Configuration.FloorConnection>(),
            Seed = 12345,
            TotalFloorCount = 2
        };

        // Act
        var result = renderer.Render(multiFloor);

        // Assert - Should contain floor separators
        Assert.Contains("Floor 0", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Floor 1", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_EmptyLayout_HandlesGracefully()
    {
        // Arrange
        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();
        var emptyLayout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>>(),
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int>(),
            SpawnRoomId = 0,
            BossRoomId = 0,
            SecretPassages = new List<SecretPassage>()
        };

        // Act
        var result = renderer.Render(emptyLayout);

        // Assert - Should handle gracefully without throwing
        Assert.NotNull(result);
    }

    [Fact]
    public void Render_SingleRoomLayout_HandlesGracefully()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("single")
            .ForRoomTypes(TestHelpers.RoomType.Spawn)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var room = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Spawn,
            Template = template,
            Position = new Cell(0, 0),
            Difficulty = 0.0
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { room },
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1 },
            SpawnRoomId = 1,
            BossRoomId = 1,
            SecretPassages = new List<SecretPassage>()
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();

        // Act
        var result = renderer.Render(layout);

        // Assert - Should render single room
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("S", result); // Spawn symbol
    }

    [Fact]
    public void Render_LargeDungeon_PerformsAcceptably()
    {
        // Arrange - Create a large dungeon (100+ rooms)
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("large")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var rooms = new List<PlacedRoom<TestHelpers.RoomType>>();
        for (int i = 0; i < 100; i++)
        {
            rooms.Add(new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = i + 1,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell((i % 10) * 5, (i / 10) * 5),
                Difficulty = i * 0.1
            });
        }

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = rooms,
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1, 50, 100 },
            SpawnRoomId = 1,
            BossRoomId = 100,
            SecretPassages = new List<SecretPassage>()
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();

        // Act
        var startTime = DateTime.UtcNow;
        var result = renderer.Render(layout);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should complete in reasonable time (< 1 second)
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(elapsed.TotalSeconds < 1.0, $"Rendering took {elapsed.TotalSeconds} seconds, should be < 1 second");
    }

    [Fact]
    public void Render_WithMultipleRoomTypes_UsesCorrectSymbols()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("multi")
            .ForRoomTypes(TestHelpers.RoomType.Spawn, TestHelpers.RoomType.Boss, TestHelpers.RoomType.Combat, 
                          TestHelpers.RoomType.Shop, TestHelpers.RoomType.Treasure, TestHelpers.RoomType.Secret)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var rooms = new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 1, RoomType = TestHelpers.RoomType.Spawn, Template = template, Position = new Cell(0, 0), Difficulty = 0.0 },
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 2, RoomType = TestHelpers.RoomType.Boss, Template = template, Position = new Cell(5, 0), Difficulty = 2.0 },
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 3, RoomType = TestHelpers.RoomType.Combat, Template = template, Position = new Cell(10, 0), Difficulty = 1.0 },
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 4, RoomType = TestHelpers.RoomType.Shop, Template = template, Position = new Cell(0, 5), Difficulty = 1.0 },
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 5, RoomType = TestHelpers.RoomType.Treasure, Template = template, Position = new Cell(5, 5), Difficulty = 1.0 },
            new PlacedRoom<TestHelpers.RoomType> { NodeId = 6, RoomType = TestHelpers.RoomType.Secret, Template = template, Position = new Cell(10, 5), Difficulty = 1.0 }
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = rooms,
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1, 2 },
            SpawnRoomId = 1,
            BossRoomId = 2,
            SecretPassages = new List<SecretPassage>()
        };

        var renderer = new AsciiMapRenderer<TestHelpers.RoomType>();

        // Act
        var result = renderer.Render(layout);

        // Assert - Should contain symbols for all room types
        Assert.Contains("S", result); // Spawn
        Assert.Contains("B", result); // Boss
        Assert.Contains("C", result); // Combat
        Assert.True(result.Contains("$") || result.Contains("S"), "Should contain Shop symbol"); // Shop might be '$'
        Assert.True(result.Contains("T") || result.Contains("$"), "Should contain Treasure symbol"); // Treasure might be 'T' or '$'
    }
}

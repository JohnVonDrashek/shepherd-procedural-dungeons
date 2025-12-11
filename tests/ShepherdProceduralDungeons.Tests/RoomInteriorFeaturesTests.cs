using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Serialization;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for FEATURE-002: Room Templates with Interior Obstacles and Features
/// </summary>
public class RoomInteriorFeaturesTests
{
    [Fact]
    public void RoomTemplate_WithoutInteriorFeatures_HasEmptyInteriorFeatures()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("simple")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Assert
        Assert.NotNull(template.InteriorFeatures);
        Assert.Empty(template.InteriorFeatures);
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_AddsFeatureToTemplate()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("with-pillar")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(2, 2, InteriorFeature.Pillar)
            .Build();

        // Assert
        Assert.Single(template.InteriorFeatures);
        var featureCell = new Cell(2, 2);
        Assert.True(template.InteriorFeatures.ContainsKey(featureCell));
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[featureCell]);
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_ChainsCorrectly()
    {
        // Arrange & Act
        var builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("chained")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar);

        // Assert - builder should return itself for chaining
        Assert.NotNull(builder);

        var template = builder
            .AddInteriorFeature(3, 3, InteriorFeature.Wall)
            .Build();

        Assert.Equal(2, template.InteriorFeatures.Count);
    }

    [Fact]
    public void RoomTemplateBuilder_AddMultipleInteriorFeatures_AllFeaturesPreserved()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("multi-feature")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .AddInteriorFeature(3, 1, InteriorFeature.Pillar)
            .AddInteriorFeature(1, 3, InteriorFeature.Pillar)
            .AddInteriorFeature(3, 3, InteriorFeature.Pillar)
            .AddInteriorFeature(2, 2, InteriorFeature.Hazard)
            .Build();

        // Assert
        Assert.Equal(5, template.InteriorFeatures.Count);
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[new Cell(1, 1)]);
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[new Cell(3, 1)]);
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[new Cell(1, 3)]);
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[new Cell(3, 3)]);
        Assert.Equal(InteriorFeature.Hazard, template.InteriorFeatures[new Cell(2, 2)]);
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_AllFeatureTypesSupported()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("all-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .AddInteriorFeature(2, 1, InteriorFeature.Wall)
            .AddInteriorFeature(3, 1, InteriorFeature.Hazard)
            .AddInteriorFeature(1, 2, InteriorFeature.Decorative)
            .Build();

        // Assert
        Assert.Equal(4, template.InteriorFeatures.Count);
        Assert.Equal(InteriorFeature.Pillar, template.InteriorFeatures[new Cell(1, 1)]);
        Assert.Equal(InteriorFeature.Wall, template.InteriorFeatures[new Cell(2, 1)]);
        Assert.Equal(InteriorFeature.Hazard, template.InteriorFeatures[new Cell(3, 1)]);
        Assert.Equal(InteriorFeature.Decorative, template.InteriorFeatures[new Cell(1, 2)]);
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_OutsideBounds_ThrowsException()
    {
        // Arrange
        var builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("invalid")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges();

        // Act & Assert
        Assert.Throws<InvalidConfigurationException>(() =>
            builder.AddInteriorFeature(10, 10, InteriorFeature.Pillar).Build());
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_OnExteriorEdge_ThrowsException()
    {
        // Arrange - 5x5 room, so edges are at x=0, x=4, y=0, y=4
        var builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("edge-invalid")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges();

        // Act & Assert - Try placing feature on each exterior edge
        Assert.Throws<InvalidConfigurationException>(() =>
            builder.AddInteriorFeature(0, 2, InteriorFeature.Pillar).Build()); // Left edge

        builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("edge-invalid-top")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges();
        Assert.Throws<InvalidConfigurationException>(() =>
            builder.AddInteriorFeature(2, 0, InteriorFeature.Pillar).Build()); // Top edge

        builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("edge-invalid-right")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges();
        Assert.Throws<InvalidConfigurationException>(() =>
            builder.AddInteriorFeature(4, 2, InteriorFeature.Pillar).Build()); // Right edge

        builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("edge-invalid-bottom")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges();
        Assert.Throws<InvalidConfigurationException>(() =>
            builder.AddInteriorFeature(2, 4, InteriorFeature.Pillar).Build()); // Bottom edge
    }

    [Fact]
    public void PlacedRoom_GetInteriorFeatures_ConvertsToWorldCoordinates()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("with-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .AddInteriorFeature(3, 3, InteriorFeature.Hazard)
            .Build();

        var placedRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(10, 20) // Room placed at offset (10, 20)
        };

        // Act
        var features = placedRoom.GetInteriorFeatures().ToList();

        // Assert
        Assert.Equal(2, features.Count);
        
        // Template-local (1, 1) should be world (11, 21)
        var pillarFeature = features.FirstOrDefault(f => f.Feature == InteriorFeature.Pillar);
        Assert.NotNull(pillarFeature);
        Assert.Equal(new Cell(11, 21), pillarFeature.WorldCell);

        // Template-local (3, 3) should be world (13, 23)
        var hazardFeature = features.FirstOrDefault(f => f.Feature == InteriorFeature.Hazard);
        Assert.NotNull(hazardFeature);
        Assert.Equal(new Cell(13, 23), hazardFeature.WorldCell);
    }

    [Fact]
    public void PlacedRoom_GetInteriorFeatures_WithoutFeatures_ReturnsEmpty()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("no-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var placedRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(0, 0)
        };

        // Act
        var features = placedRoom.GetInteriorFeatures().ToList();

        // Assert
        Assert.Empty(features);
    }

    [Fact]
    public void FloorLayout_InteriorFeatures_AggregatesAllRoomFeatures()
    {
        // Arrange
        var template1 = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("room1")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .Build();

        var template2 = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("room2")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(2, 2, InteriorFeature.Hazard)
            .AddInteriorFeature(3, 3, InteriorFeature.Decorative)
            .Build();

        var placedRoom1 = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template1,
            Position = new Cell(0, 0)
        };

        var placedRoom2 = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 2,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template2,
            Position = new Cell(10, 10)
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { placedRoom1, placedRoom2 },
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1, 2 },
            SpawnRoomId = 1,
            BossRoomId = 2,
            SecretPassages = new List<SecretPassage>()
        };

        // Act
        var allFeatures = layout.InteriorFeatures.ToList();

        // Assert
        Assert.Equal(3, allFeatures.Count);
        
        // Room 1: (1,1) local -> (1,1) world
        Assert.Contains(allFeatures, f => f.WorldCell == new Cell(1, 1) && f.Feature == InteriorFeature.Pillar);
        
        // Room 2: (2,2) local -> (12,12) world, (3,3) local -> (13,13) world
        Assert.Contains(allFeatures, f => f.WorldCell == new Cell(12, 12) && f.Feature == InteriorFeature.Hazard);
        Assert.Contains(allFeatures, f => f.WorldCell == new Cell(13, 13) && f.Feature == InteriorFeature.Decorative);
    }

    [Fact]
    public void FloorLayout_InteriorFeatures_EmptyWhenNoRoomsHaveFeatures()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("no-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var placedRoom = new PlacedRoom<TestHelpers.RoomType>
        {
            NodeId = 1,
            RoomType = TestHelpers.RoomType.Combat,
            Template = template,
            Position = new Cell(0, 0)
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = new List<PlacedRoom<TestHelpers.RoomType>> { placedRoom },
            Hallways = new List<Hallway>(),
            Doors = new List<Door>(),
            Seed = 12345,
            CriticalPath = new List<int> { 1 },
            SpawnRoomId = 1,
            BossRoomId = 1,
            SecretPassages = new List<SecretPassage>()
        };

        // Act
        var features = layout.InteriorFeatures.ToList();

        // Assert
        Assert.Empty(features);
    }

    [Fact]
    public void FloorGenerator_InteriorFeatures_PreservedInGeneratedLayout()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("spawn")
                .ForRoomTypes(TestHelpers.RoomType.Spawn)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("boss")
                .ForRoomTypes(TestHelpers.RoomType.Boss)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(5, 5)
                .WithId("combat-with-pillars")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
                .AddInteriorFeature(3, 1, InteriorFeature.Pillar)
                .AddInteriorFeature(1, 3, InteriorFeature.Pillar)
                .AddInteriorFeature(3, 3, InteriorFeature.Pillar)
                .Build()
        };

        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 5,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = templates
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert - Find the combat room and verify it has interior features
        var combatRoom = layout.Rooms.FirstOrDefault(r => r.RoomType == TestHelpers.RoomType.Combat && r.Template.Id == "combat-with-pillars");
        Assert.NotNull(combatRoom);
        
        var features = combatRoom.GetInteriorFeatures().ToList();
        Assert.Equal(4, features.Count);
        Assert.All(features, f => Assert.Equal(InteriorFeature.Pillar, f.Feature));
    }

    [Fact]
    public void ConfigurationSerialization_InteriorFeatures_RoundtripPreservesFeatures()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("serializable")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
            .AddInteriorFeature(3, 3, InteriorFeature.Hazard)
            .Build();

        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 5,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = new List<RoomTemplate<TestHelpers.RoomType>> { template }
        };

        // Act - Serialize and deserialize
        var json = config.ToJson();
        var deserializedConfig = ConfigurationSerializationExtensions.FromJson<TestHelpers.RoomType>(json);

        // Assert
        var deserializedTemplate = deserializedConfig.Templates.First(t => t.Id == "serializable");
        Assert.Equal(2, deserializedTemplate.InteriorFeatures.Count);
        Assert.Equal(InteriorFeature.Pillar, deserializedTemplate.InteriorFeatures[new Cell(1, 1)]);
        Assert.Equal(InteriorFeature.Hazard, deserializedTemplate.InteriorFeatures[new Cell(3, 3)]);
    }

    [Fact]
    public void ConfigurationSerialization_TemplateWithoutFeatures_DeserializesCorrectly()
    {
        // Arrange - Backward compatibility test
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("no-features")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 5,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = new List<RoomTemplate<TestHelpers.RoomType>> { template }
        };

        // Act - Serialize and deserialize
        var json = config.ToJson();
        var deserializedConfig = ConfigurationSerializationExtensions.FromJson<TestHelpers.RoomType>(json);

        // Assert
        var deserializedTemplate = deserializedConfig.Templates.First(t => t.Id == "no-features");
        Assert.NotNull(deserializedTemplate.InteriorFeatures);
        Assert.Empty(deserializedTemplate.InteriorFeatures);
    }

    [Fact]
    public void FloorGenerator_InteriorFeatures_DeterministicGeneration()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("spawn")
                .ForRoomTypes(TestHelpers.RoomType.Spawn)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("boss")
                .ForRoomTypes(TestHelpers.RoomType.Boss)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(5, 5)
                .WithId("combat")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
                .AddInteriorFeature(3, 3, InteriorFeature.Hazard)
                .Build()
        };

        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 54321,
            RoomCount = 5,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = templates
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act - Generate twice with same seed
        var layout1 = generator.Generate(config);
        var layout2 = generator.Generate(config);

        // Assert - Interior features should be in same positions
        var features1 = layout1.InteriorFeatures.OrderBy(f => f.WorldCell.X).ThenBy(f => f.WorldCell.Y).ToList();
        var features2 = layout2.InteriorFeatures.OrderBy(f => f.WorldCell.X).ThenBy(f => f.WorldCell.Y).ToList();

        Assert.Equal(features1.Count, features2.Count);
        for (int i = 0; i < features1.Count; i++)
        {
            Assert.Equal(features1[i].WorldCell, features2[i].WorldCell);
            Assert.Equal(features1[i].Feature, features2[i].Feature);
        }
    }

    [Fact]
    public void RoomTemplateBuilder_AddInteriorFeature_OverwritesExistingFeature()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("overwrite")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .AddInteriorFeature(2, 2, InteriorFeature.Pillar)
            .AddInteriorFeature(2, 2, InteriorFeature.Hazard) // Overwrite same cell
            .Build();

        // Assert
        Assert.Single(template.InteriorFeatures);
        Assert.Equal(InteriorFeature.Hazard, template.InteriorFeatures[new Cell(2, 2)]);
    }
}

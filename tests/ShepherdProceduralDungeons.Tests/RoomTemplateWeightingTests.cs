using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for FEATURE-001: Room Template Weighting
/// </summary>
public class RoomTemplateWeightingTests
{
    [Fact]
    public void RoomTemplate_HasDefaultWeightOfOne()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("test")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Act & Assert
        Assert.Equal(1.0, template.Weight);
    }

    [Fact]
    public void RoomTemplateBuilder_WithWeight_SetsWeight()
    {
        // Arrange & Act
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("weighted")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(3.5)
            .Build();

        // Assert
        Assert.Equal(3.5, template.Weight);
    }

    [Fact]
    public void RoomTemplateBuilder_WithWeight_ChainsCorrectly()
    {
        // Arrange & Act
        var builder = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("chained")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(2.0);

        // Assert - builder should return itself for chaining
        Assert.NotNull(builder);
        
        var template = builder.Build();
        Assert.Equal(2.0, template.Weight);
    }

    [Fact]
    public void IncrementalSolver_UniformWeights_SelectsUniformly()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("template1")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("template2")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("template3")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act - Run many selections
        var selections = new Dictionary<string, int>();
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            selections.TryGetValue(selected.Id, out var count);
            selections[selected.Id] = count + 1;
        }

        // Assert - Each template should be selected roughly equally (within 10% tolerance)
        var expectedCount = iterations / 3.0;
        foreach (var template in templates)
        {
            var actualCount = selections.GetValueOrDefault(template.Id, 0);
            var ratio = actualCount / expectedCount;
            Assert.True(ratio >= 0.9 && ratio <= 1.1, 
                $"Template {template.Id} selected {actualCount} times, expected ~{expectedCount}");
        }
    }

    [Fact]
    public void IncrementalSolver_WeightedSelection_RespectsWeights()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("common")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(3.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("rare")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act - Run many selections
        var selections = new Dictionary<string, int>();
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            selections.TryGetValue(selected.Id, out var count);
            selections[selected.Id] = count + 1;
        }

        // Assert - Common template should appear ~3x more often than rare
        var commonCount = selections.GetValueOrDefault("common", 0);
        var rareCount = selections.GetValueOrDefault("rare", 0);
        
        // Expected ratio is 3:1, so common should be ~75% and rare ~25%
        var commonRatio = commonCount / (double)iterations;
        var rareRatio = rareCount / (double)iterations;
        
        Assert.True(commonRatio >= 0.70 && commonRatio <= 0.80, 
            $"Common template selected {commonCount} times ({commonRatio:P}), expected ~75%");
        Assert.True(rareRatio >= 0.20 && rareRatio <= 0.30, 
            $"Rare template selected {rareCount} times ({rareRatio:P}), expected ~25%");
    }

    [Fact]
    public void IncrementalSolver_ExtremeWeights_DominatesSelection()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("dominant")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(100.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("rare")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act - Run many selections
        var selections = new Dictionary<string, int>();
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            selections.TryGetValue(selected.Id, out var count);
            selections[selected.Id] = count + 1;
        }

        // Assert - Dominant template should be selected almost always (>95%)
        var dominantCount = selections.GetValueOrDefault("dominant", 0);
        var dominantRatio = dominantCount / (double)iterations;
        
        Assert.True(dominantRatio >= 0.95, 
            $"Dominant template selected {dominantCount} times ({dominantRatio:P}), expected >95%");
    }

    [Fact]
    public void RoomTemplateBuilder_ZeroWeight_ThrowsInvalidConfigurationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidConfigurationException>(() =>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("invalid")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(0.0)
                .Build();
        });
    }

    [Fact]
    public void RoomTemplateBuilder_NegativeWeight_ThrowsInvalidConfigurationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidConfigurationException>(() =>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("invalid")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(-1.0)
                .Build();
        });
    }

    [Fact]
    public void IncrementalSolver_SameSeedAndWeights_ProducesSameSelections()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("template1")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(2.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("template2")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        const int seed = 54321;
        const int iterations = 100;

        // Act - Run two sequences with same seed
        var selections1 = new List<string>();
        var rng1 = new Random(seed);
        var solver1 = new IncrementalSolver<TestHelpers.RoomType>();
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver1, TestHelpers.RoomType.Combat, templateDict, rng1);
            selections1.Add(selected.Id);
        }

        var selections2 = new List<string>();
        var rng2 = new Random(seed);
        var solver2 = new IncrementalSolver<TestHelpers.RoomType>();
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver2, TestHelpers.RoomType.Combat, templateDict, rng2);
            selections2.Add(selected.Id);
        }

        // Assert - Selections should be identical
        Assert.Equal(selections1, selections2);
    }

    [Fact]
    public void RoomTemplate_WithoutExplicitWeight_DefaultsToOne()
    {
        // Arrange & Act - Build template without calling WithWeight
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("default")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Assert
        Assert.Equal(1.0, template.Weight);
    }

    [Fact]
    public void IncrementalSolver_SingleTemplate_AlwaysSelectsIt()
    {
        // Arrange
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("only")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(5.0) // Any weight should work
            .Build();

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, new List<RoomTemplate<TestHelpers.RoomType>> { template } }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act - Run many selections
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            // Assert - Should always select the only template
            Assert.Equal("only", selected.Id);
        }
    }

    [Fact]
    public void FloorGenerator_WeightedTemplates_RespectsWeights()
    {
        // Arrange
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            // Spawn room template
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("spawn")
                .ForRoomTypes(TestHelpers.RoomType.Spawn)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            // Boss room template
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("boss")
                .ForRoomTypes(TestHelpers.RoomType.Boss)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            // Combat room templates with weights
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("common")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(3.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("rare")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build()
        };

        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 20,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = templates
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert - Count template usage in generated layout
        var templateUsage = new Dictionary<string, int>();
        foreach (var room in layout.Rooms.Where(r => r.RoomType == TestHelpers.RoomType.Combat))
        {
            var templateId = room.Template.Id;
            templateUsage.TryGetValue(templateId, out var count);
            templateUsage[templateId] = count + 1;
        }

        var commonCount = templateUsage.GetValueOrDefault("common", 0);
        var rareCount = templateUsage.GetValueOrDefault("rare", 0);
        var totalCombatRooms = commonCount + rareCount;

        if (totalCombatRooms > 0)
        {
            // Common should appear more often than rare
            Assert.True(commonCount > rareCount || (commonCount == rareCount && totalCombatRooms < 3),
                $"Common template used {commonCount} times, rare used {rareCount} times. Common should dominate.");
        }
    }

    // Helper method to access SelectTemplate for testing
    // Uses reflection to call the private SelectTemplate method
    private RoomTemplate<TestHelpers.RoomType> SelectTemplateForTesting(
        IncrementalSolver<TestHelpers.RoomType> solver,
        TestHelpers.RoomType roomType,
        IReadOnlyDictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>> templates,
        Random rng)
    {
        var method = typeof(IncrementalSolver<TestHelpers.RoomType>).GetMethod(
            "SelectTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method == null)
            throw new InvalidOperationException("SelectTemplate method not found");

        return (RoomTemplate<TestHelpers.RoomType>)method.Invoke(solver, new object[] { roomType, templates, rng })!;
    }
}


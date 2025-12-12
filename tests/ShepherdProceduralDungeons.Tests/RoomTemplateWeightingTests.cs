using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for FEATURE-003: Room Template Weighting System
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

    // Note: Zero-weight templates should be allowed (not throw on Build)
    // They are excluded from selection instead. See RoomTemplateBuilder_ZeroWeight_AllowsZeroWeight test.

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
        // Get the 3-parameter overload (without zone parameters)
        var method = typeof(IncrementalSolver<TestHelpers.RoomType>).GetMethod(
            "SelectTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new Type[] { typeof(TestHelpers.RoomType), typeof(IReadOnlyDictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>), typeof(Random) },
            null);

        if (method == null)
            throw new InvalidOperationException("SelectTemplate method not found");

        try
        {
            return (RoomTemplate<TestHelpers.RoomType>)method.Invoke(solver, new object[] { roomType, templates, rng })!;
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
        {
            // Unwrap reflection exceptions
            throw ex.InnerException;
        }
    }

    [Fact]
    public void RoomTemplateBuilder_ZeroWeight_AllowsZeroWeight()
    {
        // Arrange & Act - Zero weight should be allowed (not throw on Build)
        // This differs from current implementation which throws on Build
        var template = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("disabled")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(0.0)
            .Build();

        // Assert
        Assert.Equal(0.0, template.Weight);
    }

    [Fact]
    public void IncrementalSolver_ZeroWeightTemplate_ExcludesFromSelection()
    {
        // Arrange - Template with zero weight should be excluded from selection
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("enabled")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("disabled")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(0.0)  // Zero weight - should be excluded
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act - Run many selections
        const int iterations = 100;
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            // Assert - Zero-weight template should never be selected
            Assert.NotEqual("disabled", selected.Id);
            Assert.Equal("enabled", selected.Id);
        }
    }

    [Fact]
    public void IncrementalSolver_AllZeroWeights_ThrowsInvalidConfigurationException()
    {
        // Arrange - All templates have zero weight
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("disabled1")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(0.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("disabled2")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(0.0)
                .Build()
        };

        var templateDict = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, templates }
        };

        var rng = new Random(12345);
        var solver = new IncrementalSolver<TestHelpers.RoomType>();

        // Act & Assert - Should throw with clear error message
        var exception = Assert.Throws<InvalidConfigurationException>(() =>
        {
            SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
        });

        Assert.Contains("room type", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weight", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IncrementalSolver_ZoneSpecificTemplates_RespectsWeights()
    {
        // Arrange - Zone-specific templates with different weights
        var globalTemplate = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(3, 3)
            .WithId("global")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(1.0)
            .Build();

        var zoneCommonTemplate = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(4, 4)
            .WithId("zone-common")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(10.0)  // Much more likely in zone
            .Build();

        var zoneRareTemplate = RoomTemplateBuilder<TestHelpers.RoomType>
            .Rectangle(5, 5)
            .WithId("zone-rare")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .WithWeight(0.1)  // Rare in zone
            .Build();

        var globalTemplates = new Dictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { TestHelpers.RoomType.Combat, new[] { globalTemplate } }
        };

        var zoneTemplates = new Dictionary<string, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>
        {
            { "test-zone", new[] { zoneCommonTemplate, zoneRareTemplate } }
        };

        var zoneAssignments = new Dictionary<int, string>
        {
            { 1, "test-zone" }
        };

        // Use reflection to set zone info and call SelectTemplate with zone parameters
        var solver = new IncrementalSolver<TestHelpers.RoomType>();
        var setZoneInfoMethod = typeof(IncrementalSolver<TestHelpers.RoomType>).GetMethod(
            "SetZoneInfo",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        setZoneInfoMethod?.Invoke(solver, new object[] { zoneAssignments, zoneTemplates });

        // Get the 6-parameter SelectTemplate overload
        var selectMethod = typeof(IncrementalSolver<TestHelpers.RoomType>).GetMethod(
            "SelectTemplate",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            new Type[] 
            { 
                typeof(TestHelpers.RoomType), 
                typeof(IReadOnlyDictionary<TestHelpers.RoomType, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>), 
                typeof(Random),
                typeof(int?),
                typeof(IReadOnlyDictionary<int, string>),
                typeof(IReadOnlyDictionary<string, IReadOnlyList<RoomTemplate<TestHelpers.RoomType>>>)
            },
            null);

        if (selectMethod == null)
            throw new InvalidOperationException("SelectTemplate method with zone parameters not found");

        var rng = new Random(12345);

        // Act - Run many selections for a node in the zone
        var selections = new Dictionary<string, int>();
        const int iterations = 1000;
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                var selected = (RoomTemplate<TestHelpers.RoomType>)selectMethod.Invoke(
                    solver, 
                    new object[] { TestHelpers.RoomType.Combat, globalTemplates, rng, 1, zoneAssignments, zoneTemplates })!;
                selections.TryGetValue(selected.Id, out var count);
                selections[selected.Id] = count + 1;
            }
            catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException != null)
            {
                // Unwrap reflection exceptions
                throw ex.InnerException;
            }
        }

        // Assert - Zone-common should appear much more often than zone-rare
        var zoneCommonCount = selections.GetValueOrDefault("zone-common", 0);
        var zoneRareCount = selections.GetValueOrDefault("zone-rare", 0);
        var globalCount = selections.GetValueOrDefault("global", 0);

        // Zone-common (weight 10) should dominate over zone-rare (weight 0.1) and global (weight 1.0)
        // Expected ratio: zone-common ~90%, zone-rare ~1%, global ~9%
        var zoneCommonRatio = zoneCommonCount / (double)iterations;
        Assert.True(zoneCommonRatio >= 0.85 && zoneCommonRatio <= 0.95,
            $"Zone-common template selected {zoneCommonCount} times ({zoneCommonRatio:P}), expected ~90%");

        var zoneRareRatio = zoneRareCount / (double)iterations;
        Assert.True(zoneRareRatio >= 0.0 && zoneRareRatio <= 0.05,
            $"Zone-rare template selected {zoneRareCount} times ({zoneRareRatio:P}), expected ~1%");
    }

    [Fact]
    public void IncrementalSolver_VeryLargeWeights_WorksCorrectly()
    {
        // Arrange - Templates with very large weights (e.g., 1e6)
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("large1")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1e6)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("large2")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1e6)
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

        // Assert - Should select uniformly (both have same weight)
        var expectedCount = iterations / 2.0;
        foreach (var template in templates)
        {
            var actualCount = selections.GetValueOrDefault(template.Id, 0);
            var ratio = actualCount / expectedCount;
            Assert.True(ratio >= 0.9 && ratio <= 1.1,
                $"Template {template.Id} selected {actualCount} times, expected ~{expectedCount}");
        }
    }

    [Fact]
    public void IncrementalSolver_VerySmallWeights_WorksCorrectly()
    {
        // Arrange - Templates with very small weights (e.g., 1e-6)
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("small1")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1e-6)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("small2")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1e-6)
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

        // Assert - Should select uniformly (both have same weight)
        var expectedCount = iterations / 2.0;
        foreach (var template in templates)
        {
            var actualCount = selections.GetValueOrDefault(template.Id, 0);
            var ratio = actualCount / expectedCount;
            Assert.True(ratio >= 0.9 && ratio <= 1.1,
                $"Template {template.Id} selected {actualCount} times, expected ~{expectedCount}");
        }
    }

    [Fact]
    public void IncrementalSolver_MixedWeightDistribution_RespectsRelativeWeights()
    {
        // Arrange - Mixed weights: 10, 1, 0.1 (should produce ~90.1%, 9.0%, 0.9%)
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>
        {
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("common")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(10.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("default")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(1.0)
                .Build(),
            RoomTemplateBuilder<TestHelpers.RoomType>
                .Rectangle(3, 3)
                .WithId("rare")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .WithWeight(0.1)
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
        const int iterations = 10000;  // More iterations for better statistical accuracy
        for (int i = 0; i < iterations; i++)
        {
            var selected = SelectTemplateForTesting(solver, TestHelpers.RoomType.Combat, templateDict, rng);
            selections.TryGetValue(selected.Id, out var count);
            selections[selected.Id] = count + 1;
        }

        // Assert - Check probabilities match expected distribution
        // Expected: common = 10/11.1 ≈ 90.1%, default = 1/11.1 ≈ 9.0%, rare = 0.1/11.1 ≈ 0.9%
        var commonCount = selections.GetValueOrDefault("common", 0);
        var defaultCount = selections.GetValueOrDefault("default", 0);
        var rareCount = selections.GetValueOrDefault("rare", 0);

        var commonRatio = commonCount / (double)iterations;
        var defaultRatio = defaultCount / (double)iterations;
        var rareRatio = rareCount / (double)iterations;

        Assert.True(commonRatio >= 0.88 && commonRatio <= 0.93,
            $"Common template selected {commonCount} times ({commonRatio:P}), expected ~90.1%");
        Assert.True(defaultRatio >= 0.07 && defaultRatio <= 0.11,
            $"Default template selected {defaultCount} times ({defaultRatio:P}), expected ~9.0%");
        Assert.True(rareRatio >= 0.005 && rareRatio <= 0.015,
            $"Rare template selected {rareCount} times ({rareRatio:P}), expected ~0.9%");
    }
}


using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Serialization;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

public class DungeonThemesAndPresetsTests
{
    [Fact]
    public void DungeonTheme_Exists()
    {
        // This test verifies the DungeonTheme class exists
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = TestHelpers.CreateSimpleConfig()
        };
        
        Assert.NotNull(theme);
    }

    [Fact]
    public void DungeonTheme_HasRequiredProperties()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            Description = "A test theme",
            BaseConfig = config,
            Tags = new HashSet<string> { "test", "custom" }
        };
        
        Assert.Equal("test-theme", theme.Id);
        Assert.Equal("Test Theme", theme.Name);
        Assert.Equal("A test theme", theme.Description);
        Assert.NotNull(theme.BaseConfig);
        Assert.Equal(2, theme.Tags.Count);
        Assert.Contains("test", theme.Tags);
        Assert.Contains("custom", theme.Tags);
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithoutOverrides_ReturnsBaseConfig()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var config = theme.ToFloorConfig();
        
        Assert.NotNull(config);
        Assert.Equal(baseConfig.Seed, config.Seed);
        Assert.Equal(baseConfig.RoomCount, config.RoomCount);
        Assert.Equal(baseConfig.SpawnRoomType, config.SpawnRoomType);
        Assert.Equal(baseConfig.BossRoomType, config.BossRoomType);
        Assert.Equal(baseConfig.DefaultRoomType, config.DefaultRoomType);
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithSeedOverride_AppliesOverride()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig(seed: 11111);
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var overrides = new ThemeOverrides { Seed = 99999 };
        var config = theme.ToFloorConfig(overrides);
        
        Assert.Equal(99999, config.Seed);
        Assert.Equal(baseConfig.RoomCount, config.RoomCount); // Other properties unchanged
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithRoomCountOverride_AppliesOverride()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig(roomCount: 10);
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var overrides = new ThemeOverrides { RoomCount = 20 };
        var config = theme.ToFloorConfig(overrides);
        
        Assert.Equal(20, config.RoomCount);
        Assert.Equal(baseConfig.Seed, config.Seed); // Other properties unchanged
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithBranchingFactorOverride_AppliesOverride()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var overrides = new ThemeOverrides { BranchingFactor = 0.7f };
        var config = theme.ToFloorConfig(overrides);
        
        Assert.Equal(0.7f, config.BranchingFactor);
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithHallwayModeOverride_AppliesOverride()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var overrides = new ThemeOverrides { HallwayMode = HallwayMode.Always };
        var config = theme.ToFloorConfig(overrides);
        
        Assert.Equal(HallwayMode.Always, config.HallwayMode);
    }

    [Fact]
    public void DungeonTheme_ToFloorConfig_WithGraphAlgorithmOverride_AppliesOverride()
    {
        var baseConfig = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig
        };
        
        var overrides = new ThemeOverrides { GraphAlgorithm = GraphAlgorithm.GridBased };
        var config = theme.ToFloorConfig(overrides);
        
        Assert.Equal(GraphAlgorithm.GridBased, config.GraphAlgorithm);
    }

    [Fact]
    public void DungeonTheme_Combine_WithOtherTheme_ReturnsNewTheme()
    {
        var baseConfig1 = TestHelpers.CreateSimpleConfig(seed: 11111, roomCount: 10);
        var theme1 = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "theme1",
            Name = "Theme 1",
            BaseConfig = baseConfig1,
            Tags = new HashSet<string> { "tag1" }
        };
        
        var baseConfig2 = TestHelpers.CreateSimpleConfig(seed: 22222, roomCount: 20);
        var theme2 = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "theme2",
            Name = "Theme 2",
            BaseConfig = baseConfig2,
            Tags = new HashSet<string> { "tag2" }
        };
        
        var combined = theme1.Combine(theme2);
        
        Assert.NotNull(combined);
        // Combined theme should have theme2's properties (theme2 takes precedence)
        Assert.Equal(22222, combined.BaseConfig.Seed);
        Assert.Equal(20, combined.BaseConfig.RoomCount);
    }

    [Fact]
    public void ThemeOverrides_Exists()
    {
        var overrides = new ThemeOverrides
        {
            Seed = 12345,
            RoomCount = 15,
            BranchingFactor = 0.5f,
            HallwayMode = HallwayMode.AsNeeded,
            GraphAlgorithm = GraphAlgorithm.SpanningTree
        };
        
        Assert.NotNull(overrides);
        Assert.Equal(12345, overrides.Seed);
        Assert.Equal(15, overrides.RoomCount);
        Assert.Equal(0.5f, overrides.BranchingFactor);
        Assert.Equal(HallwayMode.AsNeeded, overrides.HallwayMode);
        Assert.Equal(GraphAlgorithm.SpanningTree, overrides.GraphAlgorithm);
    }

    [Fact]
    public void ThemePresetLibrary_Exists()
    {
        // This test verifies the ThemePresetLibrary class exists
        var library = typeof(ThemePresetLibrary<TestHelpers.RoomType>);
        Assert.NotNull(library);
    }

    [Fact]
    public void ThemePresetLibrary_Castle_ReturnsCastleTheme()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        
        Assert.NotNull(castle);
        Assert.Equal("castle", castle.Id);
        Assert.Equal("Castle", castle.Name);
    }

    [Fact]
    public void ThemePresetLibrary_Cave_ReturnsCaveTheme()
    {
        var cave = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        
        Assert.NotNull(cave);
        Assert.Equal("cave", cave.Id);
        Assert.Equal("Cave", cave.Name);
    }

    [Fact]
    public void ThemePresetLibrary_Temple_ReturnsTempleTheme()
    {
        var temple = ThemePresetLibrary<TestHelpers.RoomType>.Temple;
        
        Assert.NotNull(temple);
        Assert.Equal("temple", temple.Id);
        Assert.Equal("Temple", temple.Name);
    }

    [Fact]
    public void ThemePresetLibrary_Laboratory_ReturnsLaboratoryTheme()
    {
        var laboratory = ThemePresetLibrary<TestHelpers.RoomType>.Laboratory;
        
        Assert.NotNull(laboratory);
        Assert.Equal("laboratory", laboratory.Id);
        Assert.Equal("Laboratory", laboratory.Name);
    }

    [Fact]
    public void ThemePresetLibrary_Crypt_ReturnsCryptTheme()
    {
        var crypt = ThemePresetLibrary<TestHelpers.RoomType>.Crypt;
        
        Assert.NotNull(crypt);
        Assert.Equal("crypt", crypt.Id);
        Assert.Equal("Crypt", crypt.Name);
    }

    [Fact]
    public void ThemePresetLibrary_Forest_ReturnsForestTheme()
    {
        var forest = ThemePresetLibrary<TestHelpers.RoomType>.Forest;
        
        Assert.NotNull(forest);
        Assert.Equal("forest", forest.Id);
        Assert.Equal("Forest", forest.Name);
    }

    [Fact]
    public void ThemePresetLibrary_GetTheme_WithValidId_ReturnsTheme()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.GetTheme("castle");
        
        Assert.NotNull(theme);
        Assert.Equal("castle", theme.Id);
    }

    [Fact]
    public void ThemePresetLibrary_GetTheme_WithInvalidId_ReturnsNull()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.GetTheme("nonexistent");
        
        Assert.Null(theme);
    }

    [Fact]
    public void ThemePresetLibrary_GetAllThemes_ReturnsAllBuiltInThemes()
    {
        var themes = ThemePresetLibrary<TestHelpers.RoomType>.GetAllThemes();
        
        Assert.NotNull(themes);
        Assert.True(themes.Count >= 5, "Should have at least 5 built-in themes");
        
        var themeIds = themes.Select(t => t.Id).ToHashSet();
        Assert.Contains("castle", themeIds);
        Assert.Contains("cave", themeIds);
        Assert.Contains("temple", themeIds);
        Assert.Contains("laboratory", themeIds);
        Assert.Contains("crypt", themeIds);
    }

    [Fact]
    public void ThemePresetLibrary_GetThemesByTags_WithMatchingTags_ReturnsFilteredThemes()
    {
        var themes = ThemePresetLibrary<TestHelpers.RoomType>.GetThemesByTags("underground");
        
        Assert.NotNull(themes);
        Assert.All(themes, t => Assert.Contains("underground", t.Tags));
    }

    [Fact]
    public void ThemePresetLibrary_GetThemesByTags_WithMultipleTags_ReturnsMatchingThemes()
    {
        var themes = ThemePresetLibrary<TestHelpers.RoomType>.GetThemesByTags("structured", "indoor");
        
        Assert.NotNull(themes);
        // Themes should match at least one of the tags
        Assert.All(themes, t => 
            Assert.True(t.Tags.Contains("structured") || t.Tags.Contains("indoor")));
    }

    [Fact]
    public void ThemePresetLibrary_GetThemesByTags_WithNoMatches_ReturnsEmpty()
    {
        var themes = ThemePresetLibrary<TestHelpers.RoomType>.GetThemesByTags("nonexistent-tag");
        
        Assert.NotNull(themes);
        Assert.Empty(themes);
    }

    [Fact]
    public void Generate_WithCastleTheme_ProducesValidDungeon()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var config = theme.ToFloorConfig(new ThemeOverrides { Seed = 12345, RoomCount = 10 });
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
        Assert.Equal(TestHelpers.RoomType.Spawn, layout.Rooms.First(r => r.NodeId == layout.SpawnRoomId).RoomType);
        Assert.Equal(TestHelpers.RoomType.Boss, layout.Rooms.First(r => r.NodeId == layout.BossRoomId).RoomType);
    }

    [Fact]
    public void Generate_WithCaveTheme_ProducesValidDungeon()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        var config = theme.ToFloorConfig(new ThemeOverrides { Seed = 12345, RoomCount = 10 });
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void Generate_WithTempleTheme_ProducesValidDungeon()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Temple;
        var config = theme.ToFloorConfig(new ThemeOverrides { Seed = 12345, RoomCount = 10 });
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void Generate_WithLaboratoryTheme_ProducesValidDungeon()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Laboratory;
        var config = theme.ToFloorConfig(new ThemeOverrides { Seed = 12345, RoomCount = 10 });
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void Generate_WithCryptTheme_ProducesValidDungeon()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Crypt;
        var config = theme.ToFloorConfig(new ThemeOverrides { Seed = 12345, RoomCount = 10 });
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void CustomTheme_Creation_Works()
    {
        var customConfig = TestHelpers.CreateSimpleConfig();
        var customTheme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "my-custom-theme",
            Name = "My Custom Theme",
            Description = "A custom theme for testing",
            BaseConfig = customConfig,
            Tags = new HashSet<string> { "custom", "experimental" }
        };
        
        Assert.NotNull(customTheme);
        Assert.Equal("my-custom-theme", customTheme.Id);
        Assert.Equal("My Custom Theme", customTheme.Name);
        Assert.Equal(2, customTheme.Tags.Count);
    }

    [Fact]
    public void CustomTheme_ToFloorConfig_ProducesValidConfig()
    {
        var customConfig = TestHelpers.CreateSimpleConfig();
        var customTheme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "my-custom-theme",
            Name = "My Custom Theme",
            BaseConfig = customConfig
        };
        
        var config = customTheme.ToFloorConfig();
        
        Assert.NotNull(config);
        Assert.Equal(customConfig.Seed, config.Seed);
        Assert.Equal(customConfig.RoomCount, config.RoomCount);
    }

    [Fact]
    public void CustomTheme_Generate_ProducesValidDungeon()
    {
        var customConfig = TestHelpers.CreateSimpleConfig();
        var customTheme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "my-custom-theme",
            Name = "My Custom Theme",
            BaseConfig = customConfig
        };
        
        var config = customTheme.ToFloorConfig();
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(customConfig.RoomCount, layout.Rooms.Count);
    }

    [Fact]
    public void Theme_Combine_WithCastleAndCrypt_ProducesCombinedTheme()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var crypt = ThemePresetLibrary<TestHelpers.RoomType>.Crypt;
        
        var combined = castle.Combine(crypt);
        
        Assert.NotNull(combined);
        // Crypt should take precedence
        Assert.Equal(crypt.BaseConfig.Seed, combined.BaseConfig.Seed);
    }

    [Fact]
    public void Theme_Serialize_ProducesValidJson()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        var json = serializer.SerializeThemeToJson(theme);
        
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"id\"", json);
        Assert.Contains("\"castle\"", json);
    }

    [Fact]
    public void Theme_Deserialize_FromJson_RestoresTheme()
    {
        var originalTheme = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeThemeToJson(originalTheme);
        
        var deserialized = serializer.DeserializeThemeFromJson(json);
        
        Assert.NotNull(deserialized);
        Assert.Equal(originalTheme.Id, deserialized.Id);
        Assert.Equal(originalTheme.Name, deserialized.Name);
    }

    [Fact]
    public void Theme_RoundTrip_Serialization_ProducesIdenticalJson()
    {
        var theme = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        var json1 = serializer.SerializeThemeToJson(theme, prettyPrint: true);
        var deserialized = serializer.DeserializeThemeFromJson(json1);
        var json2 = serializer.SerializeThemeToJson(deserialized, prettyPrint: true);
        
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void Theme_Validation_WithInvalidBaseConfig_ThrowsException()
    {
        // Create a theme with an invalid base config (missing required properties)
        // This test verifies theme validation works
        Assert.Throws<InvalidConfigurationException>(() =>
        {
            var invalidConfig = new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 12345,
                RoomCount = -1, // Invalid: negative room count
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = TestHelpers.CreateDefaultTemplates()
            };
            
            var theme = new DungeonTheme<TestHelpers.RoomType>
            {
                Id = "invalid-theme",
                Name = "Invalid Theme",
                BaseConfig = invalidConfig
            };
            
            // Validation should occur when converting to FloorConfig
            theme.ToFloorConfig();
        });
    }

    [Fact]
    public void Theme_Validation_WithEmptyId_ThrowsException()
    {
        Assert.Throws<InvalidConfigurationException>(() =>
        {
            var theme = new DungeonTheme<TestHelpers.RoomType>
            {
                Id = "", // Invalid: empty ID
                Name = "Test Theme",
                BaseConfig = TestHelpers.CreateSimpleConfig()
            };
            
            theme.ToFloorConfig();
        });
    }

    [Fact]
    public void Theme_Validation_WithEmptyName_ThrowsException()
    {
        Assert.Throws<InvalidConfigurationException>(() =>
        {
            var theme = new DungeonTheme<TestHelpers.RoomType>
            {
                Id = "test-theme",
                Name = "", // Invalid: empty name
                BaseConfig = TestHelpers.CreateSimpleConfig()
            };
            
            theme.ToFloorConfig();
        });
    }

    [Fact]
    public void Theme_WithZones_IncludesZonesInConfig()
    {
        var zone = new Zone<TestHelpers.RoomType>
        {
            Id = "test-zone",
            Name = "Test Zone",
            Boundary = new ZoneBoundary.DistanceBased
            {
                MinDistance = 0,
                MaxDistance = 3
            }
        };
        
        var baseConfig = TestHelpers.CreateSimpleConfig();
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "test-theme",
            Name = "Test Theme",
            BaseConfig = baseConfig,
            Zones = new[] { zone }
        };
        
        var config = theme.ToFloorConfig();
        
        Assert.NotNull(config.Zones);
        Assert.Single(config.Zones);
        Assert.Equal("test-zone", config.Zones[0].Id);
    }

    [Fact]
    public void Theme_Combine_WithZones_MergesZones()
    {
        var zone1 = new Zone<TestHelpers.RoomType>
        {
            Id = "zone1",
            Name = "Zone 1",
            Boundary = new ZoneBoundary.DistanceBased { MinDistance = 0, MaxDistance = 2 }
        };
        
        var zone2 = new Zone<TestHelpers.RoomType>
        {
            Id = "zone2",
            Name = "Zone 2",
            Boundary = new ZoneBoundary.DistanceBased { MinDistance = 3, MaxDistance = 5 }
        };
        
        var baseConfig1 = TestHelpers.CreateSimpleConfig();
        var theme1 = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "theme1",
            Name = "Theme 1",
            BaseConfig = baseConfig1,
            Zones = new[] { zone1 }
        };
        
        var baseConfig2 = TestHelpers.CreateSimpleConfig();
        var theme2 = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "theme2",
            Name = "Theme 2",
            BaseConfig = baseConfig2,
            Zones = new[] { zone2 }
        };
        
        var combined = theme1.Combine(theme2);
        var config = combined.ToFloorConfig();
        
        Assert.NotNull(config.Zones);
        Assert.Equal(2, config.Zones.Count);
        Assert.Contains(config.Zones, z => z.Id == "zone1");
        Assert.Contains(config.Zones, z => z.Id == "zone2");
    }

    [Fact]
    public void Theme_WithTags_CanBeFiltered()
    {
        var theme = new DungeonTheme<TestHelpers.RoomType>
        {
            Id = "tagged-theme",
            Name = "Tagged Theme",
            BaseConfig = TestHelpers.CreateSimpleConfig(),
            Tags = new HashSet<string> { "indoor", "structured", "medieval" }
        };
        
        Assert.True(theme.Tags.Contains("indoor"));
        Assert.True(theme.Tags.Contains("structured"));
        Assert.True(theme.Tags.Contains("medieval"));
    }

    [Fact]
    public void ThemePresetLibrary_CastleTheme_HasAppropriateTags()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        
        Assert.NotNull(castle.Tags);
        // Castle should have tags like "structured", "indoor", "medieval"
        Assert.NotEmpty(castle.Tags);
    }

    [Fact]
    public void ThemePresetLibrary_CaveTheme_HasAppropriateTags()
    {
        var cave = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        
        Assert.NotNull(cave.Tags);
        // Cave should have tags like "organic", "underground", "natural"
        Assert.NotEmpty(cave.Tags);
    }

    [Fact]
    public void ThemePresetLibrary_CastleTheme_UsesGridBasedAlgorithm()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var config = castle.ToFloorConfig();
        
        // Castle should use GridBased algorithm for structured layout
        Assert.Equal(GraphAlgorithm.GridBased, config.GraphAlgorithm);
    }

    [Fact]
    public void ThemePresetLibrary_CaveTheme_UsesCellularAutomataAlgorithm()
    {
        var cave = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        var config = cave.ToFloorConfig();
        
        // Cave should use CellularAutomata for organic shapes
        Assert.Equal(GraphAlgorithm.CellularAutomata, config.GraphAlgorithm);
    }

    [Fact]
    public void ThemePresetLibrary_CastleTheme_HasLowBranchingFactor()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var config = castle.ToFloorConfig();
        
        // Castle should have low branching for structured feel
        Assert.True(config.BranchingFactor < 0.4f);
    }

    [Fact]
    public void ThemePresetLibrary_CaveTheme_HasHighBranchingFactor()
    {
        var cave = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        var config = cave.ToFloorConfig();
        
        // Cave should have higher branching for organic feel
        Assert.True(config.BranchingFactor > 0.3f);
    }

    [Fact]
    public void ThemePresetLibrary_CastleTheme_UsesAsNeededHallwayMode()
    {
        var castle = ThemePresetLibrary<TestHelpers.RoomType>.Castle;
        var config = castle.ToFloorConfig();
        
        // Castle should use AsNeeded hallway mode (allows flexible placement with grid-based algorithm)
        Assert.Equal(HallwayMode.AsNeeded, config.HallwayMode);
    }

    [Fact]
    public void ThemePresetLibrary_CaveTheme_UsesAsNeededHallwayMode()
    {
        var cave = ThemePresetLibrary<TestHelpers.RoomType>.Cave;
        var config = cave.ToFloorConfig();
        
        // Cave should use AsNeeded hallway mode for organic connections
        Assert.Equal(HallwayMode.AsNeeded, config.HallwayMode);
    }
}

using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Serialization;
using ShepherdProceduralDungeons.Templates;
using ShepherdProceduralDungeons.Tests;
using System.Text.Json;

namespace ShepherdProceduralDungeons.Tests;

public class ConfigurationSerializationTests
{
    [Fact]
    public void ConfigurationSerializer_Exists()
    {
        // This test verifies the ConfigurationSerializer class exists
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        Assert.NotNull(serializer);
    }

    [Fact]
    public void SerializeFloorConfig_SimpleConfig_ProducesValidJson()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        var json = serializer.SerializeToJson(config);
        
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"seed\"", json);
        Assert.Contains("\"roomCount\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_SimpleJson_ProducesValidConfig()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": []
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config);
        Assert.Equal(12345, config.Seed);
        Assert.Equal(10, config.RoomCount);
        Assert.Equal(TestHelpers.RoomType.Spawn, config.SpawnRoomType);
        Assert.Equal(TestHelpers.RoomType.Boss, config.BossRoomType);
        Assert.Equal(TestHelpers.RoomType.Combat, config.DefaultRoomType);
    }

    [Fact]
    public void RoundTrip_SimpleConfig_ProducesIdenticalJson()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        var json1 = serializer.SerializeToJson(config, prettyPrint: true);
        var deserialized = serializer.DeserializeFromJson(json1);
        var json2 = serializer.SerializeToJson(deserialized, prettyPrint: true);
        
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void SerializeFloorConfig_WithRoomRequirements_IncludesRequirements()
    {
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            RoomRequirements = new[] { (TestHelpers.RoomType.Shop, 1), (TestHelpers.RoomType.Treasure, 2) }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"roomRequirements\"", json);
        Assert.Contains("\"Shop\"", json);
        Assert.Contains("\"Treasure\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithRoomRequirements_RestoresRequirements()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""roomRequirements"": [
                { ""type"": ""Shop"", ""count"": 1 },
                { ""type"": ""Treasure"", ""count"": 2 }
            ],
            ""templates"": []
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.RoomRequirements);
        Assert.Equal(2, config.RoomRequirements.Count);
        Assert.Equal((TestHelpers.RoomType.Shop, 1), config.RoomRequirements[0]);
        Assert.Equal((TestHelpers.RoomType.Treasure, 2), config.RoomRequirements[1]);
    }

    [Fact]
    public void SerializeFloorConfig_WithRectangleTemplate_IncludesTemplate()
    {
        var template = RoomTemplateBuilder<TestHelpers.RoomType>.Rectangle(3, 3)
            .WithId("spawn-room")
            .ForRoomTypes(TestHelpers.RoomType.Spawn)
            .WithDoorsOnAllExteriorEdges()
            .Build();
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = new[] { template }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"templates\"", json);
        Assert.Contains("\"spawn-room\"", json);
        Assert.Contains("\"rectangle\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithRectangleTemplate_RestoresTemplate()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [
                {
                    ""id"": ""spawn-room"",
                    ""validRoomTypes"": [""Spawn""],
                    ""weight"": 1.0,
                    ""shape"": {
                        ""type"": ""rectangle"",
                        ""width"": 3,
                        ""height"": 3
                    },
                    ""doorEdges"": {
                        ""strategy"": ""allExteriorEdges""
                    }
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Templates);
        Assert.Single(config.Templates);
        Assert.Equal("spawn-room", config.Templates[0].Id);
        Assert.Contains(TestHelpers.RoomType.Spawn, config.Templates[0].ValidRoomTypes);
        Assert.Equal(3, config.Templates[0].Width);
        Assert.Equal(3, config.Templates[0].Height);
    }

    [Fact]
    public void SerializeFloorConfig_WithLShapeTemplate_IncludesLShape()
    {
        var template = RoomTemplateBuilder<TestHelpers.RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
            .WithId("l-shaped-combat")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = new[] { template }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"lShape\"", json);
        Assert.Contains("\"cutoutCorner\"", json);
        Assert.Contains("\"TopRight\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithLShapeTemplate_RestoresLShape()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [
                {
                    ""id"": ""l-shaped-combat"",
                    ""validRoomTypes"": [""Combat""],
                    ""weight"": 1.0,
                    ""shape"": {
                        ""type"": ""lShape"",
                        ""width"": 5,
                        ""height"": 4,
                        ""cutoutWidth"": 2,
                        ""cutoutHeight"": 2,
                        ""cutoutCorner"": ""TopRight""
                    },
                    ""doorEdges"": {
                        ""strategy"": ""allExteriorEdges""
                    }
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Templates);
        Assert.Single(config.Templates);
        Assert.Equal("l-shaped-combat", config.Templates[0].Id);
        Assert.Equal(5, config.Templates[0].Width);
        Assert.Equal(4, config.Templates[0].Height);
    }

    [Fact]
    public void SerializeFloorConfig_WithCustomTemplate_IncludesCustomCells()
    {
        var template = new RoomTemplateBuilder<TestHelpers.RoomType>()
            .WithId("custom-shape")
            .ForRoomTypes(TestHelpers.RoomType.Treasure)
            .AddCell(0, 0)
            .AddCell(1, 0)
            .AddCell(0, 1)
            .AddCell(1, 1)
            .AddCell(2, 1)
            .WithDoorsOnAllExteriorEdges()
            .Build();
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = new[] { template }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"custom\"", json);
        Assert.Contains("\"cells\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithCustomTemplate_RestoresCustomCells()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [
                {
                    ""id"": ""custom-shape"",
                    ""validRoomTypes"": [""Treasure""],
                    ""weight"": 1.0,
                    ""shape"": {
                        ""type"": ""custom"",
                        ""cells"": [
                            { ""x"": 0, ""y"": 0 },
                            { ""x"": 1, ""y"": 0 },
                            { ""x"": 0, ""y"": 1 },
                            { ""x"": 1, ""y"": 1 },
                            { ""x"": 2, ""y"": 1 }
                        ]
                    },
                    ""doorEdges"": {
                        ""strategy"": ""allExteriorEdges""
                    }
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Templates);
        Assert.Single(config.Templates);
        Assert.Equal(5, config.Templates[0].Cells.Count);
        Assert.Contains(new Cell(0, 0), config.Templates[0].Cells);
        Assert.Contains(new Cell(2, 1), config.Templates[0].Cells);
    }

    [Fact]
    public void SerializeFloorConfig_WithMinDistanceFromStartConstraint_IncludesConstraint()
    {
        var constraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 5);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"constraints\"", json);
        Assert.Contains("\"MinDistanceFromStart\"", json);
        Assert.Contains("\"Boss\"", json);
        Assert.Contains("5", json); // JSON numbers don't have quotes
    }

    [Fact]
    public void DeserializeFloorConfig_WithMinDistanceFromStartConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MinDistanceFromStart"",
                    ""targetRoomType"": ""Boss"",
                    ""minDistance"": 5
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Constraints);
        Assert.Single(config.Constraints);
        var constraint = Assert.IsType<MinDistanceFromStartConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Boss, constraint.TargetRoomType);
        Assert.Equal(5, constraint.MinDistance);
    }

    [Fact]
    public void SerializeFloorConfig_WithMaxDistanceFromStartConstraint_IncludesConstraint()
    {
        var constraint = new MaxDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 3);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MaxDistanceFromStart\"", json);
        Assert.Contains("\"maxDistance\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMaxDistanceFromStartConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MaxDistanceFromStart"",
                    ""targetRoomType"": ""Shop"",
                    ""maxDistance"": 3
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MaxDistanceFromStartConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Equal(3, constraint.MaxDistance);
    }

    [Fact]
    public void SerializeFloorConfig_WithMustBeDeadEndConstraint_IncludesConstraint()
    {
        var constraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MustBeDeadEnd\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMustBeDeadEndConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MustBeDeadEnd"",
                    ""targetRoomType"": ""Treasure""
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MustBeDeadEndConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
    }

    [Fact]
    public void SerializeFloorConfig_WithNotOnCriticalPathConstraint_IncludesConstraint()
    {
        var constraint = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"NotOnCriticalPath\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithNotOnCriticalPathConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""NotOnCriticalPath"",
                    ""targetRoomType"": ""Shop""
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<NotOnCriticalPathConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
    }

    [Fact]
    public void SerializeFloorConfig_WithMaxPerFloorConstraint_IncludesConstraint()
    {
        var constraint = new MaxPerFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 1);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MaxPerFloor\"", json);
        Assert.Contains("\"maxCount\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMaxPerFloorConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MaxPerFloor"",
                    ""targetRoomType"": ""Shop"",
                    ""maxCount"": 1
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MaxPerFloorConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Equal(1, constraint.MaxCount);
    }

    [Fact]
    public void SerializeFloorConfig_WithMinConnectionCountConstraint_IncludesConstraint()
    {
        var constraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Combat, 3);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MinConnectionCount\"", json);
        Assert.Contains("\"minConnections\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMinConnectionCountConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MinConnectionCount"",
                    ""targetRoomType"": ""Combat"",
                    ""minConnections"": 3
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MinConnectionCountConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Combat, constraint.TargetRoomType);
        Assert.Equal(3, constraint.MinConnections);
    }

    [Fact]
    public void SerializeFloorConfig_WithMaxConnectionCountConstraint_IncludesConstraint()
    {
        var constraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Combat, 2);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MaxConnectionCount\"", json);
        Assert.Contains("\"maxConnections\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMaxConnectionCountConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MaxConnectionCount"",
                    ""targetRoomType"": ""Combat"",
                    ""maxConnections"": 2
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MaxConnectionCountConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Combat, constraint.TargetRoomType);
        Assert.Equal(2, constraint.MaxConnections);
    }

    [Fact]
    public void SerializeFloorConfig_WithMustBeAdjacentToConstraint_IncludesConstraint()
    {
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, TestHelpers.RoomType.Combat);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MustBeAdjacentTo\"", json);
        Assert.Contains("\"requiredAdjacentTypes\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMustBeAdjacentToConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MustBeAdjacentTo"",
                    ""targetRoomType"": ""Shop"",
                    ""requiredAdjacentTypes"": [""Combat""]
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MustBeAdjacentToConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.RequiredAdjacentTypes);
    }

    [Fact]
    public void SerializeFloorConfig_WithMustNotBeAdjacentToConstraint_IncludesConstraint()
    {
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, TestHelpers.RoomType.Boss);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MustNotBeAdjacentTo\"", json);
        Assert.Contains("\"forbiddenAdjacentTypes\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMustNotBeAdjacentToConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MustNotBeAdjacentTo"",
                    ""targetRoomType"": ""Shop"",
                    ""forbiddenAdjacentTypes"": [""Boss""]
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MustNotBeAdjacentToConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ForbiddenAdjacentTypes);
    }

    [Fact]
    public void SerializeFloorConfig_WithMinDistanceFromRoomTypeConstraint_IncludesConstraint()
    {
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure, TestHelpers.RoomType.Spawn, 3);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MinDistanceFromRoomType\"", json);
        Assert.Contains("\"referenceRoomTypes\"", json);
        Assert.Contains("\"minDistance\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMinDistanceFromRoomTypeConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MinDistanceFromRoomType"",
                    ""targetRoomType"": ""Treasure"",
                    ""referenceRoomTypes"": [""Spawn""],
                    ""minDistance"": 3
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
        Assert.Contains(TestHelpers.RoomType.Spawn, constraint.ReferenceRoomTypes);
        Assert.Equal(3, constraint.MinDistance);
    }

    [Fact]
    public void SerializeFloorConfig_WithMaxDistanceFromRoomTypeConstraint_IncludesConstraint()
    {
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, TestHelpers.RoomType.Spawn, 2);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MaxDistanceFromRoomType\"", json);
        Assert.Contains("\"maxDistance\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMaxDistanceFromRoomTypeConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MaxDistanceFromRoomType"",
                    ""targetRoomType"": ""Shop"",
                    ""referenceRoomTypes"": [""Spawn""],
                    ""maxDistance"": 2
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Contains(TestHelpers.RoomType.Spawn, constraint.ReferenceRoomTypes);
        Assert.Equal(2, constraint.MaxDistance);
    }

    [Fact]
    public void SerializeFloorConfig_WithMustComeBeforeConstraint_IncludesConstraint()
    {
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, TestHelpers.RoomType.Boss);
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"MustComeBefore\"", json);
        Assert.Contains("\"referenceRoomTypes\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithMustComeBeforeConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""MustComeBefore"",
                    ""targetRoomType"": ""Shop"",
                    ""referenceRoomTypes"": [""Boss""]
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<MustComeBeforeConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ReferenceRoomTypes);
    }

    [Fact]
    public void SerializeFloorConfig_WithOnlyInZoneConstraint_IncludesConstraint()
    {
        var constraint = new OnlyInZoneConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, "castle");
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { constraint }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"OnlyInZone\"", json);
        Assert.Contains("\"zoneId\"", json);
        Assert.Contains("\"castle\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithOnlyInZoneConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""OnlyInZone"",
                    ""targetRoomType"": ""Shop"",
                    ""zoneId"": ""castle""
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<OnlyInZoneConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Equal("castle", constraint.ZoneId);
    }

    [Fact]
    public void SerializeFloorConfig_WithCompositeConstraint_IncludesConstraint()
    {
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 5);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = new[] { composite }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"CompositeConstraint\"", json);
        Assert.Contains("\"operator\"", json);
        Assert.Contains("\"And\"", json);
        Assert.Contains("\"constraints\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithCompositeConstraint_RestoresConstraint()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""constraints"": [
                {
                    ""type"": ""CompositeConstraint"",
                    ""targetRoomType"": ""Boss"",
                    ""operator"": ""And"",
                    ""constraints"": [
                        {
                            ""type"": ""MinDistanceFromStart"",
                            ""targetRoomType"": ""Boss"",
                            ""minDistance"": 5
                        },
                        {
                            ""type"": ""MustBeDeadEnd"",
                            ""targetRoomType"": ""Boss""
                        }
                    ]
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        var constraint = Assert.IsType<CompositeConstraint<TestHelpers.RoomType>>(config.Constraints[0]);
        Assert.Equal(TestHelpers.RoomType.Boss, constraint.TargetRoomType);
        Assert.Equal(CompositionOperator.And, constraint.Operator);
        Assert.Equal(2, constraint.Constraints.Count);
    }

    [Fact]
    public void SerializeFloorConfig_WithDistanceBasedZone_IncludesZone()
    {
        var zone = new Zone<TestHelpers.RoomType>
        {
            Id = "castle",
            Name = "Castle",
            Boundary = new ZoneBoundary.DistanceBased
            {
                MinDistance = 0,
                MaxDistance = 3
            }
        };
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Zones = new[] { zone }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"zones\"", json);
        Assert.Contains("\"castle\"", json);
        Assert.Contains("\"DistanceBased\"", json);
        Assert.Contains("\"minDistance\"", json);
        Assert.Contains("\"maxDistance\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithDistanceBasedZone_RestoresZone()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""zones"": [
                {
                    ""id"": ""castle"",
                    ""name"": ""Castle"",
                    ""boundary"": {
                        ""type"": ""DistanceBased"",
                        ""minDistance"": 0,
                        ""maxDistance"": 3
                    }
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Zones);
        Assert.Single(config.Zones);
        Assert.Equal("castle", config.Zones[0].Id);
        Assert.Equal("Castle", config.Zones[0].Name);
        var boundary = Assert.IsType<ZoneBoundary.DistanceBased>(config.Zones[0].Boundary);
        Assert.Equal(0, boundary.MinDistance);
        Assert.Equal(3, boundary.MaxDistance);
    }

    [Fact]
    public void SerializeFloorConfig_WithCriticalPathBasedZone_IncludesZone()
    {
        var zone = new Zone<TestHelpers.RoomType>
        {
            Id = "dungeon",
            Name = "Dungeon",
            Boundary = new ZoneBoundary.CriticalPathBased
            {
                StartPercent = 0.0f,
                EndPercent = 0.5f
            }
        };
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Zones = new[] { zone }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"CriticalPathBased\"", json);
        Assert.Contains("\"startPercent\"", json);
        Assert.Contains("\"endPercent\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithCriticalPathBasedZone_RestoresZone()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""zones"": [
                {
                    ""id"": ""dungeon"",
                    ""name"": ""Dungeon"",
                    ""boundary"": {
                        ""type"": ""CriticalPathBased"",
                        ""startPercent"": 0.0,
                        ""endPercent"": 0.5
                    }
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.Zones);
        var boundary = Assert.IsType<ZoneBoundary.CriticalPathBased>(config.Zones![0].Boundary);
        Assert.Equal(0.0f, boundary.StartPercent);
        Assert.Equal(0.5f, boundary.EndPercent);
    }

    [Fact]
    public void SerializeFloorConfig_WithSecretPassageConfig_IncludesSecretPassageConfig()
    {
        var secretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
        {
            Count = 3,
            MaxSpatialDistance = 5,
            AllowedRoomTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Treasure },
            ForbiddenRoomTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Boss },
            AllowCriticalPathConnections = true,
            AllowGraphConnectedRooms = false
        };
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = secretPassageConfig
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.Contains("\"secretPassageConfig\"", json);
        Assert.Contains("\"count\"", json);
        Assert.Contains("\"maxSpatialDistance\"", json);
        Assert.Contains("\"allowedRoomTypes\"", json);
        Assert.Contains("\"forbiddenRoomTypes\"", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithSecretPassageConfig_RestoresSecretPassageConfig()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": [],
            ""secretPassageConfig"": {
                ""count"": 3,
                ""maxSpatialDistance"": 5,
                ""allowedRoomTypes"": [""Treasure""],
                ""forbiddenRoomTypes"": [""Boss""],
                ""allowCriticalPathConnections"": true,
                ""allowGraphConnectedRooms"": false
            }
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var config = serializer.DeserializeFromJson(json);
        
        Assert.NotNull(config.SecretPassageConfig);
        Assert.Equal(3, config.SecretPassageConfig.Count);
        Assert.Equal(5, config.SecretPassageConfig.MaxSpatialDistance);
        Assert.Contains(TestHelpers.RoomType.Treasure, config.SecretPassageConfig.AllowedRoomTypes);
        Assert.Contains(TestHelpers.RoomType.Boss, config.SecretPassageConfig.ForbiddenRoomTypes);
        Assert.True(config.SecretPassageConfig.AllowCriticalPathConnections);
        Assert.False(config.SecretPassageConfig.AllowGraphConnectedRooms);
    }

    [Fact]
    public void SerializeMultiFloorConfig_SimpleConfig_ProducesValidJson()
    {
        var floor1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var floor2 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 12);
        var connection = new FloorConnection
        {
            FromFloorIndex = 0,
            FromRoomNodeId = 4,
            ToFloorIndex = 1,
            ToRoomNodeId = 0,
            Type = ConnectionType.StairsDown
        };
        
        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            Floors = new[] { floor1, floor2 },
            Connections = new[] { connection }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("\"floors\"", json);
        Assert.Contains("\"connections\"", json);
    }

    [Fact]
    public void DeserializeMultiFloorConfig_SimpleJson_ProducesValidConfig()
    {
        var json = @"{
            ""seed"": 12345,
            ""floors"": [
                {
                    ""seed"": 12345,
                    ""roomCount"": 10,
                    ""spawnRoomType"": ""Spawn"",
                    ""bossRoomType"": ""Boss"",
                    ""defaultRoomType"": ""Combat"",
                    ""templates"": []
                },
                {
                    ""seed"": 12345,
                    ""roomCount"": 12,
                    ""spawnRoomType"": ""Spawn"",
                    ""bossRoomType"": ""Boss"",
                    ""defaultRoomType"": ""Combat"",
                    ""templates"": []
                }
            ],
            ""connections"": [
                {
                    ""fromFloorIndex"": 0,
                    ""fromRoomNodeId"": 4,
                    ""toFloorIndex"": 1,
                    ""toRoomNodeId"": 0,
                    ""type"": ""StairsDown""
                }
            ]
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        MultiFloorConfig<TestHelpers.RoomType> config = serializer.DeserializeMultiFloorConfigFromJson(json);
        
        Assert.NotNull(config);
        Assert.Equal(12345, config.Seed);
        Assert.Equal(2, config.Floors.Count);
        Assert.Single(config.Connections);
        Assert.Equal(ConnectionType.StairsDown, config.Connections[0].Type);
    }

    [Fact]
    public void RoundTrip_MultiFloorConfig_ProducesIdenticalJson()
    {
        var floor1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var floor2 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 12);
        var connection = new FloorConnection
        {
            FromFloorIndex = 0,
            FromRoomNodeId = 4,
            ToFloorIndex = 1,
            ToRoomNodeId = 0,
            Type = ConnectionType.StairsDown
        };
        
        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            Floors = new[] { floor1, floor2 },
            Connections = new[] { connection }
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json1 = serializer.SerializeToJson(config, prettyPrint: true);
        MultiFloorConfig<TestHelpers.RoomType> deserialized = serializer.DeserializeMultiFloorConfigFromJson(json1);
        var json2 = serializer.SerializeToJson(deserialized, prettyPrint: true);
        
        Assert.Equal(json1, json2);
    }

    [Fact]
    public void DeserializeFloorConfig_InvalidJson_ThrowsException()
    {
        var invalidJson = "{ invalid json }";
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        Assert.ThrowsAny<Exception>(() => serializer.DeserializeFromJson(invalidJson));
    }

    [Fact]
    public void DeserializeFloorConfig_MissingRequiredField_ThrowsInvalidConfigurationException()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        Assert.Throws<InvalidConfigurationException>(() => serializer.DeserializeFromJson(json));
    }

    [Fact]
    public void DeserializeFloorConfig_InvalidEnumValue_ThrowsInvalidConfigurationException()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""InvalidRoomType"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": []
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        Assert.Throws<InvalidConfigurationException>(() => serializer.DeserializeFromJson(json));
    }

    [Fact]
    public void SerializeFloorConfig_PrettyPrint_ProducesFormattedJson()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        
        var prettyJson = serializer.SerializeToJson(config, prettyPrint: true);
        var compactJson = serializer.SerializeToJson(config, prettyPrint: false);
        
        Assert.Contains("\n", prettyJson);
        Assert.DoesNotContain("\n", compactJson);
    }

    [Fact]
    public void SerializeFloorConfig_WithCustomOptions_UsesOptions()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var json = serializer.SerializeToJson(config, options);
        
        Assert.NotNull(json);
        Assert.Contains("\n", json);
    }

    [Fact]
    public void DeserializeFloorConfig_WithCustomOptions_UsesOptions()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": []
        }";
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        var config = serializer.DeserializeFromJson(json, options);
        
        Assert.NotNull(config);
        Assert.Equal(12345, config.Seed);
    }

    [Fact]
    public void FloorConfig_ToJsonExtension_Works()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var json = config.ToJson();
        
        Assert.NotNull(json);
        Assert.NotEmpty(json);
    }

    [Fact]
    public void FloorConfig_FromJsonExtension_Works()
    {
        var json = @"{
            ""seed"": 12345,
            ""roomCount"": 10,
            ""spawnRoomType"": ""Spawn"",
            ""bossRoomType"": ""Boss"",
            ""defaultRoomType"": ""Combat"",
            ""templates"": []
        }";
        
        var config = ConfigurationSerializationExtensions.FromJson<TestHelpers.RoomType>(json);
        
        Assert.NotNull(config);
        Assert.Equal(12345, config.Seed);
    }

    [Fact]
    public void FloorConfig_SaveToFileExtension_Works()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var tempFile = Path.GetTempFileName();
        
        try
        {
            config.SaveToFile(tempFile);
            Assert.True(File.Exists(tempFile));
            var content = File.ReadAllText(tempFile);
            Assert.NotEmpty(content);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void FloorConfig_LoadFromFileExtension_Works()
    {
        var config = TestHelpers.CreateSimpleConfig();
        var tempFile = Path.GetTempFileName();
        
        try
        {
            config.SaveToFile(tempFile);
            var loaded = ConfigurationSerializationExtensions.LoadFromFile<TestHelpers.RoomType>(tempFile);
            
            Assert.NotNull(loaded);
            Assert.Equal(config.Seed, loaded.Seed);
            Assert.Equal(config.RoomCount, loaded.RoomCount);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void SerializeFloorConfig_LargeConfiguration_HandlesManyTemplates()
    {
        var templates = new List<RoomTemplate<TestHelpers.RoomType>>();
        for (int i = 0; i < 50; i++)
        {
            templates.Add(RoomTemplateBuilder<TestHelpers.RoomType>.Rectangle(3, 3)
                .WithId($"template-{i}")
                .ForRoomTypes(TestHelpers.RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .Build());
        }
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = templates
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.NotNull(json);
        Assert.Contains("\"template-0\"", json);
        Assert.Contains("\"template-49\"", json);
    }

    [Fact]
    public void SerializeFloorConfig_LargeConfiguration_HandlesManyConstraints()
    {
        var constraints = new List<IConstraint<TestHelpers.RoomType>>();
        for (int i = 0; i < 20; i++)
        {
            constraints.Add(new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Combat, i));
        }
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            Constraints = constraints
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json = serializer.SerializeToJson(config);
        
        Assert.NotNull(json);
        Assert.Contains("\"constraints\"", json);
    }

    [Fact]
    public void RoundTrip_ComplexConfig_ProducesIdenticalJson()
    {
        var template1 = RoomTemplateBuilder<TestHelpers.RoomType>.Rectangle(3, 3)
            .WithId("spawn-room")
            .ForRoomTypes(TestHelpers.RoomType.Spawn)
            .WithDoorsOnAllExteriorEdges()
            .Build();
        
        var template2 = RoomTemplateBuilder<TestHelpers.RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
            .WithId("l-shaped-combat")
            .ForRoomTypes(TestHelpers.RoomType.Combat)
            .WithDoorsOnAllExteriorEdges()
            .Build();
        
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 5);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var constraint3 = new MaxPerFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 1);
        
        var zone = new Zone<TestHelpers.RoomType>
        {
            Id = "castle",
            Name = "Castle",
            Boundary = new ZoneBoundary.DistanceBased
            {
                MinDistance = 0,
                MaxDistance = 3
            }
        };
        
        var secretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
        {
            Count = 2,
            MaxSpatialDistance = 5
        };
        
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 12,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[] { (TestHelpers.RoomType.Shop, 1), (TestHelpers.RoomType.Treasure, 2) },
            Templates = new RoomTemplate<TestHelpers.RoomType>[] { template1, template2 },
            Constraints = new IConstraint<TestHelpers.RoomType>[] { constraint1, constraint2, constraint3 },
            BranchingFactor = 0.4f,
            HallwayMode = HallwayMode.Always,
            Zones = new[] { zone },
            SecretPassageConfig = secretPassageConfig
        };
        
        var serializer = new ConfigurationSerializer<TestHelpers.RoomType>();
        var json1 = serializer.SerializeToJson(config, prettyPrint: true);
        var deserialized = serializer.DeserializeFromJson(json1);
        var json2 = serializer.SerializeToJson(deserialized, prettyPrint: true);
        
        Assert.Equal(json1, json2);
        
        // Verify all properties were restored
        Assert.Equal(config.Seed, deserialized.Seed);
        Assert.Equal(config.RoomCount, deserialized.RoomCount);
        Assert.Equal(config.BranchingFactor, deserialized.BranchingFactor);
        Assert.Equal(config.HallwayMode, deserialized.HallwayMode);
        Assert.Equal(config.RoomRequirements.Count, deserialized.RoomRequirements.Count);
        Assert.Equal(config.Templates.Count, deserialized.Templates.Count);
        Assert.Equal(config.Constraints.Count, deserialized.Constraints.Count);
        Assert.NotNull(deserialized.Zones);
        Assert.Equal(config.Zones.Count, deserialized.Zones.Count);
        Assert.NotNull(deserialized.SecretPassageConfig);
        Assert.Equal(config.SecretPassageConfig.Count, deserialized.SecretPassageConfig.Count);
    }
}

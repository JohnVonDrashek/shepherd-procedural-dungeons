using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

public class MultiFloorDungeonSupportTests
{
    [Fact]
    public void Generate_BasicMultiFloor_GeneratesAllFloors()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 7),
            TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 6)
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = floorConfigs[0].RoomCount - 1, // Last room on floor 0
                ToFloorIndex = 1,
                ToRoomNodeId = 0, // First room on floor 1
                Type = ConnectionType.StairsDown
            },
            new FloorConnection
            {
                FromFloorIndex = 1,
                FromRoomNodeId = floorConfigs[1].RoomCount - 1,
                ToFloorIndex = 2,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Floors.Count);
        Assert.Equal(3, result.TotalFloorCount);
        Assert.Equal(2, result.Connections.Count);
        Assert.Equal(12345, result.Seed);

        // Verify each floor has correct structure
        for (int i = 0; i < result.Floors.Count; i++)
        {
            var floor = result.Floors[i];
            Assert.NotNull(floor);
            Assert.NotEmpty(floor.Rooms);
            Assert.Equal(floorConfigs[i].RoomCount, floor.Rooms.Count);
        }
    }

    [Fact]
    public void Generate_TeleporterConnections_ConnectsNonAdjacentFloors()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 54321, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 54321, roomCount: 6),
            TestHelpers.CreateSimpleConfig(seed: 54321, roomCount: 7)
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 2,
                ToFloorIndex = 2, // Skip floor 1
                ToRoomNodeId = 3,
                Type = ConnectionType.Teleporter
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 54321,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Connections);
        var connection = result.Connections[0];
        Assert.Equal(0, connection.FromFloorIndex);
        Assert.Equal(2, connection.ToFloorIndex);
        Assert.Equal(ConnectionType.Teleporter, connection.Type);
    }

    [Fact]
    public void Generate_FloorSpecificConstraints_BossOnlyOnFinalFloor()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 99999,
                RoomCount = 5,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                Constraints = new List<IConstraint<TestHelpers.RoomType>>
                {
                    new NotOnFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, new[] { 0 })
                }
            },
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 99999,
                RoomCount = 6,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                Constraints = new List<IConstraint<TestHelpers.RoomType>>
                {
                    new OnlyOnFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, new[] { 1 })
                }
            }
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 4,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 99999,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        
        // Floor 0 should not have boss
        var floor0 = result.Floors[0];
        Assert.DoesNotContain(floor0.Rooms, r => r.RoomType == TestHelpers.RoomType.Boss);
        
        // Floor 1 should have boss
        var floor1 = result.Floors[1];
        Assert.Contains(floor1.Rooms, r => r.RoomType == TestHelpers.RoomType.Boss);
        Assert.Equal(floor1.BossRoomId, floor1.Rooms.First(r => r.RoomType == TestHelpers.RoomType.Boss).NodeId);
    }

    [Fact]
    public void Generate_BranchingFloors_CreatesMultiplePaths()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 11111, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 11111, roomCount: 4), // Floor 2A
            TestHelpers.CreateSimpleConfig(seed: 11111, roomCount: 4), // Floor 2B
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 2,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            },
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 3,
                ToFloorIndex = 2,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 11111,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Floors.Count);
        Assert.Equal(2, result.Connections.Count);
        
        // Both connections should originate from floor 0
        var floor0Connections = result.Connections.Where(c => c.FromFloorIndex == 0).ToList();
        Assert.Equal(2, floor0Connections.Count);
        Assert.Contains(floor0Connections, c => c.ToFloorIndex == 1);
        Assert.Contains(floor0Connections, c => c.ToFloorIndex == 2);
    }

    [Fact]
    public void Generate_SameSeed_ProducesIdenticalLayout()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs1 = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 77777, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 77777, roomCount: 6)
        };
        var floorConfigs2 = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 77777, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 77777, roomCount: 6)
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 4,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config1 = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 77777,
            Floors = floorConfigs1,
            Connections = connections
        };

        var config2 = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 77777,
            Floors = floorConfigs2,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result1 = generator.Generate(config1);
        var result2 = generator.Generate(config2);

        // Assert
        Assert.Equal(result1.TotalFloorCount, result2.TotalFloorCount);
        Assert.Equal(result1.Connections.Count, result2.Connections.Count);
        
        for (int i = 0; i < result1.Floors.Count; i++)
        {
            var floor1 = result1.Floors[i];
            var floor2 = result2.Floors[i];
            Assert.Equal(floor1.Rooms.Count, floor2.Rooms.Count);
            Assert.Equal(floor1.SpawnRoomId, floor2.SpawnRoomId);
            Assert.Equal(floor1.BossRoomId, floor2.BossRoomId);
        }
    }

    [Fact]
    public void Generate_SingleFloor_BackwardCompatible()
    {
        // Arrange
        var singleFloorConfig = TestHelpers.CreateSimpleConfig(seed: 88888, roomCount: 8);
        var multiFloorConfig = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 88888,
            Floors = new[] { singleFloorConfig },
            Connections = Array.Empty<FloorConnection>()
        };

        var multiFloorGenerator = new MultiFloorGenerator<TestHelpers.RoomType>();
        var singleFloorGenerator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var multiFloorResult = multiFloorGenerator.Generate(multiFloorConfig);
        var singleFloorResult = singleFloorGenerator.Generate(singleFloorConfig);

        // Assert
        Assert.NotNull(multiFloorResult);
        Assert.Single(multiFloorResult.Floors);
        Assert.Empty(multiFloorResult.Connections);
        
        var multiFloor = multiFloorResult.Floors[0];
        Assert.Equal(singleFloorResult.Rooms.Count, multiFloor.Rooms.Count);
        Assert.Equal(singleFloorResult.SpawnRoomId, multiFloor.SpawnRoomId);
        Assert.Equal(singleFloorResult.BossRoomId, multiFloor.BossRoomId);
    }

    [Fact]
    public void Generate_InvalidConnection_ThrowsException()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 22222, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 22222, roomCount: 6)
        };

        // Connection references non-existent room (room ID 999 doesn't exist)
        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 999, // Invalid room ID
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 22222,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act & Assert
        Assert.Throws<InvalidConfigurationException>(() => generator.Generate(config));
    }

    [Fact]
    public void Generate_EmptyFloorConnections_HandlesGracefully()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 33333, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 33333, roomCount: 6),
            TestHelpers.CreateSimpleConfig(seed: 33333, roomCount: 7)
        };

        // No connections between floors
        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 33333,
            Floors = floorConfigs,
            Connections = Array.Empty<FloorConnection>()
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Floors.Count);
        Assert.Empty(result.Connections);
        
        // Each floor should still be valid
        foreach (var floor in result.Floors)
        {
            Assert.NotEmpty(floor.Rooms);
            Assert.NotNull(floor.GetRoom(floor.SpawnRoomId));
            Assert.NotNull(floor.GetRoom(floor.BossRoomId));
        }
    }

    [Fact]
    public void Generate_MinFloorConstraint_RestrictsRoomPlacement()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 44444,
                RoomCount = 5,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                Constraints = new List<IConstraint<TestHelpers.RoomType>>
                {
                    new MinFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure, minFloor: 1)
                }
            },
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 44444,
                RoomCount = 6,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                RoomRequirements = new[] { (TestHelpers.RoomType.Treasure, 2) },
                Constraints = new List<IConstraint<TestHelpers.RoomType>>()
            }
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 4,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 44444,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        
        // Floor 0 should not have treasure rooms (minFloor is 1, so floor 0 is excluded)
        var floor0 = result.Floors[0];
        Assert.DoesNotContain(floor0.Rooms, r => r.RoomType == TestHelpers.RoomType.Treasure);
        
        // Floor 1 should have treasure rooms
        var floor1 = result.Floors[1];
        var treasureRooms = floor1.Rooms.Where(r => r.RoomType == TestHelpers.RoomType.Treasure).ToList();
        Assert.Equal(2, treasureRooms.Count);
    }

    [Fact]
    public void Generate_MaxFloorConstraint_RestrictsRoomPlacement()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 55555,
                RoomCount = 5,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                RoomRequirements = new[] { (TestHelpers.RoomType.Shop, 1) },
                Constraints = new List<IConstraint<TestHelpers.RoomType>>
                {
                    new MaxFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, maxFloor: 0)
                }
            },
            new FloorConfig<TestHelpers.RoomType>
            {
                Seed = 55555,
                RoomCount = 6,
                SpawnRoomType = TestHelpers.RoomType.Spawn,
                BossRoomType = TestHelpers.RoomType.Boss,
                DefaultRoomType = TestHelpers.RoomType.Combat,
                Templates = templates,
                Constraints = new List<IConstraint<TestHelpers.RoomType>>
                {
                    new NotOnFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, new[] { 1 })
                }
            }
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 4,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 55555,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        
        // Floor 0 should have shop (maxFloor is 0, so floor 0 is allowed)
        var floor0 = result.Floors[0];
        Assert.Contains(floor0.Rooms, r => r.RoomType == TestHelpers.RoomType.Shop);
        
        // Floor 1 should not have shop
        var floor1 = result.Floors[1];
        Assert.DoesNotContain(floor1.Rooms, r => r.RoomType == TestHelpers.RoomType.Shop);
    }

    [Fact]
    public void Generate_StairsUpAndDown_HasCorrectConnectionTypes()
    {
        // Arrange
        var templates = TestHelpers.CreateDefaultTemplates();
        var floorConfigs = new[]
        {
            TestHelpers.CreateSimpleConfig(seed: 66666, roomCount: 5),
            TestHelpers.CreateSimpleConfig(seed: 66666, roomCount: 6)
        };

        var connections = new[]
        {
            new FloorConnection
            {
                FromFloorIndex = 0,
                FromRoomNodeId = 4,
                ToFloorIndex = 1,
                ToRoomNodeId = 0,
                Type = ConnectionType.StairsDown
            },
            new FloorConnection
            {
                FromFloorIndex = 1,
                FromRoomNodeId = 5,
                ToFloorIndex = 0,
                ToRoomNodeId = 2,
                Type = ConnectionType.StairsUp
            }
        };

        var config = new MultiFloorConfig<TestHelpers.RoomType>
        {
            Seed = 66666,
            Floors = floorConfigs,
            Connections = connections
        };

        var generator = new MultiFloorGenerator<TestHelpers.RoomType>();

        // Act
        var result = generator.Generate(config);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Connections.Count);
        
        var downStairs = result.Connections.FirstOrDefault(c => c.Type == ConnectionType.StairsDown);
        var upStairs = result.Connections.FirstOrDefault(c => c.Type == ConnectionType.StairsUp);
        
        Assert.NotNull(downStairs);
        Assert.NotNull(upStairs);
        Assert.Equal(0, downStairs.FromFloorIndex);
        Assert.Equal(1, upStairs.FromFloorIndex);
    }
}

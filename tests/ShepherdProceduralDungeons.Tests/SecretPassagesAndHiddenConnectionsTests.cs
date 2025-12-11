using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for FEATURE-001: Secret Passages and Hidden Connections
/// </summary>
public class SecretPassagesAndHiddenConnectionsTests
{
    [Fact]
    public void SecretPassageConfig_HasDefaultValues()
    {
        // Arrange & Act
        var config = new SecretPassageConfig<TestHelpers.RoomType>();

        // Assert
        Assert.Equal(0, config.Count);
        Assert.Equal(5, config.MaxSpatialDistance);
        Assert.NotNull(config.AllowedRoomTypes);
        Assert.Empty(config.AllowedRoomTypes);
        Assert.NotNull(config.ForbiddenRoomTypes);
        Assert.Empty(config.ForbiddenRoomTypes);
        Assert.True(config.AllowCriticalPathConnections);
        Assert.False(config.AllowGraphConnectedRooms);
    }

    [Fact]
    public void SecretPassageConfig_CanSetProperties()
    {
        // Arrange & Act
        var allowedTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Treasure };
        var forbiddenTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Boss };
        
        var config = new SecretPassageConfig<TestHelpers.RoomType>
        {
            Count = 3,
            MaxSpatialDistance = 7,
            AllowedRoomTypes = allowedTypes,
            ForbiddenRoomTypes = forbiddenTypes,
            AllowCriticalPathConnections = false,
            AllowGraphConnectedRooms = true
        };

        // Assert
        Assert.Equal(3, config.Count);
        Assert.Equal(7, config.MaxSpatialDistance);
        Assert.Equal(allowedTypes, config.AllowedRoomTypes);
        Assert.Equal(forbiddenTypes, config.ForbiddenRoomTypes);
        Assert.False(config.AllowCriticalPathConnections);
        Assert.True(config.AllowGraphConnectedRooms);
    }

    [Fact]
    public void SecretPassage_CanBeCreated()
    {
        // Arrange
        var doorA = new Door
        {
            Position = new Cell(10, 10),
            Edge = Edge.North
        };
        var doorB = new Door
        {
            Position = new Cell(15, 15),
            Edge = Edge.South
        };

        // Act
        var secretPassage = new SecretPassage
        {
            RoomAId = 1,
            RoomBId = 2,
            DoorA = doorA,
            DoorB = doorB
        };

        // Assert
        Assert.Equal(1, secretPassage.RoomAId);
        Assert.Equal(2, secretPassage.RoomBId);
        Assert.Equal(doorA, secretPassage.DoorA);
        Assert.Equal(doorB, secretPassage.DoorB);
        Assert.Null(secretPassage.Hallway);
        Assert.False(secretPassage.RequiresHallway);
    }

    [Fact]
    public void SecretPassage_WithHallway_RequiresHallwayIsTrue()
    {
        // Arrange
        var doorA = new Door
        {
            Position = new Cell(10, 10),
            Edge = Edge.North
        };
        var doorB = new Door
        {
            Position = new Cell(15, 15),
            Edge = Edge.South
        };
        var hallway = new Hallway
        {
            Id = 1,
            Segments = Array.Empty<HallwaySegment>(),
            DoorA = doorA,
            DoorB = doorB
        };

        // Act
        var secretPassage = new SecretPassage
        {
            RoomAId = 1,
            RoomBId = 2,
            DoorA = doorA,
            DoorB = doorB,
            Hallway = hallway
        };

        // Assert
        Assert.NotNull(secretPassage.Hallway);
        Assert.True(secretPassage.RequiresHallway);
    }

    [Fact]
    public void FloorConfig_CanSetSecretPassageConfig()
    {
        // Arrange
        var secretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
        {
            Count = 3,
            MaxSpatialDistance = 5
        };

        // Act
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

        // Assert
        Assert.NotNull(config.SecretPassageConfig);
        Assert.Equal(3, config.SecretPassageConfig.Count);
        Assert.Equal(5, config.SecretPassageConfig.MaxSpatialDistance);
    }

    [Fact]
    public void FloorLayout_HasSecretPassagesProperty()
    {
        // Arrange
        var rooms = new List<PlacedRoom<TestHelpers.RoomType>>();
        var secretPassages = new List<SecretPassage>();

        // Act
        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = rooms,
            Hallways = Array.Empty<Hallway>(),
            Doors = Array.Empty<Door>(),
            Seed = 12345,
            CriticalPath = Array.Empty<int>(),
            SpawnRoomId = 0,
            BossRoomId = 1,
            SecretPassages = secretPassages
        };

        // Assert
        Assert.NotNull(layout.SecretPassages);
        Assert.Equal(secretPassages, layout.SecretPassages);
    }

    [Fact]
    public void FloorLayout_GetSecretPassagesForRoom_ReturnsCorrectPassages()
    {
        // Arrange
        var door1 = new Door { Position = new Cell(0, 0), Edge = Edge.North };
        var door2 = new Door { Position = new Cell(1, 1), Edge = Edge.South };
        var door3 = new Door { Position = new Cell(2, 2), Edge = Edge.East };

        var passage1 = new SecretPassage
        {
            RoomAId = 1,
            RoomBId = 2,
            DoorA = door1,
            DoorB = door2
        };

        var passage2 = new SecretPassage
        {
            RoomAId = 1,
            RoomBId = 3,
            DoorA = door1,
            DoorB = door3
        };

        var passage3 = new SecretPassage
        {
            RoomAId = 2,
            RoomBId = 4,
            DoorA = door2,
            DoorB = door3
        };

        var layout = new FloorLayout<TestHelpers.RoomType>
        {
            Rooms = Array.Empty<PlacedRoom<TestHelpers.RoomType>>(),
            Hallways = Array.Empty<Hallway>(),
            Doors = Array.Empty<Door>(),
            Seed = 12345,
            CriticalPath = Array.Empty<int>(),
            SpawnRoomId = 0,
            BossRoomId = 1,
            SecretPassages = new[] { passage1, passage2, passage3 }
        };

        // Act
        var passagesForRoom1 = layout.GetSecretPassagesForRoom(1).ToList();
        var passagesForRoom2 = layout.GetSecretPassagesForRoom(2).ToList();
        var passagesForRoom5 = layout.GetSecretPassagesForRoom(5).ToList();

        // Assert
        Assert.Equal(2, passagesForRoom1.Count);
        Assert.Contains(passage1, passagesForRoom1);
        Assert.Contains(passage2, passagesForRoom1);

        Assert.Equal(2, passagesForRoom2.Count);
        Assert.Contains(passage1, passagesForRoom2);
        Assert.Contains(passage3, passagesForRoom2);

        Assert.Empty(passagesForRoom5);
    }

    [Fact]
    public void Generate_WithSecretPassages_GeneratesCorrectCount()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 3,
                MaxSpatialDistance = 5
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        Assert.Equal(3, layout.SecretPassages.Count);
    }

    [Fact]
    public void Generate_WithSecretPassages_DoesNotAffectMainGraph()
    {
        // Arrange
        var configWithoutSecretPassages = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var configWithSecretPassages = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 5
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layoutWithout = generator.Generate(configWithoutSecretPassages);
        var layoutWith = generator.Generate(configWithSecretPassages);

        // Assert
        // Critical path should be identical
        Assert.Equal(layoutWithout.CriticalPath, layoutWith.CriticalPath);
        Assert.Equal(layoutWithout.SpawnRoomId, layoutWith.SpawnRoomId);
        Assert.Equal(layoutWithout.BossRoomId, layoutWith.BossRoomId);
        
        // Room count should be identical
        Assert.Equal(layoutWithout.Rooms.Count, layoutWith.Rooms.Count);
        
        // Secret passages should exist in the second layout
        Assert.NotNull(layoutWith.SecretPassages);
        Assert.NotEmpty(layoutWith.SecretPassages);
    }

    [Fact]
    public void Generate_WithRoomTypeConstraints_OnlyConnectsAllowedTypes()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[]
            {
                (TestHelpers.RoomType.Treasure, 3),
                (TestHelpers.RoomType.Shop, 2)
            },
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 10,
                AllowedRoomTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Treasure }
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        foreach (var passage in layout.SecretPassages)
        {
            var roomA = layout.GetRoom(passage.RoomAId);
            var roomB = layout.GetRoom(passage.RoomBId);
            
            Assert.NotNull(roomA);
            Assert.NotNull(roomB);
            Assert.Equal(TestHelpers.RoomType.Treasure, roomA.RoomType);
            Assert.Equal(TestHelpers.RoomType.Treasure, roomB.RoomType);
        }
    }

    [Fact]
    public void Generate_WithMaxSpatialDistance_RespectsDistanceLimit()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 10,
                MaxSpatialDistance = 3
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        foreach (var passage in layout.SecretPassages)
        {
            var roomA = layout.GetRoom(passage.RoomAId);
            var roomB = layout.GetRoom(passage.RoomBId);
            
            Assert.NotNull(roomA);
            Assert.NotNull(roomB);
            
            // Calculate Manhattan distance between room centers
            var distance = Math.Abs(roomA.Position.X - roomB.Position.X) + 
                          Math.Abs(roomA.Position.Y - roomB.Position.Y);
            
            Assert.True(distance <= 3, $"Secret passage exceeds MaxSpatialDistance: {distance}");
        }
    }

    [Fact]
    public void Generate_WithAllowGraphConnectedRoomsFalse_OnlyConnectsDisconnectedRooms()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 10,
                AllowGraphConnectedRooms = false
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        
        // Build a set of graph-connected room pairs
        var graphConnections = new HashSet<(int, int)>();
        foreach (var hallway in layout.Hallways)
        {
            // Find rooms connected by this hallway
            var doorA = hallway.DoorA;
            var doorB = hallway.DoorB;
            
            var roomA = layout.Rooms.FirstOrDefault(r => r.GetWorldCells().Contains(doorA.Position));
            var roomB = layout.Rooms.FirstOrDefault(r => r.GetWorldCells().Contains(doorB.Position));
            
            if (roomA != null && roomB != null)
            {
                var pair1 = (Math.Min(roomA.NodeId, roomB.NodeId), Math.Max(roomA.NodeId, roomB.NodeId));
                graphConnections.Add(pair1);
            }
        }

        // Verify secret passages don't connect graph-connected rooms
        foreach (var passage in layout.SecretPassages)
        {
            var pair = (Math.Min(passage.RoomAId, passage.RoomBId), Math.Max(passage.RoomAId, passage.RoomBId));
            Assert.DoesNotContain(pair, graphConnections);
        }
    }

    [Fact]
    public void Generate_WithAllowCriticalPathConnectionsFalse_ExcludesCriticalPathRooms()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 10,
                AllowCriticalPathConnections = false
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        var criticalPathSet = layout.CriticalPath.ToHashSet();
        
        foreach (var passage in layout.SecretPassages)
        {
            Assert.DoesNotContain(passage.RoomAId, criticalPathSet);
            Assert.DoesNotContain(passage.RoomBId, criticalPathSet);
        }
    }

    [Fact]
    public void Generate_WithNonAdjacentRooms_CreatesHallways()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 10
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        
        // At least some secret passages should have hallways if rooms are not adjacent
        var passagesWithHallways = layout.SecretPassages.Where(sp => sp.RequiresHallway).ToList();
        
        // Verify hallways are properly connected
        foreach (var passage in passagesWithHallways)
        {
            Assert.NotNull(passage.Hallway);
            Assert.NotNull(passage.Hallway.DoorA);
            Assert.NotNull(passage.Hallway.DoorB);
        }
    }

    [Fact]
    public void Generate_SameSeed_GeneratesIdenticalSecretPassages()
    {
        // Arrange
        var config1 = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 3,
                MaxSpatialDistance = 5
            }
        };

        var config2 = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 3,
                MaxSpatialDistance = 5
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        // Assert
        Assert.Equal(layout1.SecretPassages.Count, layout2.SecretPassages.Count);
        
        var passages1 = layout1.SecretPassages.OrderBy(sp => (sp.RoomAId, sp.RoomBId)).ToList();
        var passages2 = layout2.SecretPassages.OrderBy(sp => (sp.RoomAId, sp.RoomBId)).ToList();
        
        for (int i = 0; i < passages1.Count; i++)
        {
            Assert.Equal(passages1[i].RoomAId, passages2[i].RoomAId);
            Assert.Equal(passages1[i].RoomBId, passages2[i].RoomBId);
        }
    }

    [Fact]
    public void Generate_WithZeroCount_GeneratesNoSecretPassages()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 0,
                MaxSpatialDistance = 5
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        Assert.Empty(layout.SecretPassages);
    }

    [Fact]
    public void Generate_WithNullSecretPassageConfig_GeneratesNoSecretPassages()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = null
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        Assert.Empty(layout.SecretPassages);
    }

    [Fact]
    public void Generate_WithForbiddenRoomTypes_ExcludesForbiddenTypes()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 5,
                MaxSpatialDistance = 10,
                ForbiddenRoomTypes = new HashSet<TestHelpers.RoomType> { TestHelpers.RoomType.Boss }
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        foreach (var passage in layout.SecretPassages)
        {
            var roomA = layout.GetRoom(passage.RoomAId);
            var roomB = layout.GetRoom(passage.RoomBId);
            
            Assert.NotNull(roomA);
            Assert.NotNull(roomB);
            Assert.NotEqual(TestHelpers.RoomType.Boss, roomA.RoomType);
            Assert.NotEqual(TestHelpers.RoomType.Boss, roomB.RoomType);
        }
    }

    [Fact]
    public void Generate_SecretPassages_HaveValidDoors()
    {
        // Arrange
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 15,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            SecretPassageConfig = new SecretPassageConfig<TestHelpers.RoomType>
            {
                Count = 3,
                MaxSpatialDistance = 5
            }
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        // Act
        var layout = generator.Generate(config);

        // Assert
        Assert.NotNull(layout.SecretPassages);
        foreach (var passage in layout.SecretPassages)
        {
            var roomA = layout.GetRoom(passage.RoomAId);
            var roomB = layout.GetRoom(passage.RoomBId);
            
            Assert.NotNull(roomA);
            Assert.NotNull(roomB);
            
            // Verify doors are on room boundaries
            Assert.True(roomA.GetWorldCells().Contains(passage.DoorA.Position) ||
                        roomA.GetWorldCells().Any(c => c.North == passage.DoorA.Position || 
                                                      c.South == passage.DoorA.Position ||
                                                      c.East == passage.DoorA.Position ||
                                                      c.West == passage.DoorA.Position));
            
            Assert.True(roomB.GetWorldCells().Contains(passage.DoorB.Position) ||
                        roomB.GetWorldCells().Any(c => c.North == passage.DoorB.Position || 
                                                      c.South == passage.DoorB.Position ||
                                                      c.East == passage.DoorB.Position ||
                                                      c.West == passage.DoorB.Position));
        }
    }
}

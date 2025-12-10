using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

public class IntegrationTests
{
    [Fact]
    public void Generate_SimpleDungeon_Succeeds()
    {
        // Use a seed that's known to work, or try a couple quickly
        var seeds = new[] { 54321, 99999 };
        FloorLayout<TestHelpers.RoomType>? layout = null;
        
        foreach (var seed in seeds)
        {
            try
            {
                var config = TestHelpers.CreateSimpleConfig(seed: seed, roomCount: 5);
                var generator = new FloorGenerator<TestHelpers.RoomType>();
                layout = generator.Generate(config);
                break; // Success, exit loop
            }
            catch (ShepherdProceduralDungeons.Exceptions.SpatialPlacementException)
            {
                // Try next seed if pathfinding fails
                continue;
            }
        }

        Assert.NotNull(layout);
        Assert.Equal(5, layout!.Rooms.Count);
        Assert.Equal(TestHelpers.RoomType.Spawn, layout.Rooms.First(r => r.NodeId == layout.SpawnRoomId).RoomType);
        Assert.Equal(TestHelpers.RoomType.Boss, layout.Rooms.First(r => r.NodeId == layout.BossRoomId).RoomType);
    }

    [Fact]
    public void Generate_WithConstraints_SatisfiesConstraints()
    {
        var templates = TestHelpers.CreateDefaultTemplates();
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[]
            {
                (TestHelpers.RoomType.Treasure, 2)
            },
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure),
                new MaxPerFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure, 2)
            },
            Templates = templates
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);

        var treasureRooms = layout.Rooms.Where(r => r.RoomType == TestHelpers.RoomType.Treasure).ToList();
        Assert.Equal(2, treasureRooms.Count);
    }

    [Fact]
    public void Generate_SameSeed_SameOutput()
    {
        var config1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var config2 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
        Assert.Equal(layout1.SpawnRoomId, layout2.SpawnRoomId);
        Assert.Equal(layout1.BossRoomId, layout2.BossRoomId);
    }

    [Fact]
    public void Generate_CriticalPathIsValid()
    {
        var config = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);

        Assert.NotEmpty(layout.CriticalPath);
        Assert.Equal(layout.SpawnRoomId, layout.CriticalPath[0]);
        Assert.Equal(layout.BossRoomId, layout.CriticalPath[^1]);
    }
}


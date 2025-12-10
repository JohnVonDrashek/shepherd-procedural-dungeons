using ShepherdProceduralDungeons.Configuration;

namespace ShepherdProceduralDungeons.Tests;

public class SeedDeterminismTests
{
    [Fact]
    public void SameSeed_SameOutput()
    {
        var config1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var config2 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        // Same seed should produce identical results
        Assert.Equal(layout1.Seed, layout2.Seed);
        Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
        Assert.Equal(layout1.SpawnRoomId, layout2.SpawnRoomId);
        Assert.Equal(layout1.BossRoomId, layout2.BossRoomId);
        Assert.Equal(layout1.Hallways.Count, layout2.Hallways.Count);

        // Verify room positions match
        var rooms1 = layout1.Rooms.OrderBy(r => r.NodeId).ToList();
        var rooms2 = layout2.Rooms.OrderBy(r => r.NodeId).ToList();

        for (int i = 0; i < rooms1.Count; i++)
        {
            Assert.Equal(rooms1[i].NodeId, rooms2[i].NodeId);
            Assert.Equal(rooms1[i].Position, rooms2[i].Position);
            Assert.Equal(rooms1[i].RoomType, rooms2[i].RoomType);
        }

        // Verify critical path matches
        Assert.Equal(layout1.CriticalPath, layout2.CriticalPath);
    }

    [Fact]
    public void DifferentSeed_DifferentOutput()
    {
        var config1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var config2 = TestHelpers.CreateSimpleConfig(seed: 54321, roomCount: 10);

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        // Different seeds should produce different results
        // At least one of: positions, room types, or graph structure should differ
        bool positionsDiffer = layout1.Rooms.Any(r1 => 
            layout2.Rooms.FirstOrDefault(r2 => r2.NodeId == r1.NodeId)?.Position != r1.Position);

        bool bossDiffers = layout1.BossRoomId != layout2.BossRoomId;

        // At least one aspect should differ
        Assert.True(positionsDiffer || bossDiffers, "Different seeds should produce different layouts");
    }

    [Fact]
    public void SameSeed_DifferentConfig_DifferentOutput()
    {
        var config1 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 10);
        var config2 = TestHelpers.CreateSimpleConfig(seed: 12345, roomCount: 15);

        var generator = new FloorGenerator<TestHelpers.RoomType>();

        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        // Different configs should produce different results
        Assert.NotEqual(layout1.Rooms.Count, layout2.Rooms.Count);
    }
}


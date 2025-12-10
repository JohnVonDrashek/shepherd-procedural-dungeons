using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Helper methods for creating test configurations and data.
/// </summary>
public static class TestHelpers
{
    public enum RoomType
    {
        Spawn,
        Boss,
        Combat,
        Shop,
        Treasure,
        Secret
    }

    public static FloorConfig<RoomType> CreateSimpleConfig(int seed = 12345, int roomCount = 10)
    {
        return new FloorConfig<RoomType>
        {
            Seed = seed,
            RoomCount = roomCount,
            SpawnRoomType = RoomType.Spawn,
            BossRoomType = RoomType.Boss,
            DefaultRoomType = RoomType.Combat,
            Templates = CreateDefaultTemplates(),
            Constraints = new List<IConstraint<RoomType>>()
        };
    }

    public static List<RoomTemplate<RoomType>> CreateDefaultTemplates()
    {
        return new List<RoomTemplate<RoomType>>
        {
            RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
                .WithId("default")
                .ForRoomTypes(RoomType.Spawn, RoomType.Boss, RoomType.Combat, RoomType.Shop, RoomType.Treasure, RoomType.Secret)
                .WithDoorsOnAllExteriorEdges()
                .Build()
        };
    }
}


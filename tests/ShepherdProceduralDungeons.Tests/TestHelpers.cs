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

    /// <summary>
    /// Timeout constants for different test categories.
    /// </summary>
    public static class Timeout
    {
        /// <summary>
        /// Default timeout for unit tests (5 seconds).
        /// </summary>
        public const int UnitTestMs = 5000;

        /// <summary>
        /// Default timeout for integration tests (30 seconds).
        /// </summary>
        public const int IntegrationTestMs = 30000;

        /// <summary>
        /// Default timeout for performance tests (60 seconds).
        /// </summary>
        public const int PerformanceTestMs = 60000;

        /// <summary>
        /// Global default timeout for tests without explicit timeout (30 seconds).
        /// </summary>
        public const int DefaultMs = 30000;
    }

    /// <summary>
    /// Helper method to get the appropriate timeout for integration tests.
    /// </summary>
    /// <returns>The timeout value in milliseconds for integration tests.</returns>
    public static int GetIntegrationTestTimeout()
    {
        return Timeout.IntegrationTestMs;
    }

    /// <summary>
    /// Helper method to get the appropriate timeout for unit tests.
    /// </summary>
    /// <returns>The timeout value in milliseconds for unit tests.</returns>
    public static int GetUnitTestTimeout()
    {
        return Timeout.UnitTestMs;
    }
}


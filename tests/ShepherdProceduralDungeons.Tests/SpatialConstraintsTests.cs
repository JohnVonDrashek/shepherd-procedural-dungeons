using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for spatial constraints system that validates room placement based on 2D spatial positions.
/// </summary>
public class SpatialConstraintsTests
{
    #region MustBeInQuadrantConstraint Tests

    [Fact]
    public void MustBeInQuadrantConstraint_TopRightQuadrant_ValidatesCorrectly()
    {
        // Arrange: Shop must be in top-right quadrant
        var constraint = new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            Quadrant.TopRight);

        var placedRooms = CreatePlacedRoomsWithBounds();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position in top-right quadrant (positive X, negative Y relative to center)
        var topRightPosition = new Cell(10, -10);
        Assert.True(constraint.IsValidSpatially(topRightPosition, template, placedRooms, graph, assignments));

        // Position in top-left quadrant (negative X, negative Y)
        var topLeftPosition = new Cell(-10, -10);
        Assert.False(constraint.IsValidSpatially(topLeftPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MustBeInQuadrantConstraint_MultipleQuadrants_ValidatesCorrectly()
    {
        // Arrange: Boss can be in top-left OR top-right
        var constraint = new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss,
            Quadrant.TopLeft | Quadrant.TopRight);

        var placedRooms = CreatePlacedRoomsWithBounds();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var template = TestHelpers.CreateDefaultTemplates().First();

        var topLeftPosition = new Cell(-10, -10);
        var topRightPosition = new Cell(10, -10);
        var bottomLeftPosition = new Cell(-10, 10);

        Assert.True(constraint.IsValidSpatially(topLeftPosition, template, placedRooms, graph, assignments));
        Assert.True(constraint.IsValidSpatially(topRightPosition, template, placedRooms, graph, assignments));
        Assert.False(constraint.IsValidSpatially(bottomLeftPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MustBeInQuadrantConstraint_CenterQuadrant_ValidatesCorrectly()
    {
        // Arrange: Special room must be in center
        var constraint = new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            Quadrant.Center);

        var placedRooms = CreatePlacedRoomsWithBounds();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position near center (within center threshold)
        var centerPosition = new Cell(2, -2);
        Assert.True(constraint.IsValidSpatially(centerPosition, template, placedRooms, graph, assignments));

        // Position far from center
        var farPosition = new Cell(20, -20);
        Assert.False(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));
    }

    #endregion

    #region MinSpatialDistanceFromRoomTypeConstraint Tests

    [Fact]
    public void MinSpatialDistanceFromRoomTypeConstraint_SingleReferenceType_ValidatesCorrectly()
    {
        // Arrange: Treasure must be at least 10 cells away from Boss rooms
        var constraint = new MinSpatialDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss,
            minDistance: 10);

        var placedRooms = CreatePlacedRoomsWithBoss();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 1, TestHelpers.RoomType.Boss } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position far enough away (15 cells Manhattan distance)
        var farPosition = new Cell(15, 0);
        Assert.True(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));

        // Position too close (5 cells Manhattan distance)
        var closePosition = new Cell(5, 0);
        Assert.False(constraint.IsValidSpatially(closePosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MinSpatialDistanceFromRoomTypeConstraint_MultipleReferenceTypes_ValidatesCorrectly()
    {
        // Arrange: Secret must be at least 8 cells away from Boss OR Combat rooms
        var constraint = new MinSpatialDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            minDistance: 8,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Combat);

        var placedRooms = CreatePlacedRoomsWithBossAndCombat();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 1, TestHelpers.RoomType.Boss },
            { 2, TestHelpers.RoomType.Combat }
        };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position far from both (10 cells from Boss, 12 from Combat)
        var farPosition = new Cell(10, 0);
        Assert.True(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));

        // Position too close to Combat (5 cells)
        var closeToCombat = new Cell(5, 0);
        Assert.False(constraint.IsValidSpatially(closeToCombat, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MinSpatialDistanceFromRoomTypeConstraint_UsesManhattanDistance()
    {
        // Arrange: Test that distance calculation uses Manhattan distance
        var constraint = new MinSpatialDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss,
            minDistance: 5);

        var placedRooms = CreatePlacedRoomsWithBoss();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 1, TestHelpers.RoomType.Boss } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position at exactly 5 cells minimum distance (accounting for 3x3 room size)
        // Boss room at (0,0) occupies cells (0,0) to (2,2), rightmost at (2,2)
        // For 5-cell minimum distance, treasure room leftmost cell should be at (7,2) or further
        // So anchor at (7,2) gives cells (7,2) to (9,4), distance from (7,2) to (2,2) = 5
        var exactDistance = new Cell(7, 2);
        Assert.True(constraint.IsValidSpatially(exactDistance, template, placedRooms, graph, assignments));

        // Position too close (4 cells minimum distance)
        // Anchor at (6,2) gives cells (6,2) to (8,4), distance from (6,2) to (2,2) = 4
        var tooClose = new Cell(6, 2);
        Assert.False(constraint.IsValidSpatially(tooClose, template, placedRooms, graph, assignments));
    }

    #endregion

    #region MaxSpatialDistanceFromRoomTypeConstraint Tests

    [Fact]
    public void MaxSpatialDistanceFromRoomTypeConstraint_SingleReferenceType_ValidatesCorrectly()
    {
        // Arrange: Secret must be within 5 cells of Boss room
        var constraint = new MaxSpatialDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            maxDistance: 5);

        var placedRooms = CreatePlacedRoomsWithBoss();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 1, TestHelpers.RoomType.Boss } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position within range (3 cells)
        var closePosition = new Cell(3, 0);
        Assert.True(constraint.IsValidSpatially(closePosition, template, placedRooms, graph, assignments));

        // Position too far (10 cells)
        var farPosition = new Cell(10, 0);
        Assert.False(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MaxSpatialDistanceFromRoomTypeConstraint_MultipleReferenceTypes_ValidatesCorrectly()
    {
        // Arrange: Secret must be within 6 cells of Boss OR Shop
        var constraint = new MaxSpatialDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            maxDistance: 6,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Shop);

        var placedRooms = CreatePlacedRoomsWithBossAndShop();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 1, TestHelpers.RoomType.Boss },
            { 2, TestHelpers.RoomType.Shop }
        };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position within range of Shop (4 cells)
        var nearShop = new Cell(4, 0);
        Assert.True(constraint.IsValidSpatially(nearShop, template, placedRooms, graph, assignments));

        // Position too far from both (15 cells from Boss, 12 from Shop)
        var farFromBoth = new Cell(15, 0);
        Assert.False(constraint.IsValidSpatially(farFromBoth, template, placedRooms, graph, assignments));
    }

    #endregion

    #region MustFormSpatialClusterConstraint Tests

    [Fact]
    public void MustFormSpatialClusterConstraint_ValidCluster_ValidatesCorrectly()
    {
        // Arrange: Combat rooms must form a cluster within 5 cells of each other
        var constraint = new MustFormSpatialClusterConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            clusterRadius: 5,
            minClusterSize: 3);

        var placedRooms = CreatePlacedRoomsInCluster();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 1, TestHelpers.RoomType.Combat },
            { 2, TestHelpers.RoomType.Combat },
            { 3, TestHelpers.RoomType.Combat }
        };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position that maintains cluster (within 5 cells of existing combat rooms)
        var clusterPosition = new Cell(3, 0);
        Assert.True(constraint.IsValidSpatially(clusterPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MustFormSpatialClusterConstraint_BreaksCluster_ValidatesCorrectly()
    {
        // Arrange: Combat rooms must form a cluster within 5 cells
        var constraint = new MustFormSpatialClusterConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            clusterRadius: 5,
            minClusterSize: 3);

        var placedRooms = CreatePlacedRoomsInCluster();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 1, TestHelpers.RoomType.Combat },
            { 2, TestHelpers.RoomType.Combat },
            { 3, TestHelpers.RoomType.Combat }
        };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position that breaks cluster (more than 5 cells away)
        var isolatedPosition = new Cell(20, 0);
        Assert.False(constraint.IsValidSpatially(isolatedPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MustFormSpatialClusterConstraint_FirstRoomInCluster_AlwaysValid()
    {
        // Arrange: First room of type should always be valid (no existing cluster to check)
        var constraint = new MustFormSpatialClusterConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            clusterRadius: 5,
            minClusterSize: 3);

        var placedRooms = new List<PlacedRoom<TestHelpers.RoomType>>(); // No combat rooms yet
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var template = TestHelpers.CreateDefaultTemplates().First();

        // First room should be valid regardless of position
        var anyPosition = new Cell(100, 100);
        Assert.True(constraint.IsValidSpatially(anyPosition, template, placedRooms, graph, assignments));
    }

    #endregion

    #region MustBeInRegionConstraint Tests

    [Fact]
    public void MustBeInRegionConstraint_WithinBounds_ValidatesCorrectly()
    {
        // Arrange: Shop must be within rectangular region (0,0) to (20,20)
        var constraint = new MustBeInRegionConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            minX: 0,
            maxX: 20,
            minY: 0,
            maxY: 20);

        var placedRooms = new List<PlacedRoom<TestHelpers.RoomType>>();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position within bounds
        var validPosition = new Cell(10, 10);
        Assert.True(constraint.IsValidSpatially(validPosition, template, placedRooms, graph, assignments));

        // Position outside bounds (X too large)
        var invalidX = new Cell(25, 10);
        Assert.False(constraint.IsValidSpatially(invalidX, template, placedRooms, graph, assignments));

        // Position outside bounds (Y too large)
        var invalidY = new Cell(10, 25);
        Assert.False(constraint.IsValidSpatially(invalidY, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MustBeInRegionConstraint_ConsidersRoomSize_ValidatesCorrectly()
    {
        // Arrange: Large room template must fit entirely within region
        var constraint = new MustBeInRegionConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss,
            minX: 0,
            maxX: 10,
            minY: 0,
            maxY: 10);

        var placedRooms = new List<PlacedRoom<TestHelpers.RoomType>>();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Large template (5x5)
        var largeTemplate = RoomTemplateBuilder<TestHelpers.RoomType>.Rectangle(5, 5)
            .WithId("large")
            .ForRoomTypes(TestHelpers.RoomType.Boss)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Position where room fits (anchor at 0,0, room extends to 4,4)
        var fitsPosition = new Cell(0, 0);
        Assert.True(constraint.IsValidSpatially(fitsPosition, largeTemplate, placedRooms, graph, assignments));

        // Position where room extends beyond bounds (anchor at 7,7, room extends to 11,11)
        var exceedsPosition = new Cell(7, 7);
        Assert.False(constraint.IsValidSpatially(exceedsPosition, largeTemplate, placedRooms, graph, assignments));
    }

    #endregion

    #region MinSpatialDistanceFromStartConstraint Tests

    [Fact]
    public void MinSpatialDistanceFromStartConstraint_ValidatesCorrectly()
    {
        // Arrange: Treasure must be at least 10 cells from spawn (spatial distance, not graph distance)
        var constraint = new MinSpatialDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            minDistance: 10);

        var placedRooms = CreatePlacedRoomsWithSpawn();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 0, TestHelpers.RoomType.Spawn } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position far enough away (15 cells)
        var farPosition = new Cell(15, 0);
        Assert.True(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));

        // Position too close (5 cells)
        var closePosition = new Cell(5, 0);
        Assert.False(constraint.IsValidSpatially(closePosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MinSpatialDistanceFromStartConstraint_UsesManhattanDistance()
    {
        // Arrange: Test Manhattan distance calculation
        var constraint = new MinSpatialDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            minDistance: 5);

        var placedRooms = CreatePlacedRoomsWithSpawn();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 0, TestHelpers.RoomType.Spawn } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position at exactly 5 cells minimum distance (accounting for 3x3 room size)
        // Spawn room at (0,0) occupies cells (0,0) to (2,2), rightmost at (2,2)
        // For 5-cell minimum distance, treasure room leftmost cell should be at (7,2) or further
        // So anchor at (7,2) gives cells (7,2) to (9,4), distance from (7,2) to (2,2) = 5
        var exactDistance = new Cell(7, 2);
        Assert.True(constraint.IsValidSpatially(exactDistance, template, placedRooms, graph, assignments));

        // Position too close (4 cells minimum distance)
        // Anchor at (6,2) gives cells (6,2) to (8,4), distance from (6,2) to (2,2) = 4
        var tooClose = new Cell(6, 2);
        Assert.False(constraint.IsValidSpatially(tooClose, template, placedRooms, graph, assignments));
    }

    #endregion

    #region MaxSpatialDistanceFromStartConstraint Tests

    [Fact]
    public void MaxSpatialDistanceFromStartConstraint_ValidatesCorrectly()
    {
        // Arrange: Shop must be within 8 cells of spawn
        var constraint = new MaxSpatialDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            maxDistance: 8);

        var placedRooms = CreatePlacedRoomsWithSpawn();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 0, TestHelpers.RoomType.Spawn } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position within range (5 cells)
        var closePosition = new Cell(5, 0);
        Assert.True(constraint.IsValidSpatially(closePosition, template, placedRooms, graph, assignments));

        // Position too far (15 cells)
        var farPosition = new Cell(15, 0);
        Assert.False(constraint.IsValidSpatially(farPosition, template, placedRooms, graph, assignments));
    }

    [Fact]
    public void MaxSpatialDistanceFromStartConstraint_AtExactDistance_ValidatesCorrectly()
    {
        // Arrange: Test boundary condition
        var constraint = new MaxSpatialDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            maxDistance: 10);

        var placedRooms = CreatePlacedRoomsWithSpawn();
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType> { { 0, TestHelpers.RoomType.Spawn } };
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Position at exactly max distance (10 cells)
        var exactDistance = new Cell(10, 0);
        Assert.True(constraint.IsValidSpatially(exactDistance, template, placedRooms, graph, assignments));
    }

    #endregion

    #region Constraint Composition Tests

    [Fact]
    public void CompositeConstraint_SpatialAndGraphConstraint_BothMustPass()
    {
        // Arrange: Combine graph constraint (MinDistanceFromStart) with spatial constraint (MustBeInQuadrant)
        var graphConstraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss, 3);
        var spatialConstraint = new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss,
            Quadrant.TopRight);
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(graphConstraint, spatialConstraint);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var placedRooms = CreatePlacedRoomsWithBounds();
        var template = TestHelpers.CreateDefaultTemplates().First();

        // Find node that satisfies graph constraint
        var validNode = graph.Nodes.FirstOrDefault(n => n.DistanceFromStart >= 3)
            ?? graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First();

        // Position in top-right quadrant (satisfies spatial constraint)
        var topRightPosition = new Cell(10, -10);

        // Both constraints should be checked
        Assert.True(graphConstraint.IsValid(validNode, graph, assignments));
        Assert.True(spatialConstraint.IsValidSpatially(topRightPosition, template, placedRooms, graph, assignments));
        
        // Composite should implement ISpatialConstraint
        Assert.IsAssignableFrom<ISpatialConstraint<TestHelpers.RoomType>>(composite);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void IncrementalSolver_RespectsSpatialConstraints_PlacesRoomsCorrectly()
    {
        // Arrange: Generate dungeon with spatial constraint (shops in top-right quadrant)
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[]
            {
                (TestHelpers.RoomType.Shop, 2)
            },
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
                    TestHelpers.RoomType.Shop,
                    Quadrant.TopRight)
            },
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);

        // Assert: All shops should be in top-right quadrant
        var shops = layout.Rooms.Where(r => r.RoomType == TestHelpers.RoomType.Shop).ToList();
        Assert.Equal(2, shops.Count);

        // Calculate dungeon bounds to determine quadrants
        var allRooms = layout.Rooms.ToList();
        var minX = allRooms.SelectMany(r => r.GetWorldCells()).Min(c => c.X);
        var maxX = allRooms.SelectMany(r => r.GetWorldCells()).Max(c => c.X);
        var minY = allRooms.SelectMany(r => r.GetWorldCells()).Min(c => c.Y);
        var maxY = allRooms.SelectMany(r => r.GetWorldCells()).Max(c => c.Y);
        var centerX = (minX + maxX) / 2.0;
        var centerY = (minY + maxY) / 2.0;

        foreach (var shop in shops)
        {
            var shopCenter = shop.GetWorldCells().Select(c => new { c.X, c.Y }).ToList();
            var avgX = shopCenter.Average(c => c.X);
            var avgY = shopCenter.Average(c => c.Y);

            // Top-right: X > centerX, Y < centerY
            Assert.True(avgX > centerX && avgY < centerY, 
                $"Shop at ({avgX}, {avgY}) should be in top-right quadrant (center: {centerX}, {centerY})");
        }
    }

    [Fact]
    public void IncrementalSolver_SpatialDistanceConstraint_PlacesRoomsAtCorrectDistance()
    {
        // Arrange: Treasure rooms must be at least 8 cells from spawn
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 54321,
            RoomCount = 8,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[]
            {
                (TestHelpers.RoomType.Treasure, 2)
            },
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                new MinSpatialDistanceFromStartConstraint<TestHelpers.RoomType>(
                    TestHelpers.RoomType.Treasure,
                    minDistance: 8)
            },
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);

        var spawnRoom = layout.Rooms.First(r => r.NodeId == layout.SpawnRoomId);
        var spawnPosition = spawnRoom.Position;
        var treasures = layout.Rooms.Where(r => r.RoomType == TestHelpers.RoomType.Treasure).ToList();

        Assert.Equal(2, treasures.Count);

        foreach (var treasure in treasures)
        {
            var treasureCells = treasure.GetWorldCells().ToList();
            var minDistance = treasureCells
                .SelectMany(tc => spawnRoom.GetWorldCells().Select(sc => 
                    Math.Abs(tc.X - sc.X) + Math.Abs(tc.Y - sc.Y)))
                .Min();

            Assert.True(minDistance >= 8, 
                $"Treasure room should be at least 8 cells from spawn, but minimum distance is {minDistance}");
        }
    }

    [Fact]
    public void IncrementalSolver_ImpossibleSpatialConstraint_ThrowsException()
    {
        // Arrange: Constraint that cannot be satisfied (region too small for any room)
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 99999,
            RoomCount = 5,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            RoomRequirements = new[]
            {
                (TestHelpers.RoomType.Shop, 1)
            },
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                // Region too small (1x1) for a 3x3 room
                new MustBeInRegionConstraint<TestHelpers.RoomType>(
                    TestHelpers.RoomType.Shop,
                    minX: 0,
                    maxX: 1,
                    minY: 0,
                    maxY: 1)
            },
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();
        
        // Should throw SpatialPlacementException when constraint cannot be satisfied
        Assert.Throws<SpatialPlacementException>(() => generator.Generate(config));
    }

    #endregion

    #region Determinism Tests

    [Fact]
    public void SpatialConstraints_Deterministic_SameSeedProducesSamePlacements()
    {
        // Arrange: Generate two dungeons with same seed and spatial constraints
        var config1 = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
                    TestHelpers.RoomType.Shop,
                    Quadrant.TopRight)
            },
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var config2 = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345, // Same seed
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Constraints = new List<IConstraint<TestHelpers.RoomType>>
            {
                new MustBeInQuadrantConstraint<TestHelpers.RoomType>(
                    TestHelpers.RoomType.Shop,
                    Quadrant.TopRight)
            },
            Templates = TestHelpers.CreateDefaultTemplates()
        };

        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout1 = generator.Generate(config1);
        var layout2 = generator.Generate(config2);

        // Assert: Same seed should produce identical spatial placements
        Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
        
        var rooms1 = layout1.Rooms.OrderBy(r => r.NodeId).ToList();
        var rooms2 = layout2.Rooms.OrderBy(r => r.NodeId).ToList();

        for (int i = 0; i < rooms1.Count; i++)
        {
            Assert.Equal(rooms1[i].Position, rooms2[i].Position);
            Assert.Equal(rooms1[i].RoomType, rooms2[i].RoomType);
        }
    }

    #endregion

    #region Helper Methods

    private FloorGraph CreateSimpleGraph()
    {
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        return generator.Generate(6, 0.0f, rng);
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsWithBounds()
    {
        // Create rooms that establish dungeon bounds for quadrant calculation
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 0,
                RoomType = TestHelpers.RoomType.Spawn,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 1,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(-20, -20), // Top-left
                Difficulty = 1.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 2,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(20, 20), // Bottom-right
                Difficulty = 1.0
            }
        };
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsWithBoss()
    {
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 0,
                RoomType = TestHelpers.RoomType.Spawn,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 1,
                RoomType = TestHelpers.RoomType.Boss,
                Template = template,
                Position = new Cell(0, 0), // Boss at origin
                Difficulty = 5.0
            }
        };
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsWithBossAndCombat()
    {
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 0,
                RoomType = TestHelpers.RoomType.Spawn,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 1,
                RoomType = TestHelpers.RoomType.Boss,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 5.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 2,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 1.0
            }
        };
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsWithBossAndShop()
    {
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 0,
                RoomType = TestHelpers.RoomType.Spawn,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 1,
                RoomType = TestHelpers.RoomType.Boss,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 5.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 2,
                RoomType = TestHelpers.RoomType.Shop,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.5
            }
        };
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsInCluster()
    {
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 1,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 1.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 2,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(2, 0), // 2 cells away
                Difficulty = 1.0
            },
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 3,
                RoomType = TestHelpers.RoomType.Combat,
                Template = template,
                Position = new Cell(0, 2), // 2 cells away
                Difficulty = 1.0
            }
        };
    }

    private List<PlacedRoom<TestHelpers.RoomType>> CreatePlacedRoomsWithSpawn()
    {
        var template = TestHelpers.CreateDefaultTemplates().First();
        return new List<PlacedRoom<TestHelpers.RoomType>>
        {
            new PlacedRoom<TestHelpers.RoomType>
            {
                NodeId = 0,
                RoomType = TestHelpers.RoomType.Spawn,
                Template = template,
                Position = new Cell(0, 0),
                Difficulty = 0.0
            }
        };
    }

    #endregion
}

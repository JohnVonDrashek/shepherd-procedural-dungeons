using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class PlacedRoomCellCachingOptimizationBenchmark
{
    private List<PlacedRoom<TestRoomType>> _rooms10 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms20 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms50 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms100 = null!;
    private object _detector = null!; // ClusterDetector<TestRoomType> accessed via reflection
    private ClusterConfig<TestRoomType> _config = null!;
    private MethodInfo _detectClustersMethod = null!;
    
    // Rooms with different cell counts for GetWorldCells benchmarks
    private PlacedRoom<TestRoomType> _smallRoom = null!;  // 3x3 = 9 cells
    private PlacedRoom<TestRoomType> _mediumRoom = null!; // 5x5 = 25 cells
    private PlacedRoom<TestRoomType> _largeRoom = null!; // 10x10 = 100 cells

    [GlobalSetup]
    public void Setup()
    {
        // Use reflection to access internal ClusterDetector class
        var clusterDetectorType = typeof(FloorGenerator<TestRoomType>)
            .Assembly
            .GetTypes()
            .First(t => t.Name == "ClusterDetector`1");
        
        var genericType = clusterDetectorType.MakeGenericType(typeof(TestRoomType));
        _detector = Activator.CreateInstance(genericType)!;
        
        _detectClustersMethod = genericType.GetMethod("DetectClusters", BindingFlags.Public | BindingFlags.Instance)!;
        
        _config = new ClusterConfig<TestRoomType>
        {
            Enabled = true,
            Epsilon = 20.0,
            MinClusterSize = 2,
            MaxClusterSize = null,
            RoomTypesToCluster = null
        };

        var rng = new Random(12345); // Fixed seed for reproducibility
        
        // Create templates with different sizes
        var smallTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(3, 3)
            .WithId("small")
            .ForRoomTypes(TestRoomType.Shop)
            .WithDoorsOnAllExteriorEdges()
            .Build();
            
        var mediumTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(5, 5)
            .WithId("medium")
            .ForRoomTypes(TestRoomType.Shop)
            .WithDoorsOnAllExteriorEdges()
            .Build();
            
        var largeTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(10, 10)
            .WithId("large")
            .ForRoomTypes(TestRoomType.Shop)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Create rooms for cluster detection benchmarks
        _rooms10 = CreateRoomsInClusters(10, smallTemplate, rng);
        _rooms20 = CreateRoomsInClusters(20, smallTemplate, rng);
        _rooms50 = CreateRoomsInClusters(50, smallTemplate, rng);
        _rooms100 = CreateRoomsInClusters(100, smallTemplate, rng);

        // Create individual rooms for GetWorldCells benchmarks
        _smallRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 1,
            RoomType = TestRoomType.Shop,
            Template = smallTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        _mediumRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 2,
            RoomType = TestRoomType.Shop,
            Template = mediumTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        _largeRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 3,
            RoomType = TestRoomType.Shop,
            Template = largeTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };
    }

    // Benchmark 1: GetWorldCells() called multiple times on the same room
    [Benchmark(Baseline = true)]
    [Arguments("small", 10)]
    [Arguments("small", 100)]
    [Arguments("small", 1000)]
    [Arguments("medium", 10)]
    [Arguments("medium", 100)]
    [Arguments("medium", 1000)]
    [Arguments("large", 10)]
    [Arguments("large", 100)]
    [Arguments("large", 1000)]
    public void GetWorldCellsMultipleCalls(string roomSize, int callCount)
    {
        var room = roomSize switch
        {
            "small" => _smallRoom,
            "medium" => _mediumRoom,
            "large" => _largeRoom,
            _ => throw new ArgumentException("Invalid room size")
        };

        // Call GetWorldCells() multiple times (simulating repeated access)
        for (int i = 0; i < callCount; i++)
        {
            var cells = room.GetWorldCells().ToList();
            _ = cells.Count; // Use result to prevent optimization
        }
    }

    // Benchmark 2: Cluster detection performance with cached vs uncached cells
    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    [Arguments(100)]
    public void ClusterDetectionPerformance(int roomCount)
    {
        var rooms = roomCount switch
        {
            10 => _rooms10,
            20 => _rooms20,
            50 => _rooms50,
            100 => _rooms100,
            _ => throw new ArgumentException("Invalid room count")
        };

        // Cluster detection calls GetWorldCells() multiple times per room
        _ = _detectClustersMethod.Invoke(_detector, new object[] { rooms, _config });
    }

    // Benchmark 3: Spatial solver performance with many rooms
    // This simulates the overlap checking that occurs during spatial placement
    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    public void SpatialSolverOverlapChecking(int roomCount)
    {
        var rooms = roomCount switch
        {
            10 => _rooms10,
            20 => _rooms20,
            50 => _rooms50,
            _ => throw new ArgumentException("Invalid room count")
        };

        // Simulate overlap checking as done in IncrementalSolver
        // Each room's cells are checked against occupied cells
        var occupiedCells = new HashSet<Cell>();
        foreach (var room in rooms)
        {
            // GetWorldCells() is called for each room during overlap checking
            var roomCells = room.GetWorldCells().ToList();
            foreach (var cell in roomCells)
            {
                if (occupiedCells.Contains(cell))
                {
                    // Simulate overlap detection
                    _ = cell;
                }
                else
                {
                    occupiedCells.Add(cell);
                }
            }
        }
    }

    // Benchmark 4: Memory allocations during full dungeon generation
    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    [Arguments(100)]
    public void MemoryAllocationsDuringGeneration(int roomCount)
    {
        var rooms = roomCount switch
        {
            10 => _rooms10,
            20 => _rooms20,
            50 => _rooms50,
            100 => _rooms100,
            _ => throw new ArgumentException("Invalid room count")
        };

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Simulate multiple GetWorldCells() calls as would occur during generation
        foreach (var room in rooms)
        {
            // Simulate multiple accesses per room (as occurs in cluster detection, overlap checking, etc.)
            for (int i = 0; i < 5; i++)
            {
                var cells = room.GetWorldCells().ToList();
                _ = cells.Count;
            }
        }
    }

    // Benchmark 5: End-to-end generation time for large dungeons (50+ rooms)
    [Benchmark]
    [Arguments(50)]
    [Arguments(100)]
    public void EndToEndGenerationLargeDungeons(int roomCount)
    {
        var rooms = roomCount switch
        {
            50 => _rooms50,
            100 => _rooms100,
            _ => throw new ArgumentException("Invalid room count")
        };

        // Simulate the various operations that call GetWorldCells() during generation:
        // 1. Cluster detection
        _ = _detectClustersMethod.Invoke(_detector, new object[] { rooms, _config });
        
        // 2. Overlap checking (spatial solver)
        var occupiedCells = new HashSet<Cell>();
        foreach (var room in rooms)
        {
            var roomCells = room.GetWorldCells().ToList();
            foreach (var cell in roomCells)
            {
                occupiedCells.Add(cell);
            }
        }
        
        // 3. Bounding box calculation (as done in FloorLayout.GetBounds())
        var allCells = rooms.SelectMany(r => r.GetWorldCells()).ToList();
        if (allCells.Count > 0)
        {
            var minX = allCells.Min(c => c.X);
            var maxX = allCells.Max(c => c.X);
            var minY = allCells.Min(c => c.Y);
            var maxY = allCells.Max(c => c.Y);
            _ = (minX, maxX, minY, maxY);
        }
    }

    // Helper method: Create rooms positioned in clusters
    private List<PlacedRoom<TestRoomType>> CreateRoomsInClusters(
        int totalRooms,
        RoomTemplate<TestRoomType> template,
        Random rng)
    {
        var rooms = new List<PlacedRoom<TestRoomType>>();
        
        // Create clusters of rooms positioned close together
        int clusters = Math.Max(1, totalRooms / 5); // ~5 rooms per cluster
        int roomsPerCluster = totalRooms / clusters;
        int roomId = 0;

        for (int clusterId = 0; clusterId < clusters; clusterId++)
        {
            // Cluster center position
            int clusterX = clusterId * 50; // Space clusters apart
            int clusterY = clusterId * 50;

            // Create rooms in this cluster
            int roomsInThisCluster = (clusterId == clusters - 1) 
                ? totalRooms - roomId 
                : roomsPerCluster;

            for (int i = 0; i < roomsInThisCluster; i++)
            {
                int offsetX = rng.Next(-10, 11);
                int offsetY = rng.Next(-10, 11);
                
                rooms.Add(new PlacedRoom<TestRoomType>
                {
                    NodeId = roomId++,
                    RoomType = TestRoomType.Shop,
                    Template = template,
                    Position = new Cell(clusterX + offsetX, clusterY + offsetY),
                    Difficulty = 1.0
                });
            }
        }

        return rooms;
    }

    // Test room type enum
    private enum TestRoomType
    {
        Shop
    }
}

// Main entry point for running benchmarks
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<PlacedRoomCellCachingOptimizationBenchmark>();
    }
}

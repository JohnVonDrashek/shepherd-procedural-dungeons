using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class ClusterDetectorCentroidCachingBenchmark
{
    private List<PlacedRoom<TestRoomType>> _rooms10 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms20 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms50 = null!;
    private List<PlacedRoom<TestRoomType>> _rooms100 = null!;
    private object _detector = null!; // ClusterDetector<TestRoomType> accessed via reflection
    private ClusterConfig<TestRoomType> _config = null!;
    private MethodInfo _detectClustersMethod = null!;
    
    // Rooms with different cell counts for centroid calculation benchmarks
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
        // Position rooms in clusters to ensure clustering occurs
        _rooms10 = CreateRoomsInClusters(10, smallTemplate, rng);
        _rooms20 = CreateRoomsInClusters(20, smallTemplate, rng);
        _rooms50 = CreateRoomsInClusters(50, smallTemplate, rng);
        _rooms100 = CreateRoomsInClusters(100, smallTemplate, rng);

        // Create individual rooms for centroid calculation benchmarks
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

    // Benchmark 1: Cluster Detection Performance
    [Benchmark(Baseline = true)]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    [Arguments(100)]
    public void ClusterDetection(int roomCount)
    {
        var rooms = roomCount switch
        {
            10 => _rooms10,
            20 => _rooms20,
            50 => _rooms50,
            100 => _rooms100,
            _ => throw new ArgumentException("Invalid room count")
        };

        _ = _detectClustersMethod.Invoke(_detector, new object[] { rooms, _config });
    }

    // Benchmark 2: Centroid Calculation Performance
    // This benchmarks the CalculateCentroid method directly by simulating its usage
    // We can't call it directly since it's private, so we simulate the repeated calls
    // that occur during cluster detection
    [Benchmark]
    [Arguments("small")]
    [Arguments("medium")]
    [Arguments("large")]
    public void CentroidCalculation(string roomSize)
    {
        var room = roomSize switch
        {
            "small" => _smallRoom,
            "medium" => _mediumRoom,
            "large" => _largeRoom,
            _ => throw new ArgumentException("Invalid room size")
        };

        // Simulate repeated centroid calculations as done in BuildCompleteGraphCluster
        // Calculate centroid 100 times to simulate the repeated calls during clustering
        for (int i = 0; i < 100; i++)
        {
            var cells = room.GetWorldCells().ToList();
            if (cells.Count > 0)
            {
                var avgX = cells.Average(c => c.X);
                var avgY = cells.Average(c => c.Y);
                _ = (avgX, avgY); // Use result to prevent optimization
            }
        }
    }

    // Benchmark 3: Memory Allocations During Cluster Detection
    [Benchmark]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    [Arguments(100)]
    public void ClusterDetectionMemoryAllocations(int roomCount)
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

        _ = _detectClustersMethod.Invoke(_detector, new object[] { rooms, _config });
    }

    // Benchmark 4: CreateCluster Performance (centroid and bounding box calculation)
    [Benchmark]
    [Arguments(2)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public void CreateClusterPerformance(int clusterSize)
    {
        var template = RoomTemplateBuilder<TestRoomType>
            .Rectangle(3, 3)
            .WithId("cluster")
            .ForRoomTypes(TestRoomType.Shop)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Create a cluster of rooms
        var clusterRooms = new List<PlacedRoom<TestRoomType>>();
        for (int i = 0; i < clusterSize; i++)
        {
            clusterRooms.Add(new PlacedRoom<TestRoomType>
            {
                NodeId = i,
                RoomType = TestRoomType.Shop,
                Template = template,
                Position = new Cell(i * 5, i * 5), // Space them out
                Difficulty = 1.0
            });
        }

        // Simulate CreateCluster's centroid and bounding box calculation
        // This is what CreateCluster does internally
        var allCells = clusterRooms.SelectMany(r => r.GetWorldCells()).ToList();
        var centroidX = (int)Math.Round(allCells.Average(c => c.X));
        var centroidY = (int)Math.Round(allCells.Average(c => c.Y));
        var minX = allCells.Min(c => c.X);
        var maxX = allCells.Max(c => c.X);
        var minY = allCells.Min(c => c.Y);
        var maxY = allCells.Max(c => c.Y);
        
        _ = (centroidX, centroidY, minX, maxX, minY, maxY); // Use result
    }

    // Helper method: Create rooms positioned in clusters
    private List<PlacedRoom<TestRoomType>> CreateRoomsInClusters(
        int totalRooms,
        RoomTemplate<TestRoomType> template,
        Random rng)
    {
        var rooms = new List<PlacedRoom<TestRoomType>>();
        
        // Create clusters of rooms positioned close together
        // This ensures clustering will occur during detection
        int clusters = Math.Max(1, totalRooms / 5); // ~5 rooms per cluster
        int roomsPerCluster = totalRooms / clusters;
        int roomId = 0;

        for (int clusterId = 0; clusterId < clusters; clusterId++)
        {
            // Cluster center position
            int clusterX = clusterId * 50; // Space clusters apart
            int clusterY = clusterId * 50;

            // Create rooms in this cluster (positioned within epsilon distance)
            int roomsInThisCluster = (clusterId == clusters - 1) 
                ? totalRooms - roomId 
                : roomsPerCluster;

            for (int i = 0; i < roomsInThisCluster; i++)
            {
                // Position rooms within epsilon distance of cluster center
                int offsetX = rng.Next(-10, 11); // Within 20 cells (epsilon)
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
        var summary = BenchmarkRunner.Run<ClusterDetectorCentroidCachingBenchmark>();
    }
}

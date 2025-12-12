using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class SpatialPlacementBoundingBoxOptimizationBenchmark
{
    private object _solver = null!; // IncrementalSolver<TestRoomType> accessed via reflection
    private MethodInfo _placeNearbyMethod = null!;
    
    // Rooms with different sizes for PlaceNearby benchmarks
    private PlacedRoom<TestRoomType> _smallRoom = null!;  // 3x3 = 9 cells
    private PlacedRoom<TestRoomType> _mediumRoom = null!; // 5x5 = 25 cells
    private PlacedRoom<TestRoomType> _largeRoom = null!; // 10x10 = 100 cells
    
    // Templates for placement
    private RoomTemplate<TestRoomType> _smallTemplate = null!;
    private RoomTemplate<TestRoomType> _mediumTemplate = null!;
    private RoomTemplate<TestRoomType> _largeTemplate = null!;
    
    private HashSet<Cell> _occupiedCells = null!;
    private Random _rng = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Use reflection to access internal IncrementalSolver class
        var solverType = typeof(FloorGenerator<TestRoomType>)
            .Assembly
            .GetTypes()
            .First(t => t.Name == "IncrementalSolver`1");
        
        var genericType = solverType.MakeGenericType(typeof(TestRoomType));
        _solver = Activator.CreateInstance(genericType)!;
        
        _placeNearbyMethod = genericType.GetMethod("PlaceNearby", BindingFlags.NonPublic | BindingFlags.Instance)!;
        
        _rng = new Random(12345); // Fixed seed for reproducibility
        
        // Create templates with different sizes
        _smallTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(3, 3)
            .WithId("small")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();
            
        _mediumTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(5, 5)
            .WithId("medium")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();
            
        _largeTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(10, 10)
            .WithId("large")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Create existing rooms at origin
        _smallRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 1,
            RoomType = TestRoomType.Default,
            Template = _smallTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        _mediumRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 2,
            RoomType = TestRoomType.Default,
            Template = _mediumTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        _largeRoom = new PlacedRoom<TestRoomType>
        {
            NodeId = 3,
            RoomType = TestRoomType.Default,
            Template = _largeTemplate,
            Position = new Cell(0, 0),
            Difficulty = 1.0
        };

        // Initialize occupied cells with the existing room
        _occupiedCells = new HashSet<Cell>();
        foreach (var cell in _mediumRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
    }

    // Benchmark 1: PlaceNearby with small room (3x3)
    [Benchmark(Baseline = true)]
    public void PlaceNearbySmallRoom()
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        foreach (var cell in _smallRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Place a small template nearby
        var result = _placeNearbyMethod.Invoke(_solver, new object[] { _smallRoom, _smallTemplate, _occupiedCells, _rng });
        _ = result; // Use result to prevent optimization
    }

    // Benchmark 2: PlaceNearby with medium room (5x5)
    [Benchmark]
    public void PlaceNearbyMediumRoom()
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        foreach (var cell in _mediumRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Place a medium template nearby
        var result = _placeNearbyMethod.Invoke(_solver, new object[] { _mediumRoom, _mediumTemplate, _occupiedCells, _rng });
        _ = result; // Use result to prevent optimization
    }

    // Benchmark 3: PlaceNearby with large room (10x10)
    [Benchmark]
    public void PlaceNearbyLargeRoom()
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        foreach (var cell in _largeRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Place a large template nearby
        var result = _placeNearbyMethod.Invoke(_solver, new object[] { _largeRoom, _largeTemplate, _occupiedCells, _rng });
        _ = result; // Use result to prevent optimization
    }

    // Benchmark 4: PlaceNearby with varying maxRadius (simulated by multiple calls)
    // This benchmarks the repeated bounding box calculations in the radius loop
    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public void PlaceNearbyWithRadius(int maxRadius)
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        foreach (var cell in _mediumRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Simulate the radius loop by calling PlaceNearby multiple times
        // Each call will iterate through radius 2 to maxRadius (default 20)
        // This benchmarks the repeated bounding box calculations
        for (int i = 0; i < 5; i++)
        {
            // Create a new occupied set for each iteration to simulate multiple placements
            var tempOccupied = new HashSet<Cell>(_occupiedCells);
            
            // Place template nearby
            var result = _placeNearbyMethod.Invoke(_solver, new object[] { _mediumRoom, _mediumTemplate, tempOccupied, _rng });
            _ = result;
        }
    }

    // Benchmark 5: Memory allocations during PlaceNearby
    [Benchmark]
    public void PlaceNearbyMemoryAllocations()
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        foreach (var cell in _mediumRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        // Place template nearby
        var result = _placeNearbyMethod.Invoke(_solver, new object[] { _mediumRoom, _mediumTemplate, _occupiedCells, _rng });
        _ = result;
    }

    // Benchmark 6: End-to-end spatial solver performance with many "nearby" placements
    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    public void EndToEndSpatialSolverNearbyPlacements(int placementCount)
    {
        // Reset occupied cells
        _occupiedCells.Clear();
        var currentRoom = _mediumRoom;
        
        // Add initial room to occupied
        foreach (var cell in currentRoom.GetWorldCells())
        {
            _occupiedCells.Add(cell);
        }
        
        // Simulate multiple nearby placements (as would occur in a real dungeon)
        for (int i = 0; i < placementCount; i++)
        {
            // Place a new room nearby
            var result = _placeNearbyMethod.Invoke(_solver, new object[] { currentRoom, _mediumTemplate, _occupiedCells, _rng });
            if (result is Cell newPosition)
            {
                // Create new room at the placed position
                var newRoom = new PlacedRoom<TestRoomType>
                {
                    NodeId = 100 + i,
                    RoomType = TestRoomType.Default,
                    Template = _mediumTemplate,
                    Position = newPosition,
                    Difficulty = 1.0
                };
                
                // Add new room's cells to occupied
                foreach (var cell in newRoom.GetWorldCells())
                {
                    _occupiedCells.Add(cell);
                }
                
                // Use new room as base for next placement
                currentRoom = newRoom;
            }
        }
    }

    // Test room type enum
    private enum TestRoomType
    {
        Default
    }
}

// Main entry point for running benchmarks
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SpatialPlacementBoundingBoxOptimizationBenchmark>();
    }
}

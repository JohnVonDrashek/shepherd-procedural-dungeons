using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class HallwayExteriorEdgesCachingOptimizationBenchmark
{
    private HallwayGenerator<TestRoomType> _generator = null!;
    private List<(FloorGraph graph, List<PlacedRoom<TestRoomType>> rooms, HashSet<Cell> occupiedCells)> _testCases = null!;
    private Random _rng = null!;

    [GlobalSetup]
    public void Setup()
    {
        _generator = new HallwayGenerator<TestRoomType>();
        _rng = new Random(12345); // Fixed seed for reproducibility
        _testCases = new List<(FloorGraph, List<PlacedRoom<TestRoomType>>, HashSet<Cell>)>();

        // Create templates
        var template = RoomTemplateBuilder<TestRoomType>
            .Rectangle(5, 5)
            .WithId("hallway-template")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Create test cases with varying numbers of hallways
        CreateTestCase(5, template, _rng);
        CreateTestCase(10, template, _rng);
        CreateTestCase(20, template, _rng);
        CreateTestCase(50, template, _rng);
    }

    private void CreateTestCase(int hallwayCount, RoomTemplate<TestRoomType> template, Random rng)
    {
        // Create a graph with the specified number of connections requiring hallways
        var graphGenerator = new SpanningTreeGraphGenerator();
        var graph = graphGenerator.Generate(hallwayCount + 5, 0.3f, new Random(rng.Next())); // Extra nodes to ensure enough connections
        
        // Mark connections as requiring hallways (using reflection since RequiresHallway has internal set)
        var requiresHallwayProperty = typeof(RoomConnection).GetProperty("RequiresHallway", BindingFlags.Public | BindingFlags.Instance);
        int hallwaysMarked = 0;
        foreach (var conn in graph.Connections)
        {
            if (hallwaysMarked < hallwayCount)
            {
                requiresHallwayProperty?.SetValue(conn, true);
                hallwaysMarked++;
            }
        }

        // Create placed rooms for all nodes
        var rooms = new List<PlacedRoom<TestRoomType>>();
        var occupiedCells = new HashSet<Cell>();
        int spacing = 20; // Space rooms apart

        foreach (var node in graph.Nodes)
        {
            int x = node.Id * spacing;
            int y = node.Id * spacing;
            
            var room = new PlacedRoom<TestRoomType>
            {
                NodeId = node.Id,
                RoomType = TestRoomType.Default,
                Template = template,
                Position = new Cell(x, y),
                Difficulty = 1.0
            };
            
            rooms.Add(room);
            
            // Add room cells to occupied
            foreach (var cell in room.GetWorldCells())
            {
                occupiedCells.Add(cell);
            }
        }

        _testCases.Add((graph, rooms, occupiedCells));
    }

    // Benchmark 1: Hallway generation with varying numbers of hallways
    [Benchmark(Baseline = true)]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    public void HallwayGeneration(int hallwayCount)
    {
        var testCase = _testCases.FirstOrDefault(tc => 
        {
            int count = tc.graph.Connections.Count(c => c.RequiresHallway);
            return count == hallwayCount || (hallwayCount == 5 && count >= 5 && count < 10) ||
                   (hallwayCount == 10 && count >= 10 && count < 20) ||
                   (hallwayCount == 20 && count >= 20 && count < 50) ||
                   (hallwayCount == 50 && count >= 50);
        });
        
        if (testCase.graph == null)
        {
            throw new ArgumentException($"No test case found for {hallwayCount} hallways");
        }

        var hallways = _generator.Generate(testCase.rooms, testCase.graph, testCase.occupiedCells, _rng);
        _ = hallways.Count; // Use result to prevent optimization
    }

    // Benchmark 2: Hallway generation with rooms having many exterior edges
    [Benchmark]
    public void HallwayGenerationManyExteriorEdges()
    {
        // Create a template with many exterior edges (large room)
        var largeTemplate = RoomTemplateBuilder<TestRoomType>
            .Rectangle(10, 10)
            .WithId("large-hallway-template")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        var graphGenerator = new SpanningTreeGraphGenerator();
        var graph = graphGenerator.Generate(20, 0.3f, new Random(12345));
        
        // Mark all connections as requiring hallways (using reflection since RequiresHallway has internal set)
        var requiresHallwayProperty = typeof(RoomConnection).GetProperty("RequiresHallway", BindingFlags.Public | BindingFlags.Instance);
        foreach (var conn in graph.Connections)
        {
            requiresHallwayProperty?.SetValue(conn, true);
        }

        // Create placed rooms with large templates (more exterior edges)
        var rooms = new List<PlacedRoom<TestRoomType>>();
        var occupiedCells = new HashSet<Cell>();
        int spacing = 30;

        foreach (var node in graph.Nodes)
        {
            int x = node.Id * spacing;
            int y = node.Id * spacing;
            
            var room = new PlacedRoom<TestRoomType>
            {
                NodeId = node.Id,
                RoomType = TestRoomType.Default,
                Template = largeTemplate,
                Position = new Cell(x, y),
                Difficulty = 1.0
            };
            
            rooms.Add(room);
            
            foreach (var cell in room.GetWorldCells())
            {
                occupiedCells.Add(cell);
            }
        }

        var hallways = _generator.Generate(rooms, graph, occupiedCells, _rng);
        _ = hallways.Count;
    }

    // Benchmark 3: Memory allocations during hallway generation
    [Benchmark]
    [Arguments(5)]
    [Arguments(10)]
    [Arguments(20)]
    [Arguments(50)]
    public void HallwayGenerationMemoryAllocations(int hallwayCount)
    {
        var testCase = _testCases.FirstOrDefault(tc => 
        {
            int count = tc.graph.Connections.Count(c => c.RequiresHallway);
            return count == hallwayCount || (hallwayCount == 5 && count >= 5 && count < 10) ||
                   (hallwayCount == 10 && count >= 10 && count < 20) ||
                   (hallwayCount == 20 && count >= 20 && count < 50) ||
                   (hallwayCount == 50 && count >= 50);
        });
        
        if (testCase.graph == null)
        {
            throw new ArgumentException($"No test case found for {hallwayCount} hallways");
        }

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var hallways = _generator.Generate(testCase.rooms, testCase.graph, testCase.occupiedCells, _rng);
        _ = hallways.Count;
    }

    // Benchmark 4: End-to-end generation time for dungeons requiring many hallways
    [Benchmark]
    [Arguments(20)]
    [Arguments(50)]
    public void EndToEndGenerationManyHallways(int hallwayCount)
    {
        var testCase = _testCases.FirstOrDefault(tc => 
        {
            int count = tc.graph.Connections.Count(c => c.RequiresHallway);
            return (hallwayCount == 20 && count >= 20 && count < 50) ||
                   (hallwayCount == 50 && count >= 50);
        });
        
        if (testCase.graph == null)
        {
            throw new ArgumentException($"No test case found for {hallwayCount} hallways");
        }

        // Simulate full hallway generation process
        // This includes multiple GetExteriorEdgesWorld() calls per room
        var hallways = _generator.Generate(testCase.rooms, testCase.graph, testCase.occupiedCells, _rng);
        
        // Verify hallways were generated
        _ = hallways.Count;
        
        // Simulate additional processing that might access exterior edges
        foreach (var hallway in hallways)
        {
            _ = hallway.Segments.Count;
        }
    }

    // Benchmark 5: Rooms with multiple connections (same room's edges accessed multiple times)
    [Benchmark]
    public void HallwayGenerationMultipleConnectionsPerRoom()
    {
        // Create a graph where some rooms have multiple connections
        var graphGenerator = new SpanningTreeGraphGenerator();
        var graph = graphGenerator.Generate(15, 0.5f, new Random(12345)); // Higher connection probability
        
        // Mark all connections as requiring hallways (using reflection since RequiresHallway has internal set)
        var requiresHallwayProperty = typeof(RoomConnection).GetProperty("RequiresHallway", BindingFlags.Public | BindingFlags.Instance);
        foreach (var conn in graph.Connections)
        {
            requiresHallwayProperty?.SetValue(conn, true);
        }

        var template = RoomTemplateBuilder<TestRoomType>
            .Rectangle(5, 5)
            .WithId("multi-conn-template")
            .ForRoomTypes(TestRoomType.Default)
            .WithDoorsOnAllExteriorEdges()
            .Build();

        // Create placed rooms
        var rooms = new List<PlacedRoom<TestRoomType>>();
        var occupiedCells = new HashSet<Cell>();
        int spacing = 20;

        foreach (var node in graph.Nodes)
        {
            int x = node.Id * spacing;
            int y = node.Id * spacing;
            
            var room = new PlacedRoom<TestRoomType>
            {
                NodeId = node.Id,
                RoomType = TestRoomType.Default,
                Template = template,
                Position = new Cell(x, y),
                Difficulty = 1.0
            };
            
            rooms.Add(room);
            
            foreach (var cell in room.GetWorldCells())
            {
                occupiedCells.Add(cell);
            }
        }

        // Rooms with multiple connections will have GetExteriorEdgesWorld() called multiple times
        var hallways = _generator.Generate(rooms, graph, occupiedCells, _rng);
        _ = hallways.Count;
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
        var summary = BenchmarkRunner.Run<HallwayExteriorEdgesCachingOptimizationBenchmark>();
    }
}

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class NodeLookupOptimizationBenchmark
{
    private FloorGraph _graph10 = null!;
    private FloorGraph _graph50 = null!;
    private FloorGraph _graph100 = null!;
    private FloorGraph _graph200 = null!;
    private Random _rng = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rng = new Random(12345); // Fixed seed for reproducibility
        var generator = new SpanningTreeGraphGenerator();
        
        _graph10 = generator.Generate(10, 0.3f, new Random(12345));
        _graph50 = generator.Generate(50, 0.3f, new Random(12345));
        _graph100 = generator.Generate(100, 0.3f, new Random(12345));
        _graph200 = generator.Generate(200, 0.3f, new Random(12345));
    }

    // Benchmark 1: BFS Pathfinding Performance
    [Benchmark(Baseline = true)]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void BfsPathfinding(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Find path from start to farthest node (simulating boss room placement)
        int startId = graph.StartNodeId;
        int targetId = graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First().Id;
        
        FindPath(graph, startId, targetId);
    }

    // Benchmark 2: Critical Path Marking (simulating node lookups)
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void CriticalPathMarking(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Simulate critical path marking (as done in RoomTypeAssigner)
        // This simulates the O(n²) lookups that occur when marking critical path nodes
        int startId = graph.StartNodeId;
        int targetId = graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First().Id;
        var path = FindPath(graph, startId, targetId);
        
        // Simulate node lookups for each path node (current O(n²) operation)
        foreach (int nodeId in path)
        {
            var node = graph.Nodes.First(n => n.Id == nodeId);
            _ = node.Id; // Use node to prevent optimization
        }
    }

    // Benchmark 3: FloorGraph.GetNode() Lookups
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void GetNodeLookups(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Simulate multiple GetNode() calls (as done throughout codebase)
        var random = new Random(12345);
        for (int i = 0; i < nodeCount; i++)
        {
            int nodeId = random.Next(nodeCount);
            var node = graph.GetNode(nodeId);
            _ = node.Id; // Use node to prevent optimization
        }
    }

    // Benchmark 4: Hallway Generation Room Lookups
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void HallwayGenerationRoomLookups(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Simulate room lookups as done in HallwayGenerator
        // Create mock rooms list
        var template = CreateMockTemplate();
        var rooms = graph.Nodes.Select(n => new PlacedRoom<TestRoomType>
        {
            NodeId = n.Id,
            Template = template,
            Position = new Cell(0, 0),
            RoomType = TestRoomType.Default,
            Difficulty = 0.5
        }).ToList();

        // Simulate hallway generation lookups (current O(n²) operation)
        foreach (var conn in graph.Connections.Where(c => c.RequiresHallway))
        {
            var roomA = rooms.First(r => r.NodeId == conn.NodeAId);
            var roomB = rooms.First(r => r.NodeId == conn.NodeBId);
            _ = roomA.NodeId; // Use to prevent optimization
            _ = roomB.NodeId;
        }
    }

    // Benchmark 5: IncrementalSolver Node Lookups
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void IncrementalSolverNodeLookups(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Simulate node lookups as done in IncrementalSolver
        // Use graph.Connections to find connections (since node.Connections is internal)
        var queue = new Queue<int>();
        var visited = new HashSet<int> { graph.StartNodeId };
        queue.Enqueue(graph.StartNodeId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            var currentNode = graph.Nodes.First(n => n.Id == currentId); // Current O(n) lookup
            
            // Find connections for this node
            foreach (var conn in graph.Connections.Where(c => c.NodeAId == currentId || c.NodeBId == currentId))
            {
                int neighborId = conn.GetOtherNodeId(currentId);
                if (!visited.Contains(neighborId))
                {
                    visited.Add(neighborId);
                    queue.Enqueue(neighborId);
                }
            }
        }
    }

    // Benchmark 6: Multiple Operations Combined (End-to-End Simulation)
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    [Arguments(200)]
    public void CombinedOperations(int nodeCount)
    {
        var graph = nodeCount switch
        {
            10 => _graph10,
            50 => _graph50,
            100 => _graph100,
            200 => _graph200,
            _ => throw new ArgumentException("Invalid node count")
        };

        // Simulate a combination of operations that would occur during generation
        // 1. BFS pathfinding
        int startId = graph.StartNodeId;
        int targetId = graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First().Id;
        var path = FindPath(graph, startId, targetId);
        
        // 2. Critical path marking (simulate lookups)
        foreach (int nodeId in path)
        {
            var node = graph.Nodes.First(n => n.Id == nodeId);
            _ = node.Id;
        }
        
        // 3. Multiple GetNode() calls
        var random = new Random(12345);
        for (int i = 0; i < nodeCount / 2; i++)
        {
            int nodeId = random.Next(nodeCount);
            var node = graph.GetNode(nodeId);
            _ = node.Id;
        }
    }

    // Helper method: BFS pathfinding (current implementation)
    private IReadOnlyList<int> FindPath(FloorGraph graph, int fromId, int toId)
    {
        var visited = new Dictionary<int, int>(); // nodeId -> previousNodeId
        var queue = new Queue<int>();
        queue.Enqueue(fromId);
        visited[fromId] = -1;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == toId)
            {
                // Reconstruct path
                var path = new List<int>();
                int node = toId;
                while (node != -1)
                {
                    path.Add(node);
                    node = visited[node];
                }
                path.Reverse();
                return path;
            }

            var currentNode = graph.Nodes.First(n => n.Id == current); // Current O(n) lookup
            // Find connections for this node using graph.Connections
            foreach (var conn in graph.Connections.Where(c => c.NodeAId == current || c.NodeBId == current))
            {
                int neighborId = conn.GetOtherNodeId(current);
                if (!visited.ContainsKey(neighborId))
                {
                    visited[neighborId] = current;
                    queue.Enqueue(neighborId);
                }
            }
        }

        throw new InvalidOperationException("No path found - graph is disconnected");
    }

    // Helper method: Create mock room template
    private RoomTemplate<TestRoomType> CreateMockTemplate()
    {
        return new RoomTemplateBuilder<TestRoomType>()
            .WithId("benchmark-template")
            .ForRoomTypes(TestRoomType.Default)
            .AddCell(0, 0)
            .AddCell(1, 0)
            .AddCell(0, 1)
            .AddCell(1, 1)
            .WithDoorsOnAllExteriorEdges()
            .Build();
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
        var summary = BenchmarkRunner.Run<NodeLookupOptimizationBenchmark>();
    }
}

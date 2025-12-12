using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Benchmarks;

[MemoryDiagnoser]
public class ConstraintFilteringOptimizationBenchmark
{
    private FloorGraph _graph5 = null!;
    private FloorGraph _graph10 = null!;
    private FloorGraph _graph20 = null!;
    private Random _rng = null!;
    
    // Constraint sets of varying sizes
    private IReadOnlyList<IConstraint<TestRoomType>> _constraints10 = null!;
    private IReadOnlyList<IConstraint<TestRoomType>> _constraints50 = null!;
    private IReadOnlyList<IConstraint<TestRoomType>> _constraints100 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rng = new Random(12345); // Fixed seed for reproducibility
        var generator = new SpanningTreeGraphGenerator();
        
        _graph5 = generator.Generate(5, 0.3f, new Random(12345));
        _graph10 = generator.Generate(10, 0.3f, new Random(12345));
        _graph20 = generator.Generate(20, 0.3f, new Random(12345));

        // Create constraint sets with varying sizes
        _constraints10 = CreateConstraints(10);
        _constraints50 = CreateConstraints(50);
        _constraints100 = CreateConstraints(100);
    }

    // Benchmark 1: Constraint Filtering Performance (single room type)
    [Benchmark(Baseline = true)]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void ConstraintFiltering(int constraintCount)
    {
        var constraints = constraintCount switch
        {
            10 => _constraints10,
            50 => _constraints50,
            100 => _constraints100,
            _ => throw new ArgumentException("Invalid constraint count")
        };

        // Simulate current implementation: filtering constraints for a single room type
        // This is what happens at line 71, 123, 163, 243 in RoomTypeAssigner
        var filtered = constraints.Where(c => c.TargetRoomType.Equals(TestRoomType.Shop)).ToList();
        _ = filtered.Count; // Use result to prevent optimization
    }

    // Benchmark 2: Room Type Assignment Performance (end-to-end)
    [Benchmark]
    [Arguments(5, 10)]
    [Arguments(10, 50)]
    [Arguments(20, 100)]
    public void RoomTypeAssignment(int nodeCount, int constraintCount)
    {
        var graph = nodeCount switch
        {
            5 => _graph5,
            10 => _graph10,
            20 => _graph20,
            _ => throw new ArgumentException("Invalid node count")
        };

        var constraints = constraintCount switch
        {
            10 => _constraints10,
            50 => _constraints50,
            100 => _constraints100,
            _ => throw new ArgumentException("Invalid constraint count")
        };

        var assigner = new RoomTypeAssigner<TestRoomType>();
        var roomRequirements = new List<(TestRoomType type, int count)>
        {
            (TestRoomType.Shop, 1),
            (TestRoomType.Treasure, 1),
            (TestRoomType.Secret, 1)
        };

        // Simulate AssignTypes() call (without actually assigning to avoid side effects)
        // We'll simulate the constraint filtering operations that occur
        var localAssignments = new Dictionary<int, TestRoomType>();
        localAssignments[graph.StartNodeId] = TestRoomType.Spawn;

        // Simulate boss constraint filtering (line 71)
        var bossConstraints = constraints.Where(c => c.TargetRoomType.Equals(TestRoomType.Boss)).ToList();
        
        // Simulate requirement constraint filtering (line 123) - happens for each room type
        foreach (var (roomType, count) in roomRequirements)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count; // Use result
        }

        // Simulate zone requirement constraint filtering (line 163) - happens in nested loops
        foreach (var (roomType, count) in roomRequirements)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count; // Use result
        }

        // Simulate priority calculation constraint filtering (line 243) - happens for each room type
        foreach (var (roomType, count) in roomRequirements)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count; // Use result
        }
    }

    // Benchmark 3: Priority Calculation Performance
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void PriorityCalculation(int constraintCount)
    {
        var constraints = constraintCount switch
        {
            10 => _constraints10,
            50 => _constraints50,
            100 => _constraints100,
            _ => throw new ArgumentException("Invalid constraint count")
        };

        // Simulate GetConstraintPriority() calls for multiple room types
        // This is what happens when processing room requirements ordered by priority
        var roomTypes = new[] { TestRoomType.Shop, TestRoomType.Treasure, TestRoomType.Secret, TestRoomType.Combat };
        
        foreach (var roomType in roomTypes)
        {
            // Simulate current implementation: filtering constraints for priority calculation
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            
            // Simulate priority calculation (simplified)
            int priority = 0;
            foreach (var constraint in typeConstraints)
            {
                priority += constraint.GetType().Name switch
                {
                    nameof(MustBeDeadEndConstraint<TestRoomType>) => 10,
                    nameof(OnlyOnCriticalPathConstraint<TestRoomType>) => 8,
                    nameof(NotOnCriticalPathConstraint<TestRoomType>) => 7,
                    _ => 1
                };
            }
            _ = priority; // Use result
        }
    }

    // Benchmark 4: Memory Allocations During Constraint Filtering
    [Benchmark]
    [Arguments(10)]
    [Arguments(50)]
    [Arguments(100)]
    public void ConstraintFilteringMemoryAllocations(int constraintCount)
    {
        var constraints = constraintCount switch
        {
            10 => _constraints10,
            50 => _constraints50,
            100 => _constraints100,
            _ => throw new ArgumentException("Invalid constraint count")
        };

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Simulate multiple filter operations as done in RoomTypeAssigner
        // Boss constraints (1 filter)
        var bossConstraints = constraints.Where(c => c.TargetRoomType.Equals(TestRoomType.Boss)).ToList();
        
        // Requirement constraints (N filters for N room types)
        var roomTypes = new[] { TestRoomType.Shop, TestRoomType.Treasure, TestRoomType.Secret };
        foreach (var roomType in roomTypes)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count;
        }

        // Zone requirement constraints (N filters in nested loops)
        foreach (var roomType in roomTypes)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count;
        }

        // Priority calculation constraints (N filters for N room types)
        foreach (var roomType in roomTypes)
        {
            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            _ = typeConstraints.Count;
        }
    }

    // Helper method: Create constraints for testing
    private IReadOnlyList<IConstraint<TestRoomType>> CreateConstraints(int count)
    {
        var constraints = new List<IConstraint<TestRoomType>>();
        var rng = new Random(12345);
        var roomTypes = new[] { TestRoomType.Spawn, TestRoomType.Boss, TestRoomType.Shop, TestRoomType.Treasure, TestRoomType.Secret, TestRoomType.Combat };

        for (int i = 0; i < count; i++)
        {
            var roomType = roomTypes[rng.Next(roomTypes.Length)];
            
            // Create different types of constraints
            switch (i % 4)
            {
                case 0:
                    constraints.Add(new MinDistanceFromStartConstraint<TestRoomType>(roomType, rng.Next(1, 5)));
                    break;
                case 1:
                    constraints.Add(new MaxDistanceFromStartConstraint<TestRoomType>(roomType, rng.Next(5, 10)));
                    break;
                case 2:
                    constraints.Add(new MinConnectionCountConstraint<TestRoomType>(roomType, rng.Next(1, 3)));
                    break;
                case 3:
                    constraints.Add(new MaxConnectionCountConstraint<TestRoomType>(roomType, rng.Next(3, 6)));
                    break;
            }
        }

        return constraints;
    }

    // Test room type enum
    private enum TestRoomType
    {
        Spawn,
        Boss,
        Combat,
        Shop,
        Treasure,
        Secret
    }
}

// Main entry point for running benchmarks
public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ConstraintFilteringOptimizationBenchmark>();
    }
}

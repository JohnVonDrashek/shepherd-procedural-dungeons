using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class ConstraintCompositionTests
{
    [Fact]
    public void CompositeConstraint_And_AllConstraintsPass_ReturnsTrue()
    {
        // Arrange: AND composition where all constraints pass
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Find a node that satisfies both: distance >= 2 AND is a dead end
        var validNode = graph.Nodes.FirstOrDefault(n => n.DistanceFromStart >= 2 && n.ConnectionCount == 1)
            ?? graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First();

        // Act & Assert
        Assert.True(composite.IsValid(validNode, graph, assignments));
        Assert.Equal(TestHelpers.RoomType.Boss, composite.TargetRoomType);
    }

    [Fact]
    public void CompositeConstraint_And_OneConstraintFails_ReturnsFalse()
    {
        // Arrange: AND composition where one constraint fails
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that fails constraint1 (distance < 2)
        var startNode = graph.GetNode(0);

        // Act & Assert
        Assert.False(composite.IsValid(startNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Or_AtLeastOnePasses_ReturnsTrue()
    {
        // Arrange: OR composition where at least one constraint passes
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 5);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint1, constraint2);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that fails constraint1 but passes constraint2 (dead end)
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1 && n.DistanceFromStart < 5)
            ?? graph.Nodes.First(n => n.ConnectionCount == 1);

        // Act & Assert
        Assert.True(composite.IsValid(deadEndNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Or_AllConstraintsFail_ReturnsFalse()
    {
        // Arrange: OR composition where all constraints fail
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 10);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint1, constraint2);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that fails both: distance < 10 AND not a dead end (has multiple connections)
        var branchNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount > 1 && n.DistanceFromStart < 10)
            ?? graph.Nodes.First(n => n.ConnectionCount > 1);

        // Act & Assert
        Assert.False(composite.IsValid(branchNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Not_ConstraintPasses_ReturnsFalse()
    {
        // Arrange: NOT composition - constraint passes, so NOT should fail
        var constraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure, 2);
        var composite = CompositeConstraint<TestHelpers.RoomType>.Not(constraint);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that satisfies the constraint (distance >= 2)
        var validNode = graph.Nodes.FirstOrDefault(n => n.DistanceFromStart >= 2)
            ?? graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First();

        // Act & Assert
        Assert.False(composite.IsValid(validNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Not_ConstraintFails_ReturnsTrue()
    {
        // Arrange: NOT composition - constraint fails, so NOT should pass
        var constraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure, 2);
        var composite = CompositeConstraint<TestHelpers.RoomType>.Not(constraint);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that fails the constraint (distance < 2)
        var startNode = graph.GetNode(0);

        // Act & Assert
        Assert.True(composite.IsValid(startNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Nested_AndContainingOr_ValidatesCorrectly()
    {
        // Arrange: AND containing OR - (constraint1 AND (constraint2 OR constraint3))
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        var constraint3 = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        
        var orComposite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint2, constraint3);
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, orComposite);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Set up critical path for testing
        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);
        SetCriticalPath(node1, true);
        SetCriticalPath(node2, false);

        // Node that satisfies: distance >= 2 AND (dead end OR not on critical path)
        var validNode = graph.Nodes.FirstOrDefault(n => 
            n.DistanceFromStart >= 2 && (n.ConnectionCount == 1 || !n.IsOnCriticalPath))
            ?? node2;

        // Act & Assert
        Assert.True(andComposite.IsValid(validNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Nested_OrContainingAnd_ValidatesCorrectly()
    {
        // Arrange: OR containing AND - (constraint1 OR (constraint2 AND constraint3))
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 10);
        var constraint2 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 2);
        var constraint3 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(constraint2, constraint3);
        var orComposite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint1, andComposite);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that fails constraint1 but satisfies (constraint2 AND constraint3)
        var validNode = graph.Nodes.FirstOrDefault(n => 
            n.DistanceFromStart < 10 && n.DistanceFromStart >= 2 && n.ConnectionCount == 1)
            ?? graph.Nodes.First(n => n.ConnectionCount == 1);

        // Act & Assert
        Assert.True(orComposite.IsValid(validNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Nested_NotContainingAnd_ValidatesCorrectly()
    {
        // Arrange: NOT containing AND - NOT (constraint1 AND constraint2)
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);
        var notComposite = CompositeConstraint<TestHelpers.RoomType>.Not(andComposite);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that satisfies both constraints (AND passes, so NOT should fail)
        var nodeThatSatisfiesBoth = graph.Nodes.FirstOrDefault(n => 
            n.DistanceFromStart >= 2 && n.ConnectionCount == 1)
            ?? graph.Nodes.First(n => n.ConnectionCount == 1);

        // Act & Assert
        Assert.False(notComposite.IsValid(nodeThatSatisfiesBoth, graph, assignments));
        
        // Node that fails at least one constraint (AND fails, so NOT should pass)
        var nodeThatFailsOne = graph.GetNode(0); // Distance < 2
        Assert.True(notComposite.IsValid(nodeThatFailsOne, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_EmptyAnd_AlwaysPasses()
    {
        // Arrange: AND with no constraints should always pass
        var composite = CompositeConstraint<TestHelpers.RoomType>.And();

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var anyNode = graph.GetNode(0);

        // Act & Assert
        Assert.True(composite.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_EmptyOr_AlwaysFails()
    {
        // Arrange: OR with no constraints should always fail
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or();

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var anyNode = graph.GetNode(0);

        // Act & Assert
        Assert.False(composite.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_SingleConstraintAnd_BehavesLikeConstraint()
    {
        // Arrange: AND with single constraint should behave like the constraint itself
        var constraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraint);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        var startNode = graph.GetNode(0);
        var farNode = graph.Nodes.FirstOrDefault(n => n.DistanceFromStart >= 2)
            ?? graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First();

        // Act & Assert
        Assert.False(composite.IsValid(startNode, graph, assignments));
        Assert.True(composite.IsValid(farNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_SingleConstraintOr_BehavesLikeConstraint()
    {
        // Arrange: OR with single constraint should behave like the constraint itself
        var constraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure);
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1)
            ?? graph.Nodes.First();
        var branchNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount > 1)
            ?? graph.Nodes.Skip(1).First();

        // Act & Assert
        Assert.True(composite.IsValid(deadEndNode, graph, assignments));
        Assert.False(composite.IsValid(branchNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_MixedConstraintTypes_WorksCorrectly()
    {
        // Arrange: Compose different constraint types (distance, adjacency, critical path, etc.)
        var distanceConstraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 2);
        var deadEndConstraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        var criticalPathConstraint = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(
            distanceConstraint,
            deadEndConstraint,
            criticalPathConstraint
        );

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Set up critical path
        var node1 = graph.GetNode(1);
        SetCriticalPath(node1, true);
        
        // Find node that satisfies all: distance >= 2, dead end, not on critical path
        var validNode = graph.Nodes.FirstOrDefault(n => 
            n.DistanceFromStart >= 2 && 
            n.ConnectionCount == 1 && 
            !n.IsOnCriticalPath)
            ?? graph.Nodes.First(n => n.ConnectionCount == 1);

        // Act & Assert
        Assert.True(composite.IsValid(validNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_RealWorldScenario_ShopOrTreasureInDeadEnds()
    {
        // Arrange: "Shop OR Treasure in dead ends" - (dead end constraint) AND (shop OR treasure type constraint)
        // Note: This is a simplified version - in reality, we'd need room type constraints
        var shopDeadEnd = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);
        var treasureDeadEnd = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure);
        
        var shopOrTreasure = CompositeConstraint<TestHelpers.RoomType>.Or(shopDeadEnd, treasureDeadEnd);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1)
            ?? graph.Nodes.First();

        // Act & Assert
        Assert.True(shopOrTreasure.IsValid(deadEndNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_RealWorldScenario_NotOnCriticalPathAndNearSpawn()
    {
        // Arrange: "NOT (on critical path AND near spawn)"
        var onCriticalPath = new OnlyOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        var nearSpawn = new MaxDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 2);
        
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(onCriticalPath, nearSpawn);
        var notComposite = CompositeConstraint<TestHelpers.RoomType>.Not(andComposite);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Set up critical path
        var node0 = graph.GetNode(0);
        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);
        SetCriticalPath(node0, true);
        SetCriticalPath(node1, true);
        SetCriticalPath(node2, false);
        
        // Node on critical path AND near spawn (distance <= 2) - should fail NOT
        Assert.False(notComposite.IsValid(node1, graph, assignments));
        
        // Node not on critical path OR far from spawn - should pass NOT
        Assert.True(notComposite.IsValid(node2, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_RealWorldScenario_EitherFarFromStartOrOnCriticalPath()
    {
        // Arrange: "Either far from start OR on critical path"
        var farFromStart = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 5);
        var onCriticalPath = new OnlyOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or(farFromStart, onCriticalPath);

        // Create a deterministic linear graph: 0-1-2-3-4-5-6 (guarantees node 6 has distance 6 >= 5)
        var graph = CreateLinearGraph(7);
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Set up critical path
        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);
        var node6 = graph.GetNode(6);
        SetCriticalPath(node1, true);
        SetCriticalPath(node2, false);
        SetCriticalPath(node6, false); // Node 6 is far but not on critical path
        
        // Node on critical path (even if not far) - should pass
        Assert.True(composite.IsValid(node1, graph, assignments));
        
        // Node far from start (even if not on critical path) - should pass
        Assert.True(composite.IsValid(node6, graph, assignments));
        
        // Node not on critical path AND not far - should fail
        Assert.False(composite.IsValid(node2, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_OperatorProperty_ReturnsCorrectOperator()
    {
        // Arrange
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);

        // Act
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);
        var orComposite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint1, constraint2);
        var notComposite = CompositeConstraint<TestHelpers.RoomType>.Not(constraint1);

        // Assert
        Assert.Equal(CompositionOperator.And, andComposite.Operator);
        Assert.Equal(CompositionOperator.Or, orComposite.Operator);
        Assert.Equal(CompositionOperator.Not, notComposite.Operator);
    }

    [Fact]
    public void CompositeConstraint_ConstraintsProperty_ReturnsCorrectConstraints()
    {
        // Arrange
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var constraint3 = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);

        // Act
        var andComposite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2, constraint3);
        var notComposite = CompositeConstraint<TestHelpers.RoomType>.Not(constraint1);

        // Assert
        Assert.Equal(3, andComposite.Constraints.Count);
        Assert.Contains(constraint1, andComposite.Constraints);
        Assert.Contains(constraint2, andComposite.Constraints);
        Assert.Contains(constraint3, andComposite.Constraints);
        
        Assert.Single(notComposite.Constraints);
        Assert.Equal(constraint1, notComposite.Constraints[0]);
    }

    [Fact]
    public void CompositeConstraint_TargetRoomType_MatchesConstraints()
    {
        // Arrange
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);

        // Act
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Boss, composite.TargetRoomType);
    }

    [Fact]
    public void CompositeConstraint_OrWithDifferentTargetRoomTypes_AllowsDifferentTypes()
    {
        // Arrange: OR composition with constraints targeting different room types is allowed
        // This enables real-world scenarios like "Shop OR Treasure in dead ends"
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);

        // Act: Should not throw - OR allows different target room types
        var composite = CompositeConstraint<TestHelpers.RoomType>.Or(constraint1, constraint2);

        // Assert: Composite should use first constraint's target room type
        Assert.Equal(TestHelpers.RoomType.Boss, composite.TargetRoomType);
    }

    [Fact]
    public void CompositeConstraint_AndWithDifferentTargetRoomTypes_ThrowsException()
    {
        // Arrange: AND composition with constraints targeting different room types should throw
        var constraint1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 2);
        var constraint2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            CompositeConstraint<TestHelpers.RoomType>.And(constraint1, constraint2));
    }

    [Fact]
    public void CompositeConstraint_DeeplyNested_ValidatesCorrectly()
    {
        // Arrange: Deeply nested composition: AND(OR(AND(...), ...), ...)
        var c1 = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret, 1);
        var c2 = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        var c3 = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Secret);
        
        var innerAnd = CompositeConstraint<TestHelpers.RoomType>.And(c2, c3);
        var middleOr = CompositeConstraint<TestHelpers.RoomType>.Or(c1, innerAnd);
        var outerAnd = CompositeConstraint<TestHelpers.RoomType>.And(middleOr, c1);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        
        // Node that satisfies c1 (distance >= 1) - should pass outer AND
        var validNode = graph.Nodes.FirstOrDefault(n => n.DistanceFromStart >= 1)
            ?? graph.Nodes.Skip(1).First();

        // Act & Assert
        Assert.True(outerAnd.IsValid(validNode, graph, assignments));
    }

    [Fact]
    public void CompositeConstraint_Performance_LargeComposition_CompletesInReasonableTime()
    {
        // Arrange: Create composition with 10+ constraints
        var constraints = Enumerable.Range(1, 15)
            .Select(i => new MinDistanceFromStartConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Boss, i))
            .Cast<IConstraint<TestHelpers.RoomType>>()
            .ToArray();
        
        var composite = CompositeConstraint<TestHelpers.RoomType>.And(constraints);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();
        var node = graph.Nodes.OrderByDescending(n => n.DistanceFromStart).First();

        // Act
        var startTime = DateTime.UtcNow;
        var result = composite.IsValid(node, graph, assignments);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert: Should complete in reasonable time (< 1 second for 15 constraints)
        Assert.True(elapsed.TotalSeconds < 1.0, $"Large composition took {elapsed.TotalSeconds} seconds");
        // Result should be false since not all distance constraints can pass
        Assert.False(result);
    }

    private static FloorGraph CreateSimpleGraph()
    {
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(6, 0.0f, rng); // 0 branching = linear tree
        return graph;
    }

    private static FloorGraph CreateLinearGraph(int nodeCount)
    {
        // Creates a deterministic linear graph: 0-1-2-...-(nodeCount-1)
        var nodes = Enumerable.Range(0, nodeCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();
        var connectionsProperty = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        for (int i = 0; i < nodeCount - 1; i++)
        {
            var conn = new RoomConnection { NodeAId = i, NodeBId = i + 1 };
            connections.Add(conn);
            
            var node0Connections = connectionsProperty?.GetValue(nodes[i]) as List<RoomConnection>;
            var node1Connections = connectionsProperty?.GetValue(nodes[i + 1]) as List<RoomConnection>;
            node0Connections?.Add(conn);
            node1Connections?.Add(conn);
        }

        // Calculate distances from start (BFS)
        var visited = new HashSet<int>();
        var queue = new Queue<(int nodeId, int distance)>();
        queue.Enqueue((0, 0));

        var distanceProperty = typeof(RoomNode).GetProperty("DistanceFromStart",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var distanceSetter = distanceProperty?.GetSetMethod(nonPublic: true);

        while (queue.Count > 0)
        {
            var (nodeId, distance) = queue.Dequeue();
            if (visited.Contains(nodeId)) continue;
            visited.Add(nodeId);

            distanceSetter?.Invoke(nodes[nodeId], new object[] { distance });

            var nodeConnections = connectionsProperty?.GetValue(nodes[nodeId]) as List<RoomConnection>;
            if (nodeConnections != null)
            {
                foreach (var conn in nodeConnections)
                {
                    int neighborId = conn.GetOtherNodeId(nodeId);
                    if (!visited.Contains(neighborId))
                    {
                        queue.Enqueue((neighborId, distance + 1));
                    }
                }
            }
        }

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private static void SetCriticalPath(RoomNode node, bool value)
    {
        var property = typeof(RoomNode).GetProperty("IsOnCriticalPath", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(node, new object[] { value });
    }
}

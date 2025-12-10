using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class ConstraintTests
{
    [Fact]
    public void MinDistanceFromStartConstraint_ValidatesCorrectly()
    {
        var constraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss, 3);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var node1 = graph.GetNode(0); // Distance 0
        var node4 = graph.GetNode(3); // Distance 3

        Assert.False(constraint.IsValid(node1, graph, assignments));
        Assert.True(constraint.IsValid(node4, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromStartConstraint_ValidatesCorrectly()
    {
        var constraint = new MaxDistanceFromStartConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Combat, 2);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var node2 = graph.GetNode(2); // Distance 2
        var node5 = graph.GetNode(5); // Distance 3

        Assert.True(constraint.IsValid(node2, graph, assignments));
        Assert.False(constraint.IsValid(node5, graph, assignments));
    }

    [Fact]
    public void NotOnCriticalPathConstraint_ValidatesCorrectly()
    {
        var constraint = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Use reflection to set IsOnCriticalPath for testing
        var node0 = graph.GetNode(0);
        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);
        var node3 = graph.GetNode(3);

        SetCriticalPath(node0, true);
        SetCriticalPath(node1, true);
        SetCriticalPath(node3, true);

        Assert.False(constraint.IsValid(node1, graph, assignments));
        Assert.True(constraint.IsValid(node2, graph, assignments));
    }

    [Fact]
    public void OnlyOnCriticalPathConstraint_ValidatesCorrectly()
    {
        var constraint = new OnlyOnCriticalPathConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Boss);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);

        SetCriticalPath(node1, true);

        Assert.True(constraint.IsValid(node1, graph, assignments));
        Assert.False(constraint.IsValid(node2, graph, assignments));
    }

    private static void SetCriticalPath(RoomNode node, bool value)
    {
        var property = typeof(RoomNode).GetProperty("IsOnCriticalPath", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(node, new object[] { value });
    }

    [Fact]
    public void MustBeDeadEndConstraint_ValidatesCorrectly()
    {
        var constraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Treasure);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with 1 connection
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);
        // Node with multiple connections
        var branchNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount > 1);

        if (deadEndNode != null)
            Assert.True(constraint.IsValid(deadEndNode, graph, assignments));

        if (branchNode != null)
            Assert.False(constraint.IsValid(branchNode, graph, assignments));
    }

    [Fact]
    public void MaxPerFloorConstraint_ValidatesCorrectly()
    {
        var constraint = new MaxPerFloorConstraint<TestHelpers.RoomType>(TestHelpers.RoomType.Shop, 2);
        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Shop },
            { 1, TestHelpers.RoomType.Shop }
        };

        // Already at max count (2)
        Assert.False(constraint.IsValid(graph.GetNode(2), graph, assignments));

        // Remove one
        assignments.Remove(1);
        Assert.True(constraint.IsValid(graph.GetNode(2), graph, assignments));
    }

    [Fact]
    public void CustomConstraint_ValidatesCorrectly()
    {
        var constraint = new CustomConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            (node, graph, assignments) => node.DistanceFromStart % 2 == 0);

        var graph = CreateSimpleGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var node0 = graph.GetNode(0); // Distance 0 (even)
        var node1 = graph.GetNode(1); // Distance 1 (odd)

        Assert.True(constraint.IsValid(node0, graph, assignments));
        Assert.False(constraint.IsValid(node1, graph, assignments));
    }

    private FloorGraph CreateSimpleGraph()
    {
        // Use GraphGenerator to create a proper graph with distances calculated
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(6, 0.0f, rng); // 0 branching = linear tree
        
        return graph;
    }
}


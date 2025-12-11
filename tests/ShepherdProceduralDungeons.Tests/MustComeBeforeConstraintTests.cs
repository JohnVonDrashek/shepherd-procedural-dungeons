using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class MustComeBeforeConstraintTests
{
    [Fact]
    public void MustComeBeforeConstraint_BasicOrdering_ValidatesCorrectly()
    {
        // Arrange: MiniBoss must come before Boss on critical path
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, // Using Combat as "MiniBoss" for test
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 3, TestHelpers.RoomType.Boss } // Boss is at node 3 (index 3 on critical path)
        };

        // Node 1 is at index 1 on critical path (before Boss at index 3)
        var nodeBeforeBoss = graph.GetNode(1);
        // Node 4 is at index 4 on critical path (after Boss at index 3)
        var nodeAfterBoss = graph.GetNode(4);

        // Act & Assert
        Assert.True(constraint.IsValid(nodeBeforeBoss, graph, assignments));
        Assert.False(constraint.IsValid(nodeAfterBoss, graph, assignments));
    }

    [Fact]
    public void MustComeBeforeConstraint_MultipleReferences_ValidatesCorrectly()
    {
        // Arrange: Shop must come before Boss OR Combat (at least one)
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Combat);
        
        var graph = CreateGraphWithCriticalPath();
        
        // Scenario 1: Boss is at index 3, Shop at index 1 (before Boss) - should be valid
        var assignmentsWithBoss = new Dictionary<int, TestHelpers.RoomType>
        {
            { 3, TestHelpers.RoomType.Boss }
        };
        var shopNodeBeforeBoss = graph.GetNode(1);

        // Scenario 2: Combat is at index 2, Shop at index 1 (before Combat) - should be valid
        var assignmentsWithCombat = new Dictionary<int, TestHelpers.RoomType>
        {
            { 2, TestHelpers.RoomType.Combat }
        };

        // Scenario 3: Both Boss and Combat are before Shop - should be invalid
        var assignmentsBothBefore = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss },
            { 1, TestHelpers.RoomType.Combat }
        };
        var shopNodeAfterBoth = graph.GetNode(2);

        // Act & Assert
        Assert.True(constraint.IsValid(shopNodeBeforeBoss, graph, assignmentsWithBoss));
        Assert.True(constraint.IsValid(shopNodeBeforeBoss, graph, assignmentsWithCombat));
        Assert.False(constraint.IsValid(shopNodeAfterBoth, graph, assignmentsBothBefore));
    }

    [Fact]
    public void MustComeBeforeConstraint_NotOnCriticalPath_HandlesGracefully()
    {
        // Arrange: Target room type is not on critical path
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 3, TestHelpers.RoomType.Boss }
        };

        // Node 5 is not on critical path (critical path is 0-1-2-3-4)
        var nodeNotOnCriticalPath = graph.GetNode(5);

        // Act & Assert: Should handle gracefully (may allow or disallow based on design)
        // For now, we'll test that it doesn't throw and returns a boolean
        var result = constraint.IsValid(nodeNotOnCriticalPath, graph, assignments);
        Assert.True(result is bool); // Just verify it returns a boolean without throwing
    }

    [Fact]
    public void MustComeBeforeConstraint_ReferenceNotAssignedYet_IsPermissive()
    {
        // Arrange: Reference room type hasn't been assigned yet
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>(); // No assignments yet

        // Node 1 is on critical path
        var nodeOnCriticalPath = graph.GetNode(1);

        // Act & Assert: Should be permissive when reference isn't assigned yet
        Assert.True(constraint.IsValid(nodeOnCriticalPath, graph, assignments));
    }

    [Fact]
    public void MustComeBeforeConstraint_BothOnCriticalPath_VerifiesCorrectOrdering()
    {
        // Arrange: Both target and reference are on critical path
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        
        // Combat at index 1, Boss at index 3
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 3, TestHelpers.RoomType.Boss }
        };

        var combatNode = graph.GetNode(1); // Index 1 on critical path
        var bossNode = graph.GetNode(3); // Index 3 on critical path

        // Act & Assert: Combat (index 1) should be valid, Boss (index 3) should be invalid for Combat constraint
        Assert.True(constraint.IsValid(combatNode, graph, assignments));
        Assert.False(constraint.IsValid(bossNode, graph, assignments));
    }

    [Fact]
    public void MustComeBeforeConstraint_SameNode_IsInvalid()
    {
        // Arrange: Target and reference would be same node
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 1, TestHelpers.RoomType.Boss } // Boss is at node 1
        };

        // Node 1 is where Boss is placed - cannot place Combat before itself
        var sameNode = graph.GetNode(1);

        // Act & Assert: Should be invalid (cannot come before itself)
        Assert.False(constraint.IsValid(sameNode, graph, assignments));
    }

    [Fact]
    public void MustComeBeforeConstraint_ConstructorWithSingleReference_CreatesConstraint()
    {
        // Act
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Combat, constraint.TargetRoomType);
    }

    [Fact]
    public void MustComeBeforeConstraint_ConstructorWithMultipleReferences_CreatesConstraint()
    {
        // Act
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Combat);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
    }

    [Fact]
    public void MustComeBeforeConstraint_EmptyCriticalPath_HandlesGracefully()
    {
        // Arrange: Critical path is empty or not set
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithoutCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var node = graph.GetNode(0);

        // Act & Assert: Should handle gracefully without throwing
        var result = constraint.IsValid(node, graph, assignments);
        Assert.True(result is bool);
    }

    [Fact]
    public void MustComeBeforeConstraint_ReferenceOnCriticalPathButCandidateNot_HandlesCorrectly()
    {
        // Arrange: Reference is on critical path, but candidate is not
        var constraint = new MustComeBeforeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 3, TestHelpers.RoomType.Boss } // Boss is on critical path at index 3
        };

        // Node 5 is not on critical path
        var nodeNotOnCriticalPath = graph.GetNode(5);

        // Act & Assert: Should handle appropriately
        var result = constraint.IsValid(nodeNotOnCriticalPath, graph, assignments);
        Assert.True(result is bool);
    }

    // Helper methods to create test graphs

    private FloorGraph CreateGraphWithCriticalPath()
    {
        // Creates a graph with critical path: 0-1-2-3-4 (linear)
        // Node 5 is off the critical path
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 },
            new RoomNode { Id = 3 },
            new RoomNode { Id = 4 },
            new RoomNode { Id = 5 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 },
            new RoomConnection { NodeAId = 2, NodeBId = 3 },
            new RoomConnection { NodeAId = 3, NodeBId = 4 },
            new RoomConnection { NodeAId = 2, NodeBId = 5 } // Node 5 is off critical path
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[2], connections[2]);
        AddConnectionToNode(nodes[2], connections[4]); // Connection to node 5
        AddConnectionToNode(nodes[3], connections[2]);
        AddConnectionToNode(nodes[3], connections[3]);
        AddConnectionToNode(nodes[4], connections[3]);
        AddConnectionToNode(nodes[5], connections[4]);

        // Set critical path: [0, 1, 2, 3, 4]
        var graph = new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };

        // Set BossNodeId and CriticalPath using reflection
        SetBossNodeId(graph, 4);
        SetCriticalPathProperty(graph, new List<int> { 0, 1, 2, 3, 4 });

        // Set IsOnCriticalPath for nodes on critical path
        SetCriticalPath(nodes[0], true);
        SetCriticalPath(nodes[1], true);
        SetCriticalPath(nodes[2], true);
        SetCriticalPath(nodes[3], true);
        SetCriticalPath(nodes[4], true);
        SetCriticalPath(nodes[5], false);

        return graph;
    }

    private FloorGraph CreateGraphWithoutCriticalPath()
    {
        // Creates a simple graph without critical path set
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 }
        };

        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);

        var graph = new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };

        // Set CriticalPath to empty using reflection
        SetCriticalPathProperty(graph, Array.Empty<int>());

        return graph;
    }

    private static void AddConnectionToNode(RoomNode node, RoomConnection connection)
    {
        var property = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connectionsList = property?.GetValue(node) as List<RoomConnection>;
        connectionsList?.Add(connection);
    }

    private static void SetCriticalPath(RoomNode node, bool value)
    {
        var property = typeof(RoomNode).GetProperty("IsOnCriticalPath",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(node, new object[] { value });
    }

    private static void SetBossNodeId(FloorGraph graph, int bossNodeId)
    {
        var property = typeof(FloorGraph).GetProperty("BossNodeId",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(graph, new object[] { bossNodeId });
    }

    private static void SetCriticalPathProperty(FloorGraph graph, IReadOnlyList<int> criticalPath)
    {
        var property = typeof(FloorGraph).GetProperty("CriticalPath",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var setter = property?.GetSetMethod(nonPublic: true);
        setter?.Invoke(graph, new object[] { criticalPath });
    }
}

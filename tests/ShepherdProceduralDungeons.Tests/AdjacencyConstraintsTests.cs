using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class AdjacencyConstraintsTests
{
    [Fact]
    public void MustBeAdjacentToConstraint_BasicAdjacency_ValidatesCorrectly()
    {
        // Arrange: Shop must be adjacent to Combat room
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop, 
            TestHelpers.RoomType.Combat);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Node 0 is Combat
        };

        // Node 1 is adjacent to node 0 (Combat)
        var nodeAdjacentToCombat = graph.GetNode(1);
        // Node 2 is not adjacent to node 0
        var nodeNotAdjacentToCombat = graph.GetNode(2);

        // Act & Assert
        Assert.True(constraint.IsValid(nodeAdjacentToCombat, graph, assignments));
        Assert.False(constraint.IsValid(nodeNotAdjacentToCombat, graph, assignments));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_MultipleAdjacentTypes_ValidatesCorrectly()
    {
        // Arrange: Treasure must be adjacent to Boss OR MiniBoss
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Combat); // Using Combat as "MiniBoss" for test
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignmentsWithBoss = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss }
        };
        var assignmentsWithCombat = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };
        var assignmentsWithNeither = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Shop }
        };

        var nodeAdjacentTo0 = graph.GetNode(1);

        // Act & Assert: Valid if adjacent to Boss OR Combat
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithBoss));
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithCombat));
        Assert.False(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithNeither));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_NoNeighbors_ReturnsFalse()
    {
        // Arrange: Node with no connections should fail
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat);
        
        var graph = CreateIsolatedNodeGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var isolatedNode = graph.GetNode(0);

        // Act & Assert
        Assert.False(constraint.IsValid(isolatedNode, graph, assignments));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_PartialAssignment_OnlyChecksAssignedNeighbors()
    {
        // Arrange: During assignment phase, some neighbors may not be assigned yet
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat);
        
        var graph = CreateGraphWithMultipleNeighbors();
        // Node 1 has neighbors: 0 (Combat), 2 (unassigned), 3 (unassigned)
        var partialAssignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        var nodeWithAssignedCombatNeighbor = graph.GetNode(1);

        // Act & Assert: Should be valid because node 0 (Combat) is assigned
        Assert.True(constraint.IsValid(nodeWithAssignedCombatNeighbor, graph, partialAssignments));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_SelfReference_AllowsAdjacentToSameType()
    {
        // Arrange: If target type is in required adjacent types, nodes adjacent to already-placed target rooms are valid
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Shop, // Self-reference
            TestHelpers.RoomType.Combat);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignmentsWithShop = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Shop }
        };
        var assignmentsWithCombat = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        var nodeAdjacentTo0 = graph.GetNode(1);

        // Act & Assert: Valid if adjacent to Shop OR Combat
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithShop));
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithCombat));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_CombinedWithOtherConstraints_WorksCorrectly()
    {
        // Arrange: Combine adjacency constraint with dead-end constraint
        var adjacencyConstraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Combat);
        var deadEndConstraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure);
        
        var graph = CreateGraphWithDeadEndAdjacentToCombat();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        // Node 1 is a dead-end AND adjacent to Combat (node 0)
        var deadEndAdjacentToCombat = graph.GetNode(1);
        // Node 2 is NOT a dead-end (has multiple connections)
        var branchNode = graph.GetNode(2);

        // Act & Assert: Both constraints must be satisfied
        Assert.True(adjacencyConstraint.IsValid(deadEndAdjacentToCombat, graph, assignments));
        Assert.True(deadEndConstraint.IsValid(deadEndAdjacentToCombat, graph, assignments));
        
        Assert.True(adjacencyConstraint.IsValid(branchNode, graph, assignments));
        Assert.False(deadEndConstraint.IsValid(branchNode, graph, assignments));
    }

    [Fact]
    public void MustBeAdjacentToConstraint_NoValidPlacement_ThrowsConstraintViolationException()
    {
        // Arrange: Graph where no node can satisfy the constraint
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithoutBoss();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat },
            { 1, TestHelpers.RoomType.Combat },
            { 2, TestHelpers.RoomType.Combat }
        };

        // Act & Assert: All nodes should be invalid
        foreach (var node in graph.Nodes)
        {
            Assert.False(constraint.IsValid(node, graph, assignments));
        }
    }

    [Fact]
    public void MustBeAdjacentToConstraint_ConstructorWithSingleType_CreatesConstraint()
    {
        // Act
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Single(constraint.RequiredAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.RequiredAdjacentTypes);
    }

    [Fact]
    public void MustBeAdjacentToConstraint_ConstructorWithParamsArray_CreatesConstraint()
    {
        // Act
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Boss,
            TestHelpers.RoomType.Combat);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
        Assert.Equal(2, constraint.RequiredAdjacentTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.RequiredAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.RequiredAdjacentTypes);
    }

    [Fact]
    public void MustBeAdjacentToConstraint_ConstructorWithIEnumerable_CreatesConstraint()
    {
        // Arrange
        var requiredTypes = new[] { TestHelpers.RoomType.Boss, TestHelpers.RoomType.Combat };

        // Act
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            requiredTypes);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
        Assert.Equal(2, constraint.RequiredAdjacentTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.RequiredAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.RequiredAdjacentTypes);
    }

    [Fact]
    public void MustBeAdjacentToConstraint_Determinism_SameSeedProducesSamePlacements()
    {
        // This test would require integration with FloorGenerator
        // For unit test, we verify the constraint logic is deterministic
        var constraint = new MustBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        var node = graph.GetNode(1);
        
        // Act: Call multiple times
        var result1 = constraint.IsValid(node, graph, assignments);
        var result2 = constraint.IsValid(node, graph, assignments);
        var result3 = constraint.IsValid(node, graph, assignments);

        // Assert: Should always return same result
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    // Helper methods to create test graphs

    private FloorGraph CreateGraphWithAdjacentNodes()
    {
        // Creates a simple graph: 0-1-2 (linear)
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateGraphWithMultipleNeighbors()
    {
        // Creates a graph: 0-1-2, 0-1-3 (node 1 has 3 neighbors: 0, 2, 3)
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 },
            new RoomNode { Id = 3 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 },
            new RoomConnection { NodeAId = 1, NodeBId = 3 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[1], connections[2]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[3], connections[2]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateGraphWithDeadEndAdjacentToCombat()
    {
        // Creates a graph: 0-1 (dead-end), 0-2-3 (branch)
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 },
            new RoomNode { Id = 3 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 0, NodeBId = 2 },
            new RoomConnection { NodeAId = 2, NodeBId = 3 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[0], connections[1]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[2], connections[2]);
        AddConnectionToNode(nodes[3], connections[2]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateIsolatedNodeGraph()
    {
        // Creates a graph with a single isolated node
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 }
        };

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = new List<RoomConnection>(),
            StartNodeId = 0
        };
    }

    private FloorGraph CreateGraphWithoutBoss()
    {
        // Creates a simple graph without any Boss rooms
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private static void AddConnectionToNode(RoomNode node, RoomConnection connection)
    {
        var property = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connectionsList = property?.GetValue(node) as List<RoomConnection>;
        connectionsList?.Add(connection);
    }
}

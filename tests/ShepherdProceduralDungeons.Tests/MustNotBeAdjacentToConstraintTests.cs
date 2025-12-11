using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class MustNotBeAdjacentToConstraintTests
{
    [Fact]
    public void MustNotBeAdjacentToConstraint_SingleForbiddenType_ValidatesCorrectly()
    {
        // Arrange: Treasure must NOT be adjacent to Spawn room
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignmentsWithSpawn = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Spawn } // Node 0 is Spawn
        };
        var assignmentsWithCombat = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Node 0 is Combat
        };

        // Node 1 is adjacent to node 0
        var nodeAdjacentTo0 = graph.GetNode(1);
        // Node 2 is not adjacent to node 0
        var nodeNotAdjacentTo0 = graph.GetNode(2);

        // Act & Assert: Invalid if adjacent to Spawn, valid if adjacent to Combat
        Assert.False(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithSpawn));
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithCombat));
        Assert.True(constraint.IsValid(nodeNotAdjacentTo0, graph, assignmentsWithSpawn));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_MultipleForbiddenTypes_ValidatesCorrectly()
    {
        // Arrange: Shop must NOT be adjacent to Shop OR Treasure
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Treasure);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignmentsWithShop = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Shop }
        };
        var assignmentsWithTreasure = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Treasure }
        };
        var assignmentsWithCombat = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        var nodeAdjacentTo0 = graph.GetNode(1);

        // Act & Assert: Invalid if adjacent to Shop OR Treasure, valid if adjacent to Combat
        Assert.False(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithShop));
        Assert.False(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithTreasure));
        Assert.True(constraint.IsValid(nodeAdjacentTo0, graph, assignmentsWithCombat));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_NoConnections_ReturnsTrue()
    {
        // Arrange: Node with no connections should be valid (can't violate adjacency constraint)
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateIsolatedNodeGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        var isolatedNode = graph.GetNode(0);

        // Act & Assert: Should be valid because there are no neighbors to check
        Assert.True(constraint.IsValid(isolatedNode, graph, assignments));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_UnassignedNeighbors_DoesNotCauseViolation()
    {
        // Arrange: During assignment phase, some neighbors may not be assigned yet
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateGraphWithMultipleNeighbors();
        // Node 1 has neighbors: 0 (Combat), 2 (unassigned), 3 (unassigned)
        var partialAssignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }
        };

        var nodeWithUnassignedNeighbors = graph.GetNode(1);

        // Act & Assert: Should be valid because unassigned neighbors don't violate constraint
        Assert.True(constraint.IsValid(nodeWithUnassignedNeighbors, graph, partialAssignments));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_ValidPlacement_NoNeighborsHaveForbiddenTypes()
    {
        // Arrange: Treasure must NOT be adjacent to Spawn
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }, // Node 0 is Combat (not forbidden)
            { 2, TestHelpers.RoomType.Boss }    // Node 2 is Boss (not forbidden)
        };

        // Node 1 is adjacent to Combat (node 0) and Boss (node 2) - both are valid
        var nodeWithValidNeighbors = graph.GetNode(1);

        // Act & Assert: Should be valid because no neighbors have forbidden types
        Assert.True(constraint.IsValid(nodeWithValidNeighbors, graph, assignments));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_InvalidPlacement_AnyNeighborHasForbiddenType()
    {
        // Arrange: Treasure must NOT be adjacent to Spawn
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateGraphWithMultipleNeighbors();
        // Node 1 has neighbors: 0 (Spawn - forbidden!), 2 (Combat), 3 (Boss)
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Spawn },  // Forbidden type
            { 2, TestHelpers.RoomType.Combat },
            { 3, TestHelpers.RoomType.Boss }
        };

        var nodeWithForbiddenNeighbor = graph.GetNode(1);

        // Act & Assert: Should be invalid because node 0 (Spawn) is a forbidden neighbor
        Assert.False(constraint.IsValid(nodeWithForbiddenNeighbor, graph, assignments));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_EmptyForbiddenTypes_ThrowsArgumentException()
    {
        // Act & Assert: Should throw ArgumentException when no forbidden types provided
        Assert.Throws<ArgumentException>(() =>
            new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Treasure));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_NullForbiddenTypes_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<TestHelpers.RoomType>? nullTypes = null;

        // Act & Assert: Should throw ArgumentNullException when null IEnumerable provided
        Assert.Throws<ArgumentNullException>(() =>
            new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Treasure,
                nullTypes!));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_ConstructorWithSingleType_CreatesConstraint()
    {
        // Act
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
        Assert.Single(constraint.ForbiddenAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Spawn, constraint.ForbiddenAdjacentTypes);
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_ConstructorWithParamsArray_CreatesConstraint()
    {
        // Act
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Treasure);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Equal(2, constraint.ForbiddenAdjacentTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Shop, constraint.ForbiddenAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Treasure, constraint.ForbiddenAdjacentTypes);
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_ConstructorWithIEnumerable_CreatesConstraint()
    {
        // Arrange
        var forbiddenTypes = new[] { TestHelpers.RoomType.Spawn, TestHelpers.RoomType.Boss };

        // Act
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            forbiddenTypes);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Treasure, constraint.TargetRoomType);
        Assert.Equal(2, constraint.ForbiddenAdjacentTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Spawn, constraint.ForbiddenAdjacentTypes);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ForbiddenAdjacentTypes);
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_EmptyParamsArray_ThrowsArgumentException()
    {
        // Act & Assert: Should throw ArgumentException when empty params array provided
        Assert.Throws<ArgumentException>(() =>
            new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Treasure,
                Array.Empty<TestHelpers.RoomType>()));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_EmptyIEnumerable_ThrowsArgumentException()
    {
        // Arrange
        var emptyTypes = Enumerable.Empty<TestHelpers.RoomType>();

        // Act & Assert: Should throw ArgumentException when empty IEnumerable provided
        Assert.Throws<ArgumentException>(() =>
            new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Treasure,
                emptyTypes));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_CombinedWithOtherConstraints_WorksCorrectly()
    {
        // Arrange: Combine adjacency constraint with dead-end constraint
        var adjacencyConstraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        var deadEndConstraint = new MustBeDeadEndConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure);
        
        var graph = CreateGraphWithDeadEndNotAdjacentToSpawn();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Node 0 is Combat (not Spawn)
        };

        // Node 1 is a dead-end AND NOT adjacent to Spawn (node 0 is Combat)
        var deadEndNotAdjacentToSpawn = graph.GetNode(1);
        // Node 2 is NOT a dead-end (has multiple connections)
        var branchNode = graph.GetNode(2);

        // Act & Assert: Both constraints must be satisfied
        Assert.True(adjacencyConstraint.IsValid(deadEndNotAdjacentToSpawn, graph, assignments));
        Assert.True(deadEndConstraint.IsValid(deadEndNotAdjacentToSpawn, graph, assignments));
        
        Assert.True(adjacencyConstraint.IsValid(branchNode, graph, assignments));
        Assert.False(deadEndConstraint.IsValid(branchNode, graph, assignments));
    }

    [Fact]
    public void MustNotBeAdjacentToConstraint_Determinism_SameInputProducesSameResult()
    {
        // Arrange: Verify the constraint logic is deterministic
        var constraint = new MustNotBeAdjacentToConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure,
            TestHelpers.RoomType.Spawn);
        
        var graph = CreateGraphWithAdjacentNodes();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Spawn }
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

    // Helper methods to create test graphs (reused from AdjacencyConstraintsTests)

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

    private FloorGraph CreateGraphWithDeadEndNotAdjacentToSpawn()
    {
        // Creates a graph: 0-1 (dead-end), 0-2-3 (branch)
        // Node 0 is Combat, Node 1 is dead-end adjacent to Combat (not Spawn)
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

    private static void AddConnectionToNode(RoomNode node, RoomConnection connection)
    {
        var property = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connectionsList = property?.GetValue(node) as List<RoomConnection>;
        connectionsList?.Add(connection);
    }
}

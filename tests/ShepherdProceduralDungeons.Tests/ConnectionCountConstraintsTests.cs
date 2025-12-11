using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class ConnectionCountConstraintsTests
{
    [Fact]
    public void MinConnectionCountConstraint_ValidHubRoom_ValidatesCorrectly()
    {
        // Arrange: Hub room requires at least 3 connections
        var constraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss, 
            minConnections: 3);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with 3 connections (hub)
        var hubNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount >= 3);
        // Node with 2 connections (not a hub)
        var branchNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 2);
        // Node with 1 connection (dead-end)
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);

        // Act & Assert
        if (hubNode != null)
            Assert.True(constraint.IsValid(hubNode, graph, assignments));
        
        if (branchNode != null)
            Assert.False(constraint.IsValid(branchNode, graph, assignments));
        
        if (deadEndNode != null)
            Assert.False(constraint.IsValid(deadEndNode, graph, assignments));
    }

    [Fact]
    public void MaxConnectionCountConstraint_ValidLinearRoom_ValidatesCorrectly()
    {
        // Arrange: Linear room requires at most 2 connections
        var constraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            maxConnections: 2);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with 1 connection (valid)
        var deadEndNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);
        // Node with 2 connections (valid)
        var linearNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 2);
        // Node with 3+ connections (invalid)
        var hubNode = graph.Nodes.FirstOrDefault(n => n.ConnectionCount >= 3);

        // Act & Assert
        if (deadEndNode != null)
            Assert.True(constraint.IsValid(deadEndNode, graph, assignments));
        
        if (linearNode != null)
            Assert.True(constraint.IsValid(linearNode, graph, assignments));
        
        if (hubNode != null)
            Assert.False(constraint.IsValid(hubNode, graph, assignments));
    }

    [Fact]
    public void CombinedMinAndMax_ExactCount_ValidatesCorrectly()
    {
        // Arrange: Combined min(2) and max(2) = exactly 2 connections
        var minConstraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            minConnections: 2);
        var maxConstraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            maxConnections: 2);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with exactly 2 connections (valid for both)
        var exact2Node = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 2);
        // Node with 1 connection (fails min constraint)
        var node1 = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);
        // Node with 3+ connections (fails max constraint)
        var node3Plus = graph.Nodes.FirstOrDefault(n => n.ConnectionCount >= 3);

        // Act & Assert: Both constraints must be satisfied for exact count
        if (exact2Node != null)
        {
            Assert.True(minConstraint.IsValid(exact2Node, graph, assignments));
            Assert.True(maxConstraint.IsValid(exact2Node, graph, assignments));
        }
        
        if (node1 != null)
        {
            Assert.False(minConstraint.IsValid(node1, graph, assignments));
            Assert.True(maxConstraint.IsValid(node1, graph, assignments));
        }
        
        if (node3Plus != null)
        {
            Assert.True(minConstraint.IsValid(node3Plus, graph, assignments));
            Assert.False(maxConstraint.IsValid(node3Plus, graph, assignments));
        }
    }

    [Fact]
    public void MinConnectionCountConstraint_EdgeCaseZero_AllowsAllNodes()
    {
        // Arrange: Min connections = 0 should allow all nodes
        var constraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            minConnections: 0);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Act & Assert: All nodes should be valid
        foreach (var node in graph.Nodes)
        {
            Assert.True(constraint.IsValid(node, graph, assignments));
        }
    }

    [Fact]
    public void MaxConnectionCountConstraint_EdgeCaseLargeNumber_AllowsAllNodes()
    {
        // Arrange: Max connections = very large number should allow all nodes
        var constraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            maxConnections: 100);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Act & Assert: All nodes should be valid
        foreach (var node in graph.Nodes)
        {
            Assert.True(constraint.IsValid(node, graph, assignments));
        }
    }

    [Fact]
    public void MinConnectionCountConstraint_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert: Constructor should throw for negative values
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MinConnectionCountConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Combat, 
                minConnections: -1));
    }

    [Fact]
    public void MaxConnectionCountConstraint_NegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert: Constructor should throw for negative values
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MaxConnectionCountConstraint<TestHelpers.RoomType>(
                TestHelpers.RoomType.Combat, 
                maxConnections: -1));
    }

    [Fact]
    public void MinConnectionCountConstraint_PropertyAccess_ReturnsCorrectValues()
    {
        // Arrange
        var roomType = TestHelpers.RoomType.Boss;
        var minConnections = 3;

        // Act
        var constraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            roomType, 
            minConnections);

        // Assert
        Assert.Equal(roomType, constraint.TargetRoomType);
        Assert.Equal(minConnections, constraint.MinConnections);
    }

    [Fact]
    public void MaxConnectionCountConstraint_PropertyAccess_ReturnsCorrectValues()
    {
        // Arrange
        var roomType = TestHelpers.RoomType.Combat;
        var maxConnections = 2;

        // Act
        var constraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            roomType, 
            maxConnections);

        // Assert
        Assert.Equal(roomType, constraint.TargetRoomType);
        Assert.Equal(maxConnections, constraint.MaxConnections);
    }

    [Fact]
    public void MinConnectionCountConstraint_CombinedWithMinDistance_WorksCorrectly()
    {
        // Arrange: Hub room (3+ connections) far from start
        var minConnectionConstraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss, 
            minConnections: 3);
        var minDistanceConstraint = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Boss, 
            minDistance: 2);
        
        var graph = CreateGraphWithDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Find nodes that satisfy both constraints
        var validNodes = graph.Nodes
            .Where(n => n.ConnectionCount >= 3 && n.DistanceFromStart >= 2)
            .ToList();
        
        var invalidNodes = graph.Nodes
            .Where(n => !(n.ConnectionCount >= 3 && n.DistanceFromStart >= 2))
            .ToList();

        // Act & Assert: Both constraints must be satisfied
        foreach (var node in validNodes)
        {
            Assert.True(minConnectionConstraint.IsValid(node, graph, assignments));
            Assert.True(minDistanceConstraint.IsValid(node, graph, assignments));
        }
        
        foreach (var node in invalidNodes)
        {
            var connectionValid = minConnectionConstraint.IsValid(node, graph, assignments);
            var distanceValid = minDistanceConstraint.IsValid(node, graph, assignments);
            // At least one constraint should fail
            Assert.False(connectionValid && distanceValid);
        }
    }

    [Fact]
    public void MaxConnectionCountConstraint_CombinedWithNotOnCriticalPath_WorksCorrectly()
    {
        // Arrange: Linear room (max 2 connections) not on critical path
        var maxConnectionConstraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure, 
            maxConnections: 2);
        var notOnCriticalPathConstraint = new NotOnCriticalPathConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Treasure);
        
        var graph = CreateGraphWithCriticalPath();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Set up critical path
        var node0 = graph.GetNode(0);
        var node1 = graph.GetNode(1);
        var node2 = graph.GetNode(2);
        var node3 = graph.GetNode(3);

        SetCriticalPath(node0, true);
        SetCriticalPath(node1, true);
        SetCriticalPath(node3, true);
        // Node 2 is not on critical path

        // Act & Assert: Both constraints must be satisfied
        // Node 2: not on critical path, check connection count
        if (node2.ConnectionCount <= 2)
        {
            Assert.True(maxConnectionConstraint.IsValid(node2, graph, assignments));
            Assert.True(notOnCriticalPathConstraint.IsValid(node2, graph, assignments));
        }
        
        // Node 1: on critical path, should fail notOnCriticalPath constraint
        Assert.False(notOnCriticalPathConstraint.IsValid(node1, graph, assignments));
    }

    [Fact]
    public void MinConnectionCountConstraint_BoundaryConditions_ValidatesCorrectly()
    {
        // Arrange: Test boundary conditions (exactly min, one less than min)
        var constraint = new MinConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            minConnections: 2);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with exactly 2 connections (boundary - should be valid)
        var exact2Node = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 2);
        // Node with 1 connection (one less - should be invalid)
        var node1 = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);
        // Node with 3 connections (above min - should be valid)
        var node3 = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 3);

        // Act & Assert
        if (exact2Node != null)
            Assert.True(constraint.IsValid(exact2Node, graph, assignments));
        
        if (node1 != null)
            Assert.False(constraint.IsValid(node1, graph, assignments));
        
        if (node3 != null)
            Assert.True(constraint.IsValid(node3, graph, assignments));
    }

    [Fact]
    public void MaxConnectionCountConstraint_BoundaryConditions_ValidatesCorrectly()
    {
        // Arrange: Test boundary conditions (exactly max, one more than max)
        var constraint = new MaxConnectionCountConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Combat, 
            maxConnections: 2);
        
        var graph = CreateGraphWithVariousConnectionCounts();
        var assignments = new Dictionary<int, TestHelpers.RoomType>();

        // Node with exactly 2 connections (boundary - should be valid)
        var exact2Node = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 2);
        // Node with 1 connection (below max - should be valid)
        var node1 = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 1);
        // Node with 3 connections (above max - should be invalid)
        var node3 = graph.Nodes.FirstOrDefault(n => n.ConnectionCount == 3);

        // Act & Assert
        if (exact2Node != null)
            Assert.True(constraint.IsValid(exact2Node, graph, assignments));
        
        if (node1 != null)
            Assert.True(constraint.IsValid(node1, graph, assignments));
        
        if (node3 != null)
            Assert.False(constraint.IsValid(node3, graph, assignments));
    }

    // Helper methods to create test graphs

    private FloorGraph CreateGraphWithVariousConnectionCounts()
    {
        // Creates a graph with nodes having 1, 2, 3, and 4 connections
        // Structure: 0-1-2-3, 1-4, 1-5 (node 1 has 3 connections: 0, 2, 4, 5)
        // Node 0: 1 connection (dead-end)
        // Node 1: 4 connections (hub)
        // Node 2: 2 connections (linear)
        // Node 3: 1 connection (dead-end)
        // Node 4: 1 connection (dead-end)
        // Node 5: 1 connection (dead-end)
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
            new RoomConnection { NodeAId = 1, NodeBId = 4 },
            new RoomConnection { NodeAId = 1, NodeBId = 5 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[1], connections[3]);
        AddConnectionToNode(nodes[1], connections[4]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[2], connections[2]);
        AddConnectionToNode(nodes[3], connections[2]);
        AddConnectionToNode(nodes[4], connections[3]);
        AddConnectionToNode(nodes[5], connections[4]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateGraphWithDistances()
    {
        // Creates a graph with calculated distances
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(8, 0.3f, rng); // Some branching to create hub nodes
        
        return graph;
    }

    private FloorGraph CreateGraphWithCriticalPath()
    {
        // Creates a simple graph: 0-1-2, 0-3 (node 0 is start)
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
            new RoomConnection { NodeAId = 0, NodeBId = 3 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[0], connections[2]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[3], connections[2]);

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

    private static void AddConnectionToNode(RoomNode node, RoomConnection connection)
    {
        var property = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var connectionsList = property?.GetValue(node) as List<RoomConnection>;
        connectionsList?.Add(connection);
    }
}

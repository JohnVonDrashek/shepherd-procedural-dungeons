using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Tests;

namespace ShepherdProceduralDungeons.Tests;

public class DistanceBasedRoomTypeConstraintsTests
{
    [Fact]
    public void MinDistanceFromRoomTypeConstraint_ValidPlacement_WhenDistanceGreaterThanOrEqualToMin_ReturnsTrue()
    {
        // Arrange: Secret room must be at least 3 steps from Boss room
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss } // Boss at node 0
        };

        // Node 3 is at distance 3 from node 0
        var nodeAtDistance3 = graph.GetNode(3);
        // Node 4 is at distance 4 from node 0
        var nodeAtDistance4 = graph.GetNode(4);

        // Act & Assert: Should pass when distance >= 3
        Assert.True(constraint.IsValid(nodeAtDistance3, graph, assignments));
        Assert.True(constraint.IsValid(nodeAtDistance4, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_InvalidPlacement_WhenDistanceLessThanMin_ReturnsFalse()
    {
        // Arrange: Secret room must be at least 3 steps from Boss room
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss } // Boss at node 0
        };

        // Node 1 is at distance 1 from node 0
        var nodeAtDistance1 = graph.GetNode(1);
        // Node 2 is at distance 2 from node 0
        var nodeAtDistance2 = graph.GetNode(2);

        // Act & Assert: Should fail when distance < 3
        Assert.False(constraint.IsValid(nodeAtDistance1, graph, assignments));
        Assert.False(constraint.IsValid(nodeAtDistance2, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_ValidPlacement_WhenDistanceLessThanOrEqualToMax_ReturnsTrue()
    {
        // Arrange: Rest room must be within 2 steps of Combat room
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret, // Using Secret as "Rest" for test
            TestHelpers.RoomType.Combat,
            maxDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Combat at node 0
        };

        // Node 1 is at distance 1 from node 0
        var nodeAtDistance1 = graph.GetNode(1);
        // Node 2 is at distance 2 from node 0
        var nodeAtDistance2 = graph.GetNode(2);

        // Act & Assert: Should pass when distance <= 2
        Assert.True(constraint.IsValid(nodeAtDistance1, graph, assignments));
        Assert.True(constraint.IsValid(nodeAtDistance2, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_InvalidPlacement_WhenDistanceGreaterThanMax_ReturnsFalse()
    {
        // Arrange: Rest room must be within 2 steps of Combat room
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret, // Using Secret as "Rest" for test
            TestHelpers.RoomType.Combat,
            maxDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Combat at node 0
        };

        // Node 3 is at distance 3 from node 0
        var nodeAtDistance3 = graph.GetNode(3);
        // Node 4 is at distance 4 from node 0
        var nodeAtDistance4 = graph.GetNode(4);

        // Act & Assert: Should fail when distance > 2
        Assert.False(constraint.IsValid(nodeAtDistance3, graph, assignments));
        Assert.False(constraint.IsValid(nodeAtDistance4, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_MultipleReferenceTypes_ChecksDistanceToNearestOfAny()
    {
        // Arrange: Shop must be within 2 steps of Combat OR Boss
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            maxDistance: 2,
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithMultipleReferenceTypes();
        // Combat at node 0, Boss at node 4 (distance 4 from node 0)
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat },
            { 4, TestHelpers.RoomType.Boss }
        };

        // Node 2 is at distance 2 from Combat (node 0) and distance 2 from Boss (node 4)
        // Should pass because distance to Combat (2) <= maxDistance (2)
        var nodeAtDistance2FromCombat = graph.GetNode(2);
        Assert.True(constraint.IsValid(nodeAtDistance2FromCombat, graph, assignments));

        // Node 1 is at distance 1 from Combat (node 0) - nearest reference type
        var nodeAtDistance1FromCombat = graph.GetNode(1);
        Assert.True(constraint.IsValid(nodeAtDistance1FromCombat, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_MultipleReferenceTypes_ChecksDistanceToNearestOfAny()
    {
        // Arrange: Secret must be at least 3 steps from Combat OR Boss
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            minDistance: 3,
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);
        
        var graph = CreateGraphWithMultipleReferenceTypes();
        // Combat at node 0, Boss at node 4 (distance 4 from node 0)
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat },
            { 4, TestHelpers.RoomType.Boss }
        };

        // Node 3 is at distance 3 from Combat (node 0) and distance 1 from Boss (node 4)
        // Should pass because minimum distance to any reference type is 1, but we check >= 3
        // Actually, nearest is Boss at distance 1, so should fail
        var nodeAtDistance3FromCombat = graph.GetNode(3);
        Assert.False(constraint.IsValid(nodeAtDistance3FromCombat, graph, assignments));

        // Node 5 is at distance 5 from Combat and distance 1 from Boss
        // Nearest is Boss at distance 1, so should fail
        var node5 = graph.GetNode(5);
        Assert.False(constraint.IsValid(node5, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_ReferenceTypeNotAssigned_ReturnsTrue()
    {
        // Arrange: Constraint evaluated before reference room type is assigned
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>(); // No Boss assigned yet

        var anyNode = graph.GetNode(1);

        // Act & Assert: Should return true (permissive) to allow assignment order flexibility
        Assert.True(constraint.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_ReferenceTypeNotAssigned_ReturnsTrue()
    {
        // Arrange: Constraint evaluated before reference room type is assigned
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat,
            maxDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>(); // No Combat assigned yet

        var anyNode = graph.GetNode(1);

        // Act & Assert: Should return true (permissive) to allow assignment order flexibility
        Assert.True(constraint.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_NoReferenceRoomsExist_ReturnsTrue()
    {
        // Arrange: Constraint where no rooms of reference type exist yet
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat }, // No Boss rooms assigned
            { 1, TestHelpers.RoomType.Combat }
        };

        var anyNode = graph.GetNode(2);

        // Act & Assert: Should return true when no reference rooms exist
        Assert.True(constraint.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_NoReferenceRoomsExist_ReturnsFalse()
    {
        // Arrange: Constraint where no rooms of reference type exist yet
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat,
            maxDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss }, // No Combat rooms assigned
            { 1, TestHelpers.RoomType.Boss }
        };

        var anyNode = graph.GetNode(2);

        // Act & Assert: Should return false when no reference rooms exist (can't satisfy max distance)
        Assert.False(constraint.IsValid(anyNode, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_SameRoomType_ValidatesCorrectly()
    {
        // Arrange: Secret rooms must be at least 2 steps from other Secret rooms
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Secret,
            minDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Secret } // Secret at node 0
        };

        // Node 1 is at distance 1 from node 0 (too close)
        var nodeAtDistance1 = graph.GetNode(1);
        // Node 2 is at distance 2 from node 0 (exactly min distance)
        var nodeAtDistance2 = graph.GetNode(2);
        // Node 3 is at distance 3 from node 0 (greater than min distance)
        var nodeAtDistance3 = graph.GetNode(3);

        // Act & Assert
        Assert.False(constraint.IsValid(nodeAtDistance1, graph, assignments));
        Assert.True(constraint.IsValid(nodeAtDistance2, graph, assignments));
        Assert.True(constraint.IsValid(nodeAtDistance3, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_SameRoomType_ValidatesCorrectly()
    {
        // Arrange: Shop rooms must be within 2 steps of other Shop rooms
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Shop,
            maxDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Shop } // Shop at node 0
        };

        // Node 1 is at distance 1 from node 0 (within max)
        var nodeAtDistance1 = graph.GetNode(1);
        // Node 2 is at distance 2 from node 0 (exactly max distance)
        var nodeAtDistance2 = graph.GetNode(2);
        // Node 3 is at distance 3 from node 0 (too far)
        var nodeAtDistance3 = graph.GetNode(3);

        // Act & Assert
        Assert.True(constraint.IsValid(nodeAtDistance1, graph, assignments));
        Assert.True(constraint.IsValid(nodeAtDistance2, graph, assignments));
        Assert.False(constraint.IsValid(nodeAtDistance3, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_NoPathExists_ReturnsTrue()
    {
        // Arrange: If no path exists between nodes, min distance constraint should be satisfied
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        
        var graph = CreateDisconnectedGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss } // Boss in disconnected component
        };

        // Node 3 is in a different disconnected component
        var disconnectedNode = graph.GetNode(3);

        // Act & Assert: No path exists, so min distance constraint is satisfied (infinite distance >= 3)
        Assert.True(constraint.IsValid(disconnectedNode, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_NoPathExists_ReturnsFalse()
    {
        // Arrange: If no path exists between nodes, max distance constraint cannot be satisfied
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat,
            maxDistance: 2);
        
        var graph = CreateDisconnectedGraph();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Combat } // Combat in disconnected component
        };

        // Node 3 is in a different disconnected component
        var disconnectedNode = graph.GetNode(3);

        // Act & Assert: No path exists, so max distance constraint cannot be satisfied
        Assert.False(constraint.IsValid(disconnectedNode, graph, assignments));
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_CombinedWithOtherConstraints_WorksCorrectly()
    {
        // Arrange: Combine with MinDistanceFromStartConstraint
        var distanceFromRoomType = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);
        var distanceFromStart = new MinDistanceFromStartConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            minDistance: 2);
        
        var graph = CreateGraphWithKnownDistances();
        var assignments = new Dictionary<int, TestHelpers.RoomType>
        {
            { 0, TestHelpers.RoomType.Boss } // Boss at start (node 0)
        };

        // Node 3 is at distance 3 from Boss (satisfies distanceFromRoomType) and distance 3 from start (satisfies distanceFromStart)
        var node3 = graph.GetNode(3);
        Assert.True(distanceFromRoomType.IsValid(node3, graph, assignments));
        Assert.True(distanceFromStart.IsValid(node3, graph, assignments));

        // Node 1 is at distance 1 from Boss (fails distanceFromRoomType) and distance 1 from start (fails distanceFromStart)
        var node1 = graph.GetNode(1);
        Assert.False(distanceFromRoomType.IsValid(node1, graph, assignments));
        Assert.False(distanceFromStart.IsValid(node1, graph, assignments));
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_ConstructorWithSingleType_CreatesConstraint()
    {
        // Act
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            TestHelpers.RoomType.Combat,
            maxDistance: 2);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Single(constraint.ReferenceRoomTypes);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.ReferenceRoomTypes);
        Assert.Equal(2, constraint.MaxDistance);
    }

    [Fact]
    public void MaxDistanceFromRoomTypeConstraint_ConstructorWithParamsArray_CreatesConstraint()
    {
        // Act
        var constraint = new MaxDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Shop,
            maxDistance: 2,
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Shop, constraint.TargetRoomType);
        Assert.Equal(2, constraint.ReferenceRoomTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.ReferenceRoomTypes);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ReferenceRoomTypes);
        Assert.Equal(2, constraint.MaxDistance);
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_ConstructorWithSingleType_CreatesConstraint()
    {
        // Act
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            TestHelpers.RoomType.Boss,
            minDistance: 3);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Secret, constraint.TargetRoomType);
        Assert.Single(constraint.ReferenceRoomTypes);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ReferenceRoomTypes);
        Assert.Equal(3, constraint.MinDistance);
    }

    [Fact]
    public void MinDistanceFromRoomTypeConstraint_ConstructorWithParamsArray_CreatesConstraint()
    {
        // Act
        var constraint = new MinDistanceFromRoomTypeConstraint<TestHelpers.RoomType>(
            TestHelpers.RoomType.Secret,
            minDistance: 3,
            TestHelpers.RoomType.Combat,
            TestHelpers.RoomType.Boss);

        // Assert
        Assert.Equal(TestHelpers.RoomType.Secret, constraint.TargetRoomType);
        Assert.Equal(2, constraint.ReferenceRoomTypes.Count);
        Assert.Contains(TestHelpers.RoomType.Combat, constraint.ReferenceRoomTypes);
        Assert.Contains(TestHelpers.RoomType.Boss, constraint.ReferenceRoomTypes);
        Assert.Equal(3, constraint.MinDistance);
    }

    // Helper methods to create test graphs

    private FloorGraph CreateGraphWithKnownDistances()
    {
        // Creates a linear graph: 0-1-2-3-4 (distances: 0,1,2,3,4 from node 0)
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 },
            new RoomNode { Id = 3 },
            new RoomNode { Id = 4 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 },
            new RoomConnection { NodeAId = 2, NodeBId = 3 },
            new RoomConnection { NodeAId = 3, NodeBId = 4 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[2], connections[2]);
        AddConnectionToNode(nodes[3], connections[2]);
        AddConnectionToNode(nodes[3], connections[3]);
        AddConnectionToNode(nodes[4], connections[3]);

        // Calculate distances from start using BFS
        CalculateDistancesFromStart(nodes, startId: 0);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateGraphWithMultipleReferenceTypes()
    {
        // Creates a graph: 0-1-2-3-4-5
        // Node 0: Combat
        // Node 4: Boss
        // Test nodes at various distances from both
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
            new RoomConnection { NodeAId = 4, NodeBId = 5 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[2], connections[2]);
        AddConnectionToNode(nodes[3], connections[2]);
        AddConnectionToNode(nodes[3], connections[3]);
        AddConnectionToNode(nodes[4], connections[3]);
        AddConnectionToNode(nodes[4], connections[4]);
        AddConnectionToNode(nodes[5], connections[4]);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private FloorGraph CreateDisconnectedGraph()
    {
        // Creates two disconnected components: 0-1-2 and 3-4
        var nodes = new List<RoomNode>
        {
            new RoomNode { Id = 0 },
            new RoomNode { Id = 1 },
            new RoomNode { Id = 2 },
            new RoomNode { Id = 3 },
            new RoomNode { Id = 4 }
        };

        var connections = new List<RoomConnection>
        {
            new RoomConnection { NodeAId = 0, NodeBId = 1 },
            new RoomConnection { NodeAId = 1, NodeBId = 2 },
            new RoomConnection { NodeAId = 3, NodeBId = 4 }
        };

        // Add connections to nodes using reflection
        AddConnectionToNode(nodes[0], connections[0]);
        AddConnectionToNode(nodes[1], connections[0]);
        AddConnectionToNode(nodes[1], connections[1]);
        AddConnectionToNode(nodes[2], connections[1]);
        AddConnectionToNode(nodes[3], connections[2]);
        AddConnectionToNode(nodes[4], connections[2]);

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

    private static void CalculateDistancesFromStart(List<RoomNode> nodes, int startId)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<(int nodeId, int distance)>();
        queue.Enqueue((startId, 0));

        var connectionsProperty = typeof(RoomNode).GetProperty("Connections",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var distanceProperty = typeof(RoomNode).GetProperty("DistanceFromStart",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var distanceSetter = distanceProperty?.GetSetMethod(nonPublic: true);

        while (queue.Count > 0)
        {
            var (nodeId, distance) = queue.Dequeue();
            if (visited.Contains(nodeId)) continue;
            visited.Add(nodeId);

            var node = nodes.First(n => n.Id == nodeId);
            distanceSetter?.Invoke(node, new object[] { distance });

            var connections = connectionsProperty?.GetValue(node) as List<RoomConnection>;
            if (connections != null)
            {
                foreach (var conn in connections)
                {
                    int neighborId = conn.GetOtherNodeId(nodeId);
                    if (!visited.Contains(neighborId))
                    {
                        queue.Enqueue((neighborId, distance + 1));
                    }
                }
            }
        }
    }
}

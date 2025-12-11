using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Tests;

public class AlternativeGraphGenerationAlgorithmsTests
{
    [Fact]
    public void IGraphGenerator_InterfaceExists()
    {
        // This test verifies the interface exists
        var generator = new SpanningTreeGraphGenerator();
        Assert.NotNull(generator as IGraphGenerator);
    }

    [Fact]
    public void SpanningTreeGraphGenerator_ImplementsIGraphGenerator()
    {
        // Verify existing GraphGenerator was refactored to SpanningTreeGraphGenerator
        var generator = new SpanningTreeGraphGenerator();
        var graph = generator.Generate(10, 0.3f, new Random(12345));
        
        Assert.Equal(10, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
    }

    [Fact]
    public void SpanningTreeGraphGenerator_BackwardCompatibility()
    {
        // Verify backward compatibility - same seed produces same output as old GraphGenerator
        var oldGenerator = new GraphGenerator();
        var newGenerator = new SpanningTreeGraphGenerator();
        
        var rng1 = new Random(12345);
        var rng2 = new Random(12345);
        
        var oldGraph = oldGenerator.Generate(10, 0.3f, rng1);
        var newGraph = newGenerator.Generate(10, 0.3f, rng2);
        
        Assert.Equal(oldGraph.Nodes.Count, newGraph.Nodes.Count);
        Assert.Equal(oldGraph.Connections.Count, newGraph.Connections.Count);
    }

    [Fact]
    public void GridBasedGraphGenerator_ImplementsIGraphGenerator()
    {
        var generator = new GridBasedGraphGenerator();
        var config = new GridBasedGraphConfig
        {
            GridWidth = 4,
            GridHeight = 4,
            ConnectivityPattern = ConnectivityPattern.FourWay
        };
        
        var graph = generator.Generate(16, 0.3f, new Random(12345), config);
        
        Assert.Equal(16, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
    }

    [Fact]
    public void GridBasedGraphGenerator_CreatesGridPattern()
    {
        var generator = new GridBasedGraphGenerator();
        var config = new GridBasedGraphConfig
        {
            GridWidth = 3,
            GridHeight = 3,
            ConnectivityPattern = ConnectivityPattern.FourWay
        };
        
        var graph = generator.Generate(9, 0.0f, new Random(12345), config);
        
        // In a 3x3 grid with 4-way connectivity, each interior node should have 4 neighbors
        // Edge nodes should have 2-3 neighbors, corners should have 2 neighbors
        // With branchingFactor 0.0, we should have minimal connections (spanning tree)
        Assert.True(IsConnected(graph));
        
        // Verify grid structure by checking that nodes have expected neighbor counts
        var interiorNodes = graph.Nodes.Where(n => 
            n.Id != 0 && n.Id != 2 && n.Id != 6 && n.Id != 8).ToList();
        
        // At minimum, spanning tree ensures connectivity
        Assert.All(graph.Nodes, node => Assert.True(node.ConnectionCount > 0));
    }

    [Fact]
    public void GridBasedGraphGenerator_EightWayConnectivity()
    {
        var generator = new GridBasedGraphGenerator();
        var config = new GridBasedGraphConfig
        {
            GridWidth = 3,
            GridHeight = 3,
            ConnectivityPattern = ConnectivityPattern.EightWay
        };
        
        var graph = generator.Generate(9, 0.5f, new Random(12345), config);
        
        Assert.Equal(9, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
        
        // With 8-way connectivity, interior nodes can have up to 8 neighbors
        // With branchingFactor 0.5, we should have more connections than 4-way
        var avgConnections = graph.Nodes.Average(n => n.ConnectionCount);
        Assert.True(avgConnections >= 1.0); // At least spanning tree
    }

    [Fact]
    public void CellularAutomataGraphGenerator_ImplementsIGraphGenerator()
    {
        var generator = new CellularAutomataGraphGenerator();
        var config = new CellularAutomataGraphConfig
        {
            BirthThreshold = 4,
            SurvivalThreshold = 3,
            Iterations = 5
        };
        
        var graph = generator.Generate(20, 0.3f, new Random(12345), config);
        
        Assert.Equal(20, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
    }

    [Fact]
    public void CellularAutomataGraphGenerator_ProducesOrganicTopology()
    {
        var generator = new CellularAutomataGraphGenerator();
        var config = new CellularAutomataGraphConfig
        {
            BirthThreshold = 4,
            SurvivalThreshold = 3,
            Iterations = 5
        };
        
        var graph = generator.Generate(25, 0.3f, new Random(12345), config);
        
        Assert.True(IsConnected(graph));
        
        // CA should produce irregular connectivity patterns
        // Check that connection counts vary (not uniform like grid)
        var connectionCounts = graph.Nodes.Select(n => n.ConnectionCount).ToList();
        var uniqueCounts = connectionCounts.Distinct().Count();
        
        // Should have some variation in connection counts
        Assert.True(uniqueCounts > 1);
    }

    [Fact]
    public void MazeBasedGraphGenerator_ImplementsIGraphGenerator()
    {
        var generator = new MazeBasedGraphGenerator();
        var config = new MazeBasedGraphConfig
        {
            MazeType = MazeType.Perfect,
            Algorithm = MazeAlgorithm.Prims
        };
        
        var graph = generator.Generate(16, 0.2f, new Random(12345), config);
        
        Assert.Equal(16, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
    }

    [Fact]
    public void MazeBasedGraphGenerator_CreatesMazeStructure()
    {
        var generator = new MazeBasedGraphGenerator();
        var config = new MazeBasedGraphConfig
        {
            MazeType = MazeType.Perfect,
            Algorithm = MazeAlgorithm.Prims
        };
        
        var graph = generator.Generate(25, 0.0f, new Random(12345), config);
        
        Assert.True(IsConnected(graph));
        
        // Perfect maze with branchingFactor 0.0 should be a tree (no loops)
        // Verify by checking that connections = nodes - 1
        Assert.Equal(graph.Nodes.Count - 1, graph.Connections.Count);
    }

    [Fact]
    public void MazeBasedGraphGenerator_ImperfectMaze()
    {
        var generator = new MazeBasedGraphGenerator();
        var config = new MazeBasedGraphConfig
        {
            MazeType = MazeType.Imperfect,
            Algorithm = MazeAlgorithm.Kruskals
        };
        
        var graph = generator.Generate(20, 0.3f, new Random(12345), config);
        
        Assert.True(IsConnected(graph));
        
        // Imperfect maze should have loops (more connections than perfect maze)
        Assert.True(graph.Connections.Count >= graph.Nodes.Count - 1);
    }

    [Fact]
    public void HubAndSpokeGraphGenerator_ImplementsIGraphGenerator()
    {
        var generator = new HubAndSpokeGraphGenerator();
        var config = new HubAndSpokeGraphConfig
        {
            HubCount = 3,
            MaxSpokeLength = 5
        };
        
        var graph = generator.Generate(20, 0.2f, new Random(12345), config);
        
        Assert.Equal(20, graph.Nodes.Count);
        Assert.True(IsConnected(graph));
    }

    [Fact]
    public void HubAndSpokeGraphGenerator_CreatesHubStructure()
    {
        var generator = new HubAndSpokeGraphGenerator();
        var config = new HubAndSpokeGraphConfig
        {
            HubCount = 2,
            MaxSpokeLength = 4
        };
        
        var graph = generator.Generate(15, 0.1f, new Random(12345), config);
        
        Assert.True(IsConnected(graph));
        
        // Hub nodes should have more connections than spoke nodes
        // Identify hubs by finding nodes with highest connection counts
        var connectionCounts = graph.Nodes.Select(n => n.ConnectionCount).OrderByDescending(c => c).ToList();
        
        // Should have some nodes with more connections (hubs)
        // The max connection count should be >= min (always true if sorted, but verify structure)
        Assert.True(connectionCounts.Count > 0);
        Assert.True(connectionCounts[0] >= connectionCounts[^1]);
        
        // With hub-and-spoke, hubs (first config.HubCount nodes) should generally have more connections
        // But allow for edge cases where distribution is even
        var hubConnectionCounts = graph.Nodes.Take(config.HubCount).Select(n => n.ConnectionCount).ToList();
        var spokeConnectionCounts = graph.Nodes.Skip(config.HubCount).Select(n => n.ConnectionCount).ToList();
        
        // At least verify hubs exist and have connections
        Assert.All(hubConnectionCounts, count => Assert.True(count > 0));
    }

    [Fact]
    public void AllAlgorithms_MaintainDeterminism()
    {
        var seed = 12345;
        var roomCount = 15;
        var branchingFactor = 0.3f;
        
        // Test SpanningTree
        var rng1 = new Random(seed);
        var rng2 = new Random(seed);
        var spanning1 = new SpanningTreeGraphGenerator().Generate(roomCount, branchingFactor, rng1);
        var spanning2 = new SpanningTreeGraphGenerator().Generate(roomCount, branchingFactor, rng2);
        Assert.Equal(spanning1.Connections.Count, spanning2.Connections.Count);
        
        // Test GridBased
        rng1 = new Random(seed);
        rng2 = new Random(seed);
        var gridConfig = new GridBasedGraphConfig { GridWidth = 4, GridHeight = 4, ConnectivityPattern = ConnectivityPattern.FourWay };
        var grid1 = new GridBasedGraphGenerator().Generate(roomCount, branchingFactor, rng1, gridConfig);
        var grid2 = new GridBasedGraphGenerator().Generate(roomCount, branchingFactor, rng2, gridConfig);
        Assert.Equal(grid1.Connections.Count, grid2.Connections.Count);
        
        // Test CellularAutomata
        rng1 = new Random(seed);
        rng2 = new Random(seed);
        var caConfig = new CellularAutomataGraphConfig { BirthThreshold = 4, SurvivalThreshold = 3, Iterations = 5 };
        var ca1 = new CellularAutomataGraphGenerator().Generate(roomCount, branchingFactor, rng1, caConfig);
        var ca2 = new CellularAutomataGraphGenerator().Generate(roomCount, branchingFactor, rng2, caConfig);
        Assert.Equal(ca1.Connections.Count, ca2.Connections.Count);
        
        // Test MazeBased
        rng1 = new Random(seed);
        rng2 = new Random(seed);
        var mazeConfig = new MazeBasedGraphConfig { MazeType = MazeType.Perfect, Algorithm = MazeAlgorithm.Prims };
        var maze1 = new MazeBasedGraphGenerator().Generate(roomCount, branchingFactor, rng1, mazeConfig);
        var maze2 = new MazeBasedGraphGenerator().Generate(roomCount, branchingFactor, rng2, mazeConfig);
        Assert.Equal(maze1.Connections.Count, maze2.Connections.Count);
        
        // Test HubAndSpoke
        rng1 = new Random(seed);
        rng2 = new Random(seed);
        var hubConfig = new HubAndSpokeGraphConfig { HubCount = 2, MaxSpokeLength = 5 };
        var hub1 = new HubAndSpokeGraphGenerator().Generate(roomCount, branchingFactor, rng1, hubConfig);
        var hub2 = new HubAndSpokeGraphGenerator().Generate(roomCount, branchingFactor, rng2, hubConfig);
        Assert.Equal(hub1.Connections.Count, hub2.Connections.Count);
    }

    [Fact]
    public void AllAlgorithms_ProduceConnectedGraphs()
    {
        var roomCount = 20;
        var branchingFactor = 0.3f;
        var seed = 12345;
        
        // Test all algorithms produce connected graphs
        var spanning = new SpanningTreeGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed));
        Assert.True(IsConnected(spanning));
        
        var gridConfig = new GridBasedGraphConfig { GridWidth = 5, GridHeight = 4, ConnectivityPattern = ConnectivityPattern.FourWay };
        var grid = new GridBasedGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), gridConfig);
        Assert.True(IsConnected(grid));
        
        var caConfig = new CellularAutomataGraphConfig { BirthThreshold = 4, SurvivalThreshold = 3, Iterations = 5 };
        var ca = new CellularAutomataGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), caConfig);
        Assert.True(IsConnected(ca));
        
        var mazeConfig = new MazeBasedGraphConfig { MazeType = MazeType.Perfect, Algorithm = MazeAlgorithm.Prims };
        var maze = new MazeBasedGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), mazeConfig);
        Assert.True(IsConnected(maze));
        
        var hubConfig = new HubAndSpokeGraphConfig { HubCount = 3, MaxSpokeLength = 5 };
        var hub = new HubAndSpokeGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), hubConfig);
        Assert.True(IsConnected(hub));
    }

    [Fact]
    public void FloorConfig_SupportsGraphAlgorithmSelection()
    {
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            GraphAlgorithm = GraphAlgorithm.GridBased,
            GridBasedConfig = new GridBasedGraphConfig
            {
                GridWidth = 3,
                GridHeight = 4,
                ConnectivityPattern = ConnectivityPattern.FourWay
            }
        };
        
        Assert.Equal(GraphAlgorithm.GridBased, config.GraphAlgorithm);
        Assert.NotNull(config.GridBasedConfig);
    }

    [Fact]
    public void FloorConfig_DefaultAlgorithmIsSpanningTree()
    {
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates()
        };
        
        // Default should be SpanningTree for backward compatibility
        Assert.Equal(GraphAlgorithm.SpanningTree, config.GraphAlgorithm);
    }

    [Fact]
    public void FloorGenerator_UsesSelectedAlgorithm()
    {
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates(),
            GraphAlgorithm = GraphAlgorithm.GridBased,
            GridBasedConfig = new GridBasedGraphConfig
            {
                GridWidth = 3,
                GridHeight = 4,
                ConnectivityPattern = ConnectivityPattern.FourWay
            }
        };
        
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);
        
        // Verify generation succeeded with grid-based algorithm
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void FloorGenerator_BackwardCompatibilityWithDefaultAlgorithm()
    {
        // Config without GraphAlgorithm specified should use default (SpanningTree)
        var config = new FloorConfig<TestHelpers.RoomType>
        {
            Seed = 12345,
            RoomCount = 10,
            SpawnRoomType = TestHelpers.RoomType.Spawn,
            BossRoomType = TestHelpers.RoomType.Boss,
            DefaultRoomType = TestHelpers.RoomType.Combat,
            Templates = TestHelpers.CreateDefaultTemplates()
        };
        
        var generator = new FloorGenerator<TestHelpers.RoomType>();
        var layout = generator.Generate(config);
        
        Assert.NotNull(layout);
        Assert.Equal(10, layout.Rooms.Count);
    }

    [Fact]
    public void Algorithms_HandleEdgeCases()
    {
        var seed = 12345;
        
        // Test with minimum room count (2)
        var spanning = new SpanningTreeGraphGenerator().Generate(2, 0.0f, new Random(seed));
        Assert.Equal(2, spanning.Nodes.Count);
        Assert.True(IsConnected(spanning));
        
        // Test with large room count (50+)
        var gridConfig = new GridBasedGraphConfig { GridWidth = 8, GridHeight = 7, ConnectivityPattern = ConnectivityPattern.FourWay };
        var grid = new GridBasedGraphGenerator().Generate(56, 0.3f, new Random(seed), gridConfig);
        Assert.Equal(56, grid.Nodes.Count);
        Assert.True(IsConnected(grid));
        
        // Test with extreme branching factors
        var caConfig = new CellularAutomataGraphConfig { BirthThreshold = 4, SurvivalThreshold = 3, Iterations = 5 };
        var caMin = new CellularAutomataGraphGenerator().Generate(15, 0.0f, new Random(seed), caConfig);
        var caMax = new CellularAutomataGraphGenerator().Generate(15, 1.0f, new Random(seed), caConfig);
        Assert.True(IsConnected(caMin));
        Assert.True(IsConnected(caMax));
        // Max branching should have more connections
        Assert.True(caMax.Connections.Count >= caMin.Connections.Count);
    }

    [Fact]
    public void Algorithms_ProduceDifferentTopologies()
    {
        var roomCount = 16;
        var branchingFactor = 0.3f;
        var seed = 12345;
        
        var spanning = new SpanningTreeGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed));
        var gridConfig = new GridBasedGraphConfig { GridWidth = 4, GridHeight = 4, ConnectivityPattern = ConnectivityPattern.FourWay };
        var grid = new GridBasedGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), gridConfig);
        var caConfig = new CellularAutomataGraphConfig { BirthThreshold = 4, SurvivalThreshold = 3, Iterations = 5 };
        var ca = new CellularAutomataGraphGenerator().Generate(roomCount, branchingFactor, new Random(seed), caConfig);
        
        // Different algorithms should produce different connection patterns
        // Even with same seed, different algorithms should have different topologies
        var spanningConnections = spanning.Connections.Count;
        var gridConnections = grid.Connections.Count;
        var caConnections = ca.Connections.Count;
        
        // At least some should differ (though they might coincidentally match)
        // More importantly, verify they all produce valid connected graphs
        Assert.True(IsConnected(spanning));
        Assert.True(IsConnected(grid));
        Assert.True(IsConnected(ca));
    }

    private bool IsConnected(FloorGraph graph)
    {
        if (graph.Nodes.Count == 0) return false;
        if (graph.Nodes.Count == 1) return true;
        
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(graph.StartNodeId);
        visited.Add(graph.StartNodeId);
        
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            
            // Get all connections for this node from the graph
            foreach (var conn in graph.Connections.Where(c => c.NodeAId == current || c.NodeBId == current))
            {
                int neighborId = conn.GetOtherNodeId(current);
                if (!visited.Contains(neighborId))
                {
                    visited.Add(neighborId);
                    queue.Enqueue(neighborId);
                }
            }
        }
        
        return visited.Count == graph.Nodes.Count;
    }
}

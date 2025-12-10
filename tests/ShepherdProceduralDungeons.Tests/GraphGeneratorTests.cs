using ShepherdProceduralDungeons.Generation;
using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Tests;

public class GraphGeneratorTests
{
    [Fact]
    public void Generate_CreatesCorrectNumberOfNodes()
    {
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(10, 0.3f, rng);

        Assert.Equal(10, graph.Nodes.Count);
    }

    [Fact]
    public void Generate_GraphIsConnected()
    {
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(10, 0.3f, rng);

        // Check that all nodes are reachable from start
        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        queue.Enqueue(graph.StartNodeId);
        visited.Add(graph.StartNodeId);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            var connections = graph.Connections.Where(c => c.NodeAId == current || c.NodeBId == current);
            foreach (var conn in connections)
            {
                int neighborId = conn.GetOtherNodeId(current);
                if (!visited.Contains(neighborId))
                {
                    visited.Add(neighborId);
                    queue.Enqueue(neighborId);
                }
            }
        }

        Assert.Equal(graph.Nodes.Count, visited.Count);
    }

    [Fact]
    public void Generate_DistanceFromStartIsCalculated()
    {
        var generator = new GraphGenerator();
        var rng = new Random(12345);
        var graph = generator.Generate(10, 0.3f, rng);

        Assert.Equal(0, graph.Nodes.First(n => n.Id == graph.StartNodeId).DistanceFromStart);
        
        // At least some nodes should have distance > 0
        Assert.Contains(graph.Nodes, n => n.DistanceFromStart > 0);
    }

    [Fact]
    public void Generate_SameSeedProducesSameGraph()
    {
        var generator = new GraphGenerator();
        var rng1 = new Random(12345);
        var rng2 = new Random(12345);
        
        var graph1 = generator.Generate(10, 0.3f, rng1);
        var graph2 = generator.Generate(10, 0.3f, rng2);

        Assert.Equal(graph1.Nodes.Count, graph2.Nodes.Count);
        Assert.Equal(graph1.Connections.Count, graph2.Connections.Count);
    }
}


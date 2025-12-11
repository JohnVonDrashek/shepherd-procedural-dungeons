using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Legacy graph generator class. Use SpanningTreeGraphGenerator instead.
/// This class exists for backward compatibility.
/// </summary>
[Obsolete("Use SpanningTreeGraphGenerator instead. This class will be removed in a future version.")]
public sealed class GraphGenerator : IGraphGenerator
{
    private readonly SpanningTreeGraphGenerator _generator = new SpanningTreeGraphGenerator();

    /// <summary>
    /// Generates a connected graph with the specified number of nodes.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        return _generator.Generate(roomCount, branchingFactor, rng);
    }
}

using ShepherdProceduralDungeons.Graph;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Interface for graph generation algorithms that create dungeon floor topologies.
/// </summary>
public interface IGraphGenerator
{
    /// <summary>
    /// Generates a connected graph with the specified number of nodes.
    /// </summary>
    /// <param name="roomCount">Number of rooms to generate.</param>
    /// <param name="branchingFactor">0.0 = tree only, 1.0 = highly connected with loops.</param>
    /// <param name="rng">Random number generator for deterministic generation.</param>
    /// <returns>A connected floor graph.</returns>
    FloorGraph Generate(int roomCount, float branchingFactor, Random rng);
}

using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Configuration for generating a dungeon floor.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class FloorConfig<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Seed for deterministic generation.
    /// </summary>
    public required int Seed { get; init; }

    /// <summary>
    /// Total number of rooms to generate.
    /// </summary>
    public required int RoomCount { get; init; }

    /// <summary>
    /// Room type for the starting room.
    /// </summary>
    public required TRoomType SpawnRoomType { get; init; }

    /// <summary>
    /// Room type for the boss room.
    /// </summary>
    public required TRoomType BossRoomType { get; init; }

    /// <summary>
    /// Default room type for rooms without specific assignments.
    /// </summary>
    public required TRoomType DefaultRoomType { get; init; }

    /// <summary>
    /// How many rooms of each type to generate (beyond spawn/boss).
    /// </summary>
    public IReadOnlyList<(TRoomType Type, int Count)> RoomRequirements { get; init; } = Array.Empty<(TRoomType, int)>();

    /// <summary>
    /// Constraints for room type placement.
    /// </summary>
    public IReadOnlyList<IConstraint<TRoomType>> Constraints { get; init; } = Array.Empty<IConstraint<TRoomType>>();

    /// <summary>
    /// Available room templates.
    /// </summary>
    public required IReadOnlyList<RoomTemplate<TRoomType>> Templates { get; init; }

    /// <summary>
    /// Branching factor: 0.0 = tree structure, 1.0 = highly connected with loops.
    /// </summary>
    public float BranchingFactor { get; init; } = 0.3f;

    /// <summary>
    /// How to handle non-adjacent room connections.
    /// </summary>
    public HallwayMode HallwayMode { get; init; } = HallwayMode.AsNeeded;

    /// <summary>
    /// Optional zones for biome/thematic partitioning of the dungeon.
    /// </summary>
    public IReadOnlyList<Zone<TRoomType>>? Zones { get; init; }

    /// <summary>
    /// Configuration for secret passage generation.
    /// </summary>
    public SecretPassageConfig<TRoomType>? SecretPassageConfig { get; init; }

    /// <summary>
    /// Graph generation algorithm to use. Defaults to SpanningTree for backward compatibility.
    /// </summary>
    public GraphAlgorithm GraphAlgorithm { get; init; } = GraphAlgorithm.SpanningTree;

    /// <summary>
    /// Configuration for grid-based graph generation. Required when GraphAlgorithm is GridBased.
    /// </summary>
    public GridBasedGraphConfig? GridBasedConfig { get; init; }

    /// <summary>
    /// Configuration for cellular automata graph generation. Required when GraphAlgorithm is CellularAutomata.
    /// </summary>
    public CellularAutomataGraphConfig? CellularAutomataConfig { get; init; }

    /// <summary>
    /// Configuration for maze-based graph generation. Required when GraphAlgorithm is MazeBased.
    /// </summary>
    public MazeBasedGraphConfig? MazeBasedConfig { get; init; }

    /// <summary>
    /// Configuration for hub-and-spoke graph generation. Required when GraphAlgorithm is HubAndSpoke.
    /// </summary>
    public HubAndSpokeGraphConfig? HubAndSpokeConfig { get; init; }
}


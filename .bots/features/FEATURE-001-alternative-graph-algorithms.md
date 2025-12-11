# Feature: Alternative Graph Generation Algorithms

**ID**: FEATURE-001
**Status**: complete
**Created**: 2025-01-27T00:00:00Z
**Priority**: high
**Complexity**: complex

## Description

Currently, the library uses a single graph generation algorithm (spanning tree + random extra edges) that produces a consistent topology style. This feature adds support for multiple graph generation algorithms, enabling developers to create dungeons with fundamentally different connectivity patterns and gameplay experiences.

**Why this matters**: Different graph topologies create vastly different dungeon experiences:
- **Grid-based algorithms** create structured, maze-like dungeons with clear navigation patterns
- **Cellular automata** produce organic, cave-like structures with irregular connectivity
- **Maze-based algorithms** create complex, winding paths perfect for exploration-focused games
- **Hub-and-spoke** designs create central gathering areas with branching paths
- **Linear progression** creates focused, story-driven experiences

This feature unlocks new creative possibilities and makes the library suitable for a wider variety of game genres beyond traditional roguelikes.

## Requirements

- [ ] Create `IGraphGenerator` interface that abstracts graph generation
- [ ] Refactor existing `GraphGenerator` to implement `IGraphGenerator` (backward compatibility)
- [ ] Implement `GridBasedGraphGenerator` - generates rooms in a grid pattern with configurable connectivity
- [ ] Implement `CellularAutomataGraphGenerator` - uses cellular automata rules to create organic topologies
- [ ] Implement `MazeBasedGraphGenerator` - generates maze-like structures with configurable branching
- [ ] Add `GraphAlgorithm` enum to `FloorConfig` to select algorithm
- [ ] Add algorithm-specific configuration options (e.g., grid size, cellular automata rules, maze complexity)
- [ ] Ensure all algorithms maintain determinism (same seed = same output)
- [ ] Ensure all algorithms produce connected graphs (all rooms reachable)
- [ ] Update `FloorGenerator` to use selected algorithm
- [ ] Maintain backward compatibility (default to existing algorithm)

## Technical Details

### Architecture Changes

1. **New Interface**: `IGraphGenerator` with `Generate(int roomCount, float branchingFactor, Random rng)` method
2. **Refactoring**: Existing `GraphGenerator` becomes `SpanningTreeGraphGenerator` implementing `IGraphGenerator`
3. **Configuration**: Add `GraphAlgorithm` enum and algorithm-specific config classes
4. **Factory Pattern**: Graph generator factory selects appropriate implementation based on config

### Algorithm Implementations

**GridBasedGraphGenerator**:
- Arranges rooms in a 2D grid (e.g., 4x4 for 16 rooms)
- Connects adjacent rooms based on connectivity pattern (4-way, 8-way, or custom)
- Adds extra connections based on branching factor
- Ensures connectivity via spanning tree overlay if needed

**CellularAutomataGraphGenerator**:
- Uses cellular automata rules to generate organic room placement
- Rooms "grow" from seed points based on neighbor rules
- Creates irregular, cave-like topologies
- Configurable birth/survival rules for different densities

**MazeBasedGraphGenerator**:
- Generates perfect or imperfect mazes using algorithms like Prim's or Kruskal's
- Converts maze cells to room nodes
- Adds extra connections based on branching factor
- Creates complex, winding path structures

**HubAndSpokeGraphGenerator**:
- Creates central hub rooms with branching spokes
- Configurable hub count and spoke length
- Ensures all spokes connect back to hubs
- Good for creating gathering/rest areas

### Configuration API

```csharp
public enum GraphAlgorithm
{
    SpanningTree,      // Default (existing algorithm)
    GridBased,
    CellularAutomata,
    MazeBased,
    HubAndSpoke
}

public sealed class GridBasedGraphConfig
{
    public int GridWidth { get; init; }
    public int GridHeight { get; init; }
    public ConnectivityPattern Pattern { get; init; } // 4-way, 8-way, custom
}

public sealed class CellularAutomataGraphConfig
{
    public int BirthThreshold { get; init; } = 4;
    public int SurvivalThreshold { get; init; } = 3;
    public int Iterations { get; init; } = 5;
}

// Similar configs for other algorithms...
```

### Integration Points

- `FloorConfig` needs `GraphAlgorithm` property and algorithm-specific config
- `FloorGenerator` needs to select appropriate generator based on config
- All generators must produce `FloorGraph` with same structure
- Determinism must be maintained across all algorithms

## Dependencies

- None

## Test Scenarios

1. **Backward Compatibility**: Default algorithm produces identical output to current implementation
2. **Grid-Based Generation**: Grid algorithm creates rooms in grid pattern with correct connectivity
3. **Cellular Automata**: CA algorithm produces organic, connected topologies
4. **Maze Generation**: Maze algorithm creates valid maze structures with all rooms reachable
5. **Determinism**: Same seed + same algorithm + same config = identical graph
6. **Connectivity**: All algorithms produce fully connected graphs (no isolated rooms)
7. **Algorithm Switching**: Changing algorithm with same seed produces different but valid graphs
8. **Large Dungeons**: All algorithms handle 50+ room counts efficiently
9. **Edge Cases**: Algorithms handle edge cases (2 rooms, very large counts, extreme branching factors)

## Acceptance Criteria

- [x] `IGraphGenerator` interface exists and is used by `FloorGenerator`
- [x] Existing `GraphGenerator` refactored to `SpanningTreeGraphGenerator` implementing interface
- [x] At least 3 new graph generation algorithms implemented (Grid, CA, Maze)
- [x] `FloorConfig` supports selecting graph algorithm via `GraphAlgorithm` enum
- [x] Algorithm-specific configuration classes exist and are serializable
- [x] All algorithms produce connected graphs (all rooms reachable from start)
- [x] All algorithms maintain determinism (same seed = same output)
- [x] Backward compatibility maintained (default algorithm = existing behavior)
- [x] Unit tests cover each algorithm independently
- [x] Integration tests verify algorithms work with full generation pipeline
- [x] Documentation updated with algorithm descriptions and usage examples

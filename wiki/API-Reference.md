# API Reference

Quick reference for main classes and methods.

## FloorGenerator<TRoomType>

Main entry point for single-floor dungeon generation.

### Constructor

```csharp
// Default constructor (uses IncrementalSolver)
public FloorGenerator()

// Custom spatial solver
public FloorGenerator(ISpatialSolver<TRoomType> spatialSolver)
```

### Methods

#### Generate

```csharp
public FloorLayout<TRoomType> Generate(FloorConfig<TRoomType> config)
```

Generates a floor layout from configuration.

**Parameters:**
- `config` - Generation configuration

**Returns:** `FloorLayout<TRoomType>` - Generated dungeon layout

**Exceptions:**
- `InvalidConfigurationException` - Config is invalid
- `ConstraintViolationException` - Constraints cannot be satisfied
- `SpatialPlacementException` - Rooms cannot be placed

## MultiFloorGenerator<TRoomType>

Main entry point for multi-floor dungeon generation.

### Constructor

```csharp
// Default constructor (uses IncrementalSolver)
public MultiFloorGenerator()

// Custom spatial solver
public MultiFloorGenerator(ISpatialSolver<TRoomType> spatialSolver)
```

### Methods

#### Generate

```csharp
public MultiFloorLayout<TRoomType> Generate(MultiFloorConfig<TRoomType> config)
```

Generates a multi-floor dungeon layout from configuration.

**Parameters:**
- `config` - Multi-floor generation configuration

**Returns:** `MultiFloorLayout<TRoomType>` - Generated multi-floor dungeon layout

**Exceptions:**
- `InvalidConfigurationException` - Config is invalid
- `ConstraintViolationException` - Constraints cannot be satisfied
- `SpatialPlacementException` - Rooms cannot be placed

## MultiFloorConfig<TRoomType>

Configuration for multi-floor dungeon generation.

### Properties

```csharp
public required int Seed { get; init; }
public required IReadOnlyList<FloorConfig<TRoomType>> Floors { get; init; }
public required IReadOnlyList<FloorConnection> Connections { get; init; }
```

## FloorConnection

Represents a connection between two floors.

### Properties

```csharp
public required int FromFloorIndex { get; init; }
public required int FromRoomNodeId { get; init; }
public required int ToFloorIndex { get; init; }
public required int ToRoomNodeId { get; init; }
public required ConnectionType Type { get; init; }
```

## ConnectionType

Enumeration of connection types between floors.

### Values

```csharp
public enum ConnectionType
{
    StairsUp,      // Stairs going up to a higher floor
    StairsDown,    // Stairs going down to a lower floor
    Teleporter     // Teleporter pad/portal connecting floors
}
```

## MultiFloorLayout<TRoomType>

Generated multi-floor dungeon output.

### Properties

```csharp
public required IReadOnlyList<FloorLayout<TRoomType>> Floors { get; init; }
public required IReadOnlyList<FloorConnection> Connections { get; init; }
public required int Seed { get; init; }
public required int TotalFloorCount { get; init; }
```

## FloorConfig<TRoomType>

Configuration for single-floor dungeon generation.

### Properties

#### Required

```csharp
public required int Seed { get; init; }
public required int RoomCount { get; init; }
public required TRoomType SpawnRoomType { get; init; }
public required TRoomType BossRoomType { get; init; }
public required TRoomType DefaultRoomType { get; init; }
public required IReadOnlyList<RoomTemplate<TRoomType>> Templates { get; init; }
```

#### Optional

```csharp
public IReadOnlyList<(TRoomType Type, int Count)> RoomRequirements { get; init; }
public IReadOnlyList<IConstraint<TRoomType>> Constraints { get; init; }
public float BranchingFactor { get; init; }  // Default: 0.3f
public GraphAlgorithm GraphAlgorithm { get; init; }  // Default: GraphAlgorithm.SpanningTree
public GridBasedGraphConfig? GridBasedConfig { get; init; }  // Required when GraphAlgorithm is GridBased
public CellularAutomataGraphConfig? CellularAutomataConfig { get; init; }  // Required when GraphAlgorithm is CellularAutomata
public MazeBasedGraphConfig? MazeBasedConfig { get; init; }  // Required when GraphAlgorithm is MazeBased
public HubAndSpokeGraphConfig? HubAndSpokeConfig { get; init; }  // Required when GraphAlgorithm is HubAndSpoke
public HallwayMode HallwayMode { get; init; }  // Default: HallwayMode.AsNeeded
public IReadOnlyList<Zone<TRoomType>>? Zones { get; init; }  // Optional zones
public SecretPassageConfig<TRoomType>? SecretPassageConfig { get; init; }  // Optional secret passages
```

## FloorLayout<TRoomType>

Generated dungeon output.

### Properties

```csharp
public required IReadOnlyList<PlacedRoom<TRoomType>> Rooms { get; init; }
public required IReadOnlyList<Hallway> Hallways { get; init; }
public required IReadOnlyList<Door> Doors { get; init; }
public required int Seed { get; init; }
public required IReadOnlyList<int> CriticalPath { get; init; }
public required int SpawnRoomId { get; init; }
public required int BossRoomId { get; init; }
public IReadOnlyDictionary<int, string>? ZoneAssignments { get; init; }  // Node ID -> Zone ID
public IReadOnlyList<PlacedRoom<TRoomType>> TransitionRooms { get; init; }  // Rooms connecting zones
public required IReadOnlyList<SecretPassage> SecretPassages { get; init; }  // Secret passages
public IEnumerable<(Cell WorldCell, InteriorFeature Feature)> InteriorFeatures { get; }  // All interior features
```

### Methods

```csharp
public PlacedRoom<TRoomType>? GetRoom(int nodeId)
public IEnumerable<SecretPassage> GetSecretPassagesForRoom(int roomId)
public IEnumerable<Cell> GetAllRoomCells()
public IEnumerable<Cell> GetAllHallwayCells()
public (Cell Min, Cell Max) GetBounds()
```

**InteriorFeatures**: Gets all interior features from all rooms in world coordinates. Returns tuples of (world cell, feature type).

## RoomTemplate<TRoomType>

Room shape and door placement definition.

### Properties

```csharp
public required string Id { get; init; }
public required IReadOnlySet<TRoomType> ValidRoomTypes { get; init; }
public required IReadOnlySet<Cell> Cells { get; init; }
public required IReadOnlyDictionary<Cell, Edge> DoorEdges { get; init; }
public double Weight { get; init; }  // Default: 1.0
public IReadOnlyDictionary<Cell, InteriorFeature> InteriorFeatures { get; init; }  // Default: empty
public int Width { get; }
public int Height { get; }
```

**Weight**: Selection weight for this template. Higher weights increase selection probability. Default is 1.0 (uniform distribution when all templates have default weight). Must be greater than 0.

**InteriorFeatures**: Interior obstacles and features defined for this template, keyed by cell position (template-local coordinates). Default is an empty dictionary. Features must be placed in interior cells (not on exterior edges).

### Methods

```csharp
public IEnumerable<(Cell Cell, Edge Edge)> GetExteriorEdges()
public bool CanPlaceDoor(Cell cell, Edge edge)
```

## RoomTemplateBuilder<TRoomType>

Fluent builder for creating room templates.

### Static Factory Methods

```csharp
public static RoomTemplateBuilder<TRoomType> Rectangle(int width, int height)
public static RoomTemplateBuilder<TRoomType> LShape(
    int width, 
    int height, 
    int cutoutWidth, 
    int cutoutHeight, 
    Corner cutoutCorner)
```

### Instance Methods

```csharp
public RoomTemplateBuilder<TRoomType> WithId(string id)
public RoomTemplateBuilder<TRoomType> ForRoomTypes(params TRoomType[] types)
public RoomTemplateBuilder<TRoomType> AddCell(int x, int y)
public RoomTemplateBuilder<TRoomType> AddRectangle(int x, int y, int width, int height)
public RoomTemplateBuilder<TRoomType> WithDoorEdges(int x, int y, Edge edges)
public RoomTemplateBuilder<TRoomType> WithDoorsOnAllExteriorEdges()
public RoomTemplateBuilder<TRoomType> WithDoorsOnSides(Edge sides)
public RoomTemplateBuilder<TRoomType> WithWeight(double weight)
public RoomTemplateBuilder<TRoomType> AddInteriorFeature(int x, int y, InteriorFeature feature)
public RoomTemplate<TRoomType> Build()
```

**WithWeight**: Sets the selection weight for this template. Weight must be greater than 0. Default is 1.0. Higher weights increase selection probability.

**AddInteriorFeature**: Adds an interior feature at the specified cell position (template-local coordinates). The feature must be within the template's cell bounds and cannot be placed on exterior edges. Throws `InvalidConfigurationException` if placement is invalid.

## PlacedRoom<TRoomType>

A room that has been placed in the dungeon.

### Properties

```csharp
public required int NodeId { get; init; }
public required TRoomType RoomType { get; init; }
public required RoomTemplate<TRoomType> Template { get; init; }
public required Cell Position { get; init; }  // Anchor position
```

### Methods

```csharp
public IEnumerable<Cell> GetWorldCells()
public IEnumerable<(Cell LocalCell, Cell WorldCell, Edge Edge)> GetExteriorEdgesWorld()
public IEnumerable<(Cell WorldCell, InteriorFeature Feature)> GetInteriorFeatures()
```

**GetInteriorFeatures**: Gets all interior features in world coordinates. Returns tuples of (world cell, feature type).

## Constraints

### IConstraint<TRoomType>

Interface for room placement constraints.

```csharp
public interface IConstraint<TRoomType> where TRoomType : Enum
{
    TRoomType TargetRoomType { get; }
    bool IsValid(
        RoomNode node, 
        FloorGraph graph, 
        IReadOnlyDictionary<int, TRoomType> currentAssignments);
}
```

### Built-in Constraints

#### MinDistanceFromStartConstraint

```csharp
public class MinDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MinDistance { get; }
    
    public MinDistanceFromStartConstraint(TRoomType roomType, int minDistance);
}
```

#### MaxDistanceFromStartConstraint

```csharp
public class MaxDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxDistance { get; }
    
    public MaxDistanceFromStartConstraint(TRoomType roomType, int maxDistance);
}
```

#### MustBeDeadEndConstraint

```csharp
public class MustBeDeadEndConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    
    public MustBeDeadEndConstraint(TRoomType roomType);
}
```

#### MinConnectionCountConstraint

```csharp
public class MinConnectionCountConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MinConnections { get; }
    
    public MinConnectionCountConstraint(TRoomType roomType, int minConnections);
}
```

#### MaxConnectionCountConstraint

```csharp
public class MaxConnectionCountConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxConnections { get; }
    
    public MaxConnectionCountConstraint(TRoomType roomType, int maxConnections);
}
```

#### NotOnCriticalPathConstraint

```csharp
public class NotOnCriticalPathConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    
    public NotOnCriticalPathConstraint(TRoomType roomType);
}
```

#### OnlyOnCriticalPathConstraint

```csharp
public class OnlyOnCriticalPathConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    
    public OnlyOnCriticalPathConstraint(TRoomType roomType);
}
```

#### MaxPerFloorConstraint

```csharp
public class MaxPerFloorConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxCount { get; }
    
    public MaxPerFloorConstraint(TRoomType roomType, int maxCount);
}
```

#### OnlyOnFloorConstraint

```csharp
public class OnlyOnFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlyList<int> AllowedFloors { get; }
    
    public OnlyOnFloorConstraint(TRoomType roomType, IReadOnlyList<int> allowedFloors);
}
```

**Use case:** Restrict room types to specific floors (e.g., boss only on final floor).

#### NotOnFloorConstraint

```csharp
public class NotOnFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlyList<int> ForbiddenFloors { get; }
    
    public NotOnFloorConstraint(TRoomType roomType, IReadOnlyList<int> forbiddenFloors);
}
```

**Use case:** Prevent room types from appearing on specific floors.

#### MinFloorConstraint

```csharp
public class MinFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MinFloor { get; }
    
    public MinFloorConstraint(TRoomType roomType, int minFloor);
}
```

**Use case:** Require room types to appear on floor N or higher (e.g., boss only on floor 2+).

#### MaxFloorConstraint

```csharp
public class MaxFloorConstraint<TRoomType> : IFloorAwareConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxFloor { get; }
    
    public MaxFloorConstraint(TRoomType roomType, int maxFloor);
}
```

**Use case:** Require room types to appear on floor N or lower (e.g., tutorial rooms only on floor 0).

#### MustBeAdjacentToConstraint

```csharp
public class MustBeAdjacentToConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> RequiredAdjacentTypes { get; }
    
    // Single adjacent type
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, TRoomType requiredAdjacentType);
    
    // Multiple adjacent types (OR logic)
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, params TRoomType[] requiredAdjacentTypes);
    
    // Multiple adjacent types from collection
    public MustBeAdjacentToConstraint(TRoomType targetRoomType, IEnumerable<TRoomType> requiredAdjacentTypes);
}
```

#### MustNotBeAdjacentToConstraint

```csharp
public class MustNotBeAdjacentToConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> ForbiddenAdjacentTypes { get; }
    
    // Single forbidden adjacent type
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, TRoomType forbiddenAdjacentType);
    
    // Multiple forbidden adjacent types
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, params TRoomType[] forbiddenAdjacentTypes);
    
    // Multiple forbidden adjacent types from collection
    public MustNotBeAdjacentToConstraint(TRoomType targetRoomType, IEnumerable<TRoomType> forbiddenAdjacentTypes);
}
```

#### MinDistanceFromRoomTypeConstraint

```csharp
public class MinDistanceFromRoomTypeConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }
    public int MinDistance { get; }
    
    // Single reference type
    public MinDistanceFromRoomTypeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType, int minDistance);
    
    // Multiple reference types (OR logic)
    public MinDistanceFromRoomTypeConstraint(TRoomType targetRoomType, int minDistance, params TRoomType[] referenceRoomTypes);
}
```

#### MaxDistanceFromRoomTypeConstraint

```csharp
public class MaxDistanceFromRoomTypeConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }
    public int MaxDistance { get; }
    
    // Single reference type
    public MaxDistanceFromRoomTypeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType, int maxDistance);
    
    // Multiple reference types (OR logic)
    public MaxDistanceFromRoomTypeConstraint(TRoomType targetRoomType, int maxDistance, params TRoomType[] referenceRoomTypes);
}
```

#### MustComeBeforeConstraint

```csharp
public class MustComeBeforeConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public IReadOnlySet<TRoomType> ReferenceRoomTypes { get; }
    
    // Single reference type
    public MustComeBeforeConstraint(TRoomType targetRoomType, TRoomType referenceRoomType);
    
    // Multiple reference types (target must come before at least one)
    public MustComeBeforeConstraint(TRoomType targetRoomType, params TRoomType[] referenceRoomTypes);
}
```

#### CustomConstraint

```csharp
public class CustomConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    
    public CustomConstraint(
        TRoomType roomType, 
        Func<RoomNode, FloorGraph, IReadOnlyDictionary<int, TRoomType>, bool> predicate);
}
```

#### OnlyInZoneConstraint

```csharp
public class OnlyInZoneConstraint<TRoomType> : IConstraint<TRoomType>, IZoneAwareConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public string ZoneId { get; }
    
    public OnlyInZoneConstraint(TRoomType targetRoomType, string zoneId);
}
```

**Use case:** Restrict room types to specific zones (e.g., shops only in market zone).

#### CompositeConstraint

Composes multiple constraints using AND, OR, or NOT logic.

```csharp
public sealed class CompositeConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    public TRoomType TargetRoomType { get; }
    public CompositionOperator Operator { get; }
    public IReadOnlyList<IConstraint<TRoomType>> Constraints { get; }
    
    // Factory methods
    public static CompositeConstraint<TRoomType> And(params IConstraint<TRoomType>[] constraints);
    public static CompositeConstraint<TRoomType> Or(params IConstraint<TRoomType>[] constraints);
    public static CompositeConstraint<TRoomType> Not(IConstraint<TRoomType> constraint);
    
    // Implementation
    public bool IsValid(
        RoomNode node, 
        FloorGraph graph, 
        IReadOnlyDictionary<int, TRoomType> currentAssignments);
}
```

**Use case:** Express complex constraint logic with AND/OR/NOT operators.

#### CompositionOperator

Enumeration of composition operators.

```csharp
public enum CompositionOperator
{
    And,  // All constraints must pass
    Or,   // At least one constraint must pass
    Not   // The wrapped constraint must fail
}
```

## Hallway

Generated hallway connecting rooms.

### Properties

```csharp
public required int Id { get; init; }
public required IReadOnlyList<HallwaySegment> Segments { get; init; }
public required Door DoorA { get; init; }
public required Door DoorB { get; init; }
```

## HallwaySegment

A straight segment of a hallway.

### Properties

```csharp
public required Cell Start { get; init; }
public required Cell End { get; init; }
public bool IsHorizontal { get; }
public bool IsVertical { get; }
```

### Methods

```csharp
public IEnumerable<Cell> GetCells()
```

## Door

A door connecting rooms or hallways.

### Properties

```csharp
public required Cell Position { get; init; }
public required Edge Edge { get; init; }
public int? ConnectsToRoomId { get; init; }
public int? ConnectsToHallwayId { get; init; }
```

## Cell

A grid cell position.

### Properties

```csharp
public int X { get; }
public int Y { get; }
```

### Methods

```csharp
public Cell Offset(int dx, int dy)
public Cell North { get; }
public Cell South { get; }
public Cell East { get; }
public Cell West { get; }
```

## Edge

Cardinal directions for door placement.

### Values

```csharp
[Flags]
public enum Edge
{
    None = 0,
    North = 1,
    South = 2,
    East = 4,
    West = 8,
    All = North | South | East | West
}
```

### Extension Methods

```csharp
public static Edge Opposite(this Edge edge)
```

## InteriorFeature

Types of interior features that can be placed within room templates.

### Values

```csharp
public enum InteriorFeature
{
    Pillar,      // Obstacle that blocks movement but not line of sight
    Wall,        // Interior wall that creates sub-areas within rooms
    Hazard,      // Special cells that might contain traps, lava, spikes, etc.
    Decorative   // Visual markers for special areas (altars, fountains, etc.)
}
```

**Pillar**: Obstacles that block movement but not line of sight. Useful for cover mechanics in combat rooms.

**Wall**: Interior walls that create sub-areas within rooms. Useful for tactical positioning and creating chokepoints.

**Hazard**: Special cells that might contain traps, lava, spikes, etc. Useful for environmental hazards and danger zones.

**Decorative**: Visual markers for special areas (altars, fountains, etc.). Useful for thematic variety and visual interest.

## GraphAlgorithm

Algorithm used for generating the dungeon floor graph topology.

### Values

```csharp
public enum GraphAlgorithm
{
    SpanningTree,      // Default spanning tree algorithm (backward compatible)
    GridBased,         // Grid-based algorithm
    CellularAutomata,  // Cellular automata algorithm
    MazeBased,         // Maze-based algorithm
    HubAndSpoke        // Hub-and-spoke algorithm
}
```

## GridBasedGraphConfig

Configuration for grid-based graph generation.

### Properties

```csharp
public required int GridWidth { get; init; }
public required int GridHeight { get; init; }
public ConnectivityPattern ConnectivityPattern { get; init; }  // Default: ConnectivityPattern.FourWay
```

## ConnectivityPattern

Connectivity pattern for grid-based graph generation.

### Values

```csharp
public enum ConnectivityPattern
{
    FourWay,   // Four-way connectivity (north, south, east, west)
    EightWay   // Eight-way connectivity (includes diagonals)
}
```

## CellularAutomataGraphConfig

Configuration for cellular automata graph generation.

### Properties

```csharp
public int BirthThreshold { get; init; }  // Default: 4
public int SurvivalThreshold { get; init; }  // Default: 3
public int Iterations { get; init; }  // Default: 5
```

## MazeBasedGraphConfig

Configuration for maze-based graph generation.

### Properties

```csharp
public MazeType MazeType { get; init; }  // Default: MazeType.Perfect
public MazeAlgorithm Algorithm { get; init; }  // Default: MazeAlgorithm.Prims
```

## MazeType

Type of maze to generate.

### Values

```csharp
public enum MazeType
{
    Perfect,    // Perfect maze (no loops, tree structure)
    Imperfect   // Imperfect maze (may contain loops)
}
```

## MazeAlgorithm

Algorithm used for maze generation.

### Values

```csharp
public enum MazeAlgorithm
{
    Prims,      // Prim's algorithm
    Kruskals    // Kruskal's algorithm
}
```

## HubAndSpokeGraphConfig

Configuration for hub-and-spoke graph generation.

### Properties

```csharp
public required int HubCount { get; init; }
public required int MaxSpokeLength { get; init; }
```

## IGraphGenerator

Interface for graph generation algorithms.

### Methods

```csharp
FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
```

Generates a connected graph with the specified number of nodes.

**Parameters:**
- `roomCount` - Number of rooms to generate
- `branchingFactor` - 0.0 = tree only, 1.0 = highly connected with loops
- `rng` - Random number generator for deterministic generation

**Returns:** `FloorGraph` - A connected floor graph

## HallwayMode

Hallway generation mode.

### Values

```csharp
public enum HallwayMode
{
    None,      // Rooms must be adjacent
    AsNeeded,  // Generate hallways when needed
    Always     // Always generate hallways
}
```

## Corner

Corner position for L-shaped templates.

### Values

```csharp
public enum Corner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}
```

## Exceptions

### GenerationException

Base exception for all generation errors.

```csharp
public class GenerationException : Exception
{
    public GenerationException(string message);
    public GenerationException(string message, Exception innerException);
}
```

### InvalidConfigurationException

Thrown when configuration is invalid.

```csharp
public class InvalidConfigurationException : GenerationException
{
    public InvalidConfigurationException(string message);
}
```

### ConstraintViolationException

Thrown when constraints cannot be satisfied.

```csharp
public class ConstraintViolationException : GenerationException
{
    public string? ConstraintType { get; init; }
    public ConstraintViolationException(string message);
}
```

### SpatialPlacementException

Thrown when rooms cannot be placed in 2D space.

```csharp
public class SpatialPlacementException : GenerationException
{
    public int? RoomId { get; init; }
    public SpatialPlacementException(string message);
}
```

## Advanced Interfaces

### IFloorAwareConstraint<TRoomType>

Interface for constraints that are aware of the current floor index.

```csharp
public interface IFloorAwareConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    void SetFloorIndex(int floorIndex);
}
```

**Use case:** Implement floor-aware constraints that need to know which floor is being generated.

### ISpatialSolver<TRoomType>

Interface for custom spatial placement algorithms.

```csharp
public interface ISpatialSolver<TRoomType> where TRoomType : Enum
{
    IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng);
}
```

## Zone<TRoomType>

Represents a biome or thematic zone within a dungeon floor.

### Properties

```csharp
public required string Id { get; init; }
public required string Name { get; init; }
public required ZoneBoundary Boundary { get; init; }
public IReadOnlyList<(TRoomType Type, int Count)>? RoomRequirements { get; init; }
public IReadOnlyList<IConstraint<TRoomType>>? Constraints { get; init; }
public IReadOnlyList<RoomTemplate<TRoomType>>? Templates { get; init; }
```

## ZoneBoundary

Base class for zone boundary definitions. Determines which rooms belong to a zone.

### DistanceBased

```csharp
public sealed class DistanceBased : ZoneBoundary
{
    public required int MinDistance { get; init; }
    public required int MaxDistance { get; init; }
}
```

Assigns rooms based on distance from start node.

### CriticalPathBased

```csharp
public sealed class CriticalPathBased : ZoneBoundary
{
    public required float StartPercent { get; init; }
    public required float EndPercent { get; init; }
}
```

Assigns rooms based on position along critical path (0.0 to 1.0).

## IZoneAwareConstraint<TRoomType>

Interface for constraints that need to check zone assignments.

```csharp
public interface IZoneAwareConstraint<TRoomType> where TRoomType : Enum
{
    void SetZoneAssignments(IReadOnlyDictionary<int, string> zoneAssignments);
}
```

**Use case:** Implement zone-aware constraints that need to know which zone a room belongs to.

## SecretPassageConfig<TRoomType>

Configuration for generating secret passages.

### Properties

```csharp
public int Count { get; init; }  // Default: 0
public int MaxSpatialDistance { get; init; }  // Default: 5
public IReadOnlySet<TRoomType> AllowedRoomTypes { get; init; }  // Default: empty
public IReadOnlySet<TRoomType> ForbiddenRoomTypes { get; init; }  // Default: empty
public bool AllowCriticalPathConnections { get; init; }  // Default: true
public bool AllowGraphConnectedRooms { get; init; }  // Default: false
```

## SecretPassage

Represents a secret passage connecting two rooms.

### Properties

```csharp
public required int RoomAId { get; init; }
public required int RoomBId { get; init; }
public required Door DoorA { get; init; }
public required Door DoorB { get; init; }
public Hallway? Hallway { get; init; }
public bool RequiresHallway { get; }
```

**RequiresHallway**: Returns `true` if this secret passage requires a hallway (rooms are not adjacent).

## ConfigurationSerializer<TRoomType>

Serializes and deserializes dungeon configurations to/from JSON.

### Constructor

```csharp
public ConfigurationSerializer()
```

Creates a new configuration serializer with default options.

### Methods

#### SerializeToJson (FloorConfig)

```csharp
public string SerializeToJson(FloorConfig<TRoomType> config, bool prettyPrint = true)
public string SerializeToJson(FloorConfig<TRoomType> config, JsonSerializerOptions options)
```

Serializes a `FloorConfig` to JSON string.

**Parameters:**
- `config` - The configuration to serialize
- `prettyPrint` - Whether to format the JSON with indentation (default: true)
- `options` - Custom JSON serializer options

**Returns:** JSON string representation of the configuration

#### DeserializeFromJson (FloorConfig)

```csharp
public FloorConfig<TRoomType> DeserializeFromJson(string json)
public FloorConfig<TRoomType> DeserializeFromJson(string json, JsonSerializerOptions options)
```

Deserializes a JSON string to `FloorConfig`.

**Parameters:**
- `json` - The JSON string to deserialize
- `options` - Custom JSON serializer options

**Returns:** `FloorConfig<TRoomType>` instance

**Exceptions:**
- `InvalidConfigurationException` - Thrown when JSON is invalid or missing required fields

#### SerializeToJson (MultiFloorConfig)

```csharp
public string SerializeToJson(MultiFloorConfig<TRoomType> config, bool prettyPrint = true)
```

Serializes a `MultiFloorConfig` to JSON string.

**Parameters:**
- `config` - The multi-floor configuration to serialize
- `prettyPrint` - Whether to format the JSON with indentation (default: true)

**Returns:** JSON string representation of the configuration

#### DeserializeMultiFloorConfigFromJson

```csharp
public MultiFloorConfig<TRoomType> DeserializeMultiFloorConfigFromJson(string json)
```

Deserializes a JSON string to `MultiFloorConfig`.

**Parameters:**
- `json` - The JSON string to deserialize

**Returns:** `MultiFloorConfig<TRoomType>` instance

**Exceptions:**
- `InvalidConfigurationException` - Thrown when JSON is invalid or missing required fields

## ConfigurationSerializationExtensions

Extension methods for convenient configuration serialization.

### ToJson

```csharp
public static string ToJson<TRoomType>(this FloorConfig<TRoomType> config) where TRoomType : Enum
```

Serializes a `FloorConfig` to JSON string with pretty printing enabled.

**Returns:** JSON string representation of the configuration

### FromJson

```csharp
public static FloorConfig<TRoomType> FromJson<TRoomType>(string json) where TRoomType : Enum
```

Deserializes a JSON string to `FloorConfig`.

**Parameters:**
- `json` - The JSON string to deserialize

**Returns:** `FloorConfig<TRoomType>` instance

**Exceptions:**
- `InvalidConfigurationException` - Thrown when JSON is invalid or missing required fields

### SaveToFile

```csharp
public static void SaveToFile<TRoomType>(this FloorConfig<TRoomType> config, string filePath) where TRoomType : Enum
```

Saves a `FloorConfig` to a file as JSON.

**Parameters:**
- `config` - The configuration to save
- `filePath` - Path to the file to save to

### LoadFromFile

```csharp
public static FloorConfig<TRoomType> LoadFromFile<TRoomType>(string filePath) where TRoomType : Enum
```

Loads a `FloorConfig` from a JSON file.

**Parameters:**
- `filePath` - Path to the JSON file to load

**Returns:** `FloorConfig<TRoomType>` instance

**Exceptions:**
- `InvalidConfigurationException` - Thrown when JSON is invalid or missing required fields
- `FileNotFoundException` - Thrown when the file doesn't exist

## AsciiMapRenderer<TRoomType>

Generates ASCII art visualizations of dungeon layouts.

### Constructor

```csharp
public AsciiMapRenderer()
```

Creates a new ASCII map renderer.

### Methods

#### Render (FloorLayout to string)

```csharp
public string Render(FloorLayout<TRoomType> layout, AsciiRenderOptions? options = null)
```

Renders a single-floor layout to a string.

**Parameters:**
- `layout` - The floor layout to render
- `options` - Optional rendering configuration (default: null, uses default options)

**Returns:** `string` - ASCII art representation of the dungeon

#### Render (FloorLayout to TextWriter)

```csharp
public void Render(FloorLayout<TRoomType> layout, TextWriter writer, AsciiRenderOptions? options = null)
```

Renders a single-floor layout to a TextWriter.

**Parameters:**
- `layout` - The floor layout to render
- `writer` - TextWriter to write output to
- `options` - Optional rendering configuration

#### Render (FloorLayout to StringBuilder)

```csharp
public void Render(FloorLayout<TRoomType> layout, StringBuilder builder, AsciiRenderOptions? options = null)
```

Renders a single-floor layout to a StringBuilder.

**Parameters:**
- `layout` - The floor layout to render
- `builder` - StringBuilder to append output to
- `options` - Optional rendering configuration

#### Render (MultiFloorLayout to string)

```csharp
public string Render(MultiFloorLayout<TRoomType> layout, AsciiRenderOptions? options = null)
```

Renders a multi-floor layout to a string. Each floor is rendered separately with clear separators.

**Parameters:**
- `layout` - The multi-floor layout to render
- `options` - Optional rendering configuration

**Returns:** `string` - ASCII art representation of all floors

## AsciiRenderOptions

Configuration options for ASCII rendering.

### Properties

```csharp
public AsciiRenderStyle Style { get; init; }  // Default: AsciiRenderStyle.Detailed
public IReadOnlyDictionary<object, char>? CustomRoomTypeSymbols { get; init; }  // Default: null
public bool ShowRoomIds { get; init; }  // Default: false
public bool HighlightCriticalPath { get; init; }  // Default: true
public bool ShowHallways { get; init; }  // Default: true
public bool ShowDoors { get; init; }  // Default: true
public bool ShowInteriorFeatures { get; init; }  // Default: true
public bool ShowSecretPassages { get; init; }  // Default: true
public bool ShowZoneBoundaries { get; init; }  // Default: false
public (Cell Min, Cell Max)? Viewport { get; init; }  // Default: null (render entire dungeon)
public int Scale { get; init; }  // Default: 1
public bool IncludeLegend { get; init; }  // Default: true
public (int MaxWidth, int MaxHeight)? MaxSize { get; init; }  // Default: (120, 40)
```

**Style**: Rendering style preset (Minimal, Detailed, Artistic, Compact).

**CustomRoomTypeSymbols**: Custom symbol mappings for room types. Overrides style defaults. Key is the room type enum value, value is the character to use.

**ShowRoomIds**: Whether to show room IDs in the legend (not overlaid on the map).

**HighlightCriticalPath**: Whether to highlight critical path rooms (shown as uppercase symbols).

**ShowHallways**: Whether to render hallway cells (shown as '.').

**ShowDoors**: Whether to render doors (shown as '+').

**ShowInteriorFeatures**: Whether to render interior features (pillars, walls, hazards, decorative).

**ShowSecretPassages**: Whether to render secret passages (shown as '~').

**ShowZoneBoundaries**: Whether to show zone boundaries (if zones are configured).

**Viewport**: Viewport for large dungeons. Null = render entire dungeon. Specifies a rectangular region to render.

**Scale**: Scale factor for rendering (1 = normal, 2 = double size, etc.).

**IncludeLegend**: Whether to include a legend at the bottom showing symbol meanings.

**MaxSize**: Maximum width/height before auto-scaling or viewport is required. Used for performance optimization.

## AsciiRenderStyle

Rendering style presets for ASCII map visualization.

### Values

```csharp
public enum AsciiRenderStyle
{
    Minimal,    // Minimal style - just rooms and connections
    Detailed,   // Detailed style - rooms, hallways, doors, features
    Artistic,   // Artistic style - uses box-drawing characters for walls
    Compact     // Compact style - optimized for small terminals
}
```

**Minimal**: Basic visualization with just rooms and connections.

**Detailed**: Full visualization including all features (default).

**Artistic**: Enhanced visualization with box-drawing characters (future enhancement).

**Compact**: Optimized for small terminal windows.

## DungeonTheme<TRoomType>

Represents a complete dungeon theme with all generation parameters and metadata.

### Properties

```csharp
public required string Id { get; init; }
public required string Name { get; init; }
public string? Description { get; init; }
public required FloorConfig<TRoomType> BaseConfig { get; init; }
public IReadOnlyList<Zone<TRoomType>>? Zones { get; init; }
public IReadOnlySet<string> Tags { get; init; }
```

**Id**: Unique identifier for this theme (e.g., "castle", "cave").

**Name**: Display name for this theme (e.g., "Castle", "Cave").

**Description**: Optional description of this theme's characteristics.

**BaseConfig**: Base floor configuration for this theme.

**Zones**: Optional zone configurations for this theme.

**Tags**: Tags for categorizing themes (e.g., "underground", "structured", "organic").

### Methods

#### ToFloorConfig

```csharp
public FloorConfig<TRoomType> ToFloorConfig(ThemeOverrides? overrides = null)
```

Creates a `FloorConfig` from this theme with optional overrides.

**Parameters:**
- `overrides` - Optional overrides for specific properties (seed, room count, branching factor, etc.)

**Returns:** `FloorConfig<TRoomType>` - Configuration ready for generation

**Exceptions:**
- `InvalidConfigurationException` - Thrown when theme or resulting config is invalid

#### Combine

```csharp
public DungeonTheme<TRoomType> Combine(DungeonTheme<TRoomType> other)
```

Creates a new theme by combining this theme with another (other takes precedence).

**Parameters:**
- `other` - Theme to combine with (takes precedence for config and properties)

**Returns:** `DungeonTheme<TRoomType>` - Combined theme with merged zones and tags

## ThemePresetLibrary<TRoomType>

Library of built-in dungeon themes.

### Static Properties

```csharp
public static DungeonTheme<TRoomType> Castle { get; }
public static DungeonTheme<TRoomType> Cave { get; }
public static DungeonTheme<TRoomType> Temple { get; }
public static DungeonTheme<TRoomType> Laboratory { get; }
public static DungeonTheme<TRoomType> Crypt { get; }
public static DungeonTheme<TRoomType> Forest { get; }
```

Access built-in themes directly via properties.

### Static Methods

#### GetTheme

```csharp
public static DungeonTheme<TRoomType>? GetTheme(string themeId)
```

Gets a built-in theme by ID.

**Parameters:**
- `themeId` - Theme identifier (case-insensitive, e.g., "castle", "CAVE")

**Returns:** `DungeonTheme<TRoomType>?` - Theme if found, null otherwise

#### GetAllThemes

```csharp
public static IReadOnlyList<DungeonTheme<TRoomType>> GetAllThemes()
```

Gets all built-in themes.

**Returns:** `IReadOnlyList<DungeonTheme<TRoomType>>` - List of all built-in themes

#### GetThemesByTags

```csharp
public static IReadOnlyList<DungeonTheme<TRoomType>> GetThemesByTags(params string[] tags)
```

Gets themes matching the specified tags.

**Parameters:**
- `tags` - Tags to match (case-insensitive, OR logic - matches if theme has any tag)

**Returns:** `IReadOnlyList<DungeonTheme<TRoomType>>` - Themes matching at least one tag

## ThemeOverrides

Allows overriding specific aspects of a theme when converting to FloorConfig.

### Properties

```csharp
public int? Seed { get; init; }
public int? RoomCount { get; init; }
public float? BranchingFactor { get; init; }
public HallwayMode? HallwayMode { get; init; }
public GraphAlgorithm? GraphAlgorithm { get; init; }
```

**Seed**: Override the seed value.

**RoomCount**: Override the room count.

**BranchingFactor**: Override the branching factor.

**HallwayMode**: Override the hallway mode.

**GraphAlgorithm**: Override the graph algorithm.

**Note**: Only specified properties override the theme's base config. Unspecified properties use theme defaults.

## ConfigurationSerializer Theme Methods

### SerializeThemeToJson

```csharp
public string SerializeThemeToJson(DungeonTheme<TRoomType> theme, bool prettyPrint = true)
```

Serializes a `DungeonTheme` to JSON string.

**Parameters:**
- `theme` - The theme to serialize
- `prettyPrint` - Whether to format the JSON with indentation (default: true)

**Returns:** JSON string representation of the theme

### DeserializeThemeFromJson

```csharp
public DungeonTheme<TRoomType> DeserializeThemeFromJson(string json)
```

Deserializes a JSON string to `DungeonTheme`.

**Parameters:**
- `json` - The JSON string to deserialize

**Returns:** `DungeonTheme<TRoomType>` instance

**Exceptions:**
- `InvalidConfigurationException` - Thrown when JSON is invalid or missing required fields

## DebugLogger

Configurable DEBUG logging system with log levels, component filtering, and test context detection. Provides zero-overhead logging when verbose logs are disabled.

### Log Levels

```csharp
public enum LogLevel
{
    Verbose,  // Most verbose - detailed operation logs
    Info,     // Informational messages
    Warn,     // Warning messages
    Error     // Error messages (cannot be suppressed)
}
```

### Components

```csharp
public enum Component
{
    AStar,                // A* pathfinding algorithm logs
    RoomPlacement,        // Room placement and spatial solving logs
    HallwayGeneration,    // Hallway generation and connection logs
    GraphGeneration,      // Graph algorithm execution logs
    ConstraintEvaluation, // Constraint checking and validation logs
    General               // General debug information
}
```

### Static Methods

#### LogVerbose

```csharp
[Conditional("DEBUG")]
public static void LogVerbose(Component component, string message)
```

Logs a verbose message. Only outputs if VERBOSE level is enabled and component is enabled. Suppressed by default in test contexts.

**Parameters:**
- `component` - Component category for filtering
- `message` - Log message

#### LogInfo

```csharp
[Conditional("DEBUG")]
public static void LogInfo(Component component, string message)
```

Logs an info message. Only outputs if INFO level or higher is enabled and component is enabled.

**Parameters:**
- `component` - Component category for filtering
- `message` - Log message

#### LogWarn

```csharp
[Conditional("DEBUG")]
public static void LogWarn(Component component, string message)
```

Logs a warning message. Only outputs if WARN level or higher is enabled and component is enabled.

**Parameters:**
- `component` - Component category for filtering
- `message` - Log message

#### LogError

```csharp
[Conditional("DEBUG")]
public static void LogError(Component component, string message)
```

Logs an error message. Always outputs if component is enabled (ERROR level cannot be suppressed).

**Parameters:**
- `component` - Component category for filtering
- `message` - Log message

#### SetLogLevel

```csharp
public static void SetLogLevel(LogLevel level)
```

Sets the current log level programmatically.

**Parameters:**
- `level` - Minimum log level to output

#### EnableComponent

```csharp
public static void EnableComponent(Component component)
```

Enables logging for the specified component.

**Parameters:**
- `component` - Component to enable

#### DisableComponent

```csharp
public static void DisableComponent(Component component)
```

Disables logging for the specified component.

**Parameters:**
- `component` - Component to disable

#### ResetConfiguration

```csharp
public static void ResetConfiguration()
```

Resets the logger configuration to defaults. Used primarily for testing.

### Properties

#### IsTestContext

```csharp
public static bool IsTestContext { get; }
```

Gets whether the current execution context is a test context. Automatically detects test assemblies in the call stack or checks the `SHEPHERD_TEST_MODE` environment variable.

### Environment Variables

#### SHEPHERD_DEBUG_LEVEL

Sets the minimum log level to output. Values: `Verbose`, `Info`, `Warn`, `Error` (case-insensitive).

**Examples:**
```bash
# Enable all logs including verbose
export SHEPHERD_DEBUG_LEVEL=VERBOSE

# Only show warnings and errors
export SHEPHERD_DEBUG_LEVEL=WARN
```

#### SHEPHERD_DEBUG_COMPONENTS

Comma-separated list of components to enable. Only specified components will log. Values: `AStar`, `RoomPlacement`, `HallwayGeneration`, `GraphGeneration`, `ConstraintEvaluation`, `General` (case-insensitive).

**Examples:**
```bash
# Only log A* pathfinding
export SHEPHERD_DEBUG_COMPONENTS=AStar

# Log multiple components
export SHEPHERD_DEBUG_COMPONENTS=AStar,HallwayGeneration
```

#### SHEPHERD_TEST_MODE

When set to `true`, enables test context detection (suppresses VERBOSE logs by default).

**Example:**
```bash
export SHEPHERD_TEST_MODE=true
```

### Usage Examples

```csharp
// Verbose logging (suppressed in tests by default)
DebugLogger.LogVerbose(Component.AStar, $"Exploring node {nodeId}");

// Info logging (always shown unless level is WARN or ERROR)
DebugLogger.LogInfo(Component.RoomPlacement, $"Placed room {roomId} at {position}");

// Warning logging
DebugLogger.LogWarn(Component.ConstraintEvaluation, $"Constraint may be too restrictive");

// Error logging (always shown if component is enabled)
DebugLogger.LogError(Component.General, $"Failed to place room: {ex.Message}");
```

### Performance

- **Zero overhead in RELEASE builds**: All logging methods are marked with `[Conditional("DEBUG")]`, so they are completely removed in release builds.
- **Fast-path optimization**: When verbose logging is disabled, `LogVerbose` returns immediately without any string formatting or component checks.
- **Lazy evaluation**: Log messages are only formatted when the log level and component are actually enabled.

### Default Behavior

- **In test contexts**: VERBOSE logs are suppressed by default, INFO/WARN/ERROR logs are shown.
- **In non-test contexts**: Default log level is INFO (VERBOSE suppressed, INFO/WARN/ERROR shown).
- **All components enabled by default**: Unless `SHEPHERD_DEBUG_COMPONENTS` is set, all components log.

## Namespaces

- `ShepherdProceduralDungeons` - Main entry point
- `ShepherdProceduralDungeons.Configuration` - Configuration classes, including themes
- `ShepherdProceduralDungeons.Constraints` - Constraint system
- `ShepherdProceduralDungeons.Exceptions` - Exception classes
- `ShepherdProceduralDungeons.Generation` - Generation algorithms
- `ShepherdProceduralDungeons.Graph` - Graph structures
- `ShepherdProceduralDungeons.Layout` - Output layout classes
- `ShepherdProceduralDungeons.Serialization` - Serialization support, including theme serialization
- `ShepherdProceduralDungeons.Templates` - Template system
- `ShepherdProceduralDungeons.Visualization` - ASCII visualization system

## See Also

- **[Getting Started](Getting-Started)** - Learn the basics
- **[Room Templates](Room-Templates)** - Template API details
- **[Constraints](Constraints)** - Constraint API details
- **[Configuration](Configuration)** - Config API details, including serialization
- **[Working with Output](Working-with-Output)** - Layout API details


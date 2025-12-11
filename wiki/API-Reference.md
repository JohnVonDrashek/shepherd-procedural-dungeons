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
public HallwayMode HallwayMode { get; init; }  // Default: HallwayMode.AsNeeded
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
```

### Methods

```csharp
public PlacedRoom<TRoomType>? GetRoom(int nodeId)
public IEnumerable<Cell> GetAllRoomCells()
public IEnumerable<Cell> GetAllHallwayCells()
public (Cell Min, Cell Max) GetBounds()
```

## RoomTemplate<TRoomType>

Room shape and door placement definition.

### Properties

```csharp
public required string Id { get; init; }
public required IReadOnlySet<TRoomType> ValidRoomTypes { get; init; }
public required IReadOnlySet<Cell> Cells { get; init; }
public required IReadOnlyDictionary<Cell, Edge> DoorEdges { get; init; }
public double Weight { get; init; }  // Default: 1.0
public int Width { get; }
public int Height { get; }
```

**Weight**: Selection weight for this template. Higher weights increase selection probability. Default is 1.0 (uniform distribution when all templates have default weight). Must be greater than 0.

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
public RoomTemplate<TRoomType> Build()
```

**WithWeight**: Sets the selection weight for this template. Weight must be greater than 0. Default is 1.0. Higher weights increase selection probability.

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
```

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

## Namespaces

- `ShepherdProceduralDungeons` - Main entry point
- `ShepherdProceduralDungeons.Configuration` - Configuration classes
- `ShepherdProceduralDungeons.Constraints` - Constraint system
- `ShepherdProceduralDungeons.Exceptions` - Exception classes
- `ShepherdProceduralDungeons.Generation` - Generation algorithms
- `ShepherdProceduralDungeons.Graph` - Graph structures
- `ShepherdProceduralDungeons.Layout` - Output layout classes
- `ShepherdProceduralDungeons.Templates` - Template system

## See Also

- **[Getting Started](Getting-Started)** - Learn the basics
- **[Room Templates](Room-Templates)** - Template API details
- **[Constraints](Constraints)** - Constraint API details
- **[Configuration](Configuration)** - Config API details
- **[Working with Output](Working-with-Output)** - Layout API details


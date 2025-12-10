# ShepherdProceduralDungeons

<img src="src/ShepherdProceduralDungeons/icon.png" alt="ShepherdProceduralDungeons Icon" width="128" height="128" align="right">

A .NET 10.0 library for procedural dungeon generation targeting roguelike/dungeon crawler games. Produces graph topology and spatial layouts; rendering is the game's responsibility.

## Features

- **Graph-based topology**: Generates connected room graphs with configurable branching
- **Room type constraints**: Flexible constraint system for placing special rooms (boss, shop, treasure, etc.)
- **Spatial layout**: Automatically places rooms in 2D grid space with collision avoidance
- **Hallway generation**: Optional hallways connect non-adjacent rooms using A* pathfinding
- **Deterministic generation**: Same seed + config = identical output
- **Generic room types**: Use your own enum for room types

## Installation

```bash
dotnet add package ShepherdProceduralDungeons
```

Or install from source by referencing the project.

## Quick Start

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

// 1. Define your room types
public enum RoomType
{
    Spawn,
    Boss,
    Combat,
    Shop,
    Treasure,
    Secret
}

// 2. Create room templates
var templates = new List<RoomTemplate<RoomType>>
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn-room")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnSides(Edge.All)
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss-arena")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("treasure")
        .ForRoomTypes(RoomType.Treasure)
        .WithDoorsOnSides(Edge.All)
        .Build()
};

// 3. Define constraints
var constraints = new List<IConstraint<RoomType>>
{
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, minDistance: 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, maxCount: 2)
};

// 4. Create configuration
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 2)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.2f,
    HallwayMode = HallwayMode.AsNeeded
};

// 5. Generate the floor
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// 6. Use the output
Console.WriteLine($"Generated floor with seed {layout.Seed}");
Console.WriteLine($"Spawn room: {layout.SpawnRoomId}");
Console.WriteLine($"Boss room: {layout.BossRoomId}");
Console.WriteLine($"Critical path: {string.Join(" -> ", layout.CriticalPath)}");
Console.WriteLine($"Total rooms: {layout.Rooms.Count}");
Console.WriteLine($"Total hallways: {layout.Hallways.Count}");

foreach (var room in layout.Rooms)
{
    Console.WriteLine($"  Room {room.NodeId}: {room.RoomType} at ({room.Position.X}, {room.Position.Y}) using template '{room.Template.Id}'");
}
```

## Core Concepts

### Room Templates

Templates define the shape and door placement for rooms:

```csharp
// Simple rectangle
var template = RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
    .WithId("combat-room")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

// L-shaped room
var lShape = RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
    .WithId("l-shaped")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

// Custom shape
var custom = new RoomTemplateBuilder<RoomType>()
    .WithId("custom")
    .ForRoomTypes(RoomType.Treasure)
    .AddCell(1, 0)
    .AddCell(0, 1)
    .AddCell(1, 1)
    .AddCell(2, 1)
    .AddCell(1, 2)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

### Constraints

Constraints control where specific room types can be placed:

- **`MinDistanceFromStartConstraint`** - Room must be at least N steps from start
- **`MaxDistanceFromStartConstraint`** - Room must be at most N steps from start
- **`NotOnCriticalPathConstraint`** - Room must NOT be on critical path (spawn to boss)
- **`OnlyOnCriticalPathConstraint`** - Room MUST be on critical path
- **`MustBeDeadEndConstraint`** - Room must have exactly one connection
- **`MaxPerFloorConstraint`** - At most N rooms of this type per floor
- **`CustomConstraint`** - Custom callback-based constraint

### Hallway Modes

- **`HallwayMode.None`** - Rooms must share a wall (throws if impossible)
- **`HallwayMode.AsNeeded`** - Generate hallways only when rooms can't touch directly
- **`HallwayMode.Always`** - Always generate hallways between all rooms

## What This Library Does

✅ Generates dungeon floor topology (which rooms connect to which)  
✅ Assigns room types based on configurable constraints  
✅ Solves spatial layout (positions rooms in 2D grid space)  
✅ Places doors between connected rooms  
✅ Generates hallways when rooms can't be adjacent  
✅ All generation is deterministic given a seed  

## What This Library Does NOT Do

❌ Rendering (your responsibility)  
❌ Room interior content (enemies, obstacles, tiles)  
❌ Physics or collision  
❌ Multi-floor/stairs (out of scope for v1)  

## Deterministic Generation

Same seed + same config = identical output. The library uses separate random number generators for each generation phase to ensure consistency:

- Graph generation phase
- Room type assignment phase
- Template selection phase
- Spatial placement phase
- Hallway generation phase

## Error Handling

The library throws specific exceptions when generation fails:

- **`InvalidConfigurationException`** - Configuration is invalid before generation starts
- **`ConstraintViolationException`** - Room type constraints cannot be satisfied
- **`SpatialPlacementException`** - Rooms cannot be placed in 2D space

## Documentation

See [DESIGN.md](DESIGN.md) for complete design documentation including:
- Detailed API reference
- Algorithm descriptions
- Testing strategy
- Implementation details

## License

MIT

## Contributing

This is a work in progress. Issues and pull requests welcome!


# Getting Started

This guide will walk you through creating your first procedural dungeon in 5 simple steps.

## Step 1: Define Your Room Types

First, create an enum that defines the different types of rooms in your game:

```csharp
public enum RoomType
{
    Spawn,    // Starting room
    Boss,     // Final boss room
    Combat,   // Regular combat encounters
    Shop,     // Merchant room
    Treasure, // Loot room
    Secret    // Hidden room
}
```

You can use any enum values you want - the library is generic and works with your enum.

## Step 2: Create Room Templates

Templates define the shape and door placement for rooms. Use the `RoomTemplateBuilder` to create them:

```csharp
using ShepherdProceduralDungeons.Templates;

var templates = new List<RoomTemplate<RoomType>>
{
    // Spawn room - small, doors on all sides
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn-room")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnAllExteriorEdges()
        .Build(),

    // Boss room - large arena, door only on south
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss-arena")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build(),

    // Combat room - medium size
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),

    // Small treasure room
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("treasure")
        .ForRoomTypes(RoomType.Treasure)
        .WithDoorsOnSides(Edge.All)
        .Build()
};
```

**Key points:**
- Each template needs a unique `Id`
- `ForRoomTypes()` specifies which room types can use this template
- `WithDoorsOnAllExteriorEdges()` allows doors anywhere on the perimeter
- `WithDoorsOnSides()` restricts doors to specific sides (North, South, East, West)

See [Room Templates](Room-Templates) for more details.

## Step 3: Define Constraints (Optional)

Constraints control where special rooms can be placed. For example, you might want boss rooms far from spawn:

```csharp
using ShepherdProceduralDungeons.Constraints;

var constraints = new List<IConstraint<RoomType>>
{
    // Boss must be at least 5 steps from spawn
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, minDistance: 5),
    
    // Boss must be a dead end (only one connection)
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Treasure rooms not on critical path (spawn to boss)
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    
    // Treasure rooms must be dead ends
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    
    // Maximum 2 treasure rooms per floor
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, maxCount: 2)
};
```

See [Constraints](Constraints) for all available constraint types.

## Step 4: Create Configuration

The `FloorConfig` object contains all settings for generation:

```csharp
using ShepherdProceduralDungeons.Configuration;

var config = new FloorConfig<RoomType>
{
    Seed = 12345,  // For deterministic generation
    RoomCount = 12,  // Total number of rooms
    
    // Required room types
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,  // Fills remaining rooms
    
    // How many of each special type
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 2)  // 2 treasure rooms
    },
    
    // Constraints and templates
    Constraints = constraints,
    Templates = templates,
    
    // Graph structure: 0.0 = tree, 1.0 = highly connected
    BranchingFactor = 0.2f,
    
    // Hallway generation mode
    HallwayMode = HallwayMode.AsNeeded
};
```

**Configuration options:**
- `Seed`: Same seed = same dungeon (deterministic)
- `RoomCount`: Total rooms to generate
- `BranchingFactor`: 0.0 = linear path, 1.0 = many loops
- `HallwayMode`: How to handle non-adjacent rooms

See [Configuration](Configuration) for complete details.

## Step 5: Generate and Use

Create a generator and call `Generate()`:

```csharp
using ShepherdProceduralDungeons;

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

The `layout` object contains everything you need:

```csharp
// Basic info
Console.WriteLine($"Seed: {layout.Seed}");
Console.WriteLine($"Spawn Room ID: {layout.SpawnRoomId}");
Console.WriteLine($"Boss Room ID: {layout.BossRoomId}");
Console.WriteLine($"Total Rooms: {layout.Rooms.Count}");
Console.WriteLine($"Total Hallways: {layout.Hallways.Count}");

// Critical path (spawn to boss)
Console.WriteLine($"Critical Path: {string.Join(" -> ", layout.CriticalPath)}");

// Iterate through rooms
foreach (var room in layout.Rooms)
{
    Console.WriteLine($"Room {room.NodeId}:");
    Console.WriteLine($"  Type: {room.RoomType}");
    Console.WriteLine($"  Position: ({room.Position.X}, {room.Position.Y})");
    Console.WriteLine($"  Template: {room.Template.Id}");
    
    // Get all cells this room occupies
    foreach (var cell in room.GetWorldCells())
    {
        Console.WriteLine($"    Cell: ({cell.X}, {cell.Y})");
    }
}

// Iterate through hallways
foreach (var hallway in layout.Hallways)
{
    Console.WriteLine($"Hallway {hallway.Id}:");
    foreach (var segment in hallway.Segments)
    {
        Console.WriteLine($"  Segment: ({segment.Start.X}, {segment.Start.Y}) to ({segment.End.X}, {segment.End.Y})");
    }
}

// Get bounding box
var (min, max) = layout.GetBounds();
Console.WriteLine($"Bounds: ({min.X}, {min.Y}) to ({max.X}, {max.Y})");
```

## Complete Example

Here's a complete, runnable example:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Treasure
}

class Program
{
    static void Main()
    {
        // Templates
        var templates = new List<RoomTemplate<RoomType>>
        {
            RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
                .WithId("spawn")
                .ForRoomTypes(RoomType.Spawn)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            
            RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
                .WithId("boss")
                .ForRoomTypes(RoomType.Boss)
                .WithDoorsOnSides(Edge.South)
                .Build(),
            
            RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
                .WithId("combat")
                .ForRoomTypes(RoomType.Combat)
                .WithDoorsOnAllExteriorEdges()
                .Build(),
            
            RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
                .WithId("treasure")
                .ForRoomTypes(RoomType.Treasure)
                .WithDoorsOnSides(Edge.All)
                .Build()
        };

        // Constraints
        var constraints = new List<IConstraint<RoomType>>
        {
            new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
            new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
            new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
            new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
            new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
        };

        // Config
        var config = new FloorConfig<RoomType>
        {
            Seed = 12345,
            RoomCount = 12,
            SpawnRoomType = RoomType.Spawn,
            BossRoomType = RoomType.Boss,
            DefaultRoomType = RoomType.Combat,
            RoomRequirements = new[] { (RoomType.Treasure, 2) },
            Constraints = constraints,
            Templates = templates,
            BranchingFactor = 0.2f,
            HallwayMode = HallwayMode.AsNeeded
        };

        // Generate
        var generator = new FloorGenerator<RoomType>();
        var layout = generator.Generate(config);

        // Output
        Console.WriteLine($"Generated {layout.Rooms.Count} rooms with seed {layout.Seed}");
        Console.WriteLine($"Critical path: {string.Join(" -> ", layout.CriticalPath)}");
    }
}
```

## Next Steps

- **[Room Templates](Room-Templates)** - Learn about creating custom room shapes
- **[Constraints](Constraints)** - Master the constraint system
- **[Examples](Examples)** - See more complete examples
- **[Configuration](Configuration)** - Understand all configuration options

## Common Issues

If you get errors, check:
1. **InvalidConfigurationException**: Your config has invalid values (see [Troubleshooting](Troubleshooting))
2. **ConstraintViolationException**: Constraints can't be satisfied (try relaxing constraints)
3. **SpatialPlacementException**: Rooms can't fit in 2D space (try different templates or `HallwayMode`)

See [Troubleshooting](Troubleshooting) for solutions to common problems.


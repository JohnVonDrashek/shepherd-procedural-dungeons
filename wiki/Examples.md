# Examples

Complete, runnable examples for common dungeon generation scenarios.

## Simple Dungeon

Minimal configuration for a basic dungeon:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat
}

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
        .Build()
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

Console.WriteLine($"Generated {layout.Rooms.Count} rooms");
```

## Roguelike Dungeon

Complete roguelike setup with boss, treasure, and shop:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// Templates
var templates = new List<RoomTemplate<RoomType>>
{
    // Spawn
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Boss
    RoomTemplateBuilder<RoomType>.Rectangle(8, 8)
        .WithId("boss-arena")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build(),
    
    // Combat (multiple sizes with weights)
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("combat-small")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(4.0)  // Small rooms are more common
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(2.0)  // Medium rooms are moderate
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-large")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(1.0)  // Large rooms are rare
        .Build(),
    
    // Shop
    RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South | Edge.North)
        .Build(),
    
    // Treasure
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("treasure")
        .ForRoomTypes(RoomType.Treasure)
        .WithDoorsOnSides(Edge.All)
        .Build()
};

// Constraints
var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 6),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Shop constraints
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat),
    
    // Treasure constraints
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 3),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Treasure, RoomType.Boss)
};

// Config
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 3)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.25f,
    HallwayMode = HallwayMode.AsNeeded
};

// Generate
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Output
Console.WriteLine($"Generated {layout.Rooms.Count} rooms");
Console.WriteLine($"Critical path: {string.Join(" -> ", layout.CriticalPath)}");
Console.WriteLine($"Treasure rooms: {layout.Rooms.Count(r => r.RoomType == RoomType.Treasure)}");
```

## Adjacency Constraints

Example using adjacency constraints to create spatial relationships between room types:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure, Rest
}

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
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South | Edge.North)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("treasure")
        .ForRoomTypes(RoomType.Treasure)
        .WithDoorsOnSides(Edge.All)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("rest")
        .ForRoomTypes(RoomType.Rest)
        .WithDoorsOnSides(Edge.All)
        .Build()
};

var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Shop must be adjacent to Combat rooms (shops near danger)
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat),
    new MustNotBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Shop),  // Prevent shop clustering
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    
    // Treasure must be adjacent to Boss OR Combat (reward near challenge)
    new MustBeAdjacentToConstraint<RoomType>(
        RoomType.Treasure, 
        RoomType.Boss, 
        RoomType.Combat
    ),
    new MustNotBeAdjacentToConstraint<RoomType>(RoomType.Treasure, RoomType.Spawn),  // Keep treasure away from spawn
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2),
    
    // Rest rooms must be adjacent to Combat (safe havens after fights)
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Rest, RoomType.Combat),
    new MaxPerFloorConstraint<RoomType>(RoomType.Rest, 2)
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2),
        (RoomType.Rest, 2)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Verify adjacency constraints are satisfied
foreach (var shop in layout.Rooms.Where(r => r.RoomType == RoomType.Shop))
{
    // Find adjacent rooms in the graph
    var shopNode = layout.Rooms.First(r => r.NodeId == shop.NodeId);
    var adjacentRooms = layout.Rooms.Where(r => 
        layout.CriticalPath.Contains(r.NodeId) && 
        Math.Abs(layout.CriticalPath.IndexOf(r.NodeId) - 
                 layout.CriticalPath.IndexOf(shop.NodeId)) == 1
    );
    
    Console.WriteLine($"Shop at node {shop.NodeId} is adjacent to: " +
        string.Join(", ", adjacentRooms.Select(r => r.RoomType)));
}
```

This example demonstrates:
- **Shop adjacent to Combat**: Shops are placed near combat areas
- **Treasure adjacent to Boss or Combat**: Rewards are near challenging content
- **Rest adjacent to Combat**: Safe rooms are accessible after fights

## Connection Count Constraints

Example using connection count constraints to control room connectivity:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Hub, Linear, Treasure
}

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
    
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("hub")
        .ForRoomTypes(RoomType.Hub)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("linear")
        .ForRoomTypes(RoomType.Linear)
        .WithDoorsOnAllExteriorEdges()
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

var constraints = new List<IConstraint<RoomType>>
{
    // Boss is a hub (3+ connections) far from spawn
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MinConnectionCountConstraint<RoomType>(RoomType.Boss, 3),
    
    // Hub rooms are important branching points (3-5 connections)
    new MinConnectionCountConstraint<RoomType>(RoomType.Hub, 3),
    new MaxConnectionCountConstraint<RoomType>(RoomType.Hub, 5),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Hub, 2),
    
    // Linear rooms have exactly 2 connections (simple progression)
    new MinConnectionCountConstraint<RoomType>(RoomType.Linear, 2),
    new MaxConnectionCountConstraint<RoomType>(RoomType.Linear, 2),
    
    // Treasure rooms are simple (max 2 connections)
    new MaxConnectionCountConstraint<RoomType>(RoomType.Treasure, 2),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Hub, 2),
        (RoomType.Linear, 3),
        (RoomType.Treasure, 2)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Verify connection counts
foreach (var room in layout.Rooms)
{
    // Find the node in the graph to check connection count
    var nodeId = room.NodeId;
    Console.WriteLine($"{room.RoomType} at node {nodeId}");
}

// Count rooms by type
var hubCount = layout.Rooms.Count(r => r.RoomType == RoomType.Hub);
var linearCount = layout.Rooms.Count(r => r.RoomType == RoomType.Linear);
var treasureCount = layout.Rooms.Count(r => r.RoomType == RoomType.Treasure);

Console.WriteLine($"Hub rooms: {hubCount}");
Console.WriteLine($"Linear rooms: {linearCount}");
Console.WriteLine($"Treasure rooms: {treasureCount}");
```

This example demonstrates:
- **Boss as hub**: Boss rooms have 3+ connections (important areas)
- **Hub rooms**: Important branching points with 3-5 connections
- **Linear rooms**: Simple progression rooms with exactly 2 connections
- **Simple treasure**: Treasure rooms have at most 2 connections

## Distance-Based Room Type Constraints

Example using distance-based constraints to control relationships between room types:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Rest, Secret
}

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
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South | Edge.North)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("rest")
        .ForRoomTypes(RoomType.Rest)
        .WithDoorsOnSides(Edge.All)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("secret")
        .ForRoomTypes(RoomType.Secret)
        .WithDoorsOnSides(Edge.All)
        .Build()
};

var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Shop must be within 2 steps of Combat rooms (convenient access)
    new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Shop, RoomType.Combat, 2),
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    
    // Rest rooms must be within 2 steps of Combat (accessible after fights)
    new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Rest, RoomType.Combat, 2),
    new MaxPerFloorConstraint<RoomType>(RoomType.Rest, 2),
    
    // Secret rooms must be at least 3 steps from Boss (hidden away)
    new MinDistanceFromRoomTypeConstraint<RoomType>(RoomType.Secret, RoomType.Boss, 3),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Secret, 2),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
    new MaxPerFloorConstraint<RoomType>(RoomType.Secret, 1)
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Rest, 2),
        (RoomType.Secret, 1)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Verify distance constraints are satisfied
foreach (var shop in layout.Rooms.Where(r => r.RoomType == RoomType.Shop))
{
    Console.WriteLine($"Shop at node {shop.NodeId}");
    // In a real scenario, you would calculate distances to verify constraints
}

foreach (var rest in layout.Rooms.Where(r => r.RoomType == RoomType.Rest))
{
    Console.WriteLine($"Rest room at node {rest.NodeId}");
}

foreach (var secret in layout.Rooms.Where(r => r.RoomType == RoomType.Secret))
{
    Console.WriteLine($"Secret room at node {secret.NodeId}");
}
```

This example demonstrates:
- **Shop near Combat**: Shops are placed within 2 steps of combat areas for convenient access
- **Rest near Combat**: Rest rooms are accessible within 2 steps of combat encounters
- **Secret away from Boss**: Secret rooms are hidden at least 3 steps from boss rooms
- **Multiple reference types**: Can specify multiple reference types (e.g., "within 2 steps of Combat OR Boss")

## Critical Path Ordering Constraints

Example using `MustComeBeforeConstraint` to enforce ordering on the critical path:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, MiniBoss, Shop, KeyItem
}

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
    
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("miniboss")
        .ForRoomTypes(RoomType.MiniBoss)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South | Edge.North)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("keyitem")
        .ForRoomTypes(RoomType.KeyItem)
        .WithDoorsOnSides(Edge.All)
        .Build()
};

var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 6),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Mini-boss must come before Boss on critical path
    new MustComeBeforeConstraint<RoomType>(RoomType.MiniBoss, RoomType.Boss),
    new OnlyOnCriticalPathConstraint<RoomType>(RoomType.MiniBoss),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.MiniBoss, 3),
    
    // Shop must come before Boss OR MiniBoss (at least one)
    new MustComeBeforeConstraint<RoomType>(
        RoomType.Shop, 
        RoomType.Boss, 
        RoomType.MiniBoss
    ),
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 5),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    
    // Key item must come before Boss (progression gate)
    new MustComeBeforeConstraint<RoomType>(RoomType.KeyItem, RoomType.Boss),
    new OnlyOnCriticalPathConstraint<RoomType>(RoomType.KeyItem),
    new MaxPerFloorConstraint<RoomType>(RoomType.KeyItem, 1)
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.MiniBoss, 1),
        (RoomType.Shop, 1),
        (RoomType.KeyItem, 1)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Verify ordering constraints are satisfied
var criticalPath = layout.CriticalPath;
var miniBossIndex = criticalPath.IndexOf(
    layout.Rooms.First(r => r.RoomType == RoomType.MiniBoss).NodeId
);
var bossIndex = criticalPath.IndexOf(layout.BossRoomId);
var shopIndex = criticalPath.IndexOf(
    layout.Rooms.First(r => r.RoomType == RoomType.Shop).NodeId
);
var keyItemIndex = criticalPath.IndexOf(
    layout.Rooms.First(r => r.RoomType == RoomType.KeyItem).NodeId
);

Console.WriteLine($"Critical path ordering:");
Console.WriteLine($"  MiniBoss at index {miniBossIndex}, Boss at index {bossIndex}");
Console.WriteLine($"  Shop at index {shopIndex}");
Console.WriteLine($"  KeyItem at index {keyItemIndex}, Boss at index {bossIndex}");

// Verify constraints
Assert.True(miniBossIndex < bossIndex, "MiniBoss must come before Boss");
Assert.True(shopIndex < bossIndex || shopIndex < miniBossIndex, "Shop must come before Boss or MiniBoss");
Assert.True(keyItemIndex < bossIndex, "KeyItem must come before Boss");
```

This example demonstrates:
- **Mini-boss before Boss**: Mini-boss must appear before the final boss on the critical path
- **Shop before Boss or MiniBoss**: Shop must come before at least one of them (OR logic)
- **Key item before Boss**: Key item acts as a progression gate that must be encountered before the boss
- **Critical path ordering**: Ensures players encounter content in the intended sequence

## Linear Dungeon

Minimal branching for a linear progression:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 8,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.0f,  // No branching - pure tree
    HallwayMode = HallwayMode.AsNeeded
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

## Highly Branched Dungeon

Many loops and exploration paths:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 20,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.5f,  // Many loops
    HallwayMode = HallwayMode.Always  // Maximum flexibility
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

## Weighted Template Selection

Using weights to control template frequency:

```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Spawn
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Boss
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build(),
    
    // Combat templates with different weights
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-common")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(5.0)  // Very common (appears ~71% of the time)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-standard")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(2.0)  // Standard (appears ~29% of the time)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
        .WithId("combat-special")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(0.5)  // Rare special variant (appears ~7% of the time)
        .Build()
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.3f,
    HallwayMode = HallwayMode.AsNeeded
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Count template usage
var templateCounts = layout.Rooms
    .Where(r => r.RoomType == RoomType.Combat)
    .GroupBy(r => r.Template.Id)
    .ToDictionary(g => g.Key, g => g.Count());

foreach (var (templateId, count) in templateCounts)
{
    Console.WriteLine($"{templateId}: {count} rooms");
}
```

## Custom Room Shapes

Using L-shaped and custom templates:

```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Standard rectangles
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-standard")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // L-shaped combat room
    RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
        .WithId("combat-l")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Custom cross-shaped treasure room
    new RoomTemplateBuilder<RoomType>()
        .WithId("treasure-cross")
        .ForRoomTypes(RoomType.Treasure)
        .AddCell(1, 0)  //   X
        .AddCell(0, 1)  // X X X
        .AddCell(1, 1)  //   X
        .AddCell(2, 1)
        .AddCell(1, 2)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Custom T-shaped room
    new RoomTemplateBuilder<RoomType>()
        .WithId("combat-t")
        .ForRoomTypes(RoomType.Combat)
        .AddRectangle(0, 0, 3, 1)  // Top bar
        .AddRectangle(1, 1, 1, 2)  // Stem
        .WithDoorsOnAllExteriorEdges()
        .Build()
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.3f,
    HallwayMode = HallwayMode.AsNeeded
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

## Multiple Floors

Generate multiple floors with different seeds:

```csharp
var floorConfigs = new List<FloorConfig<RoomType>>();

for (int floor = 1; floor <= 5; floor++)
{
    var config = new FloorConfig<RoomType>
    {
        Seed = 1000 + floor,  // Different seed per floor
        RoomCount = 10 + floor * 2,  // Increasing difficulty
        SpawnRoomType = RoomType.Spawn,
        BossRoomType = RoomType.Boss,
        DefaultRoomType = RoomType.Combat,
        Templates = templates,
        BranchingFactor = 0.2f + (floor * 0.05f),  // More complex each floor
        HallwayMode = HallwayMode.AsNeeded
    };
    
    floorConfigs.Add(config);
}

var generator = new FloorGenerator<RoomType>();
var floors = floorConfigs.Select(c => generator.Generate(c)).ToList();

Console.WriteLine($"Generated {floors.Count} floors");
foreach (var floor in floors)
{
    Console.WriteLine($"Floor has {floor.Rooms.Count} rooms");
}
```

## Rendering Example

Complete example of rendering a dungeon:

```csharp
var layout = generator.Generate(config);

// Get bounds for viewport
var (min, max) = layout.GetBounds();
int width = max.X - min.X + 1;
int height = max.Y - min.Y + 1;

// Create 2D array for rendering
var tiles = new TileType[width, height];

// Initialize to empty
for (int x = 0; x < width; x++)
{
    for (int y = 0; y < height; y++)
    {
        tiles[x, y] = TileType.Empty;
    }
}

// Render rooms
foreach (var room in layout.Rooms)
{
    var tileType = room.RoomType switch
    {
        RoomType.Spawn => TileType.SpawnRoom,
        RoomType.Boss => TileType.BossRoom,
        RoomType.Combat => TileType.CombatRoom,
        RoomType.Shop => TileType.ShopRoom,
        RoomType.Treasure => TileType.TreasureRoom,
        _ => TileType.Floor
    };
    
    foreach (var cell in room.GetWorldCells())
    {
        int x = cell.X - min.X;
        int y = cell.Y - min.Y;
        tiles[x, y] = tileType;
    }
}

// Render hallways
foreach (var hallway in layout.Hallways)
{
    foreach (var segment in hallway.Segments)
    {
        foreach (var cell in segment.GetCells())
        {
            int x = cell.X - min.X;
            int y = cell.Y - min.Y;
            if (tiles[x, y] == TileType.Empty)
            {
                tiles[x, y] = TileType.Hallway;
            }
        }
    }
}

// Render doors
foreach (var door in layout.Doors)
{
    int x = door.Position.X - min.X;
    int y = door.Position.Y - min.Y;
    tiles[x, y] = TileType.Door;
}

// Print ASCII representation
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        char c = tiles[x, y] switch
        {
            TileType.SpawnRoom => 'S',
            TileType.BossRoom => 'B',
            TileType.CombatRoom => 'C',
            TileType.ShopRoom => '$',
            TileType.TreasureRoom => 'T',
            TileType.Hallway => '.',
            TileType.Door => 'D',
            _ => ' '
        };
        Console.Write(c);
    }
    Console.WriteLine();
}
```

## Game Integration Example

Example of integrating with a game engine:

```csharp
public class DungeonManager
{
    private FloorLayout<RoomType> _layout;
    private Dictionary<int, RoomEntity> _roomEntities = new();
    
    public void GenerateDungeon(int seed, int roomCount)
    {
        var config = CreateConfig(seed, roomCount);
        var generator = new FloorGenerator<RoomType>();
        _layout = generator.Generate(config);
        
        CreateRoomEntities();
    }
    
    private void CreateRoomEntities()
    {
        foreach (var room in _layout.Rooms)
        {
            var entity = new RoomEntity
            {
                NodeId = room.NodeId,
                RoomType = room.RoomType,
                Position = new Vector2(
                    room.Position.X * TileSize,
                    room.Position.Y * TileSize
                ),
                Cells = room.GetWorldCells().ToList()
            };
            
            _roomEntities[room.NodeId] = entity;
        }
    }
    
    public RoomEntity GetRoom(int nodeId)
    {
        return _roomEntities[nodeId];
    }
    
    public IEnumerable<RoomEntity> GetConnectedRooms(int nodeId)
    {
        // Find all rooms connected to this one via doors/hallways
        var connected = new List<RoomEntity>();
        
        foreach (var door in _layout.Doors)
        {
            if (door.ConnectsToRoomId == nodeId)
            {
                // Find room this door belongs to
                var room = _layout.Rooms.FirstOrDefault(r => 
                    r.GetWorldCells().Contains(door.Position));
                if (room != null)
                {
                    connected.Add(_roomEntities[room.NodeId]);
                }
            }
        }
        
        return connected;
    }
    
    public bool IsOnCriticalPath(int nodeId)
    {
        return _layout.CriticalPath.Contains(nodeId);
    }
}
```

## Testing Example

Example of testing dungeon generation:

```csharp
[Fact]
public void Generate_ValidConfig_ProducesValidDungeon()
{
    var config = CreateTestConfig();
    var generator = new FloorGenerator<RoomType>();
    
    var layout = generator.Generate(config);
    
    // Assertions
    Assert.NotNull(layout);
    Assert.Equal(config.RoomCount, layout.Rooms.Count);
    Assert.Equal(config.SpawnRoomType, 
        layout.GetRoom(layout.SpawnRoomId).RoomType);
    Assert.Equal(config.BossRoomType, 
        layout.GetRoom(layout.BossRoomId).RoomType);
    
    // Critical path is valid
    Assert.NotEmpty(layout.CriticalPath);
    Assert.Equal(layout.SpawnRoomId, layout.CriticalPath[0]);
    Assert.Equal(layout.BossRoomId, layout.CriticalPath[^1]);
    
    // Required rooms exist
    var treasureCount = layout.Rooms.Count(r => r.RoomType == RoomType.Treasure);
    Assert.Equal(2, treasureCount);
}

[Fact]
public void Generate_SameSeed_SameOutput()
{
    var config1 = CreateTestConfig(seed: 12345);
    var config2 = CreateTestConfig(seed: 12345);
    
    var generator = new FloorGenerator<RoomType>();
    var layout1 = generator.Generate(config1);
    var layout2 = generator.Generate(config2);
    
    Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
    Assert.Equal(layout1.SpawnRoomId, layout2.SpawnRoomId);
    Assert.Equal(layout1.BossRoomId, layout2.BossRoomId);
}
```

## Next Steps

- **[Getting Started](Getting-Started)** - Learn the basics
- **[Room Templates](Room-Templates)** - Create custom shapes
- **[Constraints](Constraints)** - Control room placement
- **[Working with Output](Working-with-Output)** - Use generated layouts


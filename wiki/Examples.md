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

## Constraint Composition (AND/OR/NOT)

Example using `CompositeConstraint` to express complex constraint logic:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure, Secret, Special
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
    
    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("secret")
        .ForRoomTypes(RoomType.Secret)
        .WithDoorsOnSides(Edge.All)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("special")
        .ForRoomTypes(RoomType.Special)
        .WithDoorsOnAllExteriorEdges()
        .Build()
};

var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // OR: Shop OR Treasure in dead ends (either constraint passes)
    CompositeConstraint<RoomType>.Or(
        new MustBeDeadEndConstraint<RoomType>(RoomType.Shop),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
    ),
    
    // NOT: Secret NOT near spawn (exclude certain conditions)
    CompositeConstraint<RoomType>.Not(
        new MaxDistanceFromStartConstraint<RoomType>(RoomType.Secret, 2)
    ),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
    new MaxPerFloorConstraint<RoomType>(RoomType.Secret, 1),
    
    // Nested: Special room - far from start AND (dead end OR not on critical path)
    CompositeConstraint<RoomType>.And(
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Special, 3),
        CompositeConstraint<RoomType>.Or(
            new MustBeDeadEndConstraint<RoomType>(RoomType.Special),
            new NotOnCriticalPathConstraint<RoomType>(RoomType.Special)
        )
    ),
    new MaxPerFloorConstraint<RoomType>(RoomType.Special, 1)
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
        (RoomType.Treasure, 1),
        (RoomType.Secret, 1),
        (RoomType.Special, 1)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Verify composition constraints
var shopRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Shop).ToList();
var treasureRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Treasure).ToList();
var secretRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Secret).ToList();
var specialRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Special).ToList();

Console.WriteLine($"Shop rooms: {shopRooms.Count}");
Console.WriteLine($"Treasure rooms: {treasureRooms.Count}");
Console.WriteLine($"Secret rooms: {secretRooms.Count}");
Console.WriteLine($"Special rooms: {specialRooms.Count}");

// Shop OR Treasure constraint: At least one should be in a dead end
// (Both could be, but at least one must be)
```

This example demonstrates:
- **OR composition**: "Shop OR Treasure in dead ends" - either constraint can pass
- **NOT composition**: "Secret NOT near spawn" - excludes rooms within 2 steps of spawn
- **Nested composition**: "Special room - far from start AND (dead end OR not on critical path)" - complex logic
- **Real-world scenarios**: Expressing game design requirements that need alternative placement strategies

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

## Difficulty Scaling

Example using room difficulty scaling to create progressive difficulty curves:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, EasyCombat, Elite, Shop, Treasure
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
    
    // Combat (default)
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Easy Combat
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("easy-combat")
        .ForRoomTypes(RoomType.EasyCombat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Elite
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("elite")
        .ForRoomTypes(RoomType.Elite)
        .WithDoorsOnAllExteriorEdges()
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

// Constraints with difficulty requirements
var constraints = new List<IConstraint<RoomType>>
{
    // Boss: high difficulty AND far from start
    new MinDifficultyConstraint<RoomType>(RoomType.Boss, 7.0),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 6),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Easy Combat: low difficulty only
    new MaxDifficultyConstraint<RoomType>(RoomType.EasyCombat, 2.5),
    new MaxPerFloorConstraint<RoomType>(RoomType.EasyCombat, 3),
    
    // Elite: medium-high difficulty range
    new MinDifficultyConstraint<RoomType>(RoomType.Elite, 5.0),
    new MaxDifficultyConstraint<RoomType>(RoomType.Elite, 8.0),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Elite),
    
    // Shop: accessible early (low difficulty)
    new MaxDifficultyConstraint<RoomType>(RoomType.Shop, 3.0),
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    
    // Treasure: medium difficulty
    new MinDifficultyConstraint<RoomType>(RoomType.Treasure, 3.0),
    new MaxDifficultyConstraint<RoomType>(RoomType.Treasure, 6.0),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
};

// Config with difficulty scaling
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 20,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.EasyCombat, 3),
        (RoomType.Elite, 2),
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.3f,
    
    // Difficulty scaling configuration
    DifficultyConfig = new DifficultyConfig
    {
        BaseDifficulty = 1.0,
        ScalingFactor = 1.5,
        Function = DifficultyScalingFunction.Linear,
        MaxDifficulty = 10.0
    }
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Display difficulty information
Console.WriteLine("Room Difficulties:");
foreach (var room in layout.Rooms.OrderBy(r => r.Difficulty))
{
    Console.WriteLine($"  Node {room.NodeId,2}: {room.RoomType,-12} Difficulty: {room.Difficulty:F2}");
}

// Verify difficulty constraints
Console.WriteLine("\nDifficulty Constraint Verification:");
var bossRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Boss);
foreach (var boss in bossRooms)
{
    Console.WriteLine($"  Boss at node {boss.NodeId}: Difficulty {boss.Difficulty:F2} (>= 7.0: {boss.Difficulty >= 7.0})");
}

var easyCombatRooms = layout.Rooms.Where(r => r.RoomType == RoomType.EasyCombat);
foreach (var easy in easyCombatRooms)
{
    Console.WriteLine($"  EasyCombat at node {easy.NodeId}: Difficulty {easy.Difficulty:F2} (<= 2.5: {easy.Difficulty <= 2.5})");
}
```

**Example Output:**
```
Room Difficulties:
  Node  0: Spawn        Difficulty: 1.00
  Node  3: EasyCombat   Difficulty: 1.75
  Node  5: EasyCombat   Difficulty: 2.25
  Node  7: Shop         Difficulty: 2.50
  Node  9: Combat       Difficulty: 3.00
  Node 12: Treasure     Difficulty: 4.00
  Node 15: Elite        Difficulty: 5.50
  Node 18: Elite        Difficulty: 6.75
  Node 19: Boss         Difficulty: 7.50

Difficulty Constraint Verification:
  Boss at node 19: Difficulty 7.50 (>= 7.0: True)
  EasyCombat at node 3: Difficulty 1.75 (<= 2.5: True)
  EasyCombat at node 5: Difficulty 2.25 (<= 2.5: True)
```

**Example - Exponential Scaling:**

For more dramatic difficulty increases:

```csharp
DifficultyConfig = new DifficultyConfig
{
    BaseDifficulty = 1.0,
    ScalingFactor = 1.8,
    Function = DifficultyScalingFunction.Exponential,
    MaxDifficulty = 15.0
}
```

**Example - Custom Function:**

For custom difficulty curves:

```csharp
DifficultyConfig = new DifficultyConfig
{
    BaseDifficulty = 1.0,
    Function = DifficultyScalingFunction.Custom,
    CustomFunction = distance =>
    {
        // Slow start (distance 0-2), rapid increase (distance 3-5), plateau (distance 6+)
        if (distance <= 2)
            return 1.0 + (distance * 0.5);
        else if (distance <= 5)
            return 2.0 + ((distance - 2) * 2.0);
        else
            return 8.0;  // Plateau at difficulty 8.0
    },
    MaxDifficulty = 10.0
}
```

This example demonstrates:
- **Progressive difficulty**: Rooms become more challenging as distance increases
- **Difficulty-based constraints**: Room types placed based on difficulty ranges
- **Linear scaling**: Steady difficulty increase (can use exponential or custom)
- **Difficulty metadata**: Access difficulty values in generated layouts

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

## Multi-Floor Dungeon

Generate a multi-floor dungeon with stairs and teleporters connecting floors:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// Create templates (shared across floors)
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
        .Build()
};

// Floor 0: Entry floor
var floor0Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new NotOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 0 }),  // No boss on floor 0
        new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
        new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
    },
    BranchingFactor = 0.3f,
    HallwayMode = HallwayMode.AsNeeded
};

// Floor 1: Middle floor
var floor1Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,  // More rooms
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 3)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new NotOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 1 }),  // No boss on floor 1
        new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
        new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 3)
    },
    BranchingFactor = 0.35f,  // Slightly more complex
    HallwayMode = HallwayMode.AsNeeded
};

// Floor 2: Final floor with boss
var floor2Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,  // Most rooms
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 2)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 }),  // Boss only on floor 2
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 8),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
        new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
    },
    BranchingFactor = 0.4f,  // Most complex
    HallwayMode = HallwayMode.AsNeeded
};

// Define connections between floors
var connections = new[]
{
    // Stairs from floor 0 to floor 1 (down)
    new FloorConnection
    {
        FromFloorIndex = 0,
        FromRoomNodeId = 9,  // Last room on floor 0
        ToFloorIndex = 1,
        ToRoomNodeId = 0,   // First room on floor 1
        Type = ConnectionType.StairsDown
    },
    // Stairs from floor 1 to floor 2 (down)
    new FloorConnection
    {
        FromFloorIndex = 1,
        FromRoomNodeId = 11,  // Last room on floor 1
        ToFloorIndex = 2,
        ToRoomNodeId = 0,     // First room on floor 2
        Type = ConnectionType.StairsDown
    },
    // Teleporter from floor 0 to floor 2 (skip floor 1)
    new FloorConnection
    {
        FromFloorIndex = 0,
        FromRoomNodeId = 5,
        ToFloorIndex = 2,
        ToRoomNodeId = 7,
        Type = ConnectionType.Teleporter
    }
};

// Create multi-floor configuration
var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config, floor2Config },
    Connections = connections
};

// Generate multi-floor dungeon
var generator = new MultiFloorGenerator<RoomType>();
var multiFloorLayout = generator.Generate(multiFloorConfig);

// Access results
Console.WriteLine($"Generated {multiFloorLayout.TotalFloorCount} floors");
Console.WriteLine($"Seed: {multiFloorLayout.Seed}");

foreach (var floor in multiFloorLayout.Floors)
{
    Console.WriteLine($"Floor has {floor.Rooms.Count} rooms");
    Console.WriteLine($"  Spawn: {floor.SpawnRoomId}, Boss: {floor.BossRoomId}");
}

foreach (var connection in multiFloorLayout.Connections)
{
    Console.WriteLine($"Connection: Floor {connection.FromFloorIndex} Room {connection.FromRoomNodeId} -> " +
                     $"Floor {connection.ToFloorIndex} Room {connection.ToRoomNodeId} ({connection.Type})");
}
```

### Progressive Difficulty Multi-Floor

Example with increasing difficulty on each floor:

```csharp
var templates = CreateTemplates();

var floorConfigs = new List<FloorConfig<RoomType>>();
var connections = new List<FloorConnection>();

for (int floorIndex = 0; floorIndex < 5; floorIndex++)
{
    var floorConfig = new FloorConfig<RoomType>
    {
        Seed = 12345,
        RoomCount = 8 + (floorIndex * 2),  // 8, 10, 12, 14, 16 rooms
        SpawnRoomType = RoomType.Spawn,
        BossRoomType = RoomType.Boss,
        DefaultRoomType = RoomType.Combat,
        Templates = templates,
        Constraints = new List<IConstraint<RoomType>>
        {
            // Boss only on final floor
            new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 4 })
        },
        BranchingFactor = 0.2f + (floorIndex * 0.05f),  // Increasing complexity
        HallwayMode = HallwayMode.AsNeeded
    };
    
    floorConfigs.Add(floorConfig);
    
    // Connect to next floor (except last floor)
    if (floorIndex < 4)
    {
        connections.Add(new FloorConnection
        {
            FromFloorIndex = floorIndex,
            FromRoomNodeId = floorConfig.RoomCount - 1,  // Last room
            ToFloorIndex = floorIndex + 1,
            ToRoomNodeId = 0,  // First room on next floor
            Type = ConnectionType.StairsDown
        });
    }
}

var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = floorConfigs,
    Connections = connections
};

var generator = new MultiFloorGenerator<RoomType>();
var layout = generator.Generate(multiFloorConfig);
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

## Zones Example

Example using biome/thematic zones to create distinct dungeon areas:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// Create zone-specific templates
var castleTemplate = RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
    .WithId("castle-ornate")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

var dungeonTemplate = RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
    .WithId("dungeon-rough")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

// Global templates (fallback)
var globalTemplates = new List<RoomTemplate<RoomType>>
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
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South | Edge.North)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("combat-default")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build()
};

// Define zones
var castleZone = new Zone<RoomType>
{
    Id = "castle",
    Name = "Castle",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    },
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1)  // One shop in castle zone
    },
    Templates = new[] { castleTemplate }  // Prefer ornate castle templates
};

var dungeonZone = new Zone<RoomType>
{
    Id = "dungeon",
    Name = "Dungeon",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 4,
        MaxDistance = 7
    },
    Templates = new[] { dungeonTemplate },  // Prefer rough dungeon templates
    Constraints = new List<IConstraint<RoomType>>
    {
        // Boss only in dungeon zone
        new OnlyInZoneConstraint<RoomType>(RoomType.Boss, "dungeon")
    }
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = globalTemplates,
    Zones = new[] { castleZone, dungeonZone },
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
    }
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Access zone assignments
if (layout.ZoneAssignments != null)
{
    Console.WriteLine("Zone Assignments:");
    foreach (var room in layout.Rooms)
    {
        if (layout.ZoneAssignments.TryGetValue(room.NodeId, out var zoneId))
        {
            Console.WriteLine($"Room {room.NodeId} ({room.RoomType}) is in zone: {zoneId}");
        }
    }
    
    // Get rooms by zone
    var castleRooms = layout.Rooms.Where(r => 
        layout.ZoneAssignments.TryGetValue(r.NodeId, out var z) && z == "castle").ToList();
    var dungeonRooms = layout.Rooms.Where(r => 
        layout.ZoneAssignments.TryGetValue(r.NodeId, out var z) && z == "dungeon").ToList();
    
    Console.WriteLine($"Castle zone: {castleRooms.Count} rooms");
    Console.WriteLine($"Dungeon zone: {dungeonRooms.Count} rooms");
    
    // Transition rooms (connect different zones)
    Console.WriteLine($"Transition rooms: {layout.TransitionRooms.Count}");
    foreach (var transition in layout.TransitionRooms)
    {
        Console.WriteLine($"  Transition room: {transition.NodeId} ({transition.RoomType})");
    }
}

// Verify zone-specific constraints
var shopRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Shop).ToList();
foreach (var shop in shopRooms)
{
    var zoneId = layout.ZoneAssignments?[shop.NodeId];
    Console.WriteLine($"Shop at node {shop.NodeId} is in zone: {zoneId}");
    // Shop should be in castle zone (due to RoomRequirements)
}

var bossRoom = layout.Rooms.First(r => r.RoomType == RoomType.Boss);
var bossZoneId = layout.ZoneAssignments?[bossRoom.NodeId];
Console.WriteLine($"Boss at node {bossRoom.NodeId} is in zone: {bossZoneId}");
// Boss should be in dungeon zone (due to OnlyInZoneConstraint)
```

This example demonstrates:
- **Distance-based zones**: Castle zone (distance 0-3) and Dungeon zone (distance 4-7)
- **Zone-specific templates**: Castle uses ornate templates, dungeon uses rough templates
- **Zone-specific room requirements**: Shop required in castle zone
- **Zone-aware constraints**: Boss only in dungeon zone
- **Transition rooms**: Automatically identified rooms connecting different zones

## Critical Path-Based Zones Example

Example using critical path-based zones:

```csharp
// Define zones based on critical path position
var earlyZone = new Zone<RoomType>
{
    Id = "early",
    Name = "Early Zone",
    Boundary = new ZoneBoundary.CriticalPathBased
    {
        StartPercent = 0.0f,
        EndPercent = 0.4f  // First 40% of critical path
    },
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1)  // Shop in early zone
    }
};

var lateZone = new Zone<RoomType>
{
    Id = "late",
    Name = "Late Zone",
    Boundary = new ZoneBoundary.CriticalPathBased
    {
        StartPercent = 0.6f,
        EndPercent = 1.0f  // Last 40% of critical path
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyInZoneConstraint<RoomType>(RoomType.Boss, "late")
    }
};

var config = new FloorConfig<RoomType>
{
    // ... other config ...
    Zones = new[] { earlyZone, lateZone }
};
```

This creates zones based on progression along the critical path, ensuring shops appear early and boss appears late.

## Secret Passages Example

Example using secret passages to create hidden shortcuts and alternative routes:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
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
        .Build()
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
        (RoomType.Treasure, 3)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 6),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
        new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
    },
    Templates = templates,
    BranchingFactor = 0.3f,
    HallwayMode = HallwayMode.AsNeeded,
    // Configure secret passages
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 3,  // Generate 3 secret passages
        MaxSpatialDistance = 5,  // Only connect rooms within 5 cells
        AllowedRoomTypes = new HashSet<RoomType> { RoomType.Treasure },  // Only connect treasure rooms
        AllowGraphConnectedRooms = false,  // Only connect rooms not already connected
        AllowCriticalPathConnections = false  // Don't connect critical path rooms
    }
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Access secret passages
Console.WriteLine($"Generated {layout.SecretPassages.Count} secret passages");

foreach (var passage in layout.SecretPassages)
{
    var roomA = layout.GetRoom(passage.RoomAId);
    var roomB = layout.GetRoom(passage.RoomBId);
    
    Console.WriteLine($"Secret passage: Room {passage.RoomAId} ({roomA.RoomType}) <-> Room {passage.RoomBId} ({roomB.RoomType})");
    Console.WriteLine($"  Door A at ({passage.DoorA.Position.X}, {passage.DoorA.Position.Y})");
    Console.WriteLine($"  Door B at ({passage.DoorB.Position.X}, {passage.DoorB.Position.Y})");
    
    if (passage.RequiresHallway)
    {
        Console.WriteLine($"  Has hallway with {passage.Hallway.Segments.Count} segments");
    }
}

// Find secret passages for a specific room
var treasureRoom = layout.Rooms.First(r => r.RoomType == RoomType.Treasure);
var secretPassages = layout.GetSecretPassagesForRoom(treasureRoom.NodeId);

Console.WriteLine($"Room {treasureRoom.NodeId} has {secretPassages.Count()} secret passages");
```

### Secret Passages with Different Constraints

Example with more flexible secret passage configuration:

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 5,  // More secret passages
        MaxSpatialDistance = 7,  // Allow longer connections
        ForbiddenRoomTypes = new HashSet<RoomType> { RoomType.Boss },  // Don't connect boss rooms
        AllowGraphConnectedRooms = true,  // Allow connections between already-connected rooms
        AllowCriticalPathConnections = true  // Allow connections on critical path
    }
};
```

### Using Secret Passages in Gameplay

Example of integrating secret passages into game logic:

```csharp
public class DungeonManager
{
    private FloorLayout<RoomType> _layout;
    private HashSet<int> _discoveredSecretPassages = new();
    
    public void GenerateDungeon(int seed)
    {
        var config = CreateConfig(seed);
        var generator = new FloorGenerator<RoomType>();
        _layout = generator.Generate(config);
    }
    
    // Check if player can discover a secret passage
    public bool TryDiscoverSecretPassage(int roomId, Cell wallPosition)
    {
        var passages = _layout.GetSecretPassagesForRoom(roomId);
        
        foreach (var passage in passages)
        {
            // Check if player is interacting with a secret door
            if (passage.DoorA.Position == wallPosition || passage.DoorB.Position == wallPosition)
            {
                if (!_discoveredSecretPassages.Contains(passage.RoomAId * 1000 + passage.RoomBId))
                {
                    _discoveredSecretPassages.Add(passage.RoomAId * 1000 + passage.RoomBId);
                    RevealSecretPassage(passage);
                    return true;
                }
            }
        }
        
        return false;
    }
    
    // Get all secret passages connected to a room (for rendering)
    public IEnumerable<SecretPassage> GetSecretPassagesForRoom(int roomId)
    {
        return _layout.GetSecretPassagesForRoom(roomId);
    }
    
    // Check if a secret passage is discovered
    public bool IsSecretPassageDiscovered(SecretPassage passage)
    {
        return _discoveredSecretPassages.Contains(passage.RoomAId * 1000 + passage.RoomBId);
    }
}
```

This example demonstrates:
- **Hidden shortcuts**: Secret passages connect treasure rooms, creating shortcuts
- **Exploration rewards**: Players can discover secret passages through gameplay
- **Alternative routes**: Secret passages provide paths that bypass main routes
- **Room type filtering**: Only treasure rooms have secret passages
- **Graph independence**: Secret passages don't affect critical path or distances

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

## Secret Passages with Multi-Floor Dungeons

Secret passages work with multi-floor dungeons - each floor can have its own secret passage configuration:

```csharp
// Floor 0 configuration with secret passages
var floor0Config = new FloorConfig<RoomType>
{
    // ... other config ...
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 2,
        MaxSpatialDistance = 5
    }
};

// Floor 1 configuration with more secret passages
var floor1Config = new FloorConfig<RoomType>
{
    // ... other config ...
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 4,  // More secret passages on deeper floor
        MaxSpatialDistance = 7
    }
};

var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config },
    Connections = connections
};

var generator = new MultiFloorGenerator<RoomType>();
var multiFloorLayout = generator.Generate(multiFloorConfig);

// Access secret passages per floor
foreach (var floor in multiFloorLayout.Floors)
{
    Console.WriteLine($"Floor has {floor.SecretPassages.Count} secret passages");
}
```

## Configuration Serialization Example

Example of saving and loading dungeon configurations:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Serialization;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// Create a configuration
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
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.3f
};

// Save configuration to file
config.SaveToFile("my-dungeon-config.json");

// Load configuration from file
var loadedConfig = FloorConfig<RoomType>.LoadFromFile<RoomType>("my-dungeon-config.json");

// Generate dungeon with loaded config
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(loadedConfig);
```

### Serialization with Extension Methods

```csharp
// Serialize to JSON string
string json = config.ToJson();
Console.WriteLine(json);

// Deserialize from JSON string
var config = FloorConfig<RoomType>.FromJson<RoomType>(json);
```

### Using ConfigurationSerializer Directly

```csharp
var serializer = new ConfigurationSerializer<RoomType>();

// Serialize with pretty printing
string json = serializer.SerializeToJson(config, prettyPrint: true);

// Serialize without pretty printing (compact)
string compactJson = serializer.SerializeToJson(config, prettyPrint: false);

// Deserialize
var deserializedConfig = serializer.DeserializeFromJson(json);

// Round-trip test
string json1 = serializer.SerializeToJson(config, prettyPrint: true);
var deserialized = serializer.DeserializeFromJson(json1);
string json2 = serializer.SerializeToJson(deserialized, prettyPrint: true);
Assert.Equal(json1, json2);  // Should be identical
```

### Multi-Floor Serialization

```csharp
var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config, floor2Config },
    Connections = connections
};

var serializer = new ConfigurationSerializer<RoomType>();

// Serialize multi-floor config
string json = serializer.SerializeToJson(multiFloorConfig, prettyPrint: true);

// Save to file manually
File.WriteAllText("multi-floor-config.json", json);

// Deserialize
var loadedMultiFloorConfig = serializer.DeserializeMultiFloorConfigFromJson(json);

// Generate multi-floor dungeon
var generator = new MultiFloorGenerator<RoomType>();
var layout = generator.Generate(loadedMultiFloorConfig);
```

### JSON Configuration Example

Example of a complete JSON configuration file:

```json
{
  "seed": 12345,
  "roomCount": 15,
  "spawnRoomType": "Spawn",
  "bossRoomType": "Boss",
  "defaultRoomType": "Combat",
  "branchingFactor": 0.3,
  "hallwayMode": "AsNeeded",
  "roomRequirements": [
    { "type": "Shop", "count": 1 },
    { "type": "Treasure", "count": 2 }
  ],
  "templates": [
    {
      "id": "spawn-room",
      "validRoomTypes": ["Spawn"],
      "weight": 1.0,
      "shape": {
        "type": "rectangle",
        "width": 3,
        "height": 3
      },
      "doorEdges": {
        "strategy": "allExteriorEdges"
      }
    },
    {
      "id": "boss-arena",
      "validRoomTypes": ["Boss"],
      "weight": 1.0,
      "shape": {
        "type": "rectangle",
        "width": 6,
        "height": 6
      },
      "doorEdges": {
        "strategy": "sides",
        "sides": ["South"]
      }
    },
    {
      "id": "combat-medium",
      "validRoomTypes": ["Combat"],
      "weight": 1.0,
      "shape": {
        "type": "rectangle",
        "width": 4,
        "height": 4
      },
      "doorEdges": {
        "strategy": "allExteriorEdges"
      }
    },
    {
      "id": "shop",
      "validRoomTypes": ["Shop"],
      "weight": 1.0,
      "shape": {
        "type": "rectangle",
        "width": 4,
        "height": 3
      },
      "doorEdges": {
        "strategy": "sides",
        "sides": ["North", "South"]
      }
    },
    {
      "id": "treasure",
      "validRoomTypes": ["Treasure"],
      "weight": 1.0,
      "shape": {
        "type": "rectangle",
        "width": 2,
        "height": 2
      },
      "doorEdges": {
        "strategy": "allExteriorEdges"
      }
    }
  ],
  "constraints": [
    {
      "type": "MinDistanceFromStart",
      "targetRoomType": "Boss",
      "minDistance": 6
    },
    {
      "type": "MustBeDeadEnd",
      "targetRoomType": "Boss"
    },
    {
      "type": "NotOnCriticalPath",
      "targetRoomType": "Shop"
    },
    {
      "type": "MaxPerFloor",
      "targetRoomType": "Shop",
      "maxCount": 1
    },
    {
      "type": "NotOnCriticalPath",
      "targetRoomType": "Treasure"
    },
    {
      "type": "MustBeDeadEnd",
      "targetRoomType": "Treasure"
    }
  ]
}
```

### Error Handling

```csharp
try
{
    var config = FloorConfig<RoomType>.LoadFromFile<RoomType>("config.json");
}
catch (InvalidConfigurationException ex)
{
    Console.WriteLine($"Invalid configuration: {ex.Message}");
    // Handle error - maybe show user-friendly message
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Configuration file not found: {ex.Message}");
    // Handle missing file
}
catch (JsonException ex)
{
    Console.WriteLine($"Invalid JSON: {ex.Message}");
    // Handle JSON parsing errors
}
```

### Building Configuration Presets

Example of creating a preset library:

```csharp
public class DungeonPresetLibrary
{
    private readonly string _presetsDirectory;
    
    public DungeonPresetLibrary(string presetsDirectory)
    {
        _presetsDirectory = presetsDirectory;
        Directory.CreateDirectory(_presetsDirectory);
    }
    
    public void SavePreset(string name, FloorConfig<RoomType> config)
    {
        string filePath = Path.Combine(_presetsDirectory, $"{name}.json");
        config.SaveToFile(filePath);
    }
    
    public FloorConfig<RoomType> LoadPreset(string name)
    {
        string filePath = Path.Combine(_presetsDirectory, $"{name}.json");
        return FloorConfig<RoomType>.LoadFromFile<RoomType>(filePath);
    }
    
    public IEnumerable<string> ListPresets()
    {
        return Directory.GetFiles(_presetsDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension);
    }
}

// Usage
var library = new DungeonPresetLibrary("./presets");

// Save a preset
library.SavePreset("small-dungeon", smallConfig);
library.SavePreset("large-dungeon", largeConfig);

// List available presets
foreach (var presetName in library.ListPresets())
{
    Console.WriteLine($"Available preset: {presetName}");
}

// Load and use a preset
var config = library.LoadPreset("small-dungeon");
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

## Interior Features Example

Example using interior features to create rooms with obstacles and decorative elements:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Templates;

public enum RoomType
{
    Spawn, Boss, Combat, Special
}

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
    
    // Combat room with pillars for cover
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-with-pillars")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
        .AddInteriorFeature(3, 1, InteriorFeature.Pillar)
        .AddInteriorFeature(1, 3, InteriorFeature.Pillar)
        .AddInteriorFeature(3, 3, InteriorFeature.Pillar)
        .Build(),
    
    // Temple room with altar and hazards
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("temple-room")
        .ForRoomTypes(RoomType.Special)
        .WithDoorsOnSides(Edge.South)
        .AddInteriorFeature(2, 2, InteriorFeature.Pillar)
        .AddInteriorFeature(3, 2, InteriorFeature.Pillar)
        .AddInteriorFeature(2, 3, InteriorFeature.Decorative)  // Altar
        .AddInteriorFeature(3, 3, InteriorFeature.Decorative)
        .AddInteriorFeature(4, 4, InteriorFeature.Hazard)  // Trap
        .Build(),
    
    // Room with interior walls
    RoomTemplateBuilder<RoomType>.Rectangle(6, 4)
        .WithId("divided-room")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .AddInteriorFeature(2, 1, InteriorFeature.Wall)
        .AddInteriorFeature(2, 2, InteriorFeature.Wall)
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
    RoomRequirements = new[]
    {
        (RoomType.Special, 1)
    },
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Access interior features
foreach (var room in layout.Rooms)
{
    Console.WriteLine($"Room {room.NodeId} ({room.RoomType}):");
    foreach (var (worldCell, feature) in room.GetInteriorFeatures())
    {
        Console.WriteLine($"  {feature} at ({worldCell.X}, {worldCell.Y})");
    }
}

// Get all hazards in the dungeon
var hazards = layout.InteriorFeatures
    .Where(f => f.Feature == InteriorFeature.Hazard)
    .ToList();
Console.WriteLine($"Total hazards: {hazards.Count}");
```

### Rendering Interior Features

```csharp
var layout = generator.Generate(config);
var (min, max) = layout.GetBounds();

// Render rooms
foreach (var room in layout.Rooms)
{
    foreach (var cell in room.GetWorldCells())
    {
        int screenX = cell.X - min.X;
        int screenY = cell.Y - min.Y;
        RenderTile(screenX, screenY, GetTileForRoomType(room.RoomType));
    }
}

// Render interior features
foreach (var (worldCell, feature) in layout.InteriorFeatures)
{
    int screenX = worldCell.X - min.X;
    int screenY = worldCell.Y - min.Y;
    
    switch (feature)
    {
        case InteriorFeature.Pillar:
            RenderTile(screenX, screenY, TileType.Pillar);
            break;
        case InteriorFeature.Wall:
            RenderTile(screenX, screenY, TileType.InteriorWall);
            break;
        case InteriorFeature.Hazard:
            RenderTile(screenX, screenY, TileType.Hazard);
            break;
        case InteriorFeature.Decorative:
            RenderTile(screenX, screenY, TileType.Decorative);
            break;
    }
}
```

This example demonstrates:
- **Pillars for cover**: Combat rooms with pillars that provide tactical cover
- **Decorative elements**: Temple rooms with altars and thematic features
- **Environmental hazards**: Traps and hazards that add danger
- **Interior walls**: Rooms divided by walls for tactical positioning

## Graph Generation Algorithms

The library supports multiple graph generation algorithms, each producing different connectivity patterns. Here are examples for each algorithm:

### Grid-Based Algorithm

Creates structured, maze-like dungeons with rooms arranged in a grid:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Templates;

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
    RoomCount = 16,  // 4x4 grid
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    GraphAlgorithm = GraphAlgorithm.GridBased,
    GridBasedConfig = new GridBasedGraphConfig
    {
        GridWidth = 4,
        GridHeight = 4,
        ConnectivityPattern = ConnectivityPattern.FourWay  // Structured connections
    },
    BranchingFactor = 0.2f  // Lower branching for more structured layout
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

**Use Cases:**
- Puzzle games requiring structured navigation
- Maze-like exploration
- Grid-based gameplay mechanics

### Cellular Automata Algorithm

Creates organic, cave-like structures with irregular connectivity:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 20,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    GraphAlgorithm = GraphAlgorithm.CellularAutomata,
    CellularAutomataConfig = new CellularAutomataGraphConfig
    {
        BirthThreshold = 4,      // Controls density
        SurvivalThreshold = 3,   // Controls connectivity
        Iterations = 5           // More iterations = smoother shapes
    },
    BranchingFactor = 0.3f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

**Use Cases:**
- Cave systems
- Organic, irregular dungeons
- Natural-feeling layouts

### Maze-Based Algorithm

Creates complex, winding path structures perfect for exploration:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 25,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    GraphAlgorithm = GraphAlgorithm.MazeBased,
    MazeBasedConfig = new MazeBasedGraphConfig
    {
        MazeType = MazeType.Perfect,    // No loops, single path
        Algorithm = MazeAlgorithm.Prims // Prim's algorithm
    },
    BranchingFactor = 0.1f  // Low branching for maze-like feel
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

**Imperfect Maze Example:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    GraphAlgorithm = GraphAlgorithm.MazeBased,
    MazeBasedConfig = new MazeBasedGraphConfig
    {
        MazeType = MazeType.Imperfect,  // Allows loops
        Algorithm = MazeAlgorithm.Kruskals
    },
    BranchingFactor = 0.4f  // Higher branching for more loops
};
```

**Use Cases:**
- Exploration-focused games
- Complex navigation challenges
- Winding, maze-like experiences

### Hub-and-Spoke Algorithm

Creates central hub rooms with branching spokes:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    GraphAlgorithm = GraphAlgorithm.HubAndSpoke,
    HubAndSpokeConfig = new HubAndSpokeGraphConfig
    {
        HubCount = 2,        // Two central hub rooms
        MaxSpokeLength = 3    // Spokes branch up to 3 rooms from hubs
    },
    BranchingFactor = 0.2f
};

var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

**Use Cases:**
- Hub-based progression
- Central gathering/rest areas
- Spoke-based exploration paths

### Comparing Algorithms

Here's how to generate the same dungeon with different algorithms:

```csharp
var baseConfig = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 16,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates
};

// Spanning tree (default)
var spanningTreeConfig = baseConfig with
{
    GraphAlgorithm = GraphAlgorithm.SpanningTree
    // No algorithm-specific config needed
};

// Grid-based
var gridConfig = baseConfig with
{
    GraphAlgorithm = GraphAlgorithm.GridBased,
    GridBasedConfig = new GridBasedGraphConfig
    {
        GridWidth = 4,
        GridHeight = 4,
        ConnectivityPattern = ConnectivityPattern.FourWay
    }
};

// Cellular automata
var caConfig = baseConfig with
{
    GraphAlgorithm = GraphAlgorithm.CellularAutomata,
    CellularAutomataConfig = new CellularAutomataGraphConfig
    {
        BirthThreshold = 4,
        SurvivalThreshold = 3,
        Iterations = 5
    }
};

// Generate with each algorithm
var generator = new FloorGenerator<RoomType>();
var spanningTreeLayout = generator.Generate(spanningTreeConfig);
var gridLayout = generator.Generate(gridConfig);
var caLayout = generator.Generate(caConfig);

// Each produces different connectivity patterns
// but same seed ensures deterministic generation per algorithm
```

### Algorithm Selection Tips

**Choose SpanningTree when:**
- You want the default, organic branching
- Backward compatibility is important
- You need a general-purpose algorithm

**Choose GridBased when:**
- You want structured, maze-like layouts
- Grid-based gameplay mechanics are important
- Clear navigation patterns are desired

**Choose CellularAutomata when:**
- You want organic, cave-like structures
- Irregular connectivity is desired
- Natural-feeling layouts are important

**Choose MazeBased when:**
- Exploration-focused gameplay is key
- Complex, winding paths are desired
- Maze-like experiences are the goal

**Choose HubAndSpoke when:**
- Hub-based progression is desired
- Central gathering areas are important
- Spoke-based exploration fits your design

## ASCII Map Visualization Example

Example using the ASCII visualization system to generate text-based dungeon maps:

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Templates;
using ShepherdProceduralDungeons.Visualization;

public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// Create templates
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
        .Build()
};

var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2)
    }
};

// Generate dungeon
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// Render ASCII map
var renderer = new AsciiMapRenderer<RoomType>();
string asciiMap = renderer.Render(layout);

Console.WriteLine(asciiMap);
```

**Example Output:**
```
SSS
SSS
SSS
  .
  +
CCCC
CCCC
CCCC
CCCC
  .
  +
TT
TT
  .
  +
$$$$
$$$$
$$$$
  .
  +
BBBBBB
BBBBBB
BBBBBB
BBBBBB
BBBBBB
BBBBBB

Legend:
  B = Boss
  C = Combat
  S = Spawn
  $ = Shop
  T = Treasure
  . = Hallway
  + = Door
```

### Custom Rendering Options

```csharp
var options = new AsciiRenderOptions
{
    Style = AsciiRenderStyle.Minimal,
    HighlightCriticalPath = true,
    ShowHallways = true,
    ShowDoors = true,
    ShowInteriorFeatures = true,
    ShowSecretPassages = true,
    IncludeLegend = true,
    CustomRoomTypeSymbols = new Dictionary<object, char>
    {
        { RoomType.Spawn, 'P' },      // Use 'P' for spawn
        { RoomType.Boss, 'X' },       // Use 'X' for boss
        { RoomType.Treasure, 'G' }    // Use 'G' for treasure (gold)
    }
};

string customMap = renderer.Render(layout, options);
Console.WriteLine(customMap);
```

### Viewport for Large Dungeons

```csharp
var (min, max) = layout.GetBounds();

// Render only a portion of the dungeon
var viewportOptions = new AsciiRenderOptions
{
    Viewport = (min, new Cell(min.X + 20, min.Y + 15))  // 20x15 viewport
};

string viewportMap = renderer.Render(layout, viewportOptions);
```

### Multi-Floor Visualization

```csharp
var multiFloorGenerator = new MultiFloorGenerator<RoomType>();
var multiFloorLayout = multiFloorGenerator.Generate(multiFloorConfig);

var renderer = new AsciiMapRenderer<RoomType>();
string multiFloorMap = renderer.Render(multiFloorLayout);

Console.WriteLine(multiFloorMap);
```

**Example Output:**
```
=== Floor 0 ===
[ASCII map of floor 0]

=== Floor 1 ===
[ASCII map of floor 1]

=== Floor 2 ===
[ASCII map of floor 2]
```

### Rendering to File

```csharp
using (var writer = new StreamWriter("dungeon-map.txt"))
{
    renderer.Render(layout, writer);
}
```

### Rendering with Interior Features

```csharp
// Create template with interior features
var combatTemplate = RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
    .WithId("combat-with-features")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .AddInteriorFeature(1, 1, InteriorFeature.Pillar)
    .AddInteriorFeature(3, 1, InteriorFeature.Pillar)
    .AddInteriorFeature(2, 3, InteriorFeature.Hazard)
    .Build();

// Generate and render
var layout = generator.Generate(config);
var options = new AsciiRenderOptions
{
    ShowInteriorFeatures = true
};

string mapWithFeatures = renderer.Render(layout, options);
```

**Example Output:**
```
CCCCC
C○C○C
CCCCC
CC!CC
CCCCC

Legend:
  C = Combat
  ○ = Pillar
  ! = Hazard
  . = Hallway
  + = Door
```

### Rendering Secret Passages

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 2,
        MaxSpatialDistance = 5
    }
};

var layout = generator.Generate(config);
var options = new AsciiRenderOptions
{
    ShowSecretPassages = true
};

string mapWithSecrets = renderer.Render(layout, options);
```

Secret passages are rendered with '~' symbols to distinguish them from regular connections.

### Performance Optimization for Large Dungeons

```csharp
var options = new AsciiRenderOptions
{
    MaxSize = (80, 24),  // Limit to terminal size
    Viewport = (new Cell(0, 0), new Cell(50, 30))  // Render specific region
};

string optimizedMap = renderer.Render(layout, options);
```

## Next Steps

- **[Getting Started](Getting-Started)** - Learn the basics
- **[Room Templates](Room-Templates)** - Create custom shapes
- **[Constraints](Constraints)** - Control room placement
- **[Configuration](Configuration)** - Learn about configuration serialization
- **[Working with Output](Working-with-Output)** - Use generated layouts, including secret passages and visualization


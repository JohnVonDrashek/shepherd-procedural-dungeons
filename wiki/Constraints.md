# Constraints

Constraints control **where** specific room types can be placed in the dungeon graph. They're essential for creating meaningful dungeon layouts.

## Overview

Constraints are evaluated during room type assignment. Each constraint checks if a node (room position in the graph) is valid for a specific room type.

**Key concepts:**
- Constraints apply to **room types**, not individual rooms
- Multiple constraints can apply to the same room type
- All constraints for a room type must be satisfied
- Constraints are evaluated in priority order (see below)

## Built-in Constraints

### MinDistanceFromStartConstraint

Ensures a room is at least N steps from the spawn room.

```csharp
new MinDistanceFromStartConstraint<RoomType>(
    RoomType.Boss, 
    minDistance: 5
)
```

**Use case:** Boss rooms should be far from spawn.

**Example:**
```csharp
// Boss must be at least 5 steps away
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5)
```

### MaxDistanceFromStartConstraint

Ensures a room is at most N steps from the spawn room.

```csharp
new MaxDistanceFromStartConstraint<RoomType>(
    RoomType.Shop, 
    maxDistance: 3
)
```

**Use case:** Shops should be accessible early.

**Example:**
```csharp
// Shop must be within 3 steps of spawn
new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 3)
```

### MustBeDeadEndConstraint

Room must have exactly one connection (a dead end).

```csharp
new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
```

**Use case:** Treasure rooms, secret rooms, or boss rooms that should be optional.

**Example:**
```csharp
// Treasure rooms must be dead ends (optional content)
new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
```

### MinConnectionCountConstraint

Room must have at least N connections.

```csharp
new MinConnectionCountConstraint<RoomType>(
    RoomType.Hub, 
    minConnections: 3
)
```

**Use case:** Create hub rooms (important areas with multiple paths), branching points, or rooms that serve as central navigation points.

**Example:**
```csharp
// Hub rooms require at least 3 connections (important areas)
new MinConnectionCountConstraint<RoomType>(RoomType.Hub, 3)

// Boss room must be a major hub (4+ connections)
new MinConnectionCountConstraint<RoomType>(RoomType.Boss, 4)
```

**Behavior:**
- Validates that `node.ConnectionCount >= minConnections`
- Throws `ArgumentOutOfRangeException` if `minConnections < 0`
- Setting `minConnections = 0` allows all nodes (no minimum requirement)

### MaxConnectionCountConstraint

Room must have at most N connections.

```csharp
new MaxConnectionCountConstraint<RoomType>(
    RoomType.Linear, 
    maxConnections: 2
)
```

**Use case:** Ensure linear progression rooms (exactly 2 connections), prevent rooms from becoming hubs, or create simple branching points.

**Example:**
```csharp
// Linear rooms have at most 2 connections (simple progression)
new MaxConnectionCountConstraint<RoomType>(RoomType.Linear, 2)

// Treasure rooms should be simple (max 2 connections)
new MaxConnectionCountConstraint<RoomType>(RoomType.Treasure, 2)
```

**Behavior:**
- Validates that `node.ConnectionCount <= maxConnections`
- Throws `ArgumentOutOfRangeException` if `maxConnections < 0`
- Setting `maxConnections` to a very large number allows all nodes (no maximum limit)

**Combining Min and Max:**

You can combine both constraints to create exact connection count requirements:

```csharp
// Exactly 2 connections (linear rooms)
new MinConnectionCountConstraint<RoomType>(RoomType.Linear, 2),
new MaxConnectionCountConstraint<RoomType>(RoomType.Linear, 2)

// 2-4 connections (branching points)
new MinConnectionCountConstraint<RoomType>(RoomType.Branch, 2),
new MaxConnectionCountConstraint<RoomType>(RoomType.Branch, 4)
```

### NotOnCriticalPathConstraint

Room must NOT be on the critical path (spawn to boss).

```csharp
new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
```

**Use case:** Optional content that shouldn't block progression.

**Example:**
```csharp
// Treasure rooms are optional, not required
new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
```

### OnlyOnCriticalPathConstraint

Room MUST be on the critical path.

```csharp
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.Boss)
```

**Use case:** Rooms that must be encountered (though boss is automatically on critical path).

**Example:**
```csharp
// Mini-boss must be on the way to final boss
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.MiniBoss)
```

### MaxPerFloorConstraint

At most N rooms of this type per floor.

```csharp
new MaxPerFloorConstraint<RoomType>(
    RoomType.Shop, 
    maxCount: 1
)
```

**Use case:** Limit special rooms (shops, treasure, etc.).

**Example:**
```csharp
// Maximum 1 shop per floor
new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1)

// Maximum 2 treasure rooms
new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
```

### OnlyOnFloorConstraint

Room type must ONLY be placed on specific floors. This constraint is floor-aware and only works in multi-floor dungeons.

```csharp
new OnlyOnFloorConstraint<RoomType>(
    RoomType.Boss, 
    allowedFloors: new[] { 2 }
)
```

**Use case:** Restrict room types to specific floors (e.g., boss only on final floor).

**Example:**
```csharp
// Boss only on floor 2 (final floor)
new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 })

// Shop only on floors 0 and 1
new OnlyOnFloorConstraint<RoomType>(RoomType.Shop, new[] { 0, 1 })
```

**Important:** This constraint implements `IFloorAwareConstraint` and requires the floor index to be set during multi-floor generation. It's automatically handled by `MultiFloorGenerator`.

### NotOnFloorConstraint

Room type must NOT be placed on specific floors. This constraint is floor-aware and only works in multi-floor dungeons.

```csharp
new NotOnFloorConstraint<RoomType>(
    RoomType.Boss, 
    forbiddenFloors: new[] { 0, 1 }
)
```

**Use case:** Prevent room types from appearing on specific floors.

**Example:**
```csharp
// Boss not on floors 0 or 1 (only on deeper floors)
new NotOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 0, 1 })

// Tutorial rooms not on floors 2+
new NotOnFloorConstraint<RoomType>(RoomType.Tutorial, new[] { 2, 3, 4 })
```

**Important:** This constraint implements `IFloorAwareConstraint` and requires the floor index to be set during multi-floor generation. It's automatically handled by `MultiFloorGenerator`.

### MinFloorConstraint

Room type must be placed on floor N or higher (0-based). This constraint is floor-aware and only works in multi-floor dungeons.

```csharp
new MinFloorConstraint<RoomType>(
    RoomType.Boss, 
    minFloor: 2
)
```

**Use case:** Require room types to appear on floor N or higher (e.g., boss only on floor 2+).

**Example:**
```csharp
// Boss only on floor 2 or higher
new MinFloorConstraint<RoomType>(RoomType.Boss, 2)

// Elite enemies only on floor 1 or higher
new MinFloorConstraint<RoomType>(RoomType.Elite, 1)
```

**Important:** This constraint implements `IFloorAwareConstraint` and requires the floor index to be set during multi-floor generation. It's automatically handled by `MultiFloorGenerator`.

### MaxFloorConstraint

Room type must be placed on floor N or lower (0-based). This constraint is floor-aware and only works in multi-floor dungeons.

```csharp
new MaxFloorConstraint<RoomType>(
    RoomType.Tutorial, 
    maxFloor: 0
)
```

**Use case:** Require room types to appear on floor N or lower (e.g., tutorial rooms only on floor 0).

**Example:**
```csharp
// Tutorial rooms only on floor 0
new MaxFloorConstraint<RoomType>(RoomType.Tutorial, 0)

// Easy combat only on floors 0-1
new MaxFloorConstraint<RoomType>(RoomType.EasyCombat, 1)
```

**Important:** This constraint implements `IFloorAwareConstraint` and requires the floor index to be set during multi-floor generation. It's automatically handled by `MultiFloorGenerator`.

### MustBeAdjacentToConstraint

Room must be adjacent to at least one of the specified room types in the graph topology.

```csharp
new MustBeAdjacentToConstraint<RoomType>(
    RoomType.Shop, 
    RoomType.Combat
)

// Multiple adjacent types (OR logic)
new MustBeAdjacentToConstraint<RoomType>(
    RoomType.Treasure,
    RoomType.Boss,
    RoomType.MiniBoss
)
```

**Use case:** Create spatial relationships between room types, such as requiring shops to be near combat rooms, or treasure rooms to be adjacent to boss rooms.

**Important:** This constraint operates on the **graph structure** (which rooms connect to which), not the spatial layout. It ensures that when room types are assigned, the target room type is only placed on nodes that have at least one neighbor with the required adjacent room type.

**Example:**
```csharp
// Shop must be adjacent to at least one Combat room
new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat)

// Treasure room must be adjacent to either Boss OR MiniBoss
new MustBeAdjacentToConstraint<RoomType>(
    RoomType.Treasure, 
    RoomType.Boss, 
    RoomType.MiniBoss
)

// Rest room must be adjacent to Combat rooms
new MustBeAdjacentToConstraint<RoomType>(RoomType.Rest, RoomType.Combat)
```

**Behavior:**
- Checks all neighbors of the candidate node
- Returns `true` if at least one neighbor has been assigned one of the required adjacent room types
- Works correctly with partially assigned graphs (only checks already-assigned neighbors)
- Returns `false` if the node has no connections
- If target room type is in the required adjacent types list, nodes adjacent to already-placed target rooms will be valid

### MustNotBeAdjacentToConstraint

Room must NOT be adjacent to any of the specified room types in the graph topology.

```csharp
new MustNotBeAdjacentToConstraint<RoomType>(
    RoomType.Treasure, 
    RoomType.Spawn
)

// Multiple forbidden adjacent types
new MustNotBeAdjacentToConstraint<RoomType>(
    RoomType.Shop,
    RoomType.Shop,
    RoomType.Treasure
)

// Using IEnumerable
new MustNotBeAdjacentToConstraint<RoomType>(
    RoomType.Boss,
    forbiddenTypes
)
```

**Use case:** Prevent specific room types from being placed next to each other. Common scenarios include:
- Preventing two shops from being adjacent
- Ensuring treasure rooms don't appear next to spawn rooms
- Keeping boss rooms isolated from certain room types
- Creating separation between conflicting room types

**Important:** This constraint operates on the **graph structure** (which rooms connect to which), not the spatial layout. It ensures that when room types are assigned, the target room type is only placed on nodes that have no neighbors with any of the forbidden adjacent room types.

**Example:**
```csharp
// Treasure must NOT be adjacent to Spawn room
new MustNotBeAdjacentToConstraint<RoomType>(RoomType.Treasure, RoomType.Spawn)

// Shop must NOT be adjacent to Shop OR Treasure (prevent clustering)
new MustNotBeAdjacentToConstraint<RoomType>(
    RoomType.Shop, 
    RoomType.Shop, 
    RoomType.Treasure
)

// Boss must NOT be adjacent to Spawn or Rest rooms
new MustNotBeAdjacentToConstraint<RoomType>(
    RoomType.Boss,
    RoomType.Spawn,
    RoomType.Rest
)
```

**Behavior:**
- Checks all neighbors of the candidate node
- Returns `false` if any neighbor has been assigned one of the forbidden adjacent room types
- Returns `true` if no neighbors have forbidden types (or neighbors are unassigned)
- Works correctly with partially assigned graphs (only checks already-assigned neighbors)
- Returns `true` if the node has no connections (can't violate adjacency constraint)
- Unassigned neighbors don't cause violations

### MinDistanceFromRoomTypeConstraint

Room must be at least N steps from rooms of specified type(s) in the graph topology.

```csharp
// Single reference type
new MinDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Secret, 
    RoomType.Boss, 
    minDistance: 3
)

// Multiple reference types (at least N steps from ANY of these)
new MinDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Secret,
    minDistance: 3,
    RoomType.Boss,
    RoomType.Combat
)
```

**Use case:** Create separation between room types. Common scenarios include:
- Secret rooms should be hidden away from boss rooms
- Rest rooms should be separated from spawn rooms
- Special rooms should maintain minimum distance from each other
- Prevent clustering of specific room types

**Important:** This constraint operates on the **graph structure** (shortest path distance between nodes), not spatial distance. Distance is measured in graph steps (number of edges), not spatial coordinates.

**Example:**
```csharp
// Secret rooms must be at least 3 steps from Boss rooms
new MinDistanceFromRoomTypeConstraint<RoomType>(RoomType.Secret, RoomType.Boss, 3)

// Rest rooms must be at least 2 steps from Spawn OR Combat rooms
new MinDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Rest,
    2,
    RoomType.Spawn,
    RoomType.Combat
)

// Secret rooms must be at least 2 steps from other Secret rooms (prevent clustering)
new MinDistanceFromRoomTypeConstraint<RoomType>(RoomType.Secret, RoomType.Secret, 2)
```

**Behavior:**
- Calculates shortest path distance using BFS (Breadth-First Search)
- Returns `true` if shortest distance to nearest reference room type is >= minDistance
- Returns `true` if no reference rooms exist yet (permissive, allows assignment order flexibility)
- Returns `true` if no path exists (disconnected graph) - infinite distance satisfies minimum requirement
- Works correctly with partially assigned graphs (only checks already-assigned reference rooms)
- If target and reference are the same type, ensures minimum separation between rooms of that type

### MaxDistanceFromRoomTypeConstraint

Room must be at most N steps from rooms of specified type(s) in the graph topology.

```csharp
// Single reference type
new MaxDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Rest, 
    RoomType.Combat, 
    maxDistance: 2
)

// Multiple reference types (within N steps of ANY of these)
new MaxDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Shop,
    maxDistance: 2,
    RoomType.Combat,
    RoomType.Boss
)
```

**Use case:** Ensure accessibility and proximity between room types. Common scenarios include:
- Rest/healing rooms should be accessible after combat encounters
- Shop rooms should be conveniently located near combat areas
- Special rooms should be within reach of key areas
- Create convenient placement relationships

**Important:** This constraint operates on the **graph structure** (shortest path distance between nodes), not spatial distance. Distance is measured in graph steps (number of edges), not spatial coordinates.

**Example:**
```csharp
// Rest rooms must be within 2 steps of Combat rooms
new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Rest, RoomType.Combat, 2)

// Shop rooms must be within 2 steps of Combat OR Boss rooms
new MaxDistanceFromRoomTypeConstraint<RoomType>(
    RoomType.Shop,
    2,
    RoomType.Combat,
    RoomType.Boss
)

// Shop rooms must be within 1 step of other Shop rooms (allow clustering)
new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Shop, RoomType.Shop, 1)
```

**Behavior:**
- Calculates shortest path distance using BFS (Breadth-First Search)
- Returns `true` if shortest distance to nearest reference room type is <= maxDistance
- Returns `true` if no assignments exist yet (permissive, allows assignment order flexibility)
- Returns `false` if reference rooms exist but none are within maxDistance
- Returns `false` if no path exists (disconnected graph) - infinite distance violates maximum requirement
- Works correctly with partially assigned graphs (only checks already-assigned reference rooms)
- If target and reference are the same type, allows clustering within the specified distance

### MustComeBeforeConstraint

Ensures a room type must appear before another room type (or types) on the critical path (the shortest path from spawn to boss).

```csharp
// Single reference type
new MustComeBeforeConstraint<RoomType>(
    RoomType.MiniBoss, 
    RoomType.Boss
)

// Multiple reference types (target must come before at least one)
new MustComeBeforeConstraint<RoomType>(
    RoomType.Shop,
    RoomType.Boss,
    RoomType.MiniBoss
)
```

**Use case:** Enforce ordering constraints on the critical path. Common scenarios include:
- Mini-boss must appear before the final boss
- Shop must appear before boss or mini-boss
- Key items must be encountered before certain encounters
- Create progression gates and ordering requirements

**Important:** This constraint operates on the **critical path** (spawn-to-boss shortest path), not the full graph. It ensures that when room types are assigned, the target room type is only placed on nodes that come before the reference room type(s) on the critical path.

**Example:**
```csharp
// Mini-boss must come before Boss on critical path
new MustComeBeforeConstraint<RoomType>(RoomType.MiniBoss, RoomType.Boss)

// Shop must come before Boss OR MiniBoss (at least one)
new MustComeBeforeConstraint<RoomType>(
    RoomType.Shop, 
    RoomType.Boss, 
    RoomType.MiniBoss
)

// Key item room must come before final boss
new MustComeBeforeConstraint<RoomType>(RoomType.KeyItem, RoomType.Boss)
```

**Behavior:**
- Checks if the candidate node is on the critical path
- If candidate is not on critical path: Returns `true` (permissive, allows placement off critical path)
- If critical path is empty or not set: Returns `true` (permissive)
- If reference room type(s) haven't been assigned yet: Returns `true` (permissive, allows assignment order flexibility)
- If reference room types are assigned: Returns `true` only if candidate comes before at least one reference type on the critical path
- Works correctly with partially assigned graphs (only checks already-assigned reference rooms)
- The constraint is satisfied if the candidate comes before **any** of the reference types (OR logic for multiple references)

**Note:** This constraint is particularly useful for game design patterns where certain encounters must happen in a specific order. Since the critical path represents the main progression route, this ensures players encounter content in the intended sequence.

### OnlyInZoneConstraint

Requires a room type to only be placed in a specific zone. This constraint is zone-aware and requires zones to be configured.

```csharp
new OnlyInZoneConstraint<RoomType>(
    RoomType.Shop, 
    zoneId: "market"
)
```

**Use case:** Restrict room types to specific zones. Common scenarios include:
- Shops only in market zone
- Boss rooms only in final zone
- Special rooms restricted to specific thematic areas
- Create zone-specific room placement rules

**Important:** This constraint implements `IZoneAwareConstraint` and requires zones to be configured in `FloorConfig`. The zone assignments are automatically set during generation.

**Example:**
```csharp
// Shop only in market zone
var marketZone = new Zone<RoomType>
{
    Id = "market",
    Name = "Market",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    }
};

var constraint = new OnlyInZoneConstraint<RoomType>(RoomType.Shop, "market");

var config = new FloorConfig<RoomType>
{
    // ... other config ...
    Zones = new[] { marketZone },
    Constraints = new List<IConstraint<RoomType>> { constraint },
    RoomRequirements = new[]
    {
        (RoomType.Shop, 2)  // Shops will only be placed in market zone
    }
};
```

**Behavior:**
- Returns `true` if the node is assigned to the required zone
- Returns `false` if the node is assigned to a different zone
- Returns `true` if zones haven't been assigned yet (permissive during early assignment phases)
- Works correctly with zone assignment system - automatically receives zone assignments during generation

### CompositeConstraint

Composes multiple constraints using AND, OR, or NOT logic. Enables complex constraint patterns that would otherwise require `CustomConstraint`.

```csharp
// AND: All constraints must pass
CompositeConstraint<RoomType>.And(
    constraint1,
    constraint2,
    constraint3
)

// OR: At least one constraint must pass
CompositeConstraint<RoomType>.Or(
    constraint1,
    constraint2
)

// NOT: The wrapped constraint must fail
CompositeConstraint<RoomType>.Not(constraint)
```

**Use case:** Express complex logic like "Shop OR Treasure in dead ends" or "NOT (on critical path AND near spawn)".

**Example - OR Composition:**
```csharp
// Shop OR Treasure room in dead ends (either constraint passes)
var shopOrTreasure = CompositeConstraint<RoomType>.Or(
    new MustBeDeadEndConstraint<RoomType>(RoomType.Shop),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
);
```

**Example - AND Composition (explicit):**
```csharp
// Multiple constraints (existing behavior, now explicit)
var complexBoss = CompositeConstraint<RoomType>.And(
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Boss)
);
```

**Example - NOT Composition:**
```csharp
// Secret room NOT near spawn (exclude certain conditions)
var notNearSpawn = CompositeConstraint<RoomType>.Not(
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Secret, 2)
);
```

**Example - Nested Composition:**
```csharp
// Complex logic: far from start AND (dead end OR not on critical path)
var complex = CompositeConstraint<RoomType>.And(
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Special, 3),
    CompositeConstraint<RoomType>.Or(
        new MustBeDeadEndConstraint<RoomType>(RoomType.Special),
        new NotOnCriticalPathConstraint<RoomType>(RoomType.Special)
    )
);
```

**Behavior:**
- **AND**: All constraints must pass (short-circuits on first failure)
- **OR**: At least one constraint must pass (short-circuits on first success)
- **NOT**: The wrapped constraint must fail
- **Empty AND**: Always passes (no constraints = always valid)
- **Empty OR**: Always fails (no constraints = never valid)
- **OR with different target types**: Allowed (e.g., "Shop OR Treasure" - uses first constraint's target type)

**Important Notes:**
- For AND compositions, all constraints must target the same room type
- For OR compositions, constraints can target different room types (enables "Shop OR Treasure" scenarios)
- CompositeConstraint implements `IConstraint<TRoomType>`, so it works seamlessly with existing constraint system
- Can be nested arbitrarily (AND containing OR, OR containing AND, NOT containing compositions, etc.)

### CustomConstraint

Custom callback-based constraint for advanced logic.

```csharp
new CustomConstraint<RoomType>(
    RoomType.Special,
    (node, graph, assignments) =>
    {
        // Your custom logic here
        // Return true if node is valid for this room type
        return node.DistanceFromStart % 2 == 0;  // Only even distances
    }
)
```

**Use case:** Complex placement rules that built-in constraints can't express. Consider using `CompositeConstraint` first for AND/OR/NOT logic.

**Example:**
```csharp
// Room must be at least distance 3, but not on critical path
new CustomConstraint<RoomType>(
    RoomType.Secret,
    (node, graph, assignments) =>
    {
        return node.DistanceFromStart >= 3 
            && !node.IsOnCriticalPath
            && node.ConnectionCount == 1;
    }
)
```

**Note:** Many scenarios that previously required `CustomConstraint` can now use `CompositeConstraint` instead. For example, the above could be written as:
```csharp
CompositeConstraint<RoomType>.And(
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Secret, 3),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Secret)
)
```

## Combining Constraints

You can apply multiple constraints to the same room type. By default, **all** constraints must be satisfied (implicit AND logic). You can also use `CompositeConstraint` to explicitly control how constraints are combined with AND/OR/NOT logic.

### Implicit AND (Default Behavior)

When you list multiple constraints for the same room type, they are combined with implicit AND logic - all must pass:

```csharp
var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MinConnectionCountConstraint<RoomType>(RoomType.Boss, 3),  // Boss is a hub
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Hub room constraints (important branching points)
    new MinConnectionCountConstraint<RoomType>(RoomType.Hub, 3),
    new MaxConnectionCountConstraint<RoomType>(RoomType.Hub, 5),
    
    // Treasure constraints
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MaxConnectionCountConstraint<RoomType>(RoomType.Treasure, 2),  // Simple rooms
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Treasure, RoomType.Boss),
    
    // Shop constraints
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 3),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat),
    new MustNotBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Shop),  // Prevent shop clustering
    new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Shop, RoomType.Combat, 2),  // Near combat areas
    
    // Rest room constraints
    new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Rest, RoomType.Combat, 2),  // Accessible after fights
    new MaxPerFloorConstraint<RoomType>(RoomType.Rest, 2),
    
    // Secret room constraints
    new MinDistanceFromRoomTypeConstraint<RoomType>(RoomType.Secret, RoomType.Boss, 3),  // Hidden from boss
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
    
    // Mini-boss must come before Boss on critical path
    new MustComeBeforeConstraint<RoomType>(RoomType.MiniBoss, RoomType.Boss)
};
```

### Explicit Composition with CompositeConstraint

Use `CompositeConstraint` when you need OR or NOT logic, or want to explicitly express AND logic:

```csharp
var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints (explicit AND)
    CompositeConstraint<RoomType>.And(
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
    ),
    
    // Shop OR Treasure in dead ends (OR logic)
    CompositeConstraint<RoomType>.Or(
        new MustBeDeadEndConstraint<RoomType>(RoomType.Shop),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
    ),
    
    // Secret NOT near spawn (NOT logic)
    CompositeConstraint<RoomType>.Not(
        new MaxDistanceFromStartConstraint<RoomType>(RoomType.Secret, 2)
    ),
    
    // Complex nested: far from start AND (dead end OR not on critical path)
    CompositeConstraint<RoomType>.And(
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Special, 3),
        CompositeConstraint<RoomType>.Or(
            new MustBeDeadEndConstraint<RoomType>(RoomType.Special),
            new NotOnCriticalPathConstraint<RoomType>(RoomType.Special)
        )
    )
};
```

**When to use CompositeConstraint:**
- **OR logic**: "Shop OR Treasure" - either constraint can pass
- **NOT logic**: "NOT near spawn" - exclude certain conditions
- **Nested logic**: Complex combinations like "A AND (B OR C)"
- **Explicit AND**: When you want to make AND logic explicit (though implicit AND is usually sufficient)

**When NOT to use CompositeConstraint:**
- Simple AND logic: Just list multiple constraints (implicit AND is cleaner)
- Single constraint: No need to wrap in CompositeConstraint

## Constraint Evaluation Order

Constraints are evaluated in this order:

1. **Spawn** - Always assigned to node 0
2. **Boss** - Assigned to farthest valid node
3. **Required room types** (from `RoomRequirements`) - In order specified
4. **Default type** - Fills remaining nodes

Within each category, constraints are checked in the order they appear in your list.

## Common Patterns

### Boss Room Pattern

```csharp
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
new MinConnectionCountConstraint<RoomType>(RoomType.Boss, 3),  // Boss is a hub
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
```

Boss is far from spawn, is a hub (3+ connections), and is a dead end (final encounter).

**Alternative:** If you want boss to be a simple dead end without hub requirement:
```csharp
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)  // Exactly 1 connection
```

### Treasure Room Pattern

```csharp
new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
```

Treasure is optional, hidden in dead ends, limited quantity.

### Shop Pattern

```csharp
new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 3),
new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat),
new MustNotBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Shop)  // Prevent clustering
```

Shop is accessible early, optional, one per floor, adjacent to combat rooms, and not adjacent to other shops.

### Secret Room Pattern

```csharp
new MinDistanceFromStartConstraint<RoomType>(RoomType.Secret, 3),
new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
new MaxPerFloorConstraint<RoomType>(RoomType.Secret, 1)
```

Secret room is hidden, optional, far enough to be meaningful.

### Hub Room Pattern

```csharp
new MinConnectionCountConstraint<RoomType>(RoomType.Hub, 3),
new MaxConnectionCountConstraint<RoomType>(RoomType.Hub, 5),
new MinDistanceFromStartConstraint<RoomType>(RoomType.Hub, 2)
```

Hub rooms are important branching points (3-5 connections) that appear after initial exploration.

### Linear Room Pattern

```csharp
new MinConnectionCountConstraint<RoomType>(RoomType.Linear, 2),
new MaxConnectionCountConstraint<RoomType>(RoomType.Linear, 2)
```

Linear rooms have exactly 2 connections, creating simple progression paths.

### Rest Room Pattern (Distance-Based)

```csharp
new MaxDistanceFromRoomTypeConstraint<RoomType>(RoomType.Rest, RoomType.Combat, 2),
new MaxPerFloorConstraint<RoomType>(RoomType.Rest, 2)
```

Rest rooms are accessible within 2 steps of combat areas, providing safe havens after fights.

### Secret Room Pattern (Distance-Based)

```csharp
new MinDistanceFromRoomTypeConstraint<RoomType>(RoomType.Secret, RoomType.Boss, 3),
new MinDistanceFromStartConstraint<RoomType>(RoomType.Secret, 2),
new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
new MustBeDeadEndConstraint<RoomType>(RoomType.Secret)
```

Secret rooms are hidden away (at least 3 steps from boss, at least 2 from spawn), optional, and in dead ends.

### Mini-Boss Pattern (Ordering)

```csharp
new MustComeBeforeConstraint<RoomType>(RoomType.MiniBoss, RoomType.Boss),
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.MiniBoss),
new MinDistanceFromStartConstraint<RoomType>(RoomType.MiniBoss, 3)
```

Mini-boss must appear before the final boss on the critical path, ensuring proper progression order.

### Multi-Floor Boss Pattern

```csharp
// Boss only on final floor
new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 }),
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 8),
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
```

Boss appears only on the final floor (floor 2), far from spawn, and is a dead end.

### Multi-Floor Progressive Difficulty Pattern

```csharp
// Easy rooms only on early floors
new MaxFloorConstraint<RoomType>(RoomType.EasyCombat, 1),

// Hard rooms only on later floors
new MinFloorConstraint<RoomType>(RoomType.HardCombat, 2),

// Boss only on final floor
new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 4 })
```

Creates progressive difficulty where easy rooms appear early, hard rooms appear later, and boss is on the final floor.

## Troubleshooting Constraints

### ConstraintViolationException

If you get this exception, your constraints are too restrictive:

```
ConstraintViolationException: Could only place 1/2 rooms of type Treasure
```

**Solutions:**
1. **Reduce requirements**: Lower the count in `RoomRequirements`
2. **Relax constraints**: Remove or adjust restrictive constraints
3. **Increase RoomCount**: More rooms = more valid positions
4. **Increase BranchingFactor**: More connections = more dead ends

**Example fix:**
```csharp
// Before: Too restrictive
RoomRequirements = new[] { (RoomType.Treasure, 5) },
Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
}

// After: More flexible
RoomRequirements = new[] { (RoomType.Treasure, 2) },  // Reduced count
Constraints = new[]
{
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
    // Removed MustBeDeadEndConstraint for more flexibility
}
```

### No Valid Boss Location

If boss constraints can't be satisfied:

```
ConstraintViolationException: No valid location for Boss
```

**Solutions:**
1. **Reduce MinDistance**: Lower the minimum distance requirement
2. **Remove MustBeDeadEnd**: Boss doesn't have to be dead end
3. **Increase RoomCount**: More rooms = farther possible distance

### Conflicting Constraints

Some constraint combinations are impossible:

```csharp
// ❌ Impossible: Must be dead end AND on critical path
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.Boss)
```

Dead ends can't be on critical path (critical path requires connections).

## Advanced: Custom Constraints

For complex logic, use `CustomConstraint`:

```csharp
// Room must be at distance 4-6, not on critical path, and have 2-3 connections
new CustomConstraint<RoomType>(
    RoomType.Special,
    (node, graph, assignments) =>
    {
        int distance = node.DistanceFromStart;
        int connections = node.ConnectionCount;
        
        return distance >= 4 
            && distance <= 6
            && !node.IsOnCriticalPath
            && connections >= 2
            && connections <= 3;
    }
)
```

**Available node properties:**
- `node.Id` - Node ID
- `node.DistanceFromStart` - Steps from spawn
- `node.IsOnCriticalPath` - On spawn-to-boss path
- `node.ConnectionCount` - Number of connections
- `node.Connections` - List of connections

**Available graph context:**
- `graph.Nodes` - All nodes
- `graph.Connections` - All connections
- `graph.StartNodeId` - Spawn node ID
- `graph.BossNodeId` - Boss node ID (after assignment)
- `graph.CriticalPath` - List of node IDs on critical path

**Available assignments:**
- `assignments[nodeId]` - Room type for a node (if assigned)
- `assignments.Values` - All assigned room types
- `assignments.Keys` - All assigned node IDs

## Best Practices

1. **Start simple**: Use basic constraints first, add complexity later
2. **Test incrementally**: Add constraints one at a time
3. **Balance requirements**: Don't require too many special rooms
4. **Consider graph structure**: More branching = more dead ends
5. **Use CustomConstraint sparingly**: Built-in constraints are usually enough

## Floor-Aware Constraints

Floor-aware constraints (`IFloorAwareConstraint`) are special constraints that know which floor is being generated. They're used in multi-floor dungeons to control room placement based on floor number.

### Available Floor-Aware Constraints

- `OnlyOnFloorConstraint` - Room type must be on specific floors
- `NotOnFloorConstraint` - Room type cannot be on specific floors
- `MinFloorConstraint` - Room type must be on floor N or higher
- `MaxFloorConstraint` - Room type must be on floor N or lower

### How They Work

When using `MultiFloorGenerator`, floor-aware constraints are automatically configured with the current floor index:

```csharp
var floor0Config = new FloorConfig<RoomType>
{
    // ...
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 })
    }
};

var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Floors = new[] { floor0Config, floor1Config, floor2Config },
    // ...
};

// MultiFloorGenerator automatically sets floor index on floor-aware constraints
var generator = new MultiFloorGenerator<RoomType>();
var layout = generator.Generate(multiFloorConfig);
```

### Backward Compatibility

Floor-aware constraints work in single-floor dungeons too - they simply allow all placements when the floor index isn't set, ensuring backward compatibility.

## Zone-Aware Constraints

Zone-aware constraints (`IZoneAwareConstraint`) are special constraints that check zone assignments. They're used with zones to control room placement based on which zone a room belongs to.

### Available Zone-Aware Constraints

- `OnlyInZoneConstraint` - Room type must be in a specific zone

### How They Work

When zones are configured in `FloorConfig`, zone-aware constraints are automatically configured with zone assignments during generation:

```csharp
var marketZone = new Zone<RoomType>
{
    Id = "market",
    Name = "Market",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    }
};

var config = new FloorConfig<RoomType>
{
    // ...
    Zones = new[] { marketZone },
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyInZoneConstraint<RoomType>(RoomType.Shop, "market")
    }
};

// FloorGenerator automatically sets zone assignments on zone-aware constraints
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);
```

### Backward Compatibility

Zone-aware constraints work without zones too - they simply allow all placements when zones aren't configured, ensuring backward compatibility.

## Next Steps

- **[Configuration](Configuration)** - How to use constraints in config, including multi-floor configs
- **[Examples](Examples)** - See constraints in action, including multi-floor examples
- **[Troubleshooting](Troubleshooting)** - Fix constraint issues
- **[Advanced Topics](Advanced-Topics#multi-floor-dungeons)** - Learn more about multi-floor generation


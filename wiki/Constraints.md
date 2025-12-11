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

**Use case:** Complex placement rules that built-in constraints can't express.

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

## Combining Constraints

You can apply multiple constraints to the same room type. **All** constraints must be satisfied:

```csharp
var constraints = new List<IConstraint<RoomType>>
{
    // Boss constraints
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    
    // Treasure constraints
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Treasure, RoomType.Boss),
    
    // Shop constraints
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 3),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
    new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat)
};
```

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
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
```

Boss is far from spawn and is a dead end (final encounter).

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
new MustBeAdjacentToConstraint<RoomType>(RoomType.Shop, RoomType.Combat)
```

Shop is accessible early, optional, one per floor, adjacent to combat rooms.

### Secret Room Pattern

```csharp
new MinDistanceFromStartConstraint<RoomType>(RoomType.Secret, 3),
new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
new MaxPerFloorConstraint<RoomType>(RoomType.Secret, 1)
```

Secret room is hidden, optional, far enough to be meaningful.

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

## Next Steps

- **[Configuration](Configuration)** - How to use constraints in config
- **[Examples](Examples)** - See constraints in action
- **[Troubleshooting](Troubleshooting)** - Fix constraint issues


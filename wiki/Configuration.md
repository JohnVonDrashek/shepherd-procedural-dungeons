# Configuration

The `FloorConfig<TRoomType>` class contains all settings for dungeon generation. This guide covers every property in detail.

## Required Properties

### Seed

```csharp
Seed = 12345
```

**Type:** `int`

**Description:** Seed for deterministic generation. Same seed + same config = identical output.

**Tips:**
- Use different seeds for different dungeons
- Save seeds to regenerate specific dungeons
- Can be any integer value

### RoomCount

```csharp
RoomCount = 12
```

**Type:** `int`

**Description:** Total number of rooms to generate.

**Constraints:**
- Must be at least 2 (spawn + boss)
- Must be >= 1 + 1 + sum of `RoomRequirements` counts

**Tips:**
- More rooms = larger, more complex dungeons
- Consider performance for very large counts (50+)
- Balance with template sizes

### SpawnRoomType

```csharp
SpawnRoomType = RoomType.Spawn
```

**Type:** `TRoomType`

**Description:** Room type for the starting room.

**Notes:**
- Always placed at node 0
- Must have a template available
- Typically has doors on all sides

### BossRoomType

```csharp
BossRoomType = RoomType.Boss
```

**Type:** `TRoomType`

**Description:** Room type for the final boss room.

**Notes:**
- Placed at farthest valid node (satisfying constraints)
- Must have a template available
- Often has restricted door placement

### DefaultRoomType

```csharp
DefaultRoomType = RoomType.Combat
```

**Type:** `TRoomType`

**Description:** Room type for rooms without specific assignments.

**Notes:**
- Fills all remaining nodes after required types are placed
- Must have a template available
- Usually your most common room type

### Templates

```csharp
Templates = templates
```

**Type:** `IReadOnlyList<RoomTemplate<TRoomType>>`

**Description:** Available room templates.

**Requirements:**
- Must include templates for: `SpawnRoomType`, `BossRoomType`, `DefaultRoomType`
- Must include templates for all types in `RoomRequirements`
- Each template must have unique `Id`

See [Room Templates](Room-Templates) for details.

## Optional Properties

### RoomRequirements

```csharp
RoomRequirements = new[]
{
    (RoomType.Treasure, 2),
    (RoomType.Shop, 1)
}
```

**Type:** `IReadOnlyList<(TRoomType Type, int Count)>`

**Default:** Empty array

**Description:** How many rooms of each type to generate (beyond spawn/boss).

**Example:**
```csharp
RoomRequirements = new[]
{
    (RoomType.Treasure, 2),  // 2 treasure rooms
    (RoomType.Shop, 1),      // 1 shop
    (RoomType.Secret, 1)     // 1 secret room
}
```

**Constraints:**
- `RoomCount` must be >= 1 (spawn) + 1 (boss) + sum of counts
- Each type must have templates available

### Constraints

```csharp
Constraints = new List<IConstraint<RoomType>>
{
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
}
```

**Type:** `IReadOnlyList<IConstraint<TRoomType>>`

**Default:** Empty array

**Description:** Constraints for room type placement.

**Notes:**
- Constraints are evaluated in order
- All constraints for a room type must be satisfied
- Can cause `ConstraintViolationException` if too restrictive

See [Constraints](Constraints) for details.

### BranchingFactor

```csharp
BranchingFactor = 0.2f
```

**Type:** `float`

**Default:** `0.3f`

**Range:** `0.0` to `1.0`

**Description:** Controls graph connectivity.

- **0.0**: Tree structure (no loops, linear paths)
- **0.3**: Some branching, occasional loops (recommended)
- **1.0**: Highly connected, many loops

**Tips:**
- Lower values = more dead ends (good for treasure rooms)
- Higher values = more exploration paths
- 0.2-0.3 is a good default for most games

**Example effects:**
```
BranchingFactor = 0.0:
Spawn -> Room1 -> Room2 -> Room3 -> Boss
         └-> Room4 (dead end)

BranchingFactor = 0.5:
Spawn -> Room1 -> Room2 -> Room3 -> Boss
         ├-> Room4 (dead end)
         └-> Room5 -> Room3 (loop back)
```

### HallwayMode

```csharp
HallwayMode = HallwayMode.AsNeeded
```

**Type:** `HallwayMode` enum

**Default:** `HallwayMode.AsNeeded`

**Options:**
- `HallwayMode.None` - Rooms must be adjacent (throws if impossible)
- `HallwayMode.AsNeeded` - Generate hallways only when rooms can't touch
- `HallwayMode.Always` - Always generate hallways between all rooms

**Tips:**
- `None`: Fastest, but may fail with large/complex templates
- `AsNeeded`: Good balance (recommended)
- `Always`: Most flexible, but more hallways to render

See [Hallway Modes](Hallway-Modes) for details.

## Complete Example

```csharp
var config = new FloorConfig<RoomType>
{
    // Required
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    
    // Optional
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 3),
        (RoomType.Shop, 1),
        (RoomType.Secret, 1)
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 6),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
        new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
        new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 3),
        new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
        new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
        new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1),
        new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
        new MaxPerFloorConstraint<RoomType>(RoomType.Secret, 1)
    },
    BranchingFactor = 0.25f,
    HallwayMode = HallwayMode.AsNeeded
};
```

## Configuration Validation

The generator validates your config before generation. Common errors:

### RoomCount Too Small

```csharp
// ❌ Error: RoomCount must be at least 2
RoomCount = 1
```

### RoomCount Insufficient for Requirements

```csharp
// ❌ Error: RoomCount is too small for spawn + boss + all required rooms
RoomCount = 5,
RoomRequirements = new[] { (RoomType.Treasure, 10) }  // Need at least 12 total
```

### Missing Templates

```csharp
// ❌ Error: No template available for room type Boss
Templates = new[] { /* only Spawn and Combat templates */ }
```

### Invalid BranchingFactor

```csharp
// ❌ Error: BranchingFactor must be between 0.0 and 1.0
BranchingFactor = 1.5f
```

## Recommended Configurations

### Small Linear Dungeon

```csharp
new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 8,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.1f,  // Very linear
    HallwayMode = HallwayMode.AsNeeded
}
```

### Medium Branching Dungeon

```csharp
new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 2),
        (RoomType.Shop, 1)
    },
    Constraints = /* ... */,
    Templates = templates,
    BranchingFactor = 0.3f,  // Moderate branching
    HallwayMode = HallwayMode.AsNeeded
}
```

### Large Exploration Dungeon

```csharp
new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 25,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Treasure, 5),
        (RoomType.Shop, 2),
        (RoomType.Secret, 3)
    },
    Constraints = /* ... */,
    Templates = templates,
    BranchingFactor = 0.4f,  // More loops
    HallwayMode = HallwayMode.Always  // Maximum flexibility
}
```

## Tips

1. **Start with defaults**: Use default `BranchingFactor` and `HallwayMode` first
2. **Test incrementally**: Add requirements and constraints gradually
3. **Balance counts**: Don't require too many special rooms
4. **Consider performance**: Very large `RoomCount` (50+) may be slow
5. **Save working configs**: Keep seeds for good dungeons

## Next Steps

- **[Constraints](Constraints)** - Understand constraint system
- **[Hallway Modes](Hallway-Modes)** - Choose the right hallway mode
- **[Examples](Examples)** - See complete configurations


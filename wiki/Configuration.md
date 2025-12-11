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

### Zones

```csharp
Zones = new[]
{
    new Zone<RoomType>
    {
        Id = "castle",
        Name = "Castle",
        Boundary = new ZoneBoundary.DistanceBased
        {
            MinDistance = 0,
            MaxDistance = 3
        }
    }
}
```

**Type:** `IReadOnlyList<Zone<TRoomType>>?`

**Default:** `null` (zones are optional)

**Description:** Optional biome/thematic zones that partition the dungeon into distinct regions with different generation rules.

**Zone Properties:**
- `Id` (required) - Unique identifier for the zone
- `Name` (required) - Display name for the zone
- `Boundary` (required) - Defines which rooms belong to this zone
- `RoomRequirements` (optional) - Zone-specific room type requirements
- `Constraints` (optional) - Zone-specific constraints
- `Templates` (optional) - Zone-specific template pool (preferred over global templates)

**Zone Boundaries:**

1. **Distance-Based** - Rooms assigned based on distance from spawn:
   ```csharp
   Boundary = new ZoneBoundary.DistanceBased
   {
       MinDistance = 0,  // Inclusive
       MaxDistance = 3   // Inclusive
   }
   ```

2. **Critical Path-Based** - Rooms assigned based on position along critical path:
   ```csharp
   Boundary = new ZoneBoundary.CriticalPathBased
   {
       StartPercent = 0.0f,  // 0.0 to 1.0
       EndPercent = 0.5f     // 0.0 to 1.0
   }
   ```

**Zone Assignment:**
- Zones are assigned after graph generation but before room type assignment
- Rooms are assigned to zones based on their boundaries
- If zones overlap, the first zone in the list takes precedence (first match wins)
- Zone assignments are deterministic (same seed = same assignments)

**Zone-Specific Features:**
- **Room Requirements**: Zones can have their own room type requirements in addition to global requirements
- **Constraints**: Zones can have zone-specific constraints that apply only to rooms in that zone
- **Templates**: Zones can have their own template pools; rooms in a zone prefer zone-specific templates, falling back to global templates if none available

**Example:**
```csharp
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
    Templates = new[] { castleTemplate }  // Prefer castle templates
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
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyInZoneConstraint<RoomType>(RoomType.Boss, "dungeon")
    }
};

var config = new FloorConfig<RoomType>
{
    // ... other config ...
    Zones = new[] { castleZone, dungeonZone }
};
```

**Accessing Zone Assignments:**

After generation, zone assignments are available in `FloorLayout`:

```csharp
var layout = generator.Generate(config);

// Check which zone a room belongs to
if (layout.ZoneAssignments != null)
{
    foreach (var room in layout.Rooms)
    {
        if (layout.ZoneAssignments.TryGetValue(room.NodeId, out var zoneId))
        {
            Console.WriteLine($"Room {room.NodeId} is in zone {zoneId}");
        }
    }
    
    // Get transition rooms (rooms connecting different zones)
    foreach (var transition in layout.TransitionRooms)
    {
        Console.WriteLine($"Transition room: {transition.NodeId}");
    }
}
```

**Tips:**
- Use zones to create distinct areas with different themes (e.g., castle vs. dungeon)
- Zone-specific templates allow visual variety per zone
- Zone-aware constraints (like `OnlyInZoneConstraint`) enable fine-grained control
- Transition rooms are automatically identified for special handling (e.g., zone transition effects)
- Zones are optional - dungeons without zones work exactly as before

See [Advanced Topics](Advanced-Topics#zones) for more details on zones.

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

## MultiFloorConfig<TRoomType>

Configuration for generating multi-floor dungeons with vertical connections between floors.

### Properties

#### Seed

```csharp
Seed = 12345
```

**Type:** `int`

**Description:** Seed for deterministic generation. Same seed + same config = identical multi-floor dungeon.

#### Floors

```csharp
Floors = new[]
{
    floorConfig1,
    floorConfig2,
    floorConfig3
}
```

**Type:** `IReadOnlyList<FloorConfig<TRoomType>>`

**Description:** Configuration for each floor in the dungeon. Each floor is generated independently using its own `FloorConfig`.

**Requirements:**
- Must contain at least one floor
- Each floor config must be valid (see `FloorConfig` requirements above)

#### Connections

```csharp
Connections = new[]
{
    new FloorConnection
    {
        FromFloorIndex = 0,
        FromRoomNodeId = 4,
        ToFloorIndex = 1,
        ToRoomNodeId = 0,
        Type = ConnectionType.StairsDown
    }
}
```

**Type:** `IReadOnlyList<FloorConnection>`

**Description:** Connections between floors. Defines how rooms on different floors connect via stairs or teleporters.

**Requirements:**
- Floor indices must be valid (0 to Floors.Count - 1)
- Room node IDs must exist on their respective floors
- Connections must connect different floors (cannot connect a floor to itself)

### FloorConnection

Represents a connection between two floors.

#### Properties

- `FromFloorIndex` (int) - Index of the source floor (0-based)
- `FromRoomNodeId` (int) - Node ID of the room on the source floor
- `ToFloorIndex` (int) - Index of the destination floor (0-based)
- `ToRoomNodeId` (int) - Node ID of the room on the destination floor
- `Type` (ConnectionType) - Type of connection (StairsUp, StairsDown, or Teleporter)

### ConnectionType

Enumeration of connection types between floors:

- `StairsUp` - Stairs going up to a higher floor
- `StairsDown` - Stairs going down to a lower floor
- `Teleporter` - Teleporter pad/portal connecting floors

### Complete Multi-Floor Example

```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // ... templates ...
};

// Floor 0 configuration
var floor0Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.3f
};

// Floor 1 configuration
var floor1Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,  // More rooms on deeper floor
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.35f,  // Slightly more complex
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 1 })  // Boss only on floor 1
    }
};

// Floor 2 configuration
var floor2Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,  // Even more rooms
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    BranchingFactor = 0.4f,
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 })  // Boss only on floor 2
    }
};

// Define connections between floors
var connections = new[]
{
    // Stairs from floor 0 to floor 1
    new FloorConnection
    {
        FromFloorIndex = 0,
        FromRoomNodeId = 9,  // Last room on floor 0
        ToFloorIndex = 1,
        ToRoomNodeId = 0,   // First room on floor 1
        Type = ConnectionType.StairsDown
    },
    // Stairs from floor 1 to floor 2
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

// Multi-floor configuration
var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config, floor2Config },
    Connections = connections
};

// Generate multi-floor dungeon
var generator = new MultiFloorGenerator<RoomType>();
var multiFloorLayout = generator.Generate(multiFloorConfig);

// Access individual floors
foreach (var floor in multiFloorLayout.Floors)
{
    Console.WriteLine($"Floor has {floor.Rooms.Count} rooms");
}

// Access connections
foreach (var connection in multiFloorLayout.Connections)
{
    Console.WriteLine($"Floor {connection.FromFloorIndex} room {connection.FromRoomNodeId} -> " +
                     $"Floor {connection.ToFloorIndex} room {connection.ToRoomNodeId} ({connection.Type})");
}
```

### Multi-Floor Configuration Tips

1. **Use floor-aware constraints**: Use `OnlyOnFloorConstraint`, `NotOnFloorConstraint`, `MinFloorConstraint`, or `MaxFloorConstraint` to control room placement per floor
2. **Progressive difficulty**: Increase room count and complexity on deeper floors
3. **Connection planning**: Plan connections before generation to ensure valid room IDs
4. **Determinism**: Same seed produces identical multi-floor layout
5. **Backward compatibility**: Single-floor generation still works with `FloorGenerator`

### Validation

The generator validates:
- At least one floor is specified
- All floor indices in connections are valid
- All room node IDs in connections exist on their floors
- Connections connect different floors

## Next Steps

- **[Constraints](Constraints)** - Understand constraint system, including floor-aware constraints
- **[Hallway Modes](Hallway-Modes)** - Choose the right hallway mode
- **[Examples](Examples)** - See complete configurations, including multi-floor examples
- **[Advanced Topics](Advanced-Topics#multi-floor-dungeons)** - Learn more about multi-floor generation


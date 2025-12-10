# Best Practices

Tips and patterns for effective dungeon generation.

## Template Design

### Start Simple

Begin with basic rectangles, add complexity later:

```csharp
// ✅ Start here
RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
    .WithId("combat")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build()

// Add complexity later if needed
RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
    .WithId("combat-l")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build()
```

### Size Considerations

- **Small rooms (2×2 to 4×4)**: Easy to place, good for most cases
- **Medium rooms (5×5 to 6×6)**: Good for important rooms
- **Large rooms (7×7+)**: Harder to place, use sparingly

**Tip:** Balance room sizes with `RoomCount`. More large rooms = harder placement.

### Door Placement Strategy

**Flexible (recommended for most rooms):**
```csharp
.WithDoorsOnAllExteriorEdges()  // Maximum flexibility
```

**Restricted (for special rooms):**
```csharp
.WithDoorsOnSides(Edge.South)  // Single entrance (boss rooms)
.WithDoorsOnSides(Edge.North | Edge.South)  // Linear flow
```

### Multiple Templates Per Type

Add visual variety:

```csharp
var combatTemplates = new[]
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3).WithId("combat-small"),
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4).WithId("combat-medium"),
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5).WithId("combat-large"),
    RoomTemplateBuilder<RoomType>.LShape(4, 3, 2, 1, Corner.TopRight).WithId("combat-l")
};
```

## Constraint Design

### Start Without Constraints

Get basic generation working first:

```csharp
// Step 1: No constraints
var config = new FloorConfig<RoomType>
{
    // ... basic config
    Constraints = new List<IConstraint<RoomType>>()  // Empty
};

// Step 2: Add constraints one at a time
config.Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
};

// Step 3: Add more as needed
config.Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5)
};
```

### Avoid Over-Constraining

Too many constraints = impossible to satisfy:

```csharp
// ❌ Too restrictive
Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Treasure, 8),
    new MaxDistanceFromStartConstraint<RoomType>(RoomType.Treasure, 10)
}

// ✅ More reasonable
Constraints = new[]
{
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure)
}
```

### Use Constraint Patterns

Common patterns that work well:

**Boss Pattern:**
```csharp
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
```

**Treasure Pattern:**
```csharp
new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, 2)
```

**Shop Pattern:**
```csharp
new MaxDistanceFromStartConstraint<RoomType>(RoomType.Shop, 4),
new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
new MaxPerFloorConstraint<RoomType>(RoomType.Shop, 1)
```

## Configuration

### Use Sensible Defaults

Start with recommended values:

```csharp
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,  // Good starting point
    // ...
    BranchingFactor = 0.3f,  // Recommended default
    HallwayMode = HallwayMode.AsNeeded  // Recommended default
};
```

### Incremental Complexity

Build up complexity gradually:

```csharp
// Version 1: Basic
RoomCount = 10,
RoomRequirements = new[] { (RoomType.Treasure, 1) }

// Version 2: More rooms
RoomCount = 15,
RoomRequirements = new[] { (RoomType.Treasure, 2) }

// Version 3: More types
RoomCount = 15,
RoomRequirements = new[]
{
    (RoomType.Treasure, 2),
    (RoomType.Shop, 1)
}
```

### Seed Management

Save good seeds for regeneration:

```csharp
// Generate and test
var layout = generator.Generate(config);

if (IsGoodDungeon(layout))
{
    SaveSeed(config.Seed);  // Save for later
}
```

## Performance

### Room Count Guidelines

- **Small (5-10 rooms)**: Very fast, good for testing
- **Medium (10-20 rooms)**: Fast, good for most games
- **Large (20-30 rooms)**: Slower, but manageable
- **Very Large (30+ rooms)**: May be slow, test performance

### Template Size Impact

Larger templates = slower spatial solving:

```csharp
// ✅ Fast
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)

// ⚠️ Slower
RoomTemplateBuilder<RoomType>.Rectangle(10, 10)
```

### Constraint Count

More constraints = slower type assignment:

```csharp
// ✅ Fast
Constraints = new[] { new MustBeDeadEndConstraint<RoomType>(RoomType.Boss) }

// ⚠️ Slower
Constraints = /* 20+ constraints */
```

### Hallway Mode

- `HallwayMode.None`: Fastest
- `HallwayMode.AsNeeded`: Fast (recommended)
- `HallwayMode.Always`: Slower (more hallways to generate)

## Testing

### Test Determinism

```csharp
[Fact]
public void SameSeed_SameOutput()
{
    var config1 = CreateConfig(seed: 12345);
    var config2 = CreateConfig(seed: 12345);
    
    var layout1 = generator.Generate(config1);
    var layout2 = generator.Generate(config2);
    
    Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
    Assert.Equal(layout1.SpawnRoomId, layout2.SpawnRoomId);
}
```

### Test Edge Cases

```csharp
// Minimum rooms
RoomCount = 2  // Just spawn + boss

// Maximum rooms (test performance)
RoomCount = 50

// No special rooms
RoomRequirements = Array.Empty<(RoomType, int)>()

// Many special rooms
RoomRequirements = new[]
{
    (RoomType.Treasure, 10),
    (RoomType.Shop, 5)
}
```

### Validate Output

```csharp
var layout = generator.Generate(config);

// Check basic properties
Assert.NotNull(layout);
Assert.Equal(config.RoomCount, layout.Rooms.Count);
Assert.NotNull(layout.GetRoom(layout.SpawnRoomId));
Assert.NotNull(layout.GetRoom(layout.BossRoomId));

// Check critical path
Assert.NotEmpty(layout.CriticalPath);
Assert.Equal(layout.SpawnRoomId, layout.CriticalPath[0]);
Assert.Equal(layout.BossRoomId, layout.CriticalPath[^1]);

// Check required rooms
var treasureCount = layout.Rooms.Count(r => r.RoomType == RoomType.Treasure);
Assert.Equal(2, treasureCount);
```

## Code Organization

### Template Factory

Create templates in a dedicated method:

```csharp
private static List<RoomTemplate<RoomType>> CreateTemplates()
{
    return new List<RoomTemplate<RoomType>>
    {
        RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
            .WithId("spawn")
            .ForRoomTypes(RoomType.Spawn)
            .WithDoorsOnAllExteriorEdges()
            .Build(),
        // ... more templates
    };
}
```

### Constraint Factory

Create constraints in a dedicated method:

```csharp
private static List<IConstraint<RoomType>> CreateConstraints()
{
    return new List<IConstraint<RoomType>>
    {
        new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
        new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
        // ... more constraints
    };
}
```

### Config Builder

Use a builder pattern for complex configs:

```csharp
public class DungeonConfigBuilder
{
    private int _seed = 12345;
    private int _roomCount = 12;
    // ... more fields
    
    public DungeonConfigBuilder WithSeed(int seed)
    {
        _seed = seed;
        return this;
    }
    
    public FloorConfig<RoomType> Build()
    {
        return new FloorConfig<RoomType>
        {
            Seed = _seed,
            RoomCount = _roomCount,
            // ... build config
        };
    }
}
```

## Common Pitfalls

### Too Many Requirements

```csharp
// ❌ Too many special rooms
RoomCount = 10,
RoomRequirements = new[]
{
    (RoomType.Treasure, 5),
    (RoomType.Shop, 3),
    (RoomType.Secret, 2)
}  // Need 1 + 1 + 10 = 12 rooms, but only have 10!

// ✅ Reasonable
RoomCount = 15,
RoomRequirements = new[]
{
    (RoomType.Treasure, 2),
    (RoomType.Shop, 1)
}
```

### Conflicting Constraints

```csharp
// ❌ Impossible combination
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.Boss)

// ✅ Compatible
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5),
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
```

### Missing Templates

Always ensure templates exist for all required types:

```csharp
// Validate before generation
var requiredTypes = new HashSet<RoomType>
{
    config.SpawnRoomType,
    config.BossRoomType,
    config.DefaultRoomType
};

foreach (var req in config.RoomRequirements)
{
    requiredTypes.Add(req.Type);
}

var availableTypes = config.Templates
    .SelectMany(t => t.ValidRoomTypes)
    .ToHashSet();

foreach (var required in requiredTypes)
{
    if (!availableTypes.Contains(required))
    {
        throw new InvalidOperationException($"Missing template for {required}");
    }
}
```

## Next Steps

- **[Examples](Examples)** - See best practices in action
- **[Troubleshooting](Troubleshooting)** - Fix common issues
- **[Advanced Topics](Advanced-Topics)** - Extend the library


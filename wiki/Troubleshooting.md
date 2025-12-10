# Troubleshooting

Common issues and their solutions.

## Exceptions

### InvalidConfigurationException

**Error:** Configuration is invalid before generation starts.

**Common causes:**

#### RoomCount Too Small

```
InvalidConfigurationException: RoomCount must be at least 2 (spawn + boss)
```

**Solution:**
```csharp
// ❌ Too small
RoomCount = 1

// ✅ Fix
RoomCount = 10  // At least 2
```

#### RoomCount Insufficient for Requirements

```
InvalidConfigurationException: RoomCount is too small for spawn + boss + all required rooms
```

**Solution:**
```csharp
// ❌ RoomCount = 5, but need spawn(1) + boss(1) + treasure(5) = 7
RoomCount = 5,
RoomRequirements = new[] { (RoomType.Treasure, 5) }

// ✅ Fix
RoomCount = 10  // 1 + 1 + 5 = 7, so 10 is enough
```

#### Missing Templates

```
InvalidConfigurationException: No template available for room type Boss
```

**Solution:**
```csharp
// ❌ Missing Boss template
var templates = new List<RoomTemplate<RoomType>>
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn")
        .ForRoomTypes(RoomType.Spawn)
        .Build()
    // Missing Boss template!
};

// ✅ Fix - add missing template
var templates = new List<RoomTemplate<RoomType>>
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn")
        .ForRoomTypes(RoomType.Spawn)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss")
        .ForRoomTypes(RoomType.Boss)  // Add this!
        .Build()
};
```

#### Invalid BranchingFactor

```
InvalidConfigurationException: BranchingFactor must be between 0.0 and 1.0
```

**Solution:**
```csharp
// ❌ Out of range
BranchingFactor = 1.5f

// ✅ Fix
BranchingFactor = 0.3f  // Between 0.0 and 1.0
```

### ConstraintViolationException

**Error:** Room type constraints cannot be satisfied.

**Common causes:**

#### Not Enough Valid Positions

```
ConstraintViolationException: Could only place 1/2 rooms of type Treasure
```

**Solutions:**

1. **Reduce requirements:**
```csharp
// ❌ Too many required
RoomRequirements = new[] { (RoomType.Treasure, 5) }

// ✅ Reduce count
RoomRequirements = new[] { (RoomType.Treasure, 2) }
```

2. **Relax constraints:**
```csharp
// ❌ Too restrictive
Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Treasure, 5)
}

// ✅ Remove some constraints
Constraints = new[]
{
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
    // Removed MustBeDeadEndConstraint and MinDistanceFromStartConstraint
}
```

3. **Increase RoomCount:**
```csharp
// ❌ Not enough rooms
RoomCount = 8

// ✅ More rooms = more valid positions
RoomCount = 15
```

4. **Increase BranchingFactor:**
```csharp
// ❌ Low branching = fewer dead ends
BranchingFactor = 0.0f

// ✅ More branching = more dead ends
BranchingFactor = 0.3f
```

#### No Valid Boss Location

```
ConstraintViolationException: No valid location for Boss
```

**Solutions:**

1. **Reduce MinDistance:**
```csharp
// ❌ Too far
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 20)

// ✅ Reduce distance
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5)
```

2. **Remove MustBeDeadEnd:**
```csharp
// ❌ Dead end + high distance might be impossible
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 10),
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)

// ✅ Remove dead end constraint
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 10)
```

3. **Increase RoomCount:**
```csharp
// ❌ Not enough rooms to reach required distance
RoomCount = 8

// ✅ More rooms = farther possible distance
RoomCount = 15
```

#### Conflicting Constraints

Some constraint combinations are impossible:

```csharp
// ❌ Impossible: Dead end can't be on critical path
new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.Boss)

// ✅ Remove one
new OnlyOnCriticalPathConstraint<RoomType>(RoomType.Boss)
// Boss is automatically on critical path anyway
```

### SpatialPlacementException

**Error:** Rooms cannot be placed in 2D space.

**Common causes:**

#### Rooms Can't Fit Adjacent

```
SpatialPlacementException: Cannot place room 5 adjacent to room 3 and hallways are disabled
```

**Solutions:**

1. **Enable hallways:**
```csharp
// ❌ No hallways allowed
HallwayMode = HallwayMode.None

// ✅ Allow hallways
HallwayMode = HallwayMode.AsNeeded
```

2. **Use smaller templates:**
```csharp
// ❌ Large templates are hard to place
RoomTemplateBuilder<RoomType>.Rectangle(10, 10)

// ✅ Smaller templates
RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
```

3. **Reduce RoomCount:**
```csharp
// ❌ Too many large rooms
RoomCount = 50

// ✅ Fewer rooms
RoomCount = 15
```

#### No Hallway Path Found

```
SpatialPlacementException: Cannot find hallway path from (5, 10) to (20, 15)
```

**Solutions:**

1. **Use smaller templates:**
```csharp
// Large templates block paths
RoomTemplateBuilder<RoomType>.Rectangle(8, 8)

// Smaller templates leave more space
RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
```

2. **Reduce RoomCount:**
```csharp
// Too many rooms = crowded
RoomCount = 30

// Fewer rooms = more space
RoomCount = 15
```

3. **Use HallwayMode.Always:**
```csharp
// AsNeeded might fail
HallwayMode = HallwayMode.AsNeeded

// Always mode is more flexible
HallwayMode = HallwayMode.Always
```

## Template Validation Errors

### Missing ID

```
InvalidConfigurationException: Template must have an ID
```

**Solution:**
```csharp
// ❌ Missing WithId
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .ForRoomTypes(RoomType.Combat)
    .Build()

// ✅ Add ID
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("combat-room")
    .ForRoomTypes(RoomType.Combat)
    .Build()
```

### No Valid Room Types

```
InvalidConfigurationException: Template must specify at least one valid room type
```

**Solution:**
```csharp
// ❌ Missing ForRoomTypes
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .Build()

// ✅ Add room types
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .Build()
```

### No Door Edges

```
InvalidConfigurationException: Template must have at least one door edge
```

**Solution:**
```csharp
// ❌ Missing door configuration
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .Build()

// ✅ Add door configuration
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()  // Add this!
    .Build()
```

### Interior Door Edge

```
InvalidConfigurationException: Template has door on interior edge
```

**Solution:**
```csharp
// ❌ Door on interior edge
var template = new RoomTemplateBuilder<RoomType>()
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .AddRectangle(0, 0, 4, 4)
    .WithDoorEdges(1, 1, Edge.North)  // Cell (1,1) has neighbor to north!

// ✅ Door on exterior edge only
var template = new RoomTemplateBuilder<RoomType>()
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .AddRectangle(0, 0, 4, 4)
    .WithDoorsOnAllExteriorEdges()  // Automatically handles exterior edges
    .Build()
```

## Performance Issues

### Slow Generation

**Symptoms:** Generation takes a long time (several seconds).

**Causes and solutions:**

1. **Too many rooms:**
```csharp
// ❌ Very slow
RoomCount = 100

// ✅ Reasonable
RoomCount = 20
```

2. **Too many constraints:**
```csharp
// ❌ Many constraints slow down assignment
Constraints = /* 20+ constraints */

// ✅ Reduce constraints
Constraints = /* Only essential constraints */
```

3. **Large templates:**
```csharp
// ❌ Large templates slow spatial solving
RoomTemplateBuilder<RoomType>.Rectangle(20, 20)

// ✅ Smaller templates
RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
```

4. **HallwayMode.Always:**
```csharp
// ❌ Always mode generates more hallways
HallwayMode = HallwayMode.Always

// ✅ AsNeeded is faster
HallwayMode = HallwayMode.AsNeeded
```

## Debugging Tips

### Enable Detailed Logging

Add logging to see what's happening:

```csharp
try
{
    var layout = generator.Generate(config);
    Console.WriteLine($"Success: {layout.Rooms.Count} rooms");
}
catch (ConstraintViolationException ex)
{
    Console.WriteLine($"Constraint violation: {ex.Message}");
    // Check your constraints and requirements
}
catch (SpatialPlacementException ex)
{
    Console.WriteLine($"Placement failed: {ex.Message}");
    // Try smaller templates or enable hallways
}
```

### Test Incrementally

Start simple and add complexity:

```csharp
// Step 1: Basic config
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 5,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = basicTemplates
};

// Step 2: Add requirements
config.RoomRequirements = new[] { (RoomType.Treasure, 1) };

// Step 3: Add constraints
config.Constraints = new[] { new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure) };

// Step 4: Increase complexity
config.RoomCount = 15;
config.RoomRequirements = new[] { (RoomType.Treasure, 3) };
```

### Verify Template Coverage

Ensure all required room types have templates:

```csharp
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
        Console.WriteLine($"Missing template for {required}");
    }
}
```

## Common Patterns

### Pattern: Too Many Dead Ends Required

**Problem:** Requiring many dead end rooms with low branching.

**Solution:**
```csharp
// Increase branching factor
BranchingFactor = 0.3f  // More dead ends available
```

### Pattern: Boss Too Far

**Problem:** Boss distance requirement too high for room count.

**Solution:**
```csharp
// Reduce distance or increase rooms
new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, 5)  // Lower
// OR
RoomCount = 20  // More rooms = farther possible
```

### Pattern: Large Templates Don't Fit

**Problem:** Large templates can't be placed adjacent.

**Solution:**
```csharp
// Enable hallways
HallwayMode = HallwayMode.AsNeeded
// OR use smaller templates
```

## Getting Help

If you're still stuck:

1. Check the [Examples](Examples) page
2. Review [Best Practices](Best-Practices)
3. Check the [Design Document](../DESIGN.md) for technical details
4. Open an issue on GitHub with:
   - Your configuration
   - The exception message
   - What you're trying to achieve

## Next Steps

- **[Examples](Examples)** - See working configurations
- **[Best Practices](Best-Practices)** - Avoid common pitfalls
- **[Configuration](Configuration)** - Understand all options


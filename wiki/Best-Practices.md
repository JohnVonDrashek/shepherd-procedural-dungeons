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

### Test Timeouts

The test infrastructure includes comprehensive timeout support to prevent infinite execution times and improve CI/CD reliability. All tests should specify appropriate timeout values based on their complexity.

#### Per-Test Timeout Configuration

Use the `Timeout` parameter on `[Fact]` or `[Theory]` attributes to set per-test timeouts:

```csharp
// Unit test with 5-second timeout
[Fact(Timeout = 5000)]
public void QuickUnitTest()
{
    // Fast test that should complete in < 5 seconds
    Assert.True(true);
}

// Integration test with 30-second timeout
[Fact(Timeout = 30000)]
public async Task IntegrationTest()
{
    // Full generation cycle that may take longer
    var layout = await generator.GenerateAsync(config);
    Assert.NotNull(layout);
}
```

#### Global Default Timeout

Tests without explicit timeouts use the global default (30 seconds) configured in `xunit.runner.json`. This provides a safety net for tests that don't specify timeouts:

```csharp
// Uses global default timeout (30 seconds)
[Fact]
public void TestWithoutExplicitTimeout()
{
    // Will timeout after 30 seconds if it hangs
    Assert.True(true);
}
```

#### Timeout Best Practices

**Choose appropriate timeout values based on test category:**

- **Unit tests**: 1-5 seconds (fast, isolated operations)
  ```csharp
  [Fact(Timeout = 5000)]  // 5 seconds
  public void UnitTest() { }
  ```

- **Integration tests**: 10-30 seconds (full generation cycles)
  ```csharp
  [Fact(Timeout = 30000)]  // 30 seconds
  public void IntegrationTest() { }
  ```

- **Performance/stress tests**: 60+ seconds (large dungeon generation)
  ```csharp
  [Fact(Timeout = 60000)]  // 60 seconds
  public void PerformanceTest() { }
  ```

#### Using Timeout Constants

The `TestHelpers` class provides constants for consistent timeout values:

```csharp
using ShepherdProceduralDungeons.Tests;

// Use constants instead of magic numbers
[Fact(Timeout = TestHelpers.Timeout.UnitTestMs)]  // 5000ms
public void UnitTest() { }

[Fact(Timeout = TestHelpers.Timeout.IntegrationTestMs)]  // 30000ms
public void IntegrationTest() { }

[Fact(Timeout = TestHelpers.Timeout.PerformanceTestMs)]  // 60000ms
public void PerformanceTest() { }
```

#### Integration Test Attribute

For integration tests, use the `[IntegrationTest]` attribute along with the timeout:

```csharp
using ShepherdProceduralDungeons.Tests;

[Fact(Timeout = IntegrationTestAttribute.DefaultTimeoutMs)]
[IntegrationTest]
[Trait("Category", "Integration")]
public void FullGenerationCycle()
{
    // Integration test that uses standard timeout
    var layout = generator.Generate(config);
    Assert.NotNull(layout);
}
```

#### When to Increase vs. Fix Slow Tests

**Increase timeout when:**
- Test legitimately needs more time (e.g., generating large dungeons)
- Test is performing complex operations that take time
- Test is marked as a performance test

**Fix the test when:**
- Test is hanging due to infinite loops or deadlocks
- Test is inefficient and can be optimized
- Test is waiting unnecessarily (e.g., fixed delays instead of proper async/await)

```csharp
// ❌ Bad: Unnecessary delay
[Fact(Timeout = 60000)]
public async Task SlowTest()
{
    await Task.Delay(10000);  // Unnecessary 10-second delay
    Assert.True(true);
}

// ✅ Good: Optimize the test
[Fact(Timeout = 5000)]
public void FastTest()
{
    // Test completes quickly without unnecessary delays
    Assert.True(true);
}
```

#### Diagnosing Timeout Failures

When a test times out, xUnit provides clear error messages:

```
Test 'MyTest' exceeded execution timeout of 5000ms
```

**Common causes:**
1. Infinite loops in test code
2. Deadlocks in async code
3. Waiting for conditions that never occur
4. Timeout value too low for legitimate operations

**Solutions:**
- Check for infinite loops or blocking operations
- Review async/await patterns for deadlocks
- Increase timeout if test legitimately needs more time
- Optimize test to run faster

#### CI/CD Integration

Timeout configuration works correctly in CI/CD environments. The global default timeout (30 seconds) prevents job-level timeouts from masking individual test failures:

```bash
# Tests will timeout at the test level, not the job level
dotnet test
```

Environment variables can override timeout values if needed (though this is rarely necessary):

```bash
# Override default timeout (optional)
export XUNIT_TEST_TIMEOUT_MS=60000
dotnet test
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


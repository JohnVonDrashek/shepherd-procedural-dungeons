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

### Template Weighting

Use weights to control template frequency and create game design mechanics:

**Common vs Rare Templates:**
```csharp
var templates = new[]
{
    // Common template (appears frequently)
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-common")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(10.0)  // Very common
        .Build(),
    
    // Rare special template (appears infrequently)
    RoomTemplateBuilder<RoomType>.LShape(6, 5, 2, 2, Corner.TopRight)
        .WithId("combat-legendary")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(0.1)  // Rare (1% chance)
        .Build()
};
```

**Size-Based Weighting:**
```csharp
// Small rooms more common, large rooms rare
var templates = new[]
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("combat-small")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(5.0)  // Most common
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(2.0)  // Moderate
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("combat-large")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(0.5)  // Rare
        .Build()
};
```

**Temporarily Disable Templates:**
```csharp
// Use zero weight to disable templates without removing them
var templates = new[]
{
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-active")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(1.0)
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-disabled")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(0.0)  // Disabled - won't be selected
        .Build()
};
```

**Best Practices:**
- Use relative weights (e.g., 10 vs 1 = 10x more likely) rather than absolute values
- Keep weights simple (1.0, 2.0, 0.5) for easier probability calculation
- Test weight distributions to ensure desired frequency
- Remember: probability = template.Weight / sum(all_template_weights)

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

### Graph Lookup Performance

The library uses optimized O(1) dictionary lookups for node access:

```csharp
// ✅ Fast - O(1) lookup
var node = graph.GetNode(nodeId);

// ❌ Slow - O(n) linear search
var node = graph.Nodes.First(n => n.Id == nodeId);
```

**Performance characteristics:**
- Node lookups scale efficiently with graph size (O(1) complexity)
- BFS pathfinding uses optimized lookups for better performance on large graphs (100+ rooms)
- The optimization provides 25-90% improvement for node lookups, scaling with graph size

### Cluster Detection Performance

When clustering is enabled (`ClusterConfig.Enabled = true`), the library uses optimized cluster detection with centroid caching:

**Performance characteristics:**
- Cluster detection scales efficiently with room count (O(n × c + n²) complexity where n is rooms and c is cells per room)
- Centroid calculations are cached to avoid repeated computations
- For 100 rooms, cluster detection completes in ~35μs (90% faster than previous implementation)
- Memory allocations reduced by 68-93% compared to previous implementation

**When clustering impacts performance:**
- Clustering is optional and only runs when `ClusterConfig.Enabled = true`
- Performance impact is minimal for small dungeons (< 20 rooms)
- For large dungeons (50+ rooms) with clustering enabled, the optimization provides 75-91% improvement in cluster detection time
- Memory usage is optimized through single-pass calculations and centroid caching

**Best practices:**
- Enable clustering only when needed for gameplay mechanics (bazaar areas, gauntlets, treasure vaults)
- Use `RoomTypesToCluster` to limit clustering to specific room types for better performance
- For very large dungeons (100+ rooms), consider limiting clustering to 1-2 room types

### Spatial Placement Performance

The library includes several optimizations for spatial placement operations:

**Spatial Placement Bounding Box Caching (OPT-004):**
- Bounding box calculations are cached outside radius search loops
- Eliminates 19x redundant calculations per `PlaceNearby` call
- Improves code clarity by separating bounding box computation from search logic
- Impact: Reduces redundant work in spatial placement hot paths

**PlacedRoom Cell Caching (OPT-005):**
- `PlacedRoom.GetWorldCells()` results are cached on first access
- Eliminates duplicate `Cell` allocations for frequently-accessed rooms
- Reduces GC pressure in cluster detection and spatial solving operations
- Impact: Significant memory savings for rooms accessed multiple times (cluster detection, overlap checking)

**Best practices:**
- The optimizations are transparent and require no code changes
- Performance benefits scale with dungeon size and number of room accesses
- Large dungeons (50+ rooms) benefit most from these optimizations

### Hallway Generation Performance

The library includes optimizations for hallway generation:

**HallwayGenerator Exterior Edges Caching (OPT-006):**
- Exterior edges are cached per room at the start of hallway generation
- Eliminates redundant edge processing for rooms with multiple connections
- Reduces enumerable allocations during hallway generation
- Impact: 5-15% improvement for dungeons with many hallways or rooms with multiple connections

**Best practices:**
- Performance benefits scale with number of hallways and connections per room
- Rooms with many connections benefit most from the optimization
- The optimization is transparent and requires no code changes

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

## Debug Logging

The library includes a configurable DEBUG logging system (`DebugLogger`) that provides detailed insights into generation without cluttering test output.

### Log Levels

Use appropriate log levels for different types of information:

```csharp
// Verbose: Detailed operation logs (suppressed in tests by default)
DebugLogger.LogVerbose(Component.AStar, $"Exploring node {nodeId}");

// Info: Important milestones (shown by default)
DebugLogger.LogInfo(Component.RoomPlacement, $"Placed room {roomId}");

// Warn: Potential issues
DebugLogger.LogWarn(Component.ConstraintEvaluation, "Constraint may be too restrictive");

// Error: Actual problems (always shown)
DebugLogger.LogError(Component.General, $"Generation failed: {ex.Message}");
```

### Enabling Verbose Logging

By default, VERBOSE logs are suppressed in test contexts to reduce output noise. To enable them:

**Via environment variable:**
```bash
# Enable all verbose logs
export SHEPHERD_DEBUG_LEVEL=VERBOSE

# Run your tests or application
dotnet test
```

**Programmatically:**
```csharp
DebugLogger.SetLogLevel(DebugLogger.LogLevel.Verbose);
```

### Component Filtering

Focus on specific components when debugging:

```bash
# Only see A* pathfinding logs
export SHEPHERD_DEBUG_COMPONENTS=AStar

# See multiple components
export SHEPHERD_DEBUG_COMPONENTS=AStar,RoomPlacement,ConstraintEvaluation
```

**Programmatically:**
```csharp
// Enable only specific components
DebugLogger.ResetConfiguration();
DebugLogger.EnableComponent(Component.AStar);
DebugLogger.EnableComponent(Component.RoomPlacement);
```

### Common Debugging Scenarios

**Debugging A* pathfinding:**
```bash
export SHEPHERD_DEBUG_LEVEL=VERBOSE
export SHEPHERD_DEBUG_COMPONENTS=AStar
```

**Debugging constraint violations:**
```bash
export SHEPHERD_DEBUG_LEVEL=INFO
export SHEPHERD_DEBUG_COMPONENTS=ConstraintEvaluation
```

**Debugging room placement:**
```bash
export SHEPHERD_DEBUG_LEVEL=VERBOSE
export SHEPHERD_DEBUG_COMPONENTS=RoomPlacement
```

### Performance Considerations

- **Zero overhead in RELEASE builds**: All logging methods are removed in release builds via `[Conditional("DEBUG")]`
- **Fast-path optimization**: When verbose logging is disabled, `LogVerbose` returns immediately without string formatting
- **Test context detection**: VERBOSE logs are automatically suppressed in test contexts to keep test output clean

### Default Behavior

- **In test contexts**: VERBOSE logs suppressed, INFO/WARN/ERROR shown
- **In non-test contexts**: Default log level is INFO (VERBOSE suppressed, INFO/WARN/ERROR shown)
- **All components enabled by default**: Unless `SHEPHERD_DEBUG_COMPONENTS` is set, all components log

## Next Steps

- **[Examples](Examples)** - See best practices in action
- **[Troubleshooting](Troubleshooting)** - Fix common issues
- **[Advanced Topics](Advanced-Topics)** - Extend the library


# Advanced Topics

Advanced usage patterns and extensibility options.

## Custom Spatial Solvers

The library uses `IncrementalSolver` by default, but you can provide your own implementation of `ISpatialSolver<TRoomType>`.

### Interface

```csharp
public interface ISpatialSolver<TRoomType> where TRoomType : Enum
{
    IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng);
}
```

### Custom Implementation

```csharp
public class MyCustomSolver<TRoomType> : ISpatialSolver<TRoomType> where TRoomType : Enum
{
    public IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng)
    {
        // Your custom placement algorithm
        var placedRooms = new List<PlacedRoom<TRoomType>>();
        
        // ... implement your algorithm ...
        
        return placedRooms;
    }
}
```

### Using Custom Solver

```csharp
var customSolver = new MyCustomSolver<RoomType>();
var generator = new FloorGenerator<RoomType>(customSolver);

var layout = generator.Generate(config);
```

### When to Use

Consider a custom solver if you need:
- Different placement algorithms (e.g., grid-based, organic)
- Special optimization goals
- Integration with existing spatial systems
- Performance optimizations

**Note:** Custom solvers must ensure rooms don't overlap and handle `HallwayMode` appropriately.

## Custom Constraints

For complex placement logic, use `CustomConstraint`:

### Basic Custom Constraint

```csharp
var constraint = new CustomConstraint<RoomType>(
    RoomType.Special,
    (node, graph, assignments) =>
    {
        // Your custom logic
        return node.DistanceFromStart >= 3 
            && !node.IsOnCriticalPath
            && node.ConnectionCount == 2;
    }
);
```

### Advanced Custom Constraint

```csharp
var constraint = new CustomConstraint<RoomType>(
    RoomType.Secret,
    (node, graph, assignments) =>
    {
        // Check distance
        if (node.DistanceFromStart < 4) return false;
        
        // Check if on critical path
        if (node.IsOnCriticalPath) return false;
        
        // Check connection count (must be dead end)
        if (node.ConnectionCount != 1) return false;
        
        // Check if any adjacent nodes are already special rooms
        foreach (var conn in node.Connections)
        {
            int neighborId = conn.NodeAId == node.Id ? conn.NodeBId : conn.NodeAId;
            if (assignments.TryGetValue(neighborId, out var neighborType) &&
                neighborType == RoomType.Special)
            {
                return false;  // Don't place next to another special room
            }
        }
        
        return true;
    }
);
```

### Available Context

In custom constraints, you have access to:

**Node properties:**
- `node.Id` - Node ID
- `node.DistanceFromStart` - Steps from spawn
- `node.IsOnCriticalPath` - On spawn-to-boss path
- `node.ConnectionCount` - Number of connections
- `node.Connections` - List of connections

**Graph context:**
- `graph.Nodes` - All nodes
- `graph.Connections` - All connections
- `graph.StartNodeId` - Spawn node ID
- `graph.BossNodeId` - Boss node ID (after assignment)
- `graph.CriticalPath` - Critical path node IDs

**Assignments:**
- `assignments[nodeId]` - Room type for a node (if assigned)
- `assignments.Values` - All assigned room types
- `assignments.Keys` - All assigned node IDs

## Deterministic Generation Details

### Seeding Strategy

The library uses separate RNGs for each phase:

```csharp
// Master RNG from seed
var masterRng = new Random(config.Seed);

// Derive child seeds
int graphSeed = masterRng.Next();
int typeSeed = masterRng.Next();
int templateSeed = masterRng.Next();
int spatialSeed = masterRng.Next();
int hallwaySeed = masterRng.Next();

// Each phase uses its own RNG
var graphRng = new Random(graphSeed);
var typeRng = new Random(typeSeed);
// ... etc
```

This ensures:
- Same seed = same output
- Phase order doesn't affect determinism
- Each phase is independently reproducible

### Ensuring Determinism

When extending the library:

1. **Use provided RNGs** - Don't create your own `Random` instances
2. **Avoid non-deterministic operations** - No `DateTime.Now`, `Guid.NewGuid()`, etc.
3. **Order collections** - Don't rely on hash set ordering
4. **No parallel operations** - That could reorder results

### Testing Determinism

```csharp
[Fact]
public void DeterministicGeneration()
{
    var config = CreateConfig(seed: 12345);
    var generator = new FloorGenerator<RoomType>();
    
    var layout1 = generator.Generate(config);
    var layout2 = generator.Generate(config);
    
    // Should be identical
    Assert.Equal(layout1.Rooms.Count, layout2.Rooms.Count);
    Assert.Equal(layout1.SpawnRoomId, layout2.SpawnRoomId);
    Assert.Equal(layout1.BossRoomId, layout2.BossRoomId);
    
    // Check room positions
    foreach (var room1 in layout1.Rooms)
    {
        var room2 = layout2.GetRoom(room1.NodeId);
        Assert.NotNull(room2);
        Assert.Equal(room1.Position, room2.Position);
        Assert.Equal(room1.RoomType, room2.RoomType);
    }
}
```

## Performance Optimization

### Large Dungeon Generation

For very large dungeons (30+ rooms):

1. **Reduce constraint complexity:**
```csharp
// ✅ Simple constraints
Constraints = new[]
{
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss)
}

// ❌ Complex constraints (slower)
Constraints = /* 20+ constraints with complex logic */
```

2. **Use smaller templates:**
```csharp
// ✅ Small templates
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)

// ❌ Large templates (slower placement)
RoomTemplateBuilder<RoomType>.Rectangle(10, 10)
```

3. **Optimize hallway mode:**
```csharp
// ✅ Fastest
HallwayMode = HallwayMode.None

// ✅ Fast (recommended)
HallwayMode = HallwayMode.AsNeeded

// ⚠️ Slower
HallwayMode = HallwayMode.Always
```

### Caching Templates

If you generate many dungeons, cache templates:

```csharp
// Create once
private static readonly List<RoomTemplate<RoomType>> Templates = CreateTemplates();

// Reuse
var config = new FloorConfig<RoomType>
{
    // ...
    Templates = Templates  // Reuse cached templates
};
```

### Batch Generation

Generate multiple floors efficiently:

```csharp
var configs = Enumerable.Range(0, 10)
    .Select(i => new FloorConfig<RoomType>
    {
        Seed = 1000 + i,
        RoomCount = 15,
        // ... same config for all
    })
    .ToList();

var generator = new FloorGenerator<RoomType>();
var layouts = configs
    .AsParallel()  // Parallel generation
    .Select(c => generator.Generate(c))
    .ToList();
```

## Extending the Library

### Adding New Constraint Types

Create a new constraint class:

```csharp
public class MinConnectionCountConstraint<TRoomType> : IConstraint<TRoomType> 
    where TRoomType : Enum
{
    public TRoomType TargetRoomType { get; }
    public int MinConnections { get; }
    
    public MinConnectionCountConstraint(TRoomType roomType, int minConnections)
    {
        TargetRoomType = roomType;
        MinConnections = minConnections;
    }
    
    public bool IsValid(
        RoomNode node, 
        FloorGraph graph, 
        IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        return node.ConnectionCount >= MinConnections;
    }
}
```

### Adding Template Helpers

Create helper methods for common template patterns:

```csharp
public static class TemplateHelpers
{
    public static RoomTemplateBuilder<TRoomType> CrossShape<TRoomType>(
        int size) where TRoomType : Enum
    {
        var builder = new RoomTemplateBuilder<TRoomType>();
        
        // Center horizontal
        builder.AddRectangle(0, size / 2, size, 1);
        // Center vertical
        builder.AddRectangle(size / 2, 0, 1, size);
        
        return builder;
    }
}

// Usage
var template = TemplateHelpers.CrossShape<RoomType>(5)
    .WithId("cross")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

## Integration Patterns

### Game Engine Integration

```csharp
public class DungeonSystem
{
    private FloorLayout<RoomType> _currentFloor;
    private Dictionary<int, RoomEntity> _roomEntities = new();
    
    public void GenerateFloor(int seed)
    {
        var config = CreateConfig(seed);
        var generator = new FloorGenerator<RoomType>();
        _currentFloor = generator.Generate(config);
        
        CreateEntities();
    }
    
    private void CreateEntities()
    {
        foreach (var room in _currentFloor.Rooms)
        {
            var entity = new RoomEntity
            {
                NodeId = room.NodeId,
                RoomType = room.RoomType,
                WorldPosition = ConvertToWorldSpace(room.Position),
                Cells = room.GetWorldCells().ToList()
            };
            
            _roomEntities[room.NodeId] = entity;
        }
    }
    
    public RoomEntity GetRoom(int nodeId) => _roomEntities[nodeId];
    
    public IEnumerable<RoomEntity> GetConnectedRooms(int nodeId)
    {
        // Find connected rooms via doors/hallways
        // ... implementation
    }
}
```

### Save/Load System

```csharp
public class DungeonSaveData
{
    public int Seed { get; set; }
    public int RoomCount { get; set; }
    public float BranchingFactor { get; set; }
    // ... other config properties
}

public void SaveDungeon(FloorLayout<RoomType> layout, FloorConfig<RoomType> config)
{
    var saveData = new DungeonSaveData
    {
        Seed = config.Seed,
        RoomCount = config.RoomCount,
        BranchingFactor = config.BranchingFactor
        // ... save config
    };
    
    // Serialize and save
    File.WriteAllText("dungeon.json", JsonSerializer.Serialize(saveData));
}

public FloorLayout<RoomType> LoadDungeon()
{
    var saveData = JsonSerializer.Deserialize<DungeonSaveData>(
        File.ReadAllText("dungeon.json"));
    
    // Recreate config
    var config = new FloorConfig<RoomType>
    {
        Seed = saveData.Seed,
        RoomCount = saveData.RoomCount,
        // ... recreate config
    };
    
    // Regenerate (deterministic!)
    var generator = new FloorGenerator<RoomType>();
    return generator.Generate(config);
}
```

## Debugging

### Visual Debugging

Create ASCII visualization:

```csharp
public static void PrintDungeon(FloorLayout<RoomType> layout)
{
    var (min, max) = layout.GetBounds();
    int width = max.X - min.X + 1;
    int height = max.Y - min.Y + 1;
    
    var grid = new char[width, height];
    
    // Initialize
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            grid[x, y] = ' ';
        }
    }
    
    // Draw rooms
    foreach (var room in layout.Rooms)
    {
        char c = room.RoomType switch
        {
            RoomType.Spawn => 'S',
            RoomType.Boss => 'B',
            RoomType.Combat => 'C',
            _ => 'R'
        };
        
        foreach (var cell in room.GetWorldCells())
        {
            int x = cell.X - min.X;
            int y = cell.Y - min.Y;
            grid[x, y] = c;
        }
    }
    
    // Draw hallways
    foreach (var hallway in layout.Hallways)
    {
        foreach (var segment in hallway.Segments)
        {
            foreach (var cell in segment.GetCells())
            {
                int x = cell.X - min.X;
                int y = cell.Y - min.Y;
                if (grid[x, y] == ' ')
                {
                    grid[x, y] = '.';
                }
            }
        }
    }
    
    // Print
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            Console.Write(grid[x, y]);
        }
        Console.WriteLine();
    }
}
```

## Multi-Floor Dungeons

The library supports generating multi-floor dungeons with vertical connections between floors via stairs and teleporters.

### Basic Multi-Floor Generation

```csharp
var floor0Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    // ... other config
};

var floor1Config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,
    // ... other config
};

var connections = new[]
{
    new FloorConnection
    {
        FromFloorIndex = 0,
        FromRoomNodeId = 9,
        ToFloorIndex = 1,
        ToRoomNodeId = 0,
        Type = ConnectionType.StairsDown
    }
};

var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config },
    Connections = connections
};

var generator = new MultiFloorGenerator<RoomType>();
var layout = generator.Generate(multiFloorConfig);
```

### Floor-Aware Constraints

Use floor-aware constraints to control room placement per floor:

```csharp
var floor0Constraints = new List<IConstraint<RoomType>>
{
    // Boss not on floor 0
    new NotOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 0 })
};

var floor2Constraints = new List<IConstraint<RoomType>>
{
    // Boss only on floor 2
    new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 2 })
};
```

### Connection Types

Three connection types are available:

- **StairsUp** - Visual stairs going up to a higher floor
- **StairsDown** - Visual stairs going down to a lower floor  
- **Teleporter** - Teleporter pad/portal connecting floors (can skip floors)

### Progressive Difficulty

Create progressive difficulty by increasing complexity on deeper floors:

```csharp
for (int floorIndex = 0; floorIndex < 5; floorIndex++)
{
    var floorConfig = new FloorConfig<RoomType>
    {
        Seed = 12345,
        RoomCount = 8 + (floorIndex * 2),  // More rooms on deeper floors
        BranchingFactor = 0.2f + (floorIndex * 0.05f),  // More complex
        Constraints = new List<IConstraint<RoomType>>
        {
            new OnlyOnFloorConstraint<RoomType>(RoomType.Boss, new[] { 4 })
        }
        // ...
    };
}
```

### Connection Planning

Plan connections carefully - room node IDs must exist on their floors:

```csharp
// ❌ Bad: Room ID might not exist
var connection = new FloorConnection
{
    FromRoomNodeId = 999,  // Might not exist!
    // ...
};

// ✅ Good: Use known room IDs
var floor0 = generator.Generate(floor0Config);
var connection = new FloorConnection
{
    FromRoomNodeId = floor0.BossRoomId,  // Known to exist
    // ...
};
```

### Determinism

Multi-floor generation is deterministic - same seed produces identical layout:

```csharp
var config1 = CreateMultiFloorConfig(seed: 12345);
var config2 = CreateMultiFloorConfig(seed: 12345);

var generator = new MultiFloorGenerator<RoomType>();
var layout1 = generator.Generate(config1);
var layout2 = generator.Generate(config2);

// Layouts are identical
Assert.Equal(layout1.TotalFloorCount, layout2.TotalFloorCount);
```

### Backward Compatibility

Single-floor generation still works unchanged:

```csharp
// Still works - no changes needed
var singleFloorConfig = new FloorConfig<RoomType> { /* ... */ };
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(singleFloorConfig);
```

### Best Practices

1. **Use floor-aware constraints** - Control room placement per floor
2. **Plan connections** - Know room IDs before creating connections
3. **Progressive difficulty** - Increase complexity on deeper floors
4. **Test determinism** - Verify same seed produces same layout
5. **Validate connections** - Ensure room IDs exist on their floors

## Zones

Zones enable partitioning dungeons into distinct biome/thematic regions with different generation rules.

### Basic Zone Setup

```csharp
var castleZone = new Zone<RoomType>
{
    Id = "castle",
    Name = "Castle",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    }
};

var config = new FloorConfig<RoomType>
{
    // ... other config ...
    Zones = new[] { castleZone }
};
```

### Zone Boundaries

**Distance-Based Zones:**
```csharp
Boundary = new ZoneBoundary.DistanceBased
{
    MinDistance = 0,  // Inclusive
    MaxDistance = 3   // Inclusive
}
```

Assigns rooms based on their distance from the spawn node. Useful for creating zones that progress from start to end.

**Critical Path-Based Zones:**
```csharp
Boundary = new ZoneBoundary.CriticalPathBased
{
    StartPercent = 0.0f,  // 0.0 to 1.0
    EndPercent = 0.5f     // 0.0 to 1.0
}
```

Assigns rooms based on their position along the critical path (spawn to boss). Useful for creating zones based on progression rather than distance.

### Zone-Specific Room Requirements

Zones can have their own room type requirements in addition to global requirements:

```csharp
var marketZone = new Zone<RoomType>
{
    Id = "market",
    Name = "Market",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    },
    RoomRequirements = new[]
    {
        (RoomType.Shop, 2)  // Two shops required in market zone
    }
};
```

### Zone-Specific Templates

Zones can have their own template pools. Rooms in a zone prefer zone-specific templates, falling back to global templates if none available:

```csharp
var castleTemplate = RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
    .WithId("castle-ornate")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

var castleZone = new Zone<RoomType>
{
    Id = "castle",
    Name = "Castle",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 3
    },
    Templates = new[] { castleTemplate }  // Prefer ornate templates
};
```

### Zone-Aware Constraints

Use zone-aware constraints to restrict room types to specific zones:

```csharp
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
```

### Accessing Zone Assignments

After generation, zone assignments are available in `FloorLayout`:

```csharp
var layout = generator.Generate(config);

// Check zone assignments
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

### Zone Overlap Handling

If zones overlap, the first zone in the list takes precedence (first match wins):

```csharp
var zone1 = new Zone<RoomType>
{
    Id = "zone1",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 0,
        MaxDistance = 5
    }
};

var zone2 = new Zone<RoomType>
{
    Id = "zone2",
    Boundary = new ZoneBoundary.DistanceBased
    {
        MinDistance = 3,  // Overlaps with zone1
        MaxDistance = 7
    }
};

// zone1 comes first, so rooms at distance 3-5 will be in zone1
Zones = new[] { zone1, zone2 }
```

### Transition Rooms

Transition rooms (rooms connecting different zones) are automatically identified:

```csharp
foreach (var transition in layout.TransitionRooms)
{
    // This room connects different zones
    // Useful for special effects, visual transitions, etc.
    ApplyZoneTransitionEffect(transition);
}
```

### Best Practices

1. **Use distance-based zones** for progression-based zones (early/mid/late game)
2. **Use critical path-based zones** for story-based zones (tutorial/boss areas)
3. **Zone-specific templates** for visual variety per zone
4. **Zone-aware constraints** for fine-grained control
5. **Transition rooms** for special effects between zones
6. **Test determinism** - same seed should produce same zone assignments

### Common Patterns

**Castle to Dungeon Progression:**
```csharp
var castleZone = new Zone<RoomType>
{
    Id = "castle",
    Boundary = new ZoneBoundary.DistanceBased { MinDistance = 0, MaxDistance = 3 },
    Templates = new[] { castleTemplates }
};

var dungeonZone = new Zone<RoomType>
{
    Id = "dungeon",
    Boundary = new ZoneBoundary.DistanceBased { MinDistance = 4, MaxDistance = 10 },
    Templates = new[] { dungeonTemplates },
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyInZoneConstraint<RoomType>(RoomType.Boss, "dungeon")
    }
};
```

**Early/Mid/Late Zones:**
```csharp
var earlyZone = new Zone<RoomType>
{
    Id = "early",
    Boundary = new ZoneBoundary.CriticalPathBased { StartPercent = 0.0f, EndPercent = 0.33f }
};

var midZone = new Zone<RoomType>
{
    Id = "mid",
    Boundary = new ZoneBoundary.CriticalPathBased { StartPercent = 0.33f, EndPercent = 0.66f }
};

var lateZone = new Zone<RoomType>
{
    Id = "late",
    Boundary = new ZoneBoundary.CriticalPathBased { StartPercent = 0.66f, EndPercent = 1.0f },
    Constraints = new List<IConstraint<RoomType>>
    {
        new OnlyInZoneConstraint<RoomType>(RoomType.Boss, "late")
    }
};
```

## Next Steps

- **[Best Practices](Best-Practices)** - Apply these patterns
- **[API Reference](API-Reference)** - Complete API documentation
- **[Examples](Examples)** - See advanced patterns in action, including multi-floor and zones examples
- **[Configuration](Configuration)** - Learn about FloorConfig and zones


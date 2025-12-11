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

### GraphAlgorithm

```csharp
GraphAlgorithm = GraphAlgorithm.GridBased
```

**Type:** `GraphAlgorithm` enum

**Default:** `GraphAlgorithm.SpanningTree` (backward compatible)

**Description:** Algorithm used for generating the dungeon floor graph topology. Different algorithms produce fundamentally different connectivity patterns and gameplay experiences.

**Available Algorithms:**

- **`SpanningTree`** (default) - Classic spanning tree algorithm with optional extra connections. Produces organic, branching structures. Backward compatible with existing behavior.
- **`GridBased`** - Arranges rooms in a 2D grid pattern with configurable connectivity. Creates structured, maze-like dungeons with clear navigation patterns.
- **`CellularAutomata`** - Uses cellular automata rules to generate organic, cave-like structures with irregular connectivity.
- **`MazeBased`** - Generates maze-like structures with complex, winding paths perfect for exploration-focused games.
- **`HubAndSpoke`** - Creates central hub rooms with branching spokes. Good for creating gathering/rest areas.

**Algorithm-Specific Configuration:**

Each algorithm (except `SpanningTree`) requires its own configuration object:

#### GridBasedGraphConfig

Required when `GraphAlgorithm` is `GridBased`:

```csharp
GraphAlgorithm = GraphAlgorithm.GridBased,
GridBasedConfig = new GridBasedGraphConfig
{
    GridWidth = 4,   // Width of the grid
    GridHeight = 4,  // Height of the grid (GridWidth * GridHeight should be >= RoomCount)
    ConnectivityPattern = ConnectivityPattern.FourWay  // or EightWay
}
```

**Properties:**
- `GridWidth` (int, required) - Width of the grid
- `GridHeight` (int, required) - Height of the grid
- `ConnectivityPattern` (ConnectivityPattern, default: `FourWay`) - How rooms connect:
  - `FourWay` - Connect to north, south, east, west neighbors
  - `EightWay` - Connect to all 8 neighbors (includes diagonals)

**Tips:**
- `GridWidth * GridHeight` should be >= `RoomCount` to fit all rooms
- `FourWay` creates more structured, maze-like layouts
- `EightWay` creates more connected, open layouts

#### CellularAutomataGraphConfig

Required when `GraphAlgorithm` is `CellularAutomata`:

```csharp
GraphAlgorithm = GraphAlgorithm.CellularAutomata,
CellularAutomataConfig = new CellularAutomataGraphConfig
{
    BirthThreshold = 4,      // Default: 4
    SurvivalThreshold = 3,   // Default: 3
    Iterations = 5           // Default: 5
}
```

**Properties:**
- `BirthThreshold` (int, default: 4) - Minimum neighbors required for a cell to become alive
- `SurvivalThreshold` (int, default: 3) - Minimum neighbors required for a cell to stay alive
- `Iterations` (int, default: 5) - Number of cellular automata iterations

**Tips:**
- Higher `BirthThreshold` = sparser, more cave-like structures
- Higher `SurvivalThreshold` = more isolated rooms
- More `Iterations` = smoother, more organic shapes

#### MazeBasedGraphConfig

Required when `GraphAlgorithm` is `MazeBased`:

```csharp
GraphAlgorithm = GraphAlgorithm.MazeBased,
MazeBasedConfig = new MazeBasedGraphConfig
{
    MazeType = MazeType.Perfect,    // or Imperfect
    Algorithm = MazeAlgorithm.Prims  // or Kruskals
}
```

**Properties:**
- `MazeType` (MazeType, default: `Perfect`) - Type of maze:
  - `Perfect` - No loops, tree structure (single path between any two rooms)
  - `Imperfect` - May contain loops
- `Algorithm` (MazeAlgorithm, default: `Prims`) - Algorithm to use:
  - `Prims` - Prim's algorithm (grows from a single point)
  - `Kruskals` - Kruskal's algorithm (connects disjoint sets)

**Tips:**
- `Perfect` mazes create linear, exploration-focused experiences
- `Imperfect` mazes allow loops and alternative routes
- Both algorithms produce similar results; choose based on preference

#### HubAndSpokeGraphConfig

Required when `GraphAlgorithm` is `HubAndSpoke`:

```csharp
GraphAlgorithm = GraphAlgorithm.HubAndSpoke,
HubAndSpokeConfig = new HubAndSpokeGraphConfig
{
    HubCount = 2,        // Number of hub rooms
    MaxSpokeLength = 3    // Maximum length of spokes from hubs
}
```

**Properties:**
- `HubCount` (int, required) - Number of central hub rooms to create
- `MaxSpokeLength` (int, required) - Maximum length of spokes branching from hubs

**Tips:**
- Hub rooms typically have many connections (good for shops, rest areas)
- Spoke rooms branch from hubs (good for exploration paths)
- `HubCount * MaxSpokeLength` should be considered when setting `RoomCount`

**Example:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    GraphAlgorithm = GraphAlgorithm.GridBased,
    GridBasedConfig = new GridBasedGraphConfig
    {
        GridWidth = 5,
        GridHeight = 5,
        ConnectivityPattern = ConnectivityPattern.FourWay
    }
};
```

**Determinism:**

All algorithms maintain determinism - same seed + same algorithm + same config = identical graph topology.

**Backward Compatibility:**

The default algorithm (`SpanningTree`) maintains backward compatibility. Existing configurations without `GraphAlgorithm` specified will use the original spanning tree algorithm.

**Choosing an Algorithm:**

- **SpanningTree** - Default, organic branching (good for most roguelikes)
- **GridBased** - Structured, maze-like (good for puzzle games, structured exploration)
- **CellularAutomata** - Organic, cave-like (good for cave systems, irregular dungeons)
- **MazeBased** - Complex, winding paths (good for exploration-focused games)
- **HubAndSpoke** - Central gathering areas (good for hub-based progression)

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

### SecretPassageConfig

```csharp
SecretPassageConfig = new SecretPassageConfig<RoomType>
{
    Count = 3,
    MaxSpatialDistance = 5,
    AllowedRoomTypes = new HashSet<RoomType> { RoomType.Treasure },
    ForbiddenRoomTypes = new HashSet<RoomType> { RoomType.Boss },
    AllowCriticalPathConnections = true,
    AllowGraphConnectedRooms = false
}
```

**Type:** `SecretPassageConfig<TRoomType>?`

**Default:** `null` (secret passages disabled)

**Description:** Configuration for generating secret passages - hidden connections between rooms that are not part of the main dungeon graph.

**Properties:**

- **Count** (int, default: 0) - Number of secret passages to generate. Set to 0 to disable secret passages.
- **MaxSpatialDistance** (int, default: 5) - Maximum spatial distance (in cells) between rooms for secret passage eligibility. Rooms further apart won't be connected.
- **AllowedRoomTypes** (IReadOnlySet<TRoomType>, default: empty) - Room types that can have secret passages. If empty, all room types are eligible.
- **ForbiddenRoomTypes** (IReadOnlySet<TRoomType>, default: empty) - Room types that cannot have secret passages.
- **AllowCriticalPathConnections** (bool, default: true) - Whether secret passages can connect rooms on the critical path.
- **AllowGraphConnectedRooms** (bool, default: false) - Whether secret passages can connect rooms that are already graph-connected. Set to false to ensure secret passages provide alternative routes.

**How Secret Passages Work:**

Secret passages are hidden connections that:
- **Don't affect graph topology** - They don't change the critical path, distances, or graph structure
- **Connect spatially close rooms** - They link rooms that are physically near each other (within MaxSpatialDistance)
- **Are optional shortcuts** - They provide alternative routes without affecting main dungeon flow
- **Can be discovered** - Games can reveal them through gameplay mechanics (pressing walls, finding switches, etc.)

**Example:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    SecretPassageConfig = new SecretPassageConfig<RoomType>
    {
        Count = 3,  // Generate 3 secret passages
        MaxSpatialDistance = 5,  // Only connect rooms within 5 cells
        AllowedRoomTypes = new HashSet<RoomType> { RoomType.Treasure },  // Only connect treasure rooms
        AllowGraphConnectedRooms = false,  // Only connect rooms not already connected
        AllowCriticalPathConnections = false  // Don't connect critical path rooms
    }
};
```

**Tips:**
- Use secret passages to create shortcuts and alternative routes
- Set `AllowGraphConnectedRooms = false` to ensure secret passages provide new paths
- Use `AllowedRoomTypes` or `ForbiddenRoomTypes` to control which room types can have secret passages
- Secret passages are generated deterministically based on seed
- Secret passages can include hallways if rooms are not adjacent (similar to regular connections)

See [Working with Output](Working-with-Output#secret-passages) for how to access secret passages in generated layouts.

### DifficultyConfig

```csharp
DifficultyConfig = new DifficultyConfig
{
    BaseDifficulty = 1.0,
    ScalingFactor = 1.0,
    Function = DifficultyScalingFunction.Linear,
    MaxDifficulty = 10.0
}
```

**Type:** `DifficultyConfig?`

**Default:** `null` (difficulty not calculated)

**Description:** Configuration for room difficulty scaling based on distance from spawn. Enables progressive difficulty curves where rooms become more challenging as players explore deeper into the dungeon.

**Properties:**

- **BaseDifficulty** (double, default: 1.0) - Base difficulty for the spawn room (distance 0). This is the starting difficulty level.
- **ScalingFactor** (double, default: 1.0) - Scaling factor used by the scaling function. Controls how quickly difficulty increases with distance.
- **Function** (DifficultyScalingFunction, default: `Linear`) - The scaling function to use:
  - `Linear` - Linear scaling: `difficulty = baseDifficulty + (distance * scalingFactor)`
  - `Exponential` - Exponential scaling: `difficulty = baseDifficulty + (scalingFactor ^ distance)`
  - `Custom` - Custom function provided via `CustomFunction` property
- **CustomFunction** (Func<int, double>?, default: null) - Custom function for difficulty calculation. Only used when `Function` is `Custom`. Takes distance as input and returns difficulty.
- **MaxDifficulty** (double, default: 10.0) - Maximum difficulty cap. All calculated difficulties will be clamped to this value.

**How Difficulty Scaling Works:**

Difficulty is calculated automatically for each room based on its distance from the spawn room:
- **Spawn room** (distance 0) always has `BaseDifficulty`
- **Other rooms** have difficulty calculated using the selected scaling function
- **All difficulties** are clamped to `MaxDifficulty` to prevent unbounded scaling
- **Deterministic** - Same seed and config produce identical difficulty assignments

**Scaling Functions:**

1. **Linear Scaling** (default):
   ```
   difficulty = BaseDifficulty + (distance * ScalingFactor)
   ```
   Example: `BaseDifficulty = 1.0`, `ScalingFactor = 1.5`
   - Distance 0: 1.0
   - Distance 1: 2.5
   - Distance 2: 4.0
   - Distance 3: 5.5

2. **Exponential Scaling**:
   ```
   difficulty = BaseDifficulty + (ScalingFactor ^ distance)
   ```
   Example: `BaseDifficulty = 1.0`, `ScalingFactor = 2.0`
   - Distance 0: 1.0
   - Distance 1: 3.0 (1.0 + 2^1)
   - Distance 2: 5.0 (1.0 + 2^2)
   - Distance 3: 9.0 (1.0 + 2^3)

3. **Custom Function**:
   ```csharp
   DifficultyConfig = new DifficultyConfig
   {
       Function = DifficultyScalingFunction.Custom,
       CustomFunction = distance => 1.0 + (distance * distance * 0.5)  // Quadratic scaling
   }
   ```

**Example - Linear Scaling:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    DifficultyConfig = new DifficultyConfig
    {
        BaseDifficulty = 1.0,
        ScalingFactor = 1.5,
        Function = DifficultyScalingFunction.Linear,
        MaxDifficulty = 10.0
    }
};
```

**Example - Exponential Scaling:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    DifficultyConfig = new DifficultyConfig
    {
        BaseDifficulty = 1.0,
        ScalingFactor = 1.8,
        Function = DifficultyScalingFunction.Exponential,
        MaxDifficulty = 15.0
    }
};
```

**Example - Custom Function:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    DifficultyConfig = new DifficultyConfig
    {
        BaseDifficulty = 1.0,
        Function = DifficultyScalingFunction.Custom,
        CustomFunction = distance =>
        {
            // Slow start, rapid increase after distance 3
            if (distance <= 3)
                return 1.0 + (distance * 0.5);
            else
                return 2.5 + ((distance - 3) * 2.0);
        },
        MaxDifficulty = 20.0
    }
};
```

**Using Difficulty in Constraints:**

Difficulty can be used with constraints to control room placement:

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    DifficultyConfig = new DifficultyConfig
    {
        BaseDifficulty = 1.0,
        ScalingFactor = 1.5,
        Function = DifficultyScalingFunction.Linear,
        MaxDifficulty = 10.0
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        // Boss only in high-difficulty areas
        new MinDifficultyConstraint<RoomType>(RoomType.Boss, 7.0),
        
        // Easy combat rooms only in low-difficulty areas
        new MaxDifficultyConstraint<RoomType>(RoomType.EasyCombat, 3.0),
        
        // Elite rooms in medium-difficulty areas
        new MinDifficultyConstraint<RoomType>(RoomType.Elite, 4.0),
        new MaxDifficultyConstraint<RoomType>(RoomType.Elite, 8.0)
    }
};
```

**Accessing Difficulty in Output:**

After generation, difficulty is available in `PlacedRoom`:

```csharp
var layout = generator.Generate(config);

foreach (var room in layout.Rooms)
{
    Console.WriteLine($"Room {room.NodeId} ({room.RoomType}): Difficulty {room.Difficulty:F2}");
}
```

**Tips:**
- Use linear scaling for predictable, steady difficulty increases
- Use exponential scaling for dramatic difficulty spikes in deeper areas
- Use custom functions for complex curves (e.g., slow start, rapid middle, plateau at end)
- Set `MaxDifficulty` to cap difficulty for balance
- Combine difficulty constraints with distance constraints for fine-grained control
- Difficulty is calculated deterministically - same seed produces same difficulties
- Difficulty is available even if not used in constraints (useful for game systems)

See [Constraints](Constraints#difficulty-constraints) for details on difficulty-aware constraints.

### ClusterConfig

```csharp
ClusterConfig = new ClusterConfig<RoomType>
{
    Enabled = true,
    Epsilon = 20.0,
    MinClusterSize = 2,
    MaxClusterSize = null,
    RoomTypesToCluster = null
}
```

**Type:** `ClusterConfig<TRoomType>?`

**Default:** `null` (clustering disabled)

**Description:** Configuration for room clustering detection. Clustering automatically identifies and groups spatially adjacent rooms of the same type into clusters, enabling gameplay patterns like bazaar areas (shops cluster together), gauntlet areas (combat rooms cluster), and treasure vaults (treasure rooms cluster together).

**Properties:**

- **Enabled** (bool, default: true) - Whether clustering is enabled. Set to `false` to disable clustering entirely.
- **Epsilon** (double, default: 20.0) - Maximum spatial distance (in cells) between rooms in the same cluster. Rooms further apart than this distance cannot be in the same cluster.
- **MinClusterSize** (int, default: 2) - Minimum number of rooms required to form a cluster. Rooms that don't meet this minimum are considered "noise" and not grouped into clusters.
- **MaxClusterSize** (int?, default: null) - Optional maximum number of rooms allowed in a cluster. If `null`, no maximum limit. If set, clusters will be limited to this size.
- **RoomTypesToCluster** (IReadOnlySet<TRoomType>?, default: null) - Optional filter for which room types to cluster. If `null`, all room types are clustered. If set, only the specified room types will be clustered.

**How Clustering Works:**

Clustering happens **after spatial placement** but **before final layout construction**:

1. Rooms are placed in 2D space
2. Clustering algorithm analyzes spatial positions
3. Rooms of the same type that are spatially close (within epsilon distance) are grouped into clusters
4. Only clusters meeting the minimum size requirement are kept
5. Clusters are stored in `FloorLayout.Clusters`

**Clustering Algorithm:**

The system uses a **complete-graph clustering algorithm** that ensures all pairs of rooms in a cluster are within the epsilon distance threshold. This produces more cohesive clusters than standard DBSCAN.

**Distance Calculation:**

Distance between rooms is calculated using **centroid distance**:
- Each room's centroid is calculated from its world cells
- Distance between centroids determines cluster membership
- All pairs in a cluster must be within epsilon distance

**Example - Basic Clustering:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    ClusterConfig = new ClusterConfig<RoomType>
    {
        Enabled = true,
        Epsilon = 20.0,        // Rooms within 20 cells can cluster
        MinClusterSize = 2     // At least 2 rooms per cluster
    }
};
```

**Example - Bazaar Area (Shops Cluster):**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    ClusterConfig = new ClusterConfig<RoomType>
    {
        Enabled = true,
        Epsilon = 15.0,        // Shops within 15 cells cluster
        MinClusterSize = 3,    // Bazaar needs at least 3 shops
        MaxClusterSize = 6,    // But not more than 6 shops
        RoomTypesToCluster = new HashSet<RoomType> { RoomType.Shop }  // Only cluster shops
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new MustFormClusterConstraint<RoomType>(RoomType.Shop),
        new MinClusterSizeConstraint<RoomType>(RoomType.Shop, 3),
        new MaxClusterSizeConstraint<RoomType>(RoomType.Shop, 6)
    }
};
```

**Example - Combat Gauntlets:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    ClusterConfig = new ClusterConfig<RoomType>
    {
        Enabled = true,
        Epsilon = 25.0,        // Combat rooms within 25 cells cluster
        MinClusterSize = 5,    // Gauntlets need at least 5 rooms
        RoomTypesToCluster = new HashSet<RoomType> { RoomType.Combat }  // Only cluster combat
    },
    Constraints = new List<IConstraint<RoomType>>
    {
        new MustFormClusterConstraint<RoomType>(RoomType.Combat),
        new MinClusterSizeConstraint<RoomType>(RoomType.Combat, 5)
    }
};
```

**Example - Multiple Room Types:**

```csharp
var config = new FloorConfig<RoomType>
{
    // ... other config ...
    ClusterConfig = new ClusterConfig<RoomType>
    {
        Enabled = true,
        Epsilon = 20.0,
        MinClusterSize = 2,
        RoomTypesToCluster = new HashSet<RoomType> 
        { 
            RoomType.Shop, 
            RoomType.Treasure 
        }  // Cluster both shops and treasure
    }
};
```

**Accessing Clusters in Output:**

After generation, clusters are available in `FloorLayout`:

```csharp
var layout = generator.Generate(config);

// Get all clusters
var allClusters = layout.Clusters;

// Get clusters for a specific room type
var shopClusters = layout.GetClustersForRoomType(RoomType.Shop);

// Get the largest cluster for a room type
var largestShopCluster = layout.GetLargestCluster(RoomType.Shop);
if (largestShopCluster != null)
{
    Console.WriteLine($"Largest shop cluster has {largestShopCluster.GetSize()} shops");
    Console.WriteLine($"Centroid: ({largestShopCluster.Centroid.X}, {largestShopCluster.Centroid.Y})");
}
```

**Tips:**
- Use `Epsilon` to control how close rooms must be to cluster (smaller = tighter clusters)
- Use `MinClusterSize` to filter out isolated rooms (noise filtering)
- Use `MaxClusterSize` to prevent clusters from becoming too large
- Use `RoomTypesToCluster` to only cluster specific room types (improves performance)
- Clustering is deterministic - same seed produces identical clusters
- Clustering runs once per floor generation (performance is acceptable for 100+ rooms)
- Cluster results are cached in `FloorLayout` to avoid recomputation

See [Constraints](Constraints#cluster-aware-constraints) for cluster-aware constraints and [Working with Output](Working-with-Output#clusters) for accessing clusters in generated layouts.

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

## Dungeon Themes and Presets

Dungeon themes provide pre-configured dungeon generation setups for common roguelike scenarios. Themes encapsulate complete configurations (templates, constraints, algorithms, etc.) with metadata (name, description, tags) for easy discovery and reuse.

### Using Built-in Themes

The library includes several built-in themes accessible through `ThemePresetLibrary<TRoomType>`:

```csharp
using ShepherdProceduralDungeons.Configuration;

// Get a theme by property
var castle = ThemePresetLibrary<RoomType>.Castle;
var cave = ThemePresetLibrary<RoomType>.Cave;
var temple = ThemePresetLibrary<RoomType>.Temple;
var laboratory = ThemePresetLibrary<RoomType>.Laboratory;
var crypt = ThemePresetLibrary<RoomType>.Crypt;
var forest = ThemePresetLibrary<RoomType>.Forest;

// Get a theme by ID
var theme = ThemePresetLibrary<RoomType>.GetTheme("castle");

// Get all themes
var allThemes = ThemePresetLibrary<RoomType>.GetAllThemes();

// Get themes by tags
var undergroundThemes = ThemePresetLibrary<RoomType>.GetThemesByTags("underground");
var structuredThemes = ThemePresetLibrary<RoomType>.GetThemesByTags("structured", "indoor");
```

### Built-in Theme Characteristics

Each built-in theme has specific characteristics:

- **Castle**: Structured, grid-based layout with low branching factor. Tags: `structured`, `indoor`, `medieval`, `grid-based`
- **Cave**: Organic, cellular automata layout with high branching factor. Tags: `organic`, `underground`, `natural`, `cave-like`
- **Temple**: Structured, maze-based layout. Tags: `structured`, `indoor`, `religious`, `maze-like`
- **Laboratory**: Structured, grid-based layout. Tags: `structured`, `indoor`, `scientific`, `grid-based`
- **Crypt**: Underground, maze-based layout. Tags: `underground`, `maze-like`, `dark`, `tomb-like`
- **Forest**: Organic, spanning tree layout. Tags: `organic`, `outdoor`, `natural`, `tree-like`

### Converting Themes to FloorConfig

Themes can be converted to `FloorConfig` with optional overrides:

```csharp
var theme = ThemePresetLibrary<RoomType>.Castle;

// Use theme defaults
var config = theme.ToFloorConfig();

// Override specific properties
var configWithOverrides = theme.ToFloorConfig(new ThemeOverrides
{
    Seed = 99999,
    RoomCount = 20,
    BranchingFactor = 0.5f,
    HallwayMode = HallwayMode.Always,
    GraphAlgorithm = GraphAlgorithm.CellularAutomata
});
```

### Creating Custom Themes

```csharp
var baseConfig = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 15,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates,
    GraphAlgorithm = GraphAlgorithm.GridBased,
    GridBasedConfig = new GridBasedGraphConfig
    {
        GridWidth = 5,
        GridHeight = 3,
        ConnectivityPattern = ConnectivityPattern.FourWay
    },
    BranchingFactor = 0.3f,
    HallwayMode = HallwayMode.AsNeeded
};

var customTheme = new DungeonTheme<RoomType>
{
    Id = "my-custom-theme",
    Name = "My Custom Theme",
    Description = "A custom theme for my game",
    BaseConfig = baseConfig,
    Tags = new HashSet<string> { "custom", "experimental" },
    Zones = optionalZones  // Optional zone configurations
};
```

### Theme Composition

Themes can be combined, with the second theme taking precedence:

```csharp
var theme1 = ThemePresetLibrary<RoomType>.Castle;
var theme2 = ThemePresetLibrary<RoomType>.Crypt;

var combined = theme1.Combine(theme2);
// Combined theme uses theme2's config but merges zones and tags
```

### Theme Validation

Themes are validated when converting to `FloorConfig`:

```csharp
var theme = new DungeonTheme<RoomType>
{
    Id = "test-theme",
    Name = "Test Theme",
    BaseConfig = config
};

// Validation occurs here - throws InvalidConfigurationException if invalid
var config = theme.ToFloorConfig();
```

**Validation checks:**
- Theme ID and name must not be empty
- Base configuration must be valid
- All required properties must be present

### Theme Serialization

Themes support JSON serialization:

```csharp
using ShepherdProceduralDungeons.Serialization;

var theme = ThemePresetLibrary<RoomType>.Castle;
var serializer = new ConfigurationSerializer<RoomType>();

// Serialize to JSON
string json = serializer.SerializeThemeToJson(theme, prettyPrint: true);

// Deserialize from JSON
var loadedTheme = serializer.DeserializeThemeFromJson(json);
```

See [Examples](Examples#dungeon-themes-and-presets) for complete theme usage examples.

## Configuration Serialization

Dungeon configurations can be serialized to and deserialized from JSON, enabling save/load functionality, sharing configurations, version control, and configuration editor tools.

### Basic Serialization

```csharp
using ShepherdProceduralDungeons.Serialization;

var config = new FloorConfig<RoomType>
{
    // ... configuration ...
};

// Serialize to JSON string
var serializer = new ConfigurationSerializer<RoomType>();
string json = serializer.SerializeToJson(config, prettyPrint: true);

// Deserialize from JSON string
var deserializedConfig = serializer.DeserializeFromJson(json);
```

### Extension Methods

For convenience, extension methods are available:

```csharp
using ShepherdProceduralDungeons.Serialization;

var config = new FloorConfig<RoomType> { /* ... */ };

// Serialize to JSON
string json = config.ToJson();

// Deserialize from JSON
var config = FloorConfig<RoomType>.FromJson<RoomType>(json);

// Save to file
config.SaveToFile("dungeon-config.json");

// Load from file
var config = FloorConfig<RoomType>.LoadFromFile<RoomType>("dungeon-config.json");
```

### Multi-Floor Serialization

Multi-floor configurations can also be serialized:

```csharp
var multiFloorConfig = new MultiFloorConfig<RoomType>
{
    Seed = 12345,
    Floors = new[] { floor0Config, floor1Config },
    Connections = connections
};

var serializer = new ConfigurationSerializer<RoomType>();
string json = serializer.SerializeToJson(multiFloorConfig, prettyPrint: true);
var deserialized = serializer.DeserializeMultiFloorConfigFromJson(json);
```

### JSON Format

Configurations are serialized using camelCase property names. Here's an example JSON structure:

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
      "id": "l-shaped-combat",
      "validRoomTypes": ["Combat"],
      "weight": 0.5,
      "shape": {
        "type": "lShape",
        "width": 5,
        "height": 4,
        "cutoutWidth": 2,
        "cutoutHeight": 2,
        "cutoutCorner": "TopRight"
      },
      "doorEdges": {
        "strategy": "allExteriorEdges"
      }
    },
    {
      "id": "custom-shape",
      "validRoomTypes": ["Treasure"],
      "weight": 1.0,
      "shape": {
        "type": "custom",
        "cells": [
          { "x": 0, "y": 0 },
          { "x": 1, "y": 0 },
          { "x": 0, "y": 1 },
          { "x": 1, "y": 1 },
          { "x": 2, "y": 1 }
        ]
      },
      "doorEdges": {
        "strategy": "explicit",
        "edges": {
          "0,0": ["North", "West"],
          "2,1": ["East"]
        }
      }
    }
  ],
  "constraints": [
    {
      "type": "MinDistanceFromStart",
      "targetRoomType": "Boss",
      "minDistance": 5
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
    }
  ],
  "zones": [
    {
      "id": "castle",
      "name": "Castle",
      "boundary": {
        "type": "DistanceBased",
        "minDistance": 0,
        "maxDistance": 3
      },
      "roomRequirements": [
        { "type": "Shop", "count": 1 }
      ],
      "templates": ["castle-template-id"]
    }
  ],
  "secretPassageConfig": {
    "count": 3,
    "maxSpatialDistance": 5,
    "allowedRoomTypes": ["Treasure"],
    "forbiddenRoomTypes": ["Boss"],
    "allowCriticalPathConnections": true,
    "allowGraphConnectedRooms": false
  }
}
```

### Template Shapes

Templates support three shape types in JSON:

1. **Rectangle**: `{ "type": "rectangle", "width": 3, "height": 3 }`
2. **L-Shape**: `{ "type": "lShape", "width": 5, "height": 4, "cutoutWidth": 2, "cutoutHeight": 2, "cutoutCorner": "TopRight" }`
3. **Custom**: `{ "type": "custom", "cells": [{ "x": 0, "y": 0 }, ...] }`

### Door Edge Strategies

Door edges can be specified using three strategies:

1. **All Exterior Edges**: `{ "strategy": "allExteriorEdges" }`
2. **Sides**: `{ "strategy": "sides", "sides": ["North", "South"] }`
3. **Explicit**: `{ "strategy": "explicit", "edges": { "0,0": ["North", "West"], "2,1": ["East"] } }`

### Constraint Types

All built-in constraint types are supported in JSON. Each constraint includes a `type` field and constraint-specific properties:

- `MinDistanceFromStart`: `{ "type": "MinDistanceFromStart", "targetRoomType": "Boss", "minDistance": 5 }`
- `MaxDistanceFromStart`: `{ "type": "MaxDistanceFromStart", "targetRoomType": "Shop", "maxDistance": 4 }`
- `MustBeDeadEnd`: `{ "type": "MustBeDeadEnd", "targetRoomType": "Boss" }`
- `NotOnCriticalPath`: `{ "type": "NotOnCriticalPath", "targetRoomType": "Treasure" }`
- `OnlyOnCriticalPath`: `{ "type": "OnlyOnCriticalPath", "targetRoomType": "Boss" }`
- `MinConnectionCount`: `{ "type": "MinConnectionCount", "targetRoomType": "Hub", "minConnections": 3 }`
- `MaxConnectionCount`: `{ "type": "MaxConnectionCount", "targetRoomType": "Treasure", "maxConnections": 2 }`
- `MaxPerFloor`: `{ "type": "MaxPerFloor", "targetRoomType": "Shop", "maxCount": 1 }`
- `OnlyOnFloor`: `{ "type": "OnlyOnFloor", "targetRoomType": "Boss", "allowedFloors": [2] }`
- `NotOnFloor`: `{ "type": "NotOnFloor", "targetRoomType": "Shop", "forbiddenFloors": [0] }`
- `MinFloor`: `{ "type": "MinFloor", "targetRoomType": "Boss", "minFloor": 2 }`
- `MaxFloor`: `{ "type": "MaxFloor", "targetRoomType": "Tutorial", "maxFloor": 0 }`
- `MustBeAdjacentTo`: `{ "type": "MustBeAdjacentTo", "targetRoomType": "Shop", "requiredAdjacentTypes": ["Combat"] }`
- `MustNotBeAdjacentTo`: `{ "type": "MustNotBeAdjacentTo", "targetRoomType": "Shop", "forbiddenAdjacentTypes": ["Shop"] }`
- `MinDistanceFromRoomType`: `{ "type": "MinDistanceFromRoomType", "targetRoomType": "Secret", "referenceRoomTypes": ["Boss"], "minDistance": 3 }`
- `MaxDistanceFromRoomType`: `{ "type": "MaxDistanceFromRoomType", "targetRoomType": "Shop", "referenceRoomTypes": ["Combat"], "maxDistance": 2 }`
- `MustComeBefore`: `{ "type": "MustComeBefore", "targetRoomType": "MiniBoss", "referenceRoomTypes": ["Boss"] }`
- `OnlyInZone`: `{ "type": "OnlyInZone", "targetRoomType": "Boss", "zoneId": "dungeon" }`
- `CompositeConstraint`: `{ "type": "CompositeConstraint", "operator": "And", "constraints": [...] }`

### Zone Boundaries

Zone boundaries support two types:

1. **Distance-Based**: `{ "type": "DistanceBased", "minDistance": 0, "maxDistance": 3 }`
2. **Critical Path-Based**: `{ "type": "CriticalPathBased", "startPercent": 0.0, "endPercent": 0.5 }`

### Error Handling

Deserialization throws `InvalidConfigurationException` for:
- Invalid JSON syntax
- Missing required fields
- Invalid enum values
- Type mismatches

```csharp
try
{
    var config = serializer.DeserializeFromJson(json);
}
catch (InvalidConfigurationException ex)
{
    Console.WriteLine($"Invalid configuration: {ex.Message}");
}
```

### Round-Trip Compatibility

Serialization and deserialization are round-trip compatible - serializing a configuration and then deserializing it produces an identical configuration:

```csharp
var config = CreateConfig();
var serializer = new ConfigurationSerializer<RoomType>();

string json1 = serializer.SerializeToJson(config, prettyPrint: true);
var deserialized = serializer.DeserializeFromJson(json1);
string json2 = serializer.SerializeToJson(deserialized, prettyPrint: true);

Assert.Equal(json1, json2);  // Identical JSON output
```

### Custom Options

You can provide custom `JsonSerializerOptions`:

```csharp
var customOptions = new JsonSerializerOptions
{
    WriteIndented = false,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};

var serializer = new ConfigurationSerializer<RoomType>();
string json = serializer.SerializeToJson(config, customOptions);
var config = serializer.DeserializeFromJson(json, customOptions);
```

**Note**: Custom converters for complex types (Cell, Edge, ZoneBoundary, RoomTemplate, Constraint) are automatically added to ensure proper serialization.

### Use Cases

- **Save/Load**: Save dungeon presets to files and load them later
- **Sharing**: Share configurations with team members or the community
- **Version Control**: Track configuration changes over time
- **Configuration Editors**: Build visual editors that work with JSON
- **Preset Libraries**: Maintain libraries of dungeon presets
- **Cross-Platform**: Share configurations between different game engines

## Next Steps

- **[Constraints](Constraints)** - Understand constraint system, including floor-aware constraints
- **[Hallway Modes](Hallway-Modes)** - Choose the right hallway mode
- **[Examples](Examples)** - See complete configurations, including multi-floor examples
- **[Advanced Topics](Advanced-Topics#multi-floor-dungeons)** - Learn more about multi-floor generation


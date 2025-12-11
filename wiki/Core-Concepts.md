# Core Concepts

Understanding the fundamental ideas behind ShepherdProceduralDungeons.

## Overview

The library generates dungeons in several phases:
1. **Graph Generation** - Creates room connectivity
2. **Room Type Assignment** - Assigns types based on constraints
3. **Template Selection** - Picks templates for each room
4. **Spatial Placement** - Positions rooms in 2D space
5. **Hallway Generation** - Creates corridors when needed

## Room Types

### Generic System

The library uses a **generic** system where you define your own room type enum:

```csharp
public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}
```

This allows you to use any room types that make sense for your game.

### Required Types

Every dungeon needs:
- **Spawn Room Type** - Starting room (always node 0)
- **Boss Room Type** - Final room (placed at farthest valid position)
- **Default Room Type** - Fills remaining rooms

### Special Types

You can define any additional types:
- `Shop`, `Treasure`, `Secret`, `MiniBoss`, etc.
- Specify how many of each via `RoomRequirements`

## Room Templates

### What They Are

Templates define:
- **Shape** - Which grid cells the room occupies
- **Door Placement** - Where doors can be placed

### Template Selection

When a room is generated:
1. Its room type is determined
2. A template is randomly selected from templates valid for that type
3. The template is placed in 2D space

### Multiple Templates

You can have multiple templates for the same room type. The generator randomly picks one:

```csharp
// Three templates for Combat rooms
var templates = new[]
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3).ForRoomTypes(RoomType.Combat),
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4).ForRoomTypes(RoomType.Combat),
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5).ForRoomTypes(RoomType.Combat)
};
```

## Graph Topology

### What It Is

The **graph** represents which rooms connect to which. It's a mathematical structure (nodes and edges), not spatial positions.

### Graph Structure

- **Nodes** - Represent rooms (not yet placed in space)
- **Edges** - Represent connections between rooms
- **Start Node** - Always node 0 (spawn)
- **Boss Node** - Determined during type assignment

### Graph Generation Algorithms

The library supports multiple graph generation algorithms, each producing different connectivity patterns:

#### SpanningTree (Default)

The classic algorithm that:
1. Creates a **spanning tree** (guarantees all rooms are reachable)
2. Adds **extra edges** based on `BranchingFactor`

**Characteristics:**
- Organic, branching structures
- Good for most roguelikes
- Backward compatible (default)

**Branching Factor:**
- **0.0** - Pure tree (no loops, linear)
- **0.3** - Some loops (recommended)
- **1.0** - Highly connected (many loops)

#### GridBased

Arranges rooms in a 2D grid pattern with configurable connectivity.

**Characteristics:**
- Structured, maze-like layouts
- Clear navigation patterns
- Good for puzzle games and structured exploration

**Configuration:**
- `GridWidth` and `GridHeight` define grid dimensions
- `ConnectivityPattern` controls connections (FourWay or EightWay)

#### CellularAutomata

Uses cellular automata rules to generate organic, cave-like structures.

**Characteristics:**
- Irregular, organic connectivity
- Cave-like topologies
- Good for cave systems and irregular dungeons

**Configuration:**
- `BirthThreshold` and `SurvivalThreshold` control density
- `Iterations` controls smoothness

#### MazeBased

Generates maze-like structures with complex, winding paths.

**Characteristics:**
- Complex, winding paths
- Perfect or imperfect mazes
- Good for exploration-focused games

**Configuration:**
- `MazeType` (Perfect or Imperfect)
- `Algorithm` (Prims or Kruskals)

#### HubAndSpoke

Creates central hub rooms with branching spokes.

**Characteristics:**
- Central gathering areas
- Branching exploration paths
- Good for hub-based progression

**Configuration:**
- `HubCount` - Number of hub rooms
- `MaxSpokeLength` - Maximum spoke length

### Choosing an Algorithm

- **SpanningTree** - Default, organic branching (good for most roguelikes)
- **GridBased** - Structured, maze-like (good for puzzle games)
- **CellularAutomata** - Organic, cave-like (good for cave systems)
- **MazeBased** - Complex, winding paths (good for exploration games)
- **HubAndSpoke** - Central gathering areas (good for hub-based progression)

### Determinism

All algorithms maintain determinism - same seed + same algorithm + same config = identical graph topology.

## Constraints

### Purpose

Constraints control **where** specific room types can be placed in the graph.

### How They Work

During room type assignment:
1. For each required room type
2. Find all nodes that satisfy all constraints for that type
3. Randomly select from valid nodes
4. Assign the room type

### Constraint Types

- **Distance constraints** - How far from spawn
- **Path constraints** - On/off critical path
- **Structure constraints** - Dead ends, connection count
- **Count constraints** - Maximum per floor
- **Custom constraints** - Your own logic

## Critical Path

### Definition

The **critical path** is the shortest path from spawn to boss. It represents the "main quest" route through the dungeon.

### Properties

- Always starts at spawn (node 0)
- Always ends at boss
- Guaranteed to exist (graph is connected)
- Rooms on critical path are required for progression

### Usage

```csharp
var criticalPath = layout.CriticalPath;

// Check if room is on critical path
bool isRequired = criticalPath.Contains(room.NodeId);

// Optional content should be off critical path
new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure)
```

## Spatial Layout

### Grid System

Rooms are placed on a 2D grid:
- **Cell** - Single grid position `(x, y)`
- **Anchor** - Top-left corner of room template
- **World Coordinates** - Absolute grid positions

### Placement Algorithm

The spatial solver:
1. Places spawn at origin `(0, 0)`
2. Uses BFS from spawn
3. Tries to place connected rooms adjacent
4. Falls back to hallways if needed (based on `HallwayMode`)

### Collision Avoidance

- Rooms cannot overlap
- Tracks all occupied cells
- Tries adjacent placement first
- Uses hallways when adjacent placement fails

## Hallways

### When They're Created

Hallways are generated when:
- `HallwayMode.AsNeeded` - Rooms can't be adjacent
- `HallwayMode.Always` - Always between all rooms
- `HallwayMode.None` - Never (throws if needed)

### How They Work

1. Find door positions on each room
2. Use A* pathfinding to find path
3. Generate straight segments
4. Avoid overlapping with rooms

### Hallway Structure

- **Segments** - Straight horizontal/vertical lines
- **Doors** - Connect to rooms at each end
- **Cells** - Grid cells the hallway occupies

## Deterministic Generation

### Same Seed = Same Output

Given the same:
- `Seed`
- `RoomCount`
- `Templates`
- `Constraints`
- `RoomRequirements`
- `BranchingFactor`
- `HallwayMode`

You get **identical** output.

### How It Works

The library uses separate random number generators for each phase:
- Graph generation
- Room type assignment
- Template selection
- Spatial placement
- Hallway generation

This ensures phase order doesn't affect determinism.

## Coordinate Systems

### Template Coordinates

Templates use **relative coordinates** (relative to anchor):
- Anchor is always `(0, 0)` in template space
- Template cells are relative to anchor

### World Coordinates

Placed rooms use **world coordinates**:
- `room.Position` is the anchor in world space
- `room.GetWorldCells()` converts template cells to world cells

### Example

```csharp
// Template has cell at (1, 2) relative to anchor
// Room placed at world (5, 10)
// World cell is (5 + 1, 10 + 2) = (6, 12)
```

## Zones

### What They Are

Zones partition the dungeon into distinct regions with different generation rules. They enable:
- **Visual variety**: Different zones can have distinct visual themes
- **Gameplay depth**: Zones can affect difficulty, enemy types, and room characteristics
- **Narrative structure**: Zones enable storytelling through spatial design

### Zone Boundaries

Zones use boundaries to determine which rooms belong to them:

1. **Distance-Based**: Rooms assigned based on distance from spawn
   ```csharp
   Boundary = new ZoneBoundary.DistanceBased
   {
       MinDistance = 0,
       MaxDistance = 3
   }
   ```

2. **Critical Path-Based**: Rooms assigned based on position along critical path
   ```csharp
   Boundary = new ZoneBoundary.CriticalPathBased
   {
       StartPercent = 0.0f,
       EndPercent = 0.5f
   }
   ```

### Zone Features

Zones can have:
- **Zone-specific room requirements**: Additional room types required in that zone
- **Zone-specific constraints**: Constraints that apply only to rooms in that zone
- **Zone-specific templates**: Template pools preferred for rooms in that zone

### Zone Assignment

- Zones are assigned after graph generation but before room type assignment
- If zones overlap, the first zone in the list takes precedence
- Zone assignments are deterministic (same seed = same assignments)
- Transition rooms (connecting different zones) are automatically identified

## Secret Passages

### What They Are

Secret passages are **hidden connections** between rooms that are not part of the main dungeon graph. They enable:
- **Shortcuts**: Alternative routes that bypass main paths
- **Exploration rewards**: Hidden connections discoverable through gameplay
- **Alternative routes**: Options for speedrunners or exploration-focused players

### Key Properties

- **Don't affect graph topology**: Secret passages don't change the critical path, distances, or graph structure
- **Spatially placed**: They connect rooms that are physically close (within MaxSpatialDistance)
- **Optional shortcuts**: They provide alternative routes without affecting main dungeon flow
- **Discoverable**: Games can reveal them through gameplay mechanics (pressing walls, finding switches, etc.)

### How They Work

Secret passages are generated **after** spatial placement:
1. Find candidate room pairs that are spatially close
2. Filter by room type constraints (allowed/forbidden types)
3. Optionally exclude graph-connected or critical path rooms
4. Select passages based on configuration
5. Generate doors and optional hallways

### Configuration

Secret passages are configured via `SecretPassageConfig`:
- **Count**: How many secret passages to generate
- **MaxSpatialDistance**: Maximum distance between rooms
- **AllowedRoomTypes**: Which room types can have secret passages
- **ForbiddenRoomTypes**: Which room types cannot have secret passages
- **AllowCriticalPathConnections**: Whether to allow connections on critical path
- **AllowGraphConnectedRooms**: Whether to allow connections between already-connected rooms

See [Configuration](Configuration#secretpassageconfig) for details.

## Generation Pipeline

```
1. Validate Config
   ↓
2. Generate Graph (topology)
   ↓
3. Assign Zones (if configured)
   ↓
4. Assign Room Types (with constraints, including zone-aware)
   ↓
5. Select Templates (prefer zone-specific, fallback to global)
   ↓
6. Place Rooms (spatial solver)
   ↓
7. Generate Hallways (if needed)
   ↓
8. Place Doors
   ↓
9. Generate Secret Passages (if configured)
   ↓
10. Identify Transition Rooms (if zones configured)
   ↓
11. Return FloorLayout
```

## Key Principles

### 1. Separation of Concerns

- **Graph** = Connectivity (which rooms connect)
- **Spatial** = Placement (where rooms are)
- **Templates** = Shape (what rooms look like)

### 2. Constraint-Based

Room placement is driven by constraints, not hardcoded rules.

### 3. Flexible

- Generic room types
- Custom templates
- Custom constraints
- Custom spatial solvers (advanced)

### 4. Deterministic

Same inputs = same outputs (important for testing and replay).

## Common Misconceptions

### "Rooms are placed first, then connected"

**Reality:** Graph (connectivity) is generated first, then rooms are placed to satisfy connections.

### "Constraints place rooms"

**Reality:** Constraints filter valid positions, but placement is still spatial.

### "Templates determine room types"

**Reality:** Room types are assigned first, then templates are selected.

### "Hallways are always needed"

**Reality:** Hallways are optional - rooms can connect directly if adjacent.

## Next Steps

- **[Getting Started](Getting-Started)** - Put concepts into practice
- **[Room Templates](Room-Templates)** - Create shapes
- **[Constraints](Constraints)** - Control placement
- **[Configuration](Configuration)** - Configure generation


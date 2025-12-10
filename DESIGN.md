# ShepherdProceduralDungeons - Design Document

A .NET 8 library for procedural dungeon generation targeting roguelike/dungeon crawler games (Enter the Gungeon style). Produces graph topology and spatial layouts; rendering is game's responsibility.

## Table of Contents

1. [Overview](#overview)
2. [Project Structure](#project-structure)
3. [Core Types](#core-types)
4. [Room Templates](#room-templates)
5. [Constraints](#constraints)
6. [Generation Pipeline](#generation-pipeline)
7. [Graph Generation](#graph-generation)
8. [Spatial Solver](#spatial-solver)
9. [Hallway Generation](#hallway-generation)
10. [Seeding Strategy](#seeding-strategy)
11. [Public API](#public-api)
12. [Error Handling](#error-handling)
13. [Testing Strategy](#testing-strategy)
14. [Implementation Order](#implementation-order)

---

## Overview

### What This Library Does

- Generates dungeon floor **topology** (which rooms connect to which)
- Assigns **room types** based on configurable constraints
- Solves **spatial layout** (positions rooms in 2D grid space)
- Places **doors** between connected rooms
- Optionally generates **hallways** when rooms can't be adjacent
- All generation is **deterministic** given a seed

### What This Library Does NOT Do

- Rendering (game's responsibility)
- Room interior content (enemy spawns, obstacles, tiles)
- Physics or collision
- Multi-floor/stairs (out of scope for v1)

### Design Principles

- **Generic room types**: Library uses `<TRoomType> where TRoomType : Enum` - game defines its own enum
- **No built-in templates**: Game provides all room shapes via builders
- **Exceptions on failure**: Invalid configurations throw, no silent fallbacks
- **Single seed reproducibility**: Same seed + same config = identical output
- **Zero required dependencies**: Core library is standalone

---

## Project Structure

```
ShepherdProceduralDungeons/
├── ShepherdProceduralDungeons.sln
├── src/
│   └── ShepherdProceduralDungeons/
│       ├── ShepherdProceduralDungeons.csproj
│       ├── FloorGenerator.cs           # Main entry point
│       ├── Configuration/
│       │   ├── FloorConfig.cs           # Generation configuration
│       │   └── HallwayMode.cs           # Enum for hallway options
│       ├── Templates/
│       │   ├── RoomTemplate.cs          # Room shape definition
│       │   ├── RoomTemplateBuilder.cs   # Fluent builder for templates
│       │   ├── Cell.cs                  # Grid cell position
│       │   └── Edge.cs                  # Edge enum (North/South/East/West)
│       ├── Constraints/
│       │   ├── IConstraint.cs           # Constraint interface
│       │   ├── MinDistanceFromStartConstraint.cs
│       │   ├── MaxDistanceFromStartConstraint.cs
│       │   ├── NotOnCriticalPathConstraint.cs
│       │   ├── OnlyOnCriticalPathConstraint.cs
│       │   ├── MaxPerFloorConstraint.cs
│       │   ├── MustBeDeadEndConstraint.cs
│       │   └── CustomConstraint.cs      # Callback-based constraint
│       ├── Graph/
│       │   ├── FloorGraph.cs            # Generated topology
│       │   ├── RoomNode.cs              # Node in the graph
│       │   └── RoomConnection.cs        # Edge in the graph
│       ├── Layout/
│       │   ├── FloorLayout.cs           # Final spatial output
│       │   ├── PlacedRoom.cs            # Room with position
│       │   ├── Hallway.cs               # Generated hallway
│       │   ├── HallwaySegment.cs        # Single hallway segment
│       │   └── Door.cs                  # Door between rooms/hallways
│       ├── Generation/
│       │   ├── GraphGenerator.cs        # Creates topology
│       │   ├── RoomTypeAssigner.cs      # Assigns types via constraints
│       │   ├── ISpatialSolver.cs        # Solver interface
│       │   ├── IncrementalSolver.cs     # Default placement algorithm
│       │   └── HallwayGenerator.cs      # Creates hallways
│       └── Exceptions/
│           ├── GenerationException.cs           # Base exception
│           ├── ConstraintViolationException.cs  # Can't satisfy constraints
│           ├── SpatialPlacementException.cs     # Can't fit rooms
│           └── InvalidConfigurationException.cs # Bad config
└── tests/
    └── ShepherdProceduralDungeons.Tests/
        ├── ShepherdProceduralDungeons.Tests.csproj
        ├── GraphGeneratorTests.cs
        ├── SpatialSolverTests.cs
        ├── ConstraintTests.cs
        ├── HallwayTests.cs
        ├── SeedDeterminismTests.cs
        └── IntegrationTests.cs
```

---

## Core Types

### Cell

Represents a single grid position.

```csharp
namespace ShepherdProceduralDungeons.Templates;

public readonly record struct Cell(int X, int Y)
{
    public Cell Offset(int dx, int dy) => new(X + dx, Y + dy);
    public Cell North => new(X, Y - 1);
    public Cell South => new(X, Y + 1);
    public Cell East => new(X + 1, Y);
    public Cell West => new(X - 1, Y);
}
```

### Edge

Cardinal directions for door placement.

```csharp
namespace ShepherdProceduralDungeons.Templates;

[Flags]
public enum Edge
{
    None = 0,
    North = 1,
    South = 2,
    East = 4,
    West = 8,
    All = North | South | East | West
}

public static class EdgeExtensions
{
    public static Edge Opposite(this Edge edge) => edge switch
    {
        Edge.North => Edge.South,
        Edge.South => Edge.North,
        Edge.East => Edge.West,
        Edge.West => Edge.East,
        _ => Edge.None
    };
}
```

### HallwayMode

```csharp
namespace ShepherdProceduralDungeons.Configuration;

public enum HallwayMode
{
    /// <summary>Rooms must share a wall. Throws if impossible.</summary>
    None,

    /// <summary>Generate hallways only when rooms cannot touch directly.</summary>
    AsNeeded,

    /// <summary>Always generate hallways between all rooms.</summary>
    Always
}
```

---

## Room Templates

### RoomTemplate

Defines a room's shape and valid door positions.

```csharp
namespace ShepherdProceduralDungeons.Templates;

public sealed class RoomTemplate
{
    /// <summary>Unique identifier for this template.</summary>
    public required string Id { get; init; }

    /// <summary>Room types this template can be used for.</summary>
    public required IReadOnlySet<TRoomType> ValidRoomTypes { get; init; }

    /// <summary>Cells this room occupies, relative to anchor (0,0).</summary>
    public required IReadOnlySet<Cell> Cells { get; init; }

    /// <summary>Cell edges where doors can be placed. Key is cell, value is valid edges.</summary>
    public required IReadOnlyDictionary<Cell, Edge> DoorEdges { get; init; }

    /// <summary>Bounding box width.</summary>
    public int Width => Cells.Max(c => c.X) - Cells.Min(c => c.X) + 1;

    /// <summary>Bounding box height.</summary>
    public int Height => Cells.Max(c => c.Y) - Cells.Min(c => c.Y) + 1;

    /// <summary>Gets all exterior edges of the room (edges not shared with another cell).</summary>
    public IEnumerable<(Cell Cell, Edge Edge)> GetExteriorEdges();

    /// <summary>Checks if a door can be placed at the given cell edge.</summary>
    public bool CanPlaceDoor(Cell cell, Edge edge);
}
```

**Note**: The class must be made generic or accept `Type` for room types. Recommend making the entire library generic with `<TRoomType>`.

### RoomTemplateBuilder

Fluent API for constructing templates.

```csharp
namespace ShepherdProceduralDungeons.Templates;

public sealed class RoomTemplateBuilder<TRoomType> where TRoomType : Enum
{
    private string? _id;
    private HashSet<TRoomType> _validTypes = new();
    private HashSet<Cell> _cells = new();
    private Dictionary<Cell, Edge> _doorEdges = new();

    /// <summary>Sets the template ID.</summary>
    public RoomTemplateBuilder<TRoomType> WithId(string id);

    /// <summary>Adds room types this template can be used for.</summary>
    public RoomTemplateBuilder<TRoomType> ForRoomTypes(params TRoomType[] types);

    /// <summary>Adds a single cell.</summary>
    public RoomTemplateBuilder<TRoomType> AddCell(int x, int y);

    /// <summary>Adds a rectangular region of cells.</summary>
    public RoomTemplateBuilder<TRoomType> AddRectangle(int x, int y, int width, int height);

    /// <summary>Creates a simple rectangle template (most common case).</summary>
    public static RoomTemplateBuilder<TRoomType> Rectangle(int width, int height);

    /// <summary>Creates an L-shaped template.</summary>
    public static RoomTemplateBuilder<TRoomType> LShape(int width, int height, int cutoutWidth, int cutoutHeight, Corner cutoutCorner);

    /// <summary>Sets door edges for a cell. Only exterior edges are valid.</summary>
    public RoomTemplateBuilder<TRoomType> WithDoorEdges(int x, int y, Edge edges);

    /// <summary>Allows doors on all exterior edges of all cells.</summary>
    public RoomTemplateBuilder<TRoomType> WithDoorsOnAllExteriorEdges();

    /// <summary>Allows doors only on specific sides of the bounding box.</summary>
    public RoomTemplateBuilder<TRoomType> WithDoorsOnSides(Edge sides);

    /// <summary>Builds the template. Throws if invalid.</summary>
    public RoomTemplate<TRoomType> Build();
}

public enum Corner { TopLeft, TopRight, BottomLeft, BottomRight }
```

### Usage Examples

```csharp
// Simple 4x3 rectangle with doors on all sides
var smallCombat = RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
    .WithId("small-combat")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

// Large boss room with doors only on south
var bossRoom = RoomTemplateBuilder<RoomType>.Rectangle(8, 8)
    .WithId("boss-arena")
    .ForRoomTypes(RoomType.Boss)
    .WithDoorsOnSides(Edge.South)
    .Build();

// L-shaped room
var lShaped = RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
    .WithId("l-shaped-combat")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

// Custom shape
var custom = new RoomTemplateBuilder<RoomType>()
    .WithId("cross-room")
    .ForRoomTypes(RoomType.Treasure)
    .AddCell(1, 0)  //   X
    .AddCell(0, 1)  // X X X
    .AddCell(1, 1)  //   X
    .AddCell(2, 1)
    .AddCell(1, 2)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

---

## Constraints

### IConstraint Interface

```csharp
namespace ShepherdProceduralDungeons.Constraints;

public interface IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>Room type this constraint applies to.</summary>
    TRoomType TargetRoomType { get; }

    /// <summary>
    /// Checks if a node is valid for the target room type.
    /// </summary>
    /// <param name="node">The node being evaluated.</param>
    /// <param name="graph">The full graph for context.</param>
    /// <param name="currentAssignments">Room types already assigned to other nodes.</param>
    /// <returns>True if this node can be assigned the target room type.</returns>
    bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments);
}
```

### Built-in Constraints

```csharp
/// <summary>Room must be at least N steps from the start node.</summary>
public class MinDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MinDistance { get; }

    public MinDistanceFromStartConstraint(TRoomType roomType, int minDistance);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.DistanceFromStart >= MinDistance;
}

/// <summary>Room must be at most N steps from the start node.</summary>
public class MaxDistanceFromStartConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxDistance { get; }

    public MaxDistanceFromStartConstraint(TRoomType roomType, int maxDistance);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.DistanceFromStart <= MaxDistance;
}

/// <summary>Room must NOT be on the critical path (start to boss).</summary>
public class NotOnCriticalPathConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }

    public NotOnCriticalPathConstraint(TRoomType roomType);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => !node.IsOnCriticalPath;
}

/// <summary>Room MUST be on the critical path.</summary>
public class OnlyOnCriticalPathConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }

    public OnlyOnCriticalPathConstraint(TRoomType roomType);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.IsOnCriticalPath;
}

/// <summary>At most N rooms of this type on the floor.</summary>
public class MaxPerFloorConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    public int MaxCount { get; }

    public MaxPerFloorConstraint(TRoomType roomType, int maxCount);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
    {
        int currentCount = currentAssignments.Values.Count(t => t.Equals(TargetRoomType));
        return currentCount < MaxCount;
    }
}

/// <summary>Room must be a dead end (exactly one connection).</summary>
public class MustBeDeadEndConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }

    public MustBeDeadEndConstraint(TRoomType roomType);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => node.ConnectionCount == 1;
}

/// <summary>Custom callback-based constraint for advanced logic.</summary>
public class CustomConstraint<TRoomType> : IConstraint<TRoomType>
{
    public TRoomType TargetRoomType { get; }
    private readonly Func<RoomNode, FloorGraph, IReadOnlyDictionary<int, TRoomType>, bool> _predicate;

    public CustomConstraint(TRoomType roomType, Func<RoomNode, FloorGraph, IReadOnlyDictionary<int, TRoomType>, bool> predicate);

    public bool IsValid(RoomNode node, FloorGraph graph, IReadOnlyDictionary<int, TRoomType> currentAssignments)
        => _predicate(node, graph, currentAssignments);
}
```

---

## Generation Pipeline

The generation process follows these steps in order:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. VALIDATE CONFIG                                                          │
│     - Check room counts make sense                                          │
│     - Verify templates exist for all required room types                    │
│     - Validate constraints are satisfiable (basic checks)                   │
├─────────────────────────────────────────────────────────────────────────────┤
│  2. GENERATE GRAPH (GraphGenerator)                                          │
│     - Create N room nodes                                                   │
│     - Connect nodes to form a tree (guarantees all rooms reachable)         │
│     - Add extra edges for loops/branches based on config                    │
│     - Mark start node (index 0)                                             │
│     - Calculate distances from start via BFS                                │
├─────────────────────────────────────────────────────────────────────────────┤
│  3. ASSIGN ROOM TYPES (RoomTypeAssigner)                                     │
│     - Assign Spawn to start node                                            │
│     - Assign Boss to farthest valid node                                    │
│     - Calculate critical path (BFS from start to boss)                      │
│     - Mark nodes on critical path                                           │
│     - Assign remaining types based on constraints (priority order)          │
│     - Fill remaining nodes with default type (e.g., Combat)                 │
├─────────────────────────────────────────────────────────────────────────────┤
│  4. SELECT TEMPLATES                                                         │
│     - For each node, pick a random valid template for its room type         │
│     - Ensure template's ValidRoomTypes contains the assigned type           │
├─────────────────────────────────────────────────────────────────────────────┤
│  5. SPATIAL PLACEMENT (IncrementalSolver)                                    │
│     - Place start room at origin                                            │
│     - BFS from start, placing connected rooms adjacent when possible        │
│     - Track occupied cells globally                                         │
│     - When direct adjacency fails and HallwayMode allows, mark for hallway  │
├─────────────────────────────────────────────────────────────────────────────┤
│  6. GENERATE HALLWAYS (HallwayGenerator)                                     │
│     - For each connection marked as needing hallway                         │
│     - Pathfind between room door positions                                  │
│     - Generate hallway segments                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  7. PLACE DOORS                                                              │
│     - For each connection, find compatible door edges                       │
│     - Create Door objects linking rooms/hallways                            │
├─────────────────────────────────────────────────────────────────────────────┤
│  8. BUILD OUTPUT                                                             │
│     - Construct FloorLayout with all placed rooms, hallways, doors          │
│     - Include metadata (seed, critical path, spawn/boss room IDs)           │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Graph Generation

### FloorGraph

```csharp
namespace ShepherdProceduralDungeons.Graph;

public sealed class FloorGraph
{
    public IReadOnlyList<RoomNode> Nodes { get; }
    public IReadOnlyList<RoomConnection> Connections { get; }
    public int StartNodeId { get; }
    public int BossNodeId { get; internal set; }
    public IReadOnlyList<int> CriticalPath { get; internal set; }
}

public sealed class RoomNode
{
    public int Id { get; }
    public int DistanceFromStart { get; internal set; }
    public bool IsOnCriticalPath { get; internal set; }
    public int ConnectionCount => Connections.Count;
    internal List<RoomConnection> Connections { get; } = new();
}

public sealed class RoomConnection
{
    public int NodeAId { get; }
    public int NodeBId { get; }
    public bool RequiresHallway { get; internal set; }
}
```

### GraphGenerator Algorithm

```csharp
namespace ShepherdProceduralDungeons.Generation;

public sealed class GraphGenerator
{
    /// <summary>
    /// Generates a connected graph with the specified number of nodes.
    /// </summary>
    public FloorGraph Generate(int roomCount, float branchingFactor, Random rng)
    {
        // 1. Create all nodes
        var nodes = Enumerable.Range(0, roomCount)
            .Select(i => new RoomNode { Id = i })
            .ToList();

        var connections = new List<RoomConnection>();

        // 2. Build spanning tree (guarantees connectivity)
        // Use randomized approach: for each node after first, connect to a random existing node
        var connectedNodes = new List<int> { 0 };
        for (int i = 1; i < roomCount; i++)
        {
            int parentIndex = rng.Next(connectedNodes.Count);
            int parentId = connectedNodes[parentIndex];

            connections.Add(new RoomConnection(parentId, i));
            connectedNodes.Add(i);
        }

        // 3. Add extra edges for loops based on branchingFactor
        // branchingFactor 0.0 = tree only, 1.0 = many extra connections
        int maxExtraEdges = (int)(roomCount * branchingFactor);
        int extraEdges = rng.Next(0, maxExtraEdges + 1);

        for (int i = 0; i < extraEdges; i++)
        {
            int a = rng.Next(roomCount);
            int b = rng.Next(roomCount);
            if (a != b && !ConnectionExists(connections, a, b))
            {
                connections.Add(new RoomConnection(a, b));
            }
        }

        // 4. Wire up node connections
        foreach (var conn in connections)
        {
            nodes[conn.NodeAId].Connections.Add(conn);
            nodes[conn.NodeBId].Connections.Add(conn);
        }

        // 5. Calculate distances from start via BFS
        CalculateDistances(nodes, startId: 0);

        return new FloorGraph
        {
            Nodes = nodes,
            Connections = connections,
            StartNodeId = 0
        };
    }

    private void CalculateDistances(List<RoomNode> nodes, int startId)
    {
        var visited = new HashSet<int>();
        var queue = new Queue<(int nodeId, int distance)>();
        queue.Enqueue((startId, 0));

        while (queue.Count > 0)
        {
            var (nodeId, distance) = queue.Dequeue();
            if (visited.Contains(nodeId)) continue;
            visited.Add(nodeId);

            nodes[nodeId].DistanceFromStart = distance;

            foreach (var conn in nodes[nodeId].Connections)
            {
                int neighborId = conn.NodeAId == nodeId ? conn.NodeBId : conn.NodeAId;
                if (!visited.Contains(neighborId))
                {
                    queue.Enqueue((neighborId, distance + 1));
                }
            }
        }
    }
}
```

### RoomTypeAssigner Algorithm

```csharp
namespace ShepherdProceduralDungeons.Generation;

public sealed class RoomTypeAssigner<TRoomType> where TRoomType : Enum
{
    public void AssignTypes(
        FloorGraph graph,
        TRoomType spawnType,
        TRoomType bossType,
        TRoomType defaultType,
        IReadOnlyList<(TRoomType type, int count)> roomRequirements,
        IReadOnlyList<IConstraint<TRoomType>> constraints,
        Random rng,
        out Dictionary<int, TRoomType> assignments)
    {
        assignments = new Dictionary<int, TRoomType>();

        // 1. Assign spawn to start node
        assignments[graph.StartNodeId] = spawnType;

        // 2. Find boss location: farthest node that satisfies boss constraints
        var bossConstraints = constraints.Where(c => c.TargetRoomType.Equals(bossType)).ToList();
        var validBossNodes = graph.Nodes
            .Where(n => n.Id != graph.StartNodeId)
            .Where(n => bossConstraints.All(c => c.IsValid(n, graph, assignments)))
            .OrderByDescending(n => n.DistanceFromStart)
            .ToList();

        if (validBossNodes.Count == 0)
            throw new ConstraintViolationException($"No valid location for {bossType}");

        var bossNode = validBossNodes.First();
        assignments[bossNode.Id] = bossType;
        graph.BossNodeId = bossNode.Id;

        // 3. Calculate critical path via BFS from start to boss
        graph.CriticalPath = FindPath(graph, graph.StartNodeId, bossNode.Id);
        foreach (int nodeId in graph.CriticalPath)
        {
            graph.Nodes.First(n => n.Id == nodeId).IsOnCriticalPath = true;
        }

        // 4. Assign required room types based on constraints
        foreach (var (roomType, count) in roomRequirements.OrderByDescending(r => GetConstraintPriority(r.type, constraints)))
        {
            if (roomType.Equals(spawnType) || roomType.Equals(bossType))
                continue; // Already handled

            var typeConstraints = constraints.Where(c => c.TargetRoomType.Equals(roomType)).ToList();
            int assigned = 0;

            var candidates = graph.Nodes
                .Where(n => !assignments.ContainsKey(n.Id))
                .Where(n => typeConstraints.All(c => c.IsValid(n, graph, assignments)))
                .ToList();

            // Shuffle candidates
            Shuffle(candidates, rng);

            foreach (var node in candidates)
            {
                if (assigned >= count) break;
                assignments[node.Id] = roomType;
                assigned++;
            }

            if (assigned < count)
                throw new ConstraintViolationException($"Could only place {assigned}/{count} rooms of type {roomType}");
        }

        // 5. Fill remaining with default type
        foreach (var node in graph.Nodes)
        {
            if (!assignments.ContainsKey(node.Id))
            {
                assignments[node.Id] = defaultType;
            }
        }
    }

    private IReadOnlyList<int> FindPath(FloorGraph graph, int fromId, int toId)
    {
        // BFS to find shortest path
        var visited = new Dictionary<int, int>(); // nodeId -> previousNodeId
        var queue = new Queue<int>();
        queue.Enqueue(fromId);
        visited[fromId] = -1;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            if (current == toId)
            {
                // Reconstruct path
                var path = new List<int>();
                int node = toId;
                while (node != -1)
                {
                    path.Add(node);
                    node = visited[node];
                }
                path.Reverse();
                return path;
            }

            var currentNode = graph.Nodes.First(n => n.Id == current);
            foreach (var conn in currentNode.Connections)
            {
                int neighborId = conn.NodeAId == current ? conn.NodeBId : conn.NodeAId;
                if (!visited.ContainsKey(neighborId))
                {
                    visited[neighborId] = current;
                    queue.Enqueue(neighborId);
                }
            }
        }

        throw new InvalidOperationException("No path found - graph is disconnected");
    }
}
```

---

## Spatial Solver

### ISpatialSolver Interface

```csharp
namespace ShepherdProceduralDungeons.Generation;

public interface ISpatialSolver<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Places all rooms in 2D space.
    /// </summary>
    /// <param name="graph">The floor graph with room types assigned.</param>
    /// <param name="assignments">Room type for each node.</param>
    /// <param name="templates">Available templates keyed by room type.</param>
    /// <param name="hallwayMode">How to handle non-adjacent rooms.</param>
    /// <param name="rng">Random number generator.</param>
    /// <returns>List of placed rooms with positions.</returns>
    IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng);
}
```

### IncrementalSolver Algorithm

```csharp
namespace ShepherdProceduralDungeons.Generation;

public sealed class IncrementalSolver<TRoomType> : ISpatialSolver<TRoomType> where TRoomType : Enum
{
    public IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng)
    {
        var placedRooms = new Dictionary<int, PlacedRoom<TRoomType>>();
        var occupiedCells = new HashSet<Cell>();

        // Track which connections need hallways
        var hallwayConnections = new HashSet<(int, int)>();

        // 1. Place start room at origin
        var startNode = graph.Nodes.First(n => n.Id == graph.StartNodeId);
        var startTemplate = SelectTemplate(assignments[startNode.Id], templates, rng);
        var startRoom = PlaceRoom(startNode.Id, startTemplate, new Cell(0, 0), assignments[startNode.Id]);
        placedRooms[startNode.Id] = startRoom;
        AddOccupiedCells(startRoom, occupiedCells);

        // 2. BFS from start, placing connected rooms
        var queue = new Queue<int>();
        var visited = new HashSet<int> { graph.StartNodeId };
        queue.Enqueue(graph.StartNodeId);

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            var currentRoom = placedRooms[currentId];
            var currentNode = graph.Nodes.First(n => n.Id == currentId);

            foreach (var conn in currentNode.Connections)
            {
                int neighborId = conn.NodeAId == currentId ? conn.NodeBId : conn.NodeAId;
                if (visited.Contains(neighborId)) continue;
                visited.Add(neighborId);

                var neighborTemplate = SelectTemplate(assignments[neighborId], templates, rng);

                // Try to place adjacent to current room
                var placement = TryPlaceAdjacent(currentRoom, neighborTemplate, occupiedCells, rng);

                if (placement.HasValue)
                {
                    var neighborRoom = PlaceRoom(neighborId, neighborTemplate, placement.Value, assignments[neighborId]);
                    placedRooms[neighborId] = neighborRoom;
                    AddOccupiedCells(neighborRoom, occupiedCells);
                }
                else if (hallwayMode != HallwayMode.None)
                {
                    // Place nearby with gap for hallway
                    var nearbyPlacement = PlaceNearby(currentRoom, neighborTemplate, occupiedCells, rng);
                    var neighborRoom = PlaceRoom(neighborId, neighborTemplate, nearbyPlacement, assignments[neighborId]);
                    placedRooms[neighborId] = neighborRoom;
                    AddOccupiedCells(neighborRoom, occupiedCells);

                    // Mark connection for hallway generation
                    conn.RequiresHallway = true;
                    hallwayConnections.Add((Math.Min(currentId, neighborId), Math.Max(currentId, neighborId)));
                }
                else
                {
                    throw new SpatialPlacementException($"Cannot place room {neighborId} adjacent to room {currentId} and hallways are disabled");
                }

                queue.Enqueue(neighborId);
            }
        }

        // Force hallways for all connections if mode is Always
        if (hallwayMode == HallwayMode.Always)
        {
            foreach (var conn in graph.Connections)
            {
                conn.RequiresHallway = true;
            }
        }

        return placedRooms.Values.ToList();
    }

    private Cell? TryPlaceAdjacent(PlacedRoom<TRoomType> existingRoom, RoomTemplate<TRoomType> template, HashSet<Cell> occupied, Random rng)
    {
        // Get all exterior edges of existing room
        var existingExterior = existingRoom.GetExteriorEdgesWorld();

        // Get all door edges of template
        var templateDoorEdges = template.DoorEdges.SelectMany(kvp =>
            Enum.GetValues<Edge>()
                .Where(e => e != Edge.None && e != Edge.All && kvp.Value.HasFlag(e))
                .Select(e => (Cell: kvp.Key, Edge: e)))
            .ToList();

        // Shuffle for randomness
        Shuffle(templateDoorEdges, rng);

        foreach (var (templateCell, templateEdge) in templateDoorEdges)
        {
            Edge requiredExistingEdge = templateEdge.Opposite();

            // Find compatible edges on existing room
            var compatibleEdges = existingExterior
                .Where(e => e.Edge == requiredExistingEdge && existingRoom.Template.CanPlaceDoor(e.LocalCell, e.Edge))
                .ToList();

            Shuffle(compatibleEdges, rng);

            foreach (var existingEdge in compatibleEdges)
            {
                // Calculate template position to align these edges
                Cell templateAnchor = CalculateAnchorForEdgeAlignment(
                    existingEdge.WorldCell, existingEdge.Edge,
                    templateCell, templateEdge);

                // Check if template fits without overlap
                if (TemplateFits(template, templateAnchor, occupied))
                {
                    return templateAnchor;
                }
            }
        }

        return null; // No valid adjacent placement found
    }

    private Cell PlaceNearby(PlacedRoom<TRoomType> existingRoom, RoomTemplate<TRoomType> template, HashSet<Cell> occupied, Random rng)
    {
        // Search in expanding radius for valid placement
        // Leave gap of 1-3 cells for hallway
        int maxRadius = 20;

        for (int radius = 2; radius <= maxRadius; radius++)
        {
            var candidates = new List<Cell>();

            // Check cells at this radius
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;

                    Cell anchor = new Cell(existingRoom.Position.X + dx, existingRoom.Position.Y + dy);
                    if (TemplateFits(template, anchor, occupied))
                    {
                        candidates.Add(anchor);
                    }
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[rng.Next(candidates.Count)];
            }
        }

        throw new SpatialPlacementException("Could not find any valid placement for room");
    }

    private bool TemplateFits(RoomTemplate<TRoomType> template, Cell anchor, HashSet<Cell> occupied)
    {
        foreach (var cell in template.Cells)
        {
            Cell world = new Cell(anchor.X + cell.X, anchor.Y + cell.Y);
            if (occupied.Contains(world))
                return false;
        }
        return true;
    }

    private RoomTemplate<TRoomType> SelectTemplate(TRoomType roomType, IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates, Random rng)
    {
        if (!templates.TryGetValue(roomType, out var available) || available.Count == 0)
            throw new InvalidConfigurationException($"No templates registered for room type {roomType}");

        return available[rng.Next(available.Count)];
    }
}
```

### PlacedRoom

```csharp
namespace ShepherdProceduralDungeons.Layout;

public sealed class PlacedRoom<TRoomType> where TRoomType : Enum
{
    public required int NodeId { get; init; }
    public required TRoomType RoomType { get; init; }
    public required RoomTemplate<TRoomType> Template { get; init; }
    public required Cell Position { get; init; }  // Anchor position in world coords

    /// <summary>Gets all cells this room occupies in world coordinates.</summary>
    public IEnumerable<Cell> GetWorldCells() =>
        Template.Cells.Select(c => new Cell(Position.X + c.X, Position.Y + c.Y));

    /// <summary>Gets all exterior edges in world coordinates.</summary>
    public IEnumerable<(Cell LocalCell, Cell WorldCell, Edge Edge)> GetExteriorEdgesWorld();
}
```

---

## Hallway Generation

### HallwayGenerator Algorithm

```csharp
namespace ShepherdProceduralDungeons.Generation;

public sealed class HallwayGenerator<TRoomType> where TRoomType : Enum
{
    public IReadOnlyList<Hallway> Generate(
        IReadOnlyList<PlacedRoom<TRoomType>> rooms,
        FloorGraph graph,
        HashSet<Cell> occupiedCells,
        Random rng)
    {
        var hallways = new List<Hallway>();
        int hallwayId = 0;

        foreach (var conn in graph.Connections.Where(c => c.RequiresHallway))
        {
            var roomA = rooms.First(r => r.NodeId == conn.NodeAId);
            var roomB = rooms.First(r => r.NodeId == conn.NodeBId);

            // Find best door positions on each room
            var (doorA, doorB) = FindBestDoorPair(roomA, roomB, rng);

            // Pathfind between doors
            var path = FindHallwayPath(doorA.WorldCell, doorA.Edge, doorB.WorldCell, doorB.Edge, occupiedCells);

            // Convert path to hallway segments
            var segments = PathToSegments(path);

            var hallway = new Hallway
            {
                Id = hallwayId++,
                Segments = segments,
                DoorA = new Door
                {
                    Position = doorA.WorldCell,
                    Edge = doorA.Edge,
                    ConnectsToRoomId = roomA.NodeId
                },
                DoorB = new Door
                {
                    Position = doorB.WorldCell,
                    Edge = doorB.Edge,
                    ConnectsToRoomId = roomB.NodeId
                }
            };

            hallways.Add(hallway);

            // Mark hallway cells as occupied
            foreach (var segment in segments)
            {
                foreach (var cell in segment.GetCells())
                {
                    occupiedCells.Add(cell);
                }
            }
        }

        return hallways;
    }

    private IReadOnlyList<Cell> FindHallwayPath(Cell startCell, Edge startEdge, Cell endCell, Edge endEdge, HashSet<Cell> occupied)
    {
        // Get the cell outside each door
        Cell start = GetAdjacentCell(startCell, startEdge);
        Cell end = GetAdjacentCell(endCell, endEdge);

        // A* pathfinding avoiding occupied cells
        var path = AStar(start, end, occupied);

        if (path == null)
            throw new SpatialPlacementException($"Cannot find hallway path from {start} to {end}");

        return path;
    }

    private IReadOnlyList<Cell>? AStar(Cell start, Cell end, HashSet<Cell> occupied)
    {
        var openSet = new PriorityQueue<Cell, int>();
        var cameFrom = new Dictionary<Cell, Cell>();
        var gScore = new Dictionary<Cell, int> { [start] = 0 };

        openSet.Enqueue(start, ManhattanDistance(start, end));

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == end)
            {
                // Reconstruct path
                var path = new List<Cell>();
                var node = end;
                while (cameFrom.ContainsKey(node))
                {
                    path.Add(node);
                    node = cameFrom[node];
                }
                path.Add(start);
                path.Reverse();
                return path;
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                if (occupied.Contains(neighbor) && neighbor != end)
                    continue;

                int tentativeG = gScore[current] + 1;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + ManhattanDistance(neighbor, end);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        return null; // No path found
    }

    private IReadOnlyList<HallwaySegment> PathToSegments(IReadOnlyList<Cell> path)
    {
        // Combine consecutive cells going same direction into segments
        var segments = new List<HallwaySegment>();

        if (path.Count < 2) return segments;

        Cell segmentStart = path[0];
        Cell? lastDir = null;

        for (int i = 1; i < path.Count; i++)
        {
            Cell current = path[i];
            Cell prev = path[i - 1];
            Cell dir = new Cell(current.X - prev.X, current.Y - prev.Y);

            if (lastDir.HasValue && dir != lastDir.Value)
            {
                // Direction changed, end current segment
                segments.Add(new HallwaySegment
                {
                    Start = segmentStart,
                    End = prev
                });
                segmentStart = prev;
            }

            lastDir = dir;
        }

        // Add final segment
        segments.Add(new HallwaySegment
        {
            Start = segmentStart,
            End = path[^1]
        });

        return segments;
    }
}
```

### Hallway Types

```csharp
namespace ShepherdProceduralDungeons.Layout;

public sealed class Hallway
{
    public required int Id { get; init; }
    public required IReadOnlyList<HallwaySegment> Segments { get; init; }
    public required Door DoorA { get; init; }
    public required Door DoorB { get; init; }
}

public sealed class HallwaySegment
{
    public required Cell Start { get; init; }
    public required Cell End { get; init; }

    public bool IsHorizontal => Start.Y == End.Y;
    public bool IsVertical => Start.X == End.X;

    public IEnumerable<Cell> GetCells()
    {
        int dx = Math.Sign(End.X - Start.X);
        int dy = Math.Sign(End.Y - Start.Y);

        Cell current = Start;
        while (current != End)
        {
            yield return current;
            current = new Cell(current.X + dx, current.Y + dy);
        }
        yield return End;
    }
}

public sealed class Door
{
    public required Cell Position { get; init; }
    public required Edge Edge { get; init; }
    public int? ConnectsToRoomId { get; init; }
    public int? ConnectsToHallwayId { get; init; }
}
```

---

## Seeding Strategy

### Implementation Details

All randomization flows through `System.Random` instances derived from the master seed.

```csharp
public sealed class FloorGenerator<TRoomType> where TRoomType : Enum
{
    public FloorLayout<TRoomType> Generate(FloorConfig<TRoomType> config)
    {
        // Master RNG from seed
        var masterRng = new Random(config.Seed);

        // Derive child seeds for each phase (ensures phase order doesn't affect other phases)
        int graphSeed = masterRng.Next();
        int typeSeed = masterRng.Next();
        int templateSeed = masterRng.Next();
        int spatialSeed = masterRng.Next();
        int hallwaySeed = masterRng.Next();

        // Each phase gets its own RNG
        var graphRng = new Random(graphSeed);
        var typeRng = new Random(typeSeed);
        var templateRng = new Random(templateSeed);
        var spatialRng = new Random(spatialSeed);
        var hallwayRng = new Random(hallwaySeed);

        // ... generation phases using respective RNGs ...
    }
}
```

### Determinism Requirements

For identical output given same seed + config:
- No `DateTime.Now` or `Guid.NewGuid()` usage
- No LINQ operations that depend on hash ordering (use ordered collections)
- No parallel operations that could reorder
- All `Random` calls must happen in deterministic order

---

## Public API

### FloorConfig

```csharp
namespace ShepherdProceduralDungeons.Configuration;

public sealed class FloorConfig<TRoomType> where TRoomType : Enum
{
    /// <summary>Seed for deterministic generation.</summary>
    public required int Seed { get; init; }

    /// <summary>Total number of rooms to generate.</summary>
    public required int RoomCount { get; init; }

    /// <summary>Room type for the starting room.</summary>
    public required TRoomType SpawnRoomType { get; init; }

    /// <summary>Room type for the boss room.</summary>
    public required TRoomType BossRoomType { get; init; }

    /// <summary>Default room type for rooms without specific assignments.</summary>
    public required TRoomType DefaultRoomType { get; init; }

    /// <summary>How many rooms of each type to generate (beyond spawn/boss).</summary>
    public IReadOnlyList<(TRoomType Type, int Count)> RoomRequirements { get; init; } = Array.Empty<(TRoomType, int)>();

    /// <summary>Constraints for room type placement.</summary>
    public IReadOnlyList<IConstraint<TRoomType>> Constraints { get; init; } = Array.Empty<IConstraint<TRoomType>>();

    /// <summary>Available room templates.</summary>
    public required IReadOnlyList<RoomTemplate<TRoomType>> Templates { get; init; }

    /// <summary>0.0 = tree structure, 1.0 = highly connected with loops.</summary>
    public float BranchingFactor { get; init; } = 0.3f;

    /// <summary>How to handle non-adjacent room connections.</summary>
    public HallwayMode HallwayMode { get; init; } = HallwayMode.AsNeeded;
}
```

### FloorLayout (Output)

```csharp
namespace ShepherdProceduralDungeons.Layout;

public sealed class FloorLayout<TRoomType> where TRoomType : Enum
{
    /// <summary>All placed rooms with positions.</summary>
    public required IReadOnlyList<PlacedRoom<TRoomType>> Rooms { get; init; }

    /// <summary>Generated hallways between rooms.</summary>
    public required IReadOnlyList<Hallway> Hallways { get; init; }

    /// <summary>All doors in the floor.</summary>
    public required IReadOnlyList<Door> Doors { get; init; }

    /// <summary>The seed used to generate this floor.</summary>
    public required int Seed { get; init; }

    /// <summary>Node IDs forming the critical path from spawn to boss.</summary>
    public required IReadOnlyList<int> CriticalPath { get; init; }

    /// <summary>ID of the spawn room.</summary>
    public required int SpawnRoomId { get; init; }

    /// <summary>ID of the boss room.</summary>
    public required int BossRoomId { get; init; }

    /// <summary>Gets a room by its node ID.</summary>
    public PlacedRoom<TRoomType>? GetRoom(int nodeId) => Rooms.FirstOrDefault(r => r.NodeId == nodeId);

    /// <summary>Gets all cells occupied by rooms (not hallways).</summary>
    public IEnumerable<Cell> GetAllRoomCells() => Rooms.SelectMany(r => r.GetWorldCells());

    /// <summary>Gets all cells occupied by hallways.</summary>
    public IEnumerable<Cell> GetAllHallwayCells() => Hallways.SelectMany(h => h.Segments.SelectMany(s => s.GetCells()));

    /// <summary>Gets bounding box of entire floor.</summary>
    public (Cell Min, Cell Max) GetBounds();
}
```

### FloorGenerator (Entry Point)

```csharp
namespace ShepherdProceduralDungeons;

public sealed class FloorGenerator<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Generates a floor layout from the given configuration.
    /// </summary>
    /// <param name="config">Generation configuration.</param>
    /// <returns>The generated floor layout.</returns>
    /// <exception cref="InvalidConfigurationException">Config is invalid.</exception>
    /// <exception cref="ConstraintViolationException">Constraints cannot be satisfied.</exception>
    /// <exception cref="SpatialPlacementException">Rooms cannot be placed.</exception>
    public FloorLayout<TRoomType> Generate(FloorConfig<TRoomType> config);
}
```

### Full Usage Example

```csharp
// 1. Define your room types
public enum RoomType
{
    Spawn,
    Boss,
    Combat,
    Shop,
    Treasure,
    Secret
}

// 2. Create templates
var templates = new List<RoomTemplate<RoomType>>
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn-room")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnSides(Edge.All)
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss-arena")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(3, 2)
        .WithId("combat-small")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("shop")
        .ForRoomTypes(RoomType.Shop)
        .WithDoorsOnSides(Edge.South)
        .Build(),

    RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
        .WithId("treasure")
        .ForRoomTypes(RoomType.Treasure)
        .WithDoorsOnSides(Edge.All)
        .Build(),

    RoomTemplateBuilder<RoomType>.LShape(4, 3, 2, 1, Corner.TopRight)
        .WithId("secret-l")
        .ForRoomTypes(RoomType.Secret)
        .WithDoorsOnAllExteriorEdges()
        .Build()
};

// 3. Define constraints
var constraints = new List<IConstraint<RoomType>>
{
    new MinDistanceFromStartConstraint<RoomType>(RoomType.Boss, minDistance: 5),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Boss),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Shop),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Treasure),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Treasure),
    new MaxPerFloorConstraint<RoomType>(RoomType.Shop, maxCount: 1),
    new MaxPerFloorConstraint<RoomType>(RoomType.Treasure, maxCount: 2),
    new NotOnCriticalPathConstraint<RoomType>(RoomType.Secret),
    new MustBeDeadEndConstraint<RoomType>(RoomType.Secret),
    new MaxPerFloorConstraint<RoomType>(RoomType.Secret, maxCount: 1)
};

// 4. Create config
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 12,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    RoomRequirements = new[]
    {
        (RoomType.Shop, 1),
        (RoomType.Treasure, 2),
        (RoomType.Secret, 1)
    },
    Constraints = constraints,
    Templates = templates,
    BranchingFactor = 0.2f,
    HallwayMode = HallwayMode.AsNeeded
};

// 5. Generate
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// 6. Use the output
Console.WriteLine($"Generated floor with seed {layout.Seed}");
Console.WriteLine($"Spawn room: {layout.SpawnRoomId}");
Console.WriteLine($"Boss room: {layout.BossRoomId}");
Console.WriteLine($"Critical path: {string.Join(" -> ", layout.CriticalPath)}");
Console.WriteLine($"Total rooms: {layout.Rooms.Count}");
Console.WriteLine($"Total hallways: {layout.Hallways.Count}");

foreach (var room in layout.Rooms)
{
    Console.WriteLine($"  Room {room.NodeId}: {room.RoomType} at ({room.Position.X}, {room.Position.Y}) using template '{room.Template.Id}'");
}
```

---

## Error Handling

All exceptions inherit from `GenerationException`:

```csharp
namespace ShepherdProceduralDungeons.Exceptions;

public class GenerationException : Exception
{
    public GenerationException(string message) : base(message) { }
    public GenerationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when configuration is invalid before generation starts.</summary>
public class InvalidConfigurationException : GenerationException
{
    public InvalidConfigurationException(string message) : base(message) { }
}

/// <summary>Thrown when room type constraints cannot be satisfied.</summary>
public class ConstraintViolationException : GenerationException
{
    public string? ConstraintType { get; init; }
    public ConstraintViolationException(string message) : base(message) { }
}

/// <summary>Thrown when rooms cannot be placed in 2D space.</summary>
public class SpatialPlacementException : GenerationException
{
    public int? RoomId { get; init; }
    public SpatialPlacementException(string message) : base(message) { }
}
```

### Validation Checks

These validations happen before generation begins:

```csharp
private void ValidateConfig(FloorConfig<TRoomType> config)
{
    if (config.RoomCount < 2)
        throw new InvalidConfigurationException("RoomCount must be at least 2 (spawn + boss)");

    if (config.RoomCount < 1 + 1 + config.RoomRequirements.Sum(r => r.Count))
        throw new InvalidConfigurationException("RoomCount is too small for spawn + boss + all required rooms");

    // Check templates exist for all required types
    var requiredTypes = new HashSet<TRoomType> { config.SpawnRoomType, config.BossRoomType, config.DefaultRoomType };
    foreach (var req in config.RoomRequirements)
        requiredTypes.Add(req.Type);

    var availableTypes = config.Templates.SelectMany(t => t.ValidRoomTypes).ToHashSet();

    foreach (var required in requiredTypes)
    {
        if (!availableTypes.Contains(required))
            throw new InvalidConfigurationException($"No template available for room type {required}");
    }

    if (config.BranchingFactor < 0 || config.BranchingFactor > 1)
        throw new InvalidConfigurationException("BranchingFactor must be between 0.0 and 1.0");
}
```

---

## Testing Strategy

### Unit Tests

**GraphGeneratorTests.cs**
- Graph is connected (all nodes reachable from start)
- Node count matches request
- DistanceFromStart is correctly calculated
- Same seed produces identical graph

**ConstraintTests.cs**
- Each constraint type correctly accepts/rejects nodes
- Constraints compose correctly
- Custom constraint callback works

**SpatialSolverTests.cs**
- Rooms don't overlap
- Adjacent rooms share wall
- Rooms with hallway mode have valid door positions

**HallwayTests.cs**
- Hallway connects correct rooms
- Hallway doesn't pass through rooms
- Path exists for all required hallways

**SeedDeterminismTests.cs**
- Same seed + config = identical output
- Different seeds = different output
- Changing config changes output

### Integration Tests

**IntegrationTests.cs**
- Full generation with various configs
- Edge cases: 2 rooms, 50 rooms
- All room types get placed
- Critical path is valid

### Test Helpers

```csharp
public static class TestHelpers
{
    public static FloorConfig<RoomType> CreateSimpleConfig(int seed = 12345, int roomCount = 10)
    {
        return new FloorConfig<RoomType>
        {
            Seed = seed,
            RoomCount = roomCount,
            SpawnRoomType = RoomType.Spawn,
            BossRoomType = RoomType.Boss,
            DefaultRoomType = RoomType.Combat,
            Templates = CreateDefaultTemplates(),
            Constraints = new List<IConstraint<RoomType>>()
        };
    }

    public static List<RoomTemplate<RoomType>> CreateDefaultTemplates()
    {
        return new List<RoomTemplate<RoomType>>
        {
            RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
                .WithId("default")
                .ForRoomTypes(RoomType.Spawn, RoomType.Boss, RoomType.Combat, RoomType.Shop, RoomType.Treasure, RoomType.Secret)
                .WithDoorsOnAllExteriorEdges()
                .Build()
        };
    }
}
```

---

## Implementation Order

Recommended order for incremental development:

### Phase 1: Core Types
1. `Cell`, `Edge`, `EdgeExtensions`
2. `HallwayMode` enum
3. All exception classes

### Phase 2: Templates
4. `RoomTemplate<TRoomType>`
5. `RoomTemplateBuilder<TRoomType>`
6. Template builder tests

### Phase 3: Graph
7. `RoomNode`, `RoomConnection`
8. `FloorGraph`
9. `GraphGenerator`
10. Graph generator tests

### Phase 4: Constraints
11. `IConstraint<TRoomType>` interface
12. All built-in constraint classes
13. `RoomTypeAssigner<TRoomType>`
14. Constraint tests

### Phase 5: Spatial
15. `PlacedRoom<TRoomType>`
16. `ISpatialSolver<TRoomType>`
17. `IncrementalSolver<TRoomType>`
18. Spatial solver tests

### Phase 6: Hallways
19. `Door`, `HallwaySegment`, `Hallway`
20. `HallwayGenerator<TRoomType>`
21. Hallway tests

### Phase 7: Assembly
22. `FloorConfig<TRoomType>`
23. `FloorLayout<TRoomType>`
24. `FloorGenerator<TRoomType>`
25. Integration tests
26. Determinism tests

### Phase 8: Polish
27. XML documentation on all public members
28. README with usage examples
29. NuGet package configuration

---

## Dependencies

### Required
- .NET 8.0+

### Optional (consider adding)
- None required for core functionality

### Test Project
- xUnit
- FluentAssertions (optional, for readable assertions)

---

## Project Files

### ShepherdProceduralDungeons.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>

    <!-- NuGet package info -->
    <PackageId>ShepherdProceduralDungeons</PackageId>
    <Version>1.0.0</Version>
    <Authors>YourName</Authors>
    <Description>Procedural dungeon generation library for roguelike games</Description>
    <PackageTags>procedural;dungeon;roguelike;gamedev;monogame</PackageTags>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>

    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

</Project>
```

### ShepherdProceduralDungeons.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\ShepherdProceduralDungeons\ShepherdProceduralDungeons.csproj" />
  </ItemGroup>

</Project>
```

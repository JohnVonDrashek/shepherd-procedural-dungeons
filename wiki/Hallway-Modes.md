# Hallway Modes

The `HallwayMode` enum controls how the library handles connections between non-adjacent rooms.

## Overview

When rooms are placed in 2D space, they may not always be able to touch directly. Hallway modes determine what happens in this situation.

## Modes

### HallwayMode.None

**Behavior:** Rooms must share a wall. Throws `SpatialPlacementException` if impossible.

**Use when:**
- You want compact, tightly-packed dungeons
- All your templates are small and similar sizes
- You prefer direct room-to-room connections
- Performance is critical (fastest mode)

**Example:**
```csharp
var config = new FloorConfig<RoomType>
{
    // ...
    HallwayMode = HallwayMode.None
};
```

**Limitations:**
- May fail with large or varied template sizes
- Less flexible room placement
- Can't handle complex graph topologies

### HallwayMode.AsNeeded

**Behavior:** Generate hallways only when rooms cannot touch directly.

**Use when:**
- You want flexibility without always using hallways
- Templates vary in size
- You want a mix of direct connections and hallways
- This is the **recommended default**

**Example:**
```csharp
var config = new FloorConfig<RoomType>
{
    // ...
    HallwayMode = HallwayMode.AsNeeded
};
```

**Benefits:**
- Best balance of flexibility and simplicity
- Rooms connect directly when possible
- Hallways only when necessary
- Good performance

### HallwayMode.Always

**Behavior:** Always generate hallways between all connected rooms.

**Use when:**
- You want consistent hallway-based navigation
- All connections should be explicit corridors
- You're building a maze-like dungeon
- Maximum placement flexibility needed

**Example:**
```csharp
var config = new FloorConfig<RoomType>
{
    // ...
    HallwayMode = HallwayMode.Always
};
```

**Benefits:**
- Maximum placement flexibility
- Consistent hallway-based design
- Easier to render (all connections are hallways)

**Trade-offs:**
- More hallways to generate and render
- Slightly slower generation
- May feel less "organic"

## Comparison

| Mode | Direct Connections | Hallways | Flexibility | Performance | Use Case |
|------|-------------------|----------|-------------|-------------|----------|
| `None` | Always | Never | Low | Fastest | Small, uniform templates |
| `AsNeeded` | When possible | When needed | High | Fast | **Recommended** |
| `Always` | Never | Always | Highest | Slower | Maze-like, explicit corridors |

## Visual Examples

### HallwayMode.None

```
[Room A][Room B]  ← Direct connection
[Room C]          ← Must be adjacent
```

If rooms can't be adjacent, generation fails.

### HallwayMode.AsNeeded

```
[Room A][Room B]  ← Direct connection
[Room C]....[Room D]  ← Hallway when needed
```

Mix of direct connections and hallways.

### HallwayMode.Always

```
[Room A]....[Room B]  ← Always hallways
[Room C]....[Room D]
```

All connections use hallways.

## Choosing a Mode

### Start with AsNeeded

```csharp
HallwayMode = HallwayMode.AsNeeded  // Good default
```

This works for most cases and provides good flexibility.

### Use None for Performance

If you have small, uniform templates and need maximum performance:

```csharp
HallwayMode = HallwayMode.None
```

### Use Always for Mazes

If you want a maze-like feel with explicit corridors:

```csharp
HallwayMode = HallwayMode.Always
```

## Hallway Generation

When hallways are generated, they use A* pathfinding to find the shortest path between room doors, avoiding occupied cells.

**Hallway properties:**
- Made of straight segments (horizontal/vertical)
- Connect room doors
- Avoid overlapping with rooms
- Can turn corners as needed

**Example:**
```csharp
foreach (var hallway in layout.Hallways)
{
    Console.WriteLine($"Hallway {hallway.Id}:");
    Console.WriteLine($"  Connects room {hallway.DoorA.ConnectsToRoomId} to {hallway.DoorB.ConnectsToRoomId}");
    
    foreach (var segment in hallway.Segments)
    {
        Console.WriteLine($"  Segment: ({segment.Start.X}, {segment.Start.Y}) to ({segment.End.X}, {segment.End.Y})");
    }
}
```

## Troubleshooting

### None Mode Fails

**Error:**
```
SpatialPlacementException: Cannot place room 5 adjacent to room 3 and hallways are disabled
```

**Solution:**
```csharp
// Switch to AsNeeded or Always
HallwayMode = HallwayMode.AsNeeded
```

### Too Many Hallways

If `Always` mode produces too many hallways:

**Solution:**
```csharp
// Use AsNeeded for fewer hallways
HallwayMode = HallwayMode.AsNeeded
```

### Hallway Path Not Found

**Error:**
```
SpatialPlacementException: Cannot find hallway path from (5, 10) to (20, 15)
```

**Solutions:**
1. Use smaller templates
2. Reduce `RoomCount`
3. Ensure rooms aren't too crowded

## Best Practices

1. **Start with AsNeeded** - It's the most flexible default
2. **Test with your templates** - Some template sizes work better with certain modes
3. **Consider rendering** - `Always` mode means more hallways to render
4. **Performance matters?** - Use `None` if possible, otherwise `AsNeeded`

## Next Steps

- **[Configuration](Configuration)** - How to set hallway mode
- **[Working with Output](Working-with-Output)** - Using hallways in your game
- **[Examples](Examples)** - See hallway modes in action


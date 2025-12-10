# ShepherdProceduralDungeons Wiki

Welcome to the ShepherdProceduralDungeons wiki! This library helps you generate procedural dungeon floors for roguelike and dungeon crawler games.

> **Note:** This wiki is automatically synced from the main repository. See the [GitHub repository](https://github.com/JohnVonDrashek/shepherd-procedural-dungeons) for the source.

## What This Library Does

ShepherdProceduralDungeons generates:
- **Graph topology** - Which rooms connect to which
- **Room type assignments** - Boss rooms, treasure rooms, shops, etc.
- **Spatial layouts** - 2D grid positions for all rooms
- **Hallways** - Optional corridors connecting non-adjacent rooms
- **Doors** - Door placements between connected rooms

All generation is **deterministic** - same seed + config = identical output.

## Quick Links

### Getting Started
- **[Getting Started](Getting-Started)** - Step-by-step tutorial to generate your first dungeon
- **[Core Concepts](Core-Concepts)** - Understanding the fundamental ideas

### Main Topics
- **[Room Templates](Room-Templates)** - Define room shapes and door placements
- **[Constraints](Constraints)** - Control where special rooms are placed
- **[Configuration](Configuration)** - Complete guide to `FloorConfig`
- **[Hallway Modes](Hallway-Modes)** - Understanding hallway generation options
- **[Working with Output](Working-with-Output)** - Using the generated `FloorLayout`

### Examples & Help
- **[Examples](Examples)** - Complete, runnable examples for common scenarios
- **[Troubleshooting](Troubleshooting)** - Common issues and solutions
- **[Best Practices](Best-Practices)** - Tips and patterns for effective dungeon generation

### Advanced
- **[Advanced Topics](Advanced-Topics)** - Custom solvers, extensibility, performance
- **[API Reference](API-Reference)** - Quick reference for main classes

## Installation

```bash
dotnet add package ShepherdProceduralDungeons
```

Or add to your `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="ShepherdProceduralDungeons" Version="1.0.1" />
</ItemGroup>
```

## Quick Example

```csharp
using ShepherdProceduralDungeons;
using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

// 1. Define your room types
public enum RoomType
{
    Spawn, Boss, Combat, Shop, Treasure
}

// 2. Create templates
var templates = new List<RoomTemplate<RoomType>>
{
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("spawn")
        .ForRoomTypes(RoomType.Spawn)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    RoomTemplateBuilder<RoomType>.Rectangle(6, 6)
        .WithId("boss")
        .ForRoomTypes(RoomType.Boss)
        .WithDoorsOnSides(Edge.South)
        .Build()
};

// 3. Create config
var config = new FloorConfig<RoomType>
{
    Seed = 12345,
    RoomCount = 10,
    SpawnRoomType = RoomType.Spawn,
    BossRoomType = RoomType.Boss,
    DefaultRoomType = RoomType.Combat,
    Templates = templates
};

// 4. Generate
var generator = new FloorGenerator<RoomType>();
var layout = generator.Generate(config);

// 5. Use the layout
foreach (var room in layout.Rooms)
{
    Console.WriteLine($"Room {room.NodeId}: {room.RoomType} at ({room.Position.X}, {room.Position.Y})");
}
```

## What This Library Does NOT Do

- ❌ Rendering (your responsibility)
- ❌ Room interior content (enemies, obstacles, tiles)
- ❌ Physics or collision
- ❌ Multi-floor/stairs (out of scope for v1)

## Next Steps

1. **[Getting Started](Getting-Started)** - Follow the tutorial to create your first dungeon
2. **[Room Templates](Room-Templates)** - Learn how to define room shapes
3. **[Constraints](Constraints)** - Control special room placement
4. **[Examples](Examples)** - See complete examples for your use case

## Resources

- [GitHub Repository](https://github.com/JohnVonDrashek/shepherd-procedural-dungeons)
- [NuGet Package](https://www.nuget.org/packages/ShepherdProceduralDungeons)
- [Design Document](../DESIGN.md) - Complete technical documentation

## License

MIT License - see [LICENSE](../LICENSE) file for details.


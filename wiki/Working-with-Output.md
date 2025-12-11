# Working with Output

After generation, you receive a `FloorLayout<TRoomType>` object containing all the dungeon data. This guide shows how to use it.

## FloorLayout Structure

```csharp
var layout = generator.Generate(config);

// Access properties
var rooms = layout.Rooms;           // All placed rooms
var hallways = layout.Hallways;     // All hallways
var doors = layout.Doors;           // All doors
var seed = layout.Seed;             // Generation seed
var criticalPath = layout.CriticalPath;  // Spawn to boss path
var spawnRoomId = layout.SpawnRoomId;    // Spawn node ID
var bossRoomId = layout.BossRoomId;      // Boss node ID
```

## Accessing Rooms

### Iterate All Rooms

```csharp
foreach (var room in layout.Rooms)
{
    Console.WriteLine($"Room {room.NodeId}: {room.RoomType}");
    Console.WriteLine($"  Position: ({room.Position.X}, {room.Position.Y})");
    Console.WriteLine($"  Template: {room.Template.Id}");
}
```

### Find Specific Room

```csharp
// By node ID
var spawnRoom = layout.GetRoom(layout.SpawnRoomId);
var bossRoom = layout.GetRoom(layout.BossRoomId);

// By room type
var treasureRooms = layout.Rooms.Where(r => r.RoomType == RoomType.Treasure);

// By template
var bossArenas = layout.Rooms.Where(r => r.Template.Id == "boss-arena");
```

### Room Properties

```csharp
var room = layout.Rooms[0];

// Basic info
int nodeId = room.NodeId;
RoomType roomType = room.RoomType;
Cell position = room.Position;  // Anchor position
RoomTemplate<RoomType> template = room.Template;

// Get all cells this room occupies (world coordinates)
foreach (var cell in room.GetWorldCells())
{
    Console.WriteLine($"Room occupies: ({cell.X}, {cell.Y})");
}

// Get exterior edges (for door placement)
foreach (var (localCell, worldCell, edge) in room.GetExteriorEdgesWorld())
{
    Console.WriteLine($"Edge at ({worldCell.X}, {worldCell.Y}): {edge}");
}
```

## Understanding Positions

### Anchor Position

`room.Position` is the **anchor** (top-left corner) of the room template. The template's cells are relative to this anchor.

```csharp
// Room at anchor (5, 10)
var room = layout.Rooms[0];
Console.WriteLine($"Anchor: ({room.Position.X}, {room.Position.Y})");

// Template cells are relative to anchor
// If template has cell (0, 0), it's at world (5, 10)
// If template has cell (2, 1), it's at world (7, 11)
```

### World Cells

Use `GetWorldCells()` to get all cells a room occupies:

```csharp
foreach (var cell in room.GetWorldCells())
{
    // This is a world coordinate cell occupied by the room
    RenderTile(cell.X, cell.Y, TileType.Floor);
}
```

## Hallways

### Iterate Hallways

```csharp
foreach (var hallway in layout.Hallways)
{
    Console.WriteLine($"Hallway {hallway.Id}");
    Console.WriteLine($"  Connects room {hallway.DoorA.ConnectsToRoomId} to {hallway.DoorB.ConnectsToRoomId}");
    
    foreach (var segment in hallway.Segments)
    {
        Console.WriteLine($"  Segment: ({segment.Start.X}, {segment.Start.Y}) to ({segment.End.X}, {segment.End.Y})");
    }
}
```

### Hallway Segments

Hallways are made of segments (straight lines):

```csharp
var hallway = layout.Hallways[0];

foreach (var segment in hallway.Segments)
{
    bool isHorizontal = segment.IsHorizontal;
    bool isVertical = segment.IsVertical;
    
    // Get all cells in this segment
    foreach (var cell in segment.GetCells())
    {
        RenderTile(cell.X, cell.Y, TileType.Hallway);
    }
}
```

### Hallway Doors

Each hallway has two doors connecting to rooms:

```csharp
var hallway = layout.Hallways[0];

var doorA = hallway.DoorA;
var doorB = hallway.DoorB;

Console.WriteLine($"Door A at ({doorA.Position.X}, {doorA.Position.Y}) on {doorA.Edge}");
Console.WriteLine($"Door B at ({doorB.Position.X}, {doorB.Position.Y}) on {doorB.Edge}");
```

## Doors

### All Doors

```csharp
foreach (var door in layout.Doors)
{
    Console.WriteLine($"Door at ({door.Position.X}, {door.Position.Y})");
    Console.WriteLine($"  Edge: {door.Edge}");
    
    if (door.ConnectsToRoomId.HasValue)
    {
        Console.WriteLine($"  Connects to room {door.ConnectsToRoomId.Value}");
    }
    
    if (door.ConnectsToHallwayId.HasValue)
    {
        Console.WriteLine($"  Connects to hallway {door.ConnectsToHallwayId.Value}");
    }
}
```

### Room-to-Room Doors

Doors between adjacent rooms (no hallway):

```csharp
var directDoors = layout.Doors
    .Where(d => d.ConnectsToRoomId.HasValue && !d.ConnectsToHallwayId.HasValue);
```

### Hallway Doors

Doors connecting rooms to hallways:

```csharp
var hallwayDoors = layout.Doors
    .Where(d => d.ConnectsToHallwayId.HasValue);
```

## Critical Path

The critical path is the shortest path from spawn to boss:

```csharp
var criticalPath = layout.CriticalPath;

Console.WriteLine($"Critical path has {criticalPath.Count} rooms:");
foreach (var nodeId in criticalPath)
{
    var room = layout.GetRoom(nodeId);
    Console.WriteLine($"  {nodeId}: {room.RoomType}");
}

// Check if a room is on critical path
bool isOnCriticalPath = criticalPath.Contains(room.NodeId);
```

## Bounding Box

Get the bounds of the entire dungeon:

```csharp
var (min, max) = layout.GetBounds();

Console.WriteLine($"Dungeon bounds:");
Console.WriteLine($"  Min: ({min.X}, {min.Y})");
Console.WriteLine($"  Max: ({max.X}, {max.Y})");
Console.WriteLine($"  Width: {max.X - min.X + 1}");
Console.WriteLine($"  Height: {max.Y - min.Y + 1}");

// Use for camera/viewport calculations
int dungeonWidth = max.X - min.X + 1;
int dungeonHeight = max.Y - min.Y + 1;
```

## Rendering Example

Here's a complete rendering example:

```csharp
var layout = generator.Generate(config);

// Get bounds for viewport
var (min, max) = layout.GetBounds();

// Render all room cells
foreach (var room in layout.Rooms)
{
    foreach (var cell in room.GetWorldCells())
    {
        // Convert to screen coordinates (offset by min)
        int screenX = cell.X - min.X;
        int screenY = cell.Y - min.Y;
        
        RenderTile(screenX, screenY, GetTileForRoomType(room.RoomType));
    }
}

// Render all hallway cells
foreach (var hallway in layout.Hallways)
{
    foreach (var segment in hallway.Segments)
    {
        foreach (var cell in segment.GetCells())
        {
            int screenX = cell.X - min.X;
            int screenY = cell.Y - min.Y;
            
            RenderTile(screenX, screenY, TileType.Hallway);
        }
    }
}

// Render doors
foreach (var door in layout.Doors)
{
    int screenX = door.Position.X - min.X;
    int screenY = door.Position.Y - min.Y;
    
    RenderDoor(screenX, screenY, door.Edge);
}
```

## Converting to Game Coordinates

The library uses grid coordinates. Convert to your game's coordinate system:

```csharp
// Library uses grid cells
var cell = new Cell(5, 10);

// Convert to world coordinates (if each cell is 32x32 pixels)
float worldX = cell.X * 32.0f;
float worldY = cell.Y * 32.0f;

// Or if using a different tile size
float worldX = cell.X * tileWidth;
float worldY = cell.Y * tileHeight;
```

## Finding Connections

Find which rooms connect to a given room:

```csharp
// Find all rooms connected to spawn
var spawnRoom = layout.GetRoom(layout.SpawnRoomId);
var connectedRooms = new List<PlacedRoom<RoomType>>();

// Check all doors
foreach (var door in layout.Doors)
{
    if (door.ConnectsToRoomId == spawnRoom.NodeId)
    {
        // Find the room this door belongs to
        var connectedRoom = layout.Rooms.FirstOrDefault(r => 
            r.GetWorldCells().Contains(door.Position));
        if (connectedRoom != null)
        {
            connectedRooms.Add(connectedRoom);
        }
    }
}

// Also check hallways
foreach (var hallway in layout.Hallways)
{
    if (hallway.DoorA.ConnectsToRoomId == spawnRoom.NodeId)
    {
        var otherRoom = layout.GetRoom(hallway.DoorB.ConnectsToRoomId.Value);
        if (otherRoom != null) connectedRooms.Add(otherRoom);
    }
    if (hallway.DoorB.ConnectsToRoomId == spawnRoom.NodeId)
    {
        var otherRoom = layout.GetRoom(hallway.DoorA.ConnectsToRoomId.Value);
        if (otherRoom != null) connectedRooms.Add(otherRoom);
    }
}
```

## Interior Features

Interior features are obstacles and decorative elements defined within room templates, such as pillars, walls, hazards, and decorative items.

### Accessing Interior Features

**For a specific room:**

```csharp
var room = layout.Rooms[0];

// Get all interior features in this room (world coordinates)
foreach (var (worldCell, feature) in room.GetInteriorFeatures())
{
    Console.WriteLine($"Feature {feature} at ({worldCell.X}, {worldCell.Y})");
    
    // Render based on feature type
    switch (feature)
    {
        case InteriorFeature.Pillar:
            RenderPillar(worldCell.X, worldCell.Y);
            break;
        case InteriorFeature.Wall:
            RenderInteriorWall(worldCell.X, worldCell.Y);
            break;
        case InteriorFeature.Hazard:
            RenderHazard(worldCell.X, worldCell.Y);
            break;
        case InteriorFeature.Decorative:
            RenderDecorative(worldCell.X, worldCell.Y);
            break;
    }
}
```

**For all rooms:**

```csharp
// Get all interior features across the entire floor
foreach (var (worldCell, feature) in layout.InteriorFeatures)
{
    RenderFeature(worldCell.X, worldCell.Y, feature);
}
```

### Feature Types

The `InteriorFeature` enum provides four types:

- **`Pillar`** - Obstacles that block movement but not line of sight
- **`Wall`** - Interior walls that create sub-areas within rooms
- **`Hazard`** - Special cells that might contain traps, lava, spikes, etc.
- **`Decorative`** - Visual markers for special areas (altars, fountains, etc.)

### Rendering Interior Features

When rendering a dungeon, include interior features:

```csharp
var layout = generator.Generate(config);
var (min, max) = layout.GetBounds();

// Render rooms first
foreach (var room in layout.Rooms)
{
    foreach (var cell in room.GetWorldCells())
    {
        int screenX = cell.X - min.X;
        int screenY = cell.Y - min.Y;
        RenderTile(screenX, screenY, GetTileForRoomType(room.RoomType));
    }
}

// Then render interior features
foreach (var (worldCell, feature) in layout.InteriorFeatures)
{
    int screenX = worldCell.X - min.X;
    int screenY = worldCell.Y - min.Y;
    
    switch (feature)
    {
        case InteriorFeature.Pillar:
            RenderTile(screenX, screenY, TileType.Pillar);
            break;
        case InteriorFeature.Wall:
            RenderTile(screenX, screenY, TileType.InteriorWall);
            break;
        case InteriorFeature.Hazard:
            RenderTile(screenX, screenY, TileType.Hazard);
            break;
        case InteriorFeature.Decorative:
            RenderTile(screenX, screenY, TileType.Decorative);
            break;
    }
}
```

### Game Integration

Interior features enable various gameplay mechanics:

**Cover Mechanics (Pillars):**
```csharp
foreach (var (cell, feature) in room.GetInteriorFeatures())
{
    if (feature == InteriorFeature.Pillar)
    {
        // Pillars block movement but not line of sight
        if (IsPlayerAt(cell))
        {
            ApplyCoverBonus();  // Player gets cover bonus
        }
    }
}
```

**Tactical Positioning (Walls):**
```csharp
foreach (var (cell, feature) in room.GetInteriorFeatures())
{
    if (feature == InteriorFeature.Wall)
    {
        // Walls create chokepoints and sub-areas
        BlockMovement(cell);
        BlockLineOfSight(cell);
    }
}
```

**Environmental Hazards:**
```csharp
foreach (var (cell, feature) in room.GetInteriorFeatures())
{
    if (feature == InteriorFeature.Hazard)
    {
        // Hazards deal damage or apply effects
        if (IsPlayerAt(cell))
        {
            ApplyHazardDamage(cell);
        }
    }
}
```

**Thematic Elements (Decorative):**
```csharp
foreach (var (cell, feature) in room.GetInteriorFeatures())
{
    if (feature == InteriorFeature.Decorative)
    {
        // Decorative features add visual interest
        RenderSpecialEffect(cell, DecorativeType.Altar);
    }
}
```

### Finding Features by Type

```csharp
// Find all hazards in the dungeon
var hazards = layout.InteriorFeatures
    .Where(f => f.Feature == InteriorFeature.Hazard)
    .ToList();

// Find all decorative features in a specific room
var room = layout.Rooms[0];
var decorativeFeatures = room.GetInteriorFeatures()
    .Where(f => f.Feature == InteriorFeature.Decorative)
    .ToList();
```

### Coordinate Systems

**Important:** Interior features use different coordinate systems:

- **In templates**: Features are defined in **template-local coordinates** (relative to anchor)
- **In placed rooms**: Features are returned in **world coordinates** (absolute position)

```csharp
// Template defines feature at template-local (2, 2)
var template = RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
    .AddInteriorFeature(2, 2, InteriorFeature.Pillar)
    .Build();

// Room is placed at world position (10, 20)
var room = new PlacedRoom<RoomType>
{
    Position = new Cell(10, 20),
    Template = template
};

// Feature appears at world position (12, 22)
var features = room.GetInteriorFeatures();
// Returns: (worldCell: (12, 22), feature: Pillar)
```

## Secret Passages

Secret passages are hidden connections between rooms that are not part of the main dungeon graph.

### Accessing Secret Passages

```csharp
// Get all secret passages
var secretPassages = layout.SecretPassages;

Console.WriteLine($"Found {secretPassages.Count} secret passages");

foreach (var passage in secretPassages)
{
    Console.WriteLine($"Secret passage connects room {passage.RoomAId} to {passage.RoomBId}");
    Console.WriteLine($"  Door A at ({passage.DoorA.Position.X}, {passage.DoorA.Position.Y})");
    Console.WriteLine($"  Door B at ({passage.DoorB.Position.X}, {passage.DoorB.Position.Y})");
    
    if (passage.RequiresHallway)
    {
        Console.WriteLine($"  Has hallway with {passage.Hallway.Segments.Count} segments");
    }
}
```

### Finding Secret Passages for a Room

```csharp
// Get all secret passages connected to a specific room
var roomId = 5;
var passages = layout.GetSecretPassagesForRoom(roomId);

foreach (var passage in passages)
{
    var otherRoomId = passage.RoomAId == roomId ? passage.RoomBId : passage.RoomAId;
    Console.WriteLine($"Room {roomId} has secret passage to room {otherRoomId}");
}
```

### Secret Passage Properties

```csharp
var passage = layout.SecretPassages[0];

// Room IDs
int roomA = passage.RoomAId;
int roomB = passage.RoomBId;

// Doors
Door doorA = passage.DoorA;
Door doorB = passage.DoorB;

// Optional hallway
if (passage.RequiresHallway)
{
    Hallway hallway = passage.Hallway;
    // Use hallway segments for rendering
}
```

### Rendering Secret Passages

Secret passages should typically be rendered differently from regular connections (e.g., hidden walls that can be discovered):

```csharp
// Render secret passages as hidden/secret doors
foreach (var passage in layout.SecretPassages)
{
    // Render door A (hidden)
    RenderSecretDoor(passage.DoorA.Position, passage.DoorA.Edge);
    
    // Render door B (hidden)
    RenderSecretDoor(passage.DoorB.Position, passage.DoorB.Edge);
    
    // Render hallway if present
    if (passage.RequiresHallway)
    {
        foreach (var segment in passage.Hallway.Segments)
        {
            foreach (var cell in segment.GetCells())
            {
                RenderSecretHallway(cell);
            }
        }
    }
}
```

### Game Integration

Secret passages enable gameplay mechanics like:
- **Hidden shortcuts**: Players can discover faster routes
- **Exploration rewards**: Secret passages lead to hidden areas
- **Alternative routes**: Bypass dangerous areas or provide speedrun paths

```csharp
// Check if a room has secret passages
bool hasSecrets = layout.GetSecretPassagesForRoom(roomId).Any();

// Reveal secret passage when player interacts with wall
public void OnWallInteraction(int roomId, Cell position)
{
    var passages = layout.GetSecretPassagesForRoom(roomId);
    var passage = passages.FirstOrDefault(p => 
        p.DoorA.Position == position || p.DoorB.Position == position);
    
    if (passage != null)
    {
        RevealSecretPassage(passage);
    }
}
```

**Important Notes:**
- Secret passages **don't affect** the main graph topology (critical path, distances, etc.)
- Secret passages connect **spatially close** rooms (within MaxSpatialDistance)
- Secret passages can optionally exclude graph-connected or critical path rooms
- Secret passages are generated **deterministically** based on seed

## Statistics

Calculate dungeon statistics:

```csharp
var stats = new
{
    TotalRooms = layout.Rooms.Count,
    TotalHallways = layout.Hallways.Count,
    TotalDoors = layout.Doors.Count,
    TotalSecretPassages = layout.SecretPassages.Count,
    CriticalPathLength = layout.CriticalPath.Count,
    
    RoomTypeCounts = layout.Rooms
        .GroupBy(r => r.RoomType)
        .ToDictionary(g => g.Key, g => g.Count()),
    
    DeadEnds = layout.Rooms.Count(r => 
        layout.Doors.Count(d => 
            d.ConnectsToRoomId == r.NodeId || 
            (d.ConnectsToHallwayId.HasValue && 
             layout.Hallways.Any(h => h.Id == d.ConnectsToHallwayId && 
                (h.DoorA.ConnectsToRoomId == r.NodeId || 
                 h.DoorB.ConnectsToRoomId == r.NodeId)))) == 1)
};

Console.WriteLine($"Total rooms: {stats.TotalRooms}");
Console.WriteLine($"Dead ends: {stats.DeadEnds}");
```

## ASCII Map Visualization

The library includes a built-in ASCII visualization system for generating text-based dungeon maps. This is perfect for debugging, development, documentation, and even in-game minimaps for text-based roguelikes.

### Basic Usage

```csharp
using ShepherdProceduralDungeons.Visualization;

var renderer = new AsciiMapRenderer<RoomType>();
string asciiMap = renderer.Render(layout);
Console.WriteLine(asciiMap);
```

### Rendering Options

```csharp
var options = new AsciiRenderOptions
{
    Style = AsciiRenderStyle.Detailed,
    HighlightCriticalPath = true,
    ShowHallways = true,
    ShowDoors = true,
    ShowInteriorFeatures = true,
    ShowSecretPassages = true,
    IncludeLegend = true,
    CustomRoomTypeSymbols = new Dictionary<object, char>
    {
        { RoomType.Spawn, 'S' },
        { RoomType.Boss, 'B' }
    }
};

string map = renderer.Render(layout, options);
```

### Multi-Floor Visualization

```csharp
var multiFloorRenderer = new AsciiMapRenderer<RoomType>();
string multiFloorMap = renderer.Render(multiFloorLayout);
```

Each floor is rendered separately with clear separators.

### Rendering to File or Stream

```csharp
// To file
using (var writer = new StreamWriter("dungeon-map.txt"))
{
    renderer.Render(layout, writer);
}

// To StringBuilder
var builder = new StringBuilder();
renderer.Render(layout, builder);
string map = builder.ToString();
```

### Viewport for Large Dungeons

For large dungeons, use a viewport to render only a specific region:

```csharp
var (min, max) = layout.GetBounds();
var viewportOptions = new AsciiRenderOptions
{
    Viewport = (min, new Cell(min.X + 30, min.Y + 20))
};

string viewportMap = renderer.Render(layout, viewportOptions);
```

### Symbol Meanings

The renderer uses standard symbols:
- **Room types**: Letters based on room type name (S=Spawn, B=Boss, C=Combat, $=Shop, T=Treasure)
- **Hallways**: `.` (period)
- **Doors**: `+` (plus)
- **Secret passages**: `~` (tilde)
- **Interior features**: `○` (pillar), `█` (wall), `!` (hazard), `◊` (decorative)
- **Critical path**: Uppercase letters (when highlighting enabled)

See **[Examples](Examples#ascii-map-visualization-example)** for complete visualization examples.

## Next Steps

- **[Examples](Examples)** - See complete usage examples, including visualization
- **[Room Templates](Room-Templates)** - Understand template structure
- **[Configuration](Configuration)** - Control generation output
- **[API Reference](API-Reference#asciimaprenderertroomtype)** - Full visualization API documentation


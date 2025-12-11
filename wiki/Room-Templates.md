# Room Templates

Room templates define the **shape** and **door placement** for rooms. They're the building blocks of your dungeon.

## Overview

A `RoomTemplate<TRoomType>` consists of:
- **Cells**: The grid cells the room occupies (relative to anchor position)
- **Valid Room Types**: Which room types can use this template
- **Door Edges**: Where doors can be placed on the template

Templates are created using the fluent `RoomTemplateBuilder<TRoomType>` API.

## Creating Templates

### Simple Rectangle

The most common case - a rectangular room:

```csharp
var template = RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
    .WithId("combat-room")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

This creates a 4×3 rectangle with doors allowed on all exterior edges.

### L-Shaped Rooms

Create L-shaped rooms with a cutout corner:

```csharp
var lShape = RoomTemplateBuilder<RoomType>.LShape(
    width: 5,
    height: 4,
    cutoutWidth: 2,
    cutoutHeight: 2,
    cutoutCorner: Corner.TopRight
)
    .WithId("l-shaped-combat")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

**Corner options:**
- `Corner.TopLeft`
- `Corner.TopRight`
- `Corner.BottomLeft`
- `Corner.BottomRight`

### Custom Shapes

Build completely custom shapes cell-by-cell:

```csharp
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

You can also add rectangular regions:

```csharp
var template = new RoomTemplateBuilder<RoomType>()
    .WithId("custom")
    .ForRoomTypes(RoomType.Combat)
    .AddRectangle(0, 0, 3, 2)  // Main area
    .AddRectangle(3, 1, 1, 1)  // Extension
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

## Door Placement

### All Exterior Edges

Allow doors anywhere on the perimeter:

```csharp
.WithDoorsOnAllExteriorEdges()
```

This is the most flexible option and works for most rooms.

### Specific Sides

Restrict doors to specific sides of the bounding box:

```csharp
.WithDoorsOnSides(Edge.South)  // Only south side
.WithDoorsOnSides(Edge.North | Edge.South)  // North and south
.WithDoorsOnSides(Edge.All)  // All sides (same as WithDoorsOnAllExteriorEdges)
```

**Edge flags:**
- `Edge.North`
- `Edge.South`
- `Edge.East`
- `Edge.West`
- `Edge.All` (all four directions)

**Example - Boss room with single entrance:**

```csharp
var bossRoom = RoomTemplateBuilder<RoomType>.Rectangle(8, 8)
    .WithId("boss-arena")
    .ForRoomTypes(RoomType.Boss)
    .WithDoorsOnSides(Edge.South)  // Only entrance from south
    .Build();
```

### Fine-Grained Control

Set door edges for specific cells:

```csharp
var template = new RoomTemplateBuilder<RoomType>()
    .WithId("custom-doors")
    .ForRoomTypes(RoomType.Combat)
    .AddRectangle(0, 0, 4, 4)
    .WithDoorEdges(0, 2, Edge.West)      // Door on west side, middle
    .WithDoorEdges(2, 0, Edge.North)     // Door on north side
    .WithDoorEdges(3, 2, Edge.East)       // Door on east side
    .Build();
```

**Important:** You can only place doors on **exterior edges** (edges not shared with another cell in the template).

## Template Properties

After building, templates have useful properties:

```csharp
var template = /* ... */ .Build();

Console.WriteLine($"ID: {template.Id}");
Console.WriteLine($"Width: {template.Width}");
Console.WriteLine($"Height: {template.Height}");
Console.WriteLine($"Weight: {template.Weight}");
Console.WriteLine($"Valid Types: {string.Join(", ", template.ValidRoomTypes)}");
Console.WriteLine($"Cell Count: {template.Cells.Count}");

// Check if door can be placed
bool canPlace = template.CanPlaceDoor(new Cell(0, 0), Edge.North);

// Get all exterior edges
foreach (var (cell, edge) in template.GetExteriorEdges())
{
    Console.WriteLine($"Cell ({cell.X}, {cell.Y}) has exterior edge {edge}");
}
```

## Multiple Templates Per Room Type

You can create multiple templates for the same room type. The generator will randomly select one:

```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Small combat room
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("combat-small")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Medium combat room
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build(),
    
    // Large combat room
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-large")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .Build()
};
```

When a `Combat` room is generated, one of these three templates will be randomly selected.

## Template Weighting

By default, templates are selected uniformly at random. You can control selection frequency using **weights**. Higher weights increase the probability that a template will be selected.

### Basic Weighting

Use `WithWeight()` to set a template's selection weight:

```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Common template (appears 3x more often)
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-common")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(3.0)  // 3x weight
        .Build(),
    
    // Rare template (appears less often)
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-rare")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(1.0)  // Default weight
        .Build()
};
```

With weights of 3.0 and 1.0, the common template will be selected approximately 75% of the time (3/(3+1)) and the rare template 25% of the time (1/(3+1)).

### Weight Calculation

Selection probability is calculated as:
```
probability = template.Weight / sum(all_template_weights)
```

**Example:**
- Template A: weight 2.0
- Template B: weight 1.0
- Template C: weight 1.0
- Total weight: 4.0
- Template A probability: 2.0/4.0 = 50%
- Template B probability: 1.0/4.0 = 25%
- Template C probability: 1.0/4.0 = 25%

### Default Weight

Templates without an explicit weight default to **1.0**, maintaining backward compatibility:

```csharp
// These are equivalent:
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template1")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .Build();

RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template2")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .WithWeight(1.0)  // Explicit default
    .Build();
```

### Common Use Cases

**Common vs Rare Templates:**
```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Default combat room (common)
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-standard")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(5.0)  // Very common
        .Build(),
    
    // Special decorative room (rare)
    RoomTemplateBuilder<RoomType>.LShape(5, 4, 2, 2, Corner.TopRight)
        .WithId("combat-special")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(0.5)  // Rare
        .Build()
};
```

**Size-Based Weighting:**
```csharp
var templates = new List<RoomTemplate<RoomType>>
{
    // Small rooms (more common)
    RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
        .WithId("combat-small")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(4.0)
        .Build(),
    
    // Medium rooms (moderate)
    RoomTemplateBuilder<RoomType>.Rectangle(4, 4)
        .WithId("combat-medium")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(2.0)
        .Build(),
    
    // Large rooms (rare, harder to place)
    RoomTemplateBuilder<RoomType>.Rectangle(5, 5)
        .WithId("combat-large")
        .ForRoomTypes(RoomType.Combat)
        .WithDoorsOnAllExteriorEdges()
        .WithWeight(1.0)
        .Build()
};
```

### Weight Validation

Weights must be **positive numbers** (greater than 0). Zero or negative weights will throw `InvalidConfigurationException`:

```csharp
// ❌ Error: Weight must be greater than 0
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("invalid")
    .ForRoomTypes(RoomType.Combat)
    .WithDoorsOnAllExteriorEdges()
    .WithWeight(0.0)  // Invalid!
    .Build();

// ❌ Error: Weight must be greater than 0
.WithWeight(-1.0)  // Invalid!
```

### Determinism

Weighted selection is deterministic when using the same seed:

```csharp
var config1 = new FloorConfig<RoomType> { Seed = 12345, /* ... */ };
var config2 = new FloorConfig<RoomType> { Seed = 12345, /* ... */ };

var generator = new FloorGenerator<RoomType>();
var layout1 = generator.Generate(config1);
var layout2 = generator.Generate(config2);

// Same seed + same weights = same template selections
```

The same seed with the same template weights will produce identical template selections across multiple runs.

## Template Validation

The builder validates templates when you call `Build()`. Common errors:

**Missing ID:**
```csharp
// ❌ Error: Template must have an ID
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .ForRoomTypes(RoomType.Combat)
    .Build();
```

**No valid room types:**
```csharp
// ❌ Error: Template must specify at least one valid room type
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .Build();
```

**No cells:**
```csharp
// ❌ Error: Template must have at least one cell
new RoomTemplateBuilder<RoomType>()
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .Build();
```

**No door edges:**
```csharp
// ❌ Error: Template must have at least one door edge
RoomTemplateBuilder<RoomType>.Rectangle(3, 3)
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    // Missing door configuration!
    .Build();
```

**Interior door edge:**
```csharp
// ❌ Error: Door on interior edge
var template = new RoomTemplateBuilder<RoomType>()
    .WithId("template")
    .ForRoomTypes(RoomType.Combat)
    .AddRectangle(0, 0, 4, 4)
    .WithDoorEdges(1, 1, Edge.North)  // This cell has a neighbor to the north!
    .Build();
```

## Common Patterns

### Small Treasure Room
```csharp
RoomTemplateBuilder<RoomType>.Rectangle(2, 2)
    .WithId("treasure-small")
    .ForRoomTypes(RoomType.Treasure)
    .WithDoorsOnSides(Edge.All)
    .Build();
```

### Large Boss Arena
```csharp
RoomTemplateBuilder<RoomType>.Rectangle(8, 8)
    .WithId("boss-arena")
    .ForRoomTypes(RoomType.Boss)
    .WithDoorsOnSides(Edge.South)  // Single entrance
    .Build();
```

### Shop Room
```csharp
RoomTemplateBuilder<RoomType>.Rectangle(4, 3)
    .WithId("shop")
    .ForRoomTypes(RoomType.Shop)
    .WithDoorsOnSides(Edge.South | Edge.North)  // Entrance and exit
    .Build();
```

### Secret Room (L-shaped)
```csharp
RoomTemplateBuilder<RoomType>.LShape(4, 3, 2, 1, Corner.TopRight)
    .WithId("secret-l")
    .ForRoomTypes(RoomType.Secret)
    .WithDoorsOnAllExteriorEdges()
    .Build();
```

## Tips

1. **Start simple**: Use rectangles for most rooms, add complexity later
2. **Door placement matters**: More door options = easier placement, but less control
3. **Size considerations**: Larger rooms are harder to place - balance size with `RoomCount`
4. **Template variety**: Multiple templates per type adds visual variety
5. **Test placement**: If rooms fail to place, try smaller templates or enable hallways

## Next Steps

- **[Constraints](Constraints)** - Control where rooms are placed
- **[Configuration](Configuration)** - Complete config reference
- **[Examples](Examples)** - See templates in action


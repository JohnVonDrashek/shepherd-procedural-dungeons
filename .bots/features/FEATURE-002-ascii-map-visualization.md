# Feature: ASCII Art Map Visualization System

**ID**: FEATURE-002
**Status**: complete
**Created**: 2025-12-11T20:30:00Z
**Priority**: high
**Complexity**: complex

## Description

A comprehensive ASCII art visualization system that generates beautiful, detailed text-based representations of generated dungeon layouts. This feature enables developers to quickly visualize and debug their dungeon generations without requiring graphics rendering, making it perfect for development, testing, documentation, and even in-game minimaps for text-based roguelikes.

The system goes far beyond simple debug output - it provides a production-ready visualization API with multiple rendering styles, configurable symbols, room type highlighting, critical path visualization, door and hallway rendering, and support for both single-floor and multi-floor dungeons. It handles large dungeons intelligently through viewport controls and scaling options.

**Why this matters**: Visual debugging is critical for procedural generation. Developers need to see what their configurations produce, verify constraint satisfaction, understand spatial relationships, and iterate quickly. A rich ASCII visualization system makes the library significantly more developer-friendly and enables new use cases like text-based roguelikes that can use the visualizations directly in-game.

## Requirements

- [ ] Public API for generating ASCII art from `FloorLayout<TRoomType>` and `MultiFloorLayout<TRoomType>`
- [ ] Configurable rendering options (symbols, colors, styles)
- [ ] Multiple built-in rendering styles (minimal, detailed, artistic)
- [ ] Room type visualization with custom symbols/colors per type
- [ ] Hallway and door rendering with appropriate symbols
- [ ] Critical path highlighting (visual distinction for spawn-to-boss path)
- [ ] Support for large dungeons (viewport, scaling, chunked output)
- [ ] Interior feature visualization (walls, pillars, hazards)
- [ ] Secret passage visualization
- [ ] Zone boundary visualization (if zones are configured)
- [ ] Multi-floor visualization with floor separators
- [ ] Legend generation showing symbol meanings
- [ ] Export to string, StringBuilder, or TextWriter
- [ ] Performance optimization for large dungeons (100+ rooms)

## Technical Details

### Core API Design

```csharp
namespace ShepherdProceduralDungeons.Visualization;

/// <summary>
/// Generates ASCII art visualizations of dungeon layouts.
/// </summary>
public sealed class AsciiMapRenderer<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Renders a single-floor layout to a string.
    /// </summary>
    public string Render(FloorLayout<TRoomType> layout, AsciiRenderOptions? options = null);
    
    /// <summary>
    /// Renders a single-floor layout to a TextWriter.
    /// </summary>
    public void Render(FloorLayout<TRoomType> layout, TextWriter writer, AsciiRenderOptions? options = null);
    
    /// <summary>
    /// Renders a multi-floor layout to a string.
    /// </summary>
    public string Render(MultiFloorLayout<TRoomType> layout, AsciiRenderOptions? options = null);
    
    /// <summary>
    /// Renders a single-floor layout to a StringBuilder.
    /// </summary>
    public void Render(FloorLayout<TRoomType> layout, StringBuilder builder, AsciiRenderOptions? options = null);
}

/// <summary>
/// Configuration options for ASCII rendering.
/// </summary>
public sealed class AsciiRenderOptions
{
    /// <summary>
    /// Rendering style preset.
    /// </summary>
    public AsciiRenderStyle Style { get; init; } = AsciiRenderStyle.Detailed;
    
    /// <summary>
    /// Custom symbol mappings for room types. Overrides style defaults.
    /// </summary>
    public IReadOnlyDictionary<object, char>? CustomRoomTypeSymbols { get; init; }
    
    /// <summary>
    /// Whether to show room IDs as numbers.
    /// </summary>
    public bool ShowRoomIds { get; init; } = false;
    
    /// <summary>
    /// Whether to highlight the critical path.
    /// </summary>
    public bool HighlightCriticalPath { get; init; } = true;
    
    /// <summary>
    /// Whether to show hallways.
    /// </summary>
    public bool ShowHallways { get; init; } = true;
    
    /// <summary>
    /// Whether to show doors.
    /// </summary>
    public bool ShowDoors { get; init; } = true;
    
    /// <summary>
    /// Whether to show interior features.
    /// </summary>
    public bool ShowInteriorFeatures { get; init; } = true;
    
    /// <summary>
    /// Whether to show secret passages.
    /// </summary>
    public bool ShowSecretPassages { get; init; } = true;
    
    /// <summary>
    /// Whether to show zone boundaries (if zones configured).
    /// </summary>
    public bool ShowZoneBoundaries { get; init; } = false;
    
    /// <summary>
    /// Viewport for large dungeons. Null = render entire dungeon.
    /// </summary>
    public (Cell Min, Cell Max)? Viewport { get; init; }
    
    /// <summary>
    /// Scale factor for rendering (1 = normal, 2 = double size, etc.).
    /// </summary>
    public int Scale { get; init; } = 1;
    
    /// <summary>
    /// Whether to include a legend at the bottom.
    /// </summary>
    public bool IncludeLegend { get; init; } = true;
    
    /// <summary>
    /// Maximum width/height before auto-scaling or viewport is required.
    /// </summary>
    public (int MaxWidth, int MaxHeight)? MaxSize { get; init; } = (120, 40);
}

public enum AsciiRenderStyle
{
    /// <summary>Minimal style - just rooms and connections.</summary>
    Minimal,
    
    /// <summary>Detailed style - rooms, hallways, doors, features.</summary>
    Detailed,
    
    /// <summary>Artistic style - uses box-drawing characters for walls.</summary>
    Artistic,
    
    /// <summary>Compact style - optimized for small terminals.</summary>
    Compact
}
```

### Symbol Mapping System

The renderer will use a flexible symbol mapping system:

- **Room Types**: Each room type gets a symbol (e.g., 'S' for Spawn, 'B' for Boss, 'C' for Combat, '$' for Shop, 'T' for Treasure)
- **Hallways**: Use '.' or '·' for hallway cells
- **Doors**: Use '+' or '#' at door positions
- **Walls**: Use '│', '─', '┌', '┐', '└', '┘' for artistic style
- **Critical Path**: Highlight with background color codes or special markers
- **Interior Features**: Use symbols like '█' for walls, '○' for pillars, '!' for hazards
- **Secret Passages**: Use '~' or '≈' to distinguish from regular connections

### Rendering Algorithm

1. **Calculate Bounds**: Determine the bounding box of all rooms and hallways
2. **Apply Viewport**: If viewport specified, clip to that region
3. **Create Grid**: Allocate character grid with appropriate size (accounting for scale)
4. **Render Layers** (in order):
   - Background/empty space
   - Hallways (lowest layer)
   - Rooms (with room type symbols)
   - Interior features (overlay on rooms)
   - Doors (at door positions)
   - Secret passages (overlay)
   - Critical path highlighting (overlay)
   - Zone boundaries (if enabled)
5. **Add Legend**: Generate symbol legend if requested
6. **Format Output**: Convert grid to string with appropriate line breaks

### Multi-Floor Support

For `MultiFloorLayout`, render each floor separately with clear separators:

```
=== Floor 0 ===
[ASCII map of floor 0]

=== Floor 1 ===
[ASCII map of floor 1]

=== Floor 2 ===
[ASCII map of floor 2]
```

### Performance Considerations

- Use efficient data structures (HashSet for occupied cells lookup)
- Lazy evaluation for large dungeons (only render viewport)
- StringBuilder for string construction
- Pre-calculate symbol mappings
- Cache bounds calculations

### Integration Points

- Extends `FloorLayout<TRoomType>` with visualization capability
- Works with `MultiFloorLayout<TRoomType>` for multi-floor dungeons
- Respects all layout properties (rooms, hallways, doors, secret passages, zones)
- Uses `PlacedRoom.GetWorldCells()` and `HallwaySegment.GetCells()` for spatial data

## Dependencies

- None (builds on existing `FloorLayout` and `MultiFloorLayout` types)

## Test Scenarios

1. **Basic Single-Floor Rendering**: Render a simple 10-room dungeon with default options, verify all rooms, hallways, and doors appear correctly
2. **Room Type Symbols**: Render dungeon with multiple room types, verify each type uses correct symbol
3. **Critical Path Highlighting**: Render dungeon and verify critical path rooms are visually distinct
4. **Large Dungeon Handling**: Render 100+ room dungeon, verify viewport or scaling works correctly
5. **Multi-Floor Rendering**: Render multi-floor dungeon, verify floors are separated and each renders correctly
6. **Custom Symbols**: Use custom room type symbols, verify they override defaults
7. **Interior Features**: Render dungeon with interior features, verify features appear at correct positions
8. **Secret Passages**: Render dungeon with secret passages, verify they're distinguished from regular connections
9. **Zone Boundaries**: Render dungeon with zones, verify boundaries are shown when enabled
10. **Legend Generation**: Verify legend includes all used symbols with correct meanings
11. **Different Styles**: Render same dungeon with different styles, verify visual differences
12. **Empty/Edge Cases**: Handle empty layouts, single-room layouts, layouts with no hallways gracefully
13. **Performance**: Render large dungeon (200+ rooms) and verify reasonable performance (< 1 second)

## Acceptance Criteria

- [ ] `AsciiMapRenderer<TRoomType>` class exists with public API matching specification
- [ ] `AsciiRenderOptions` class exists with all specified properties
- [ ] `AsciiRenderStyle` enum exists with all specified values
- [ ] Can render `FloorLayout<TRoomType>` to string
- [ ] Can render `FloorLayout<TRoomType>` to TextWriter
- [ ] Can render `MultiFloorLayout<TRoomType>` to string
- [ ] Room types are rendered with appropriate symbols
- [ ] Hallways are rendered correctly
- [ ] Doors are rendered at correct positions
- [ ] Critical path highlighting works
- [ ] Interior features are rendered when enabled
- [ ] Secret passages are rendered and distinguished
- [ ] Zone boundaries can be shown (if zones configured)
- [ ] Viewport support works for large dungeons
- [ ] Scale factor works correctly
- [ ] Legend generation includes all used symbols
- [ ] Multiple rendering styles produce visually distinct output
- [ ] Performance is acceptable for large dungeons (100+ rooms)
- [ ] All test scenarios pass
- [ ] XML documentation exists for all public members
- [ ] No breaking changes to existing APIs

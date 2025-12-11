using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;
using System.Text;

namespace ShepherdProceduralDungeons.Visualization;

/// <summary>
/// Generates ASCII art visualizations of dungeon layouts.
/// </summary>
public sealed class AsciiMapRenderer<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Renders a single-floor layout to a string.
    /// </summary>
    public string Render(FloorLayout<TRoomType> layout, AsciiRenderOptions? options = null)
    {
        var builder = new StringBuilder();
        Render(layout, builder, options);
        return builder.ToString();
    }
    
    /// <summary>
    /// Renders a single-floor layout to a TextWriter.
    /// </summary>
    public void Render(FloorLayout<TRoomType> layout, TextWriter writer, AsciiRenderOptions? options = null)
    {
        var builder = new StringBuilder();
        Render(layout, builder, options);
        writer.Write(builder.ToString());
    }
    
    /// <summary>
    /// Renders a multi-floor layout to a string.
    /// </summary>
    public string Render(MultiFloorLayout<TRoomType> layout, AsciiRenderOptions? options = null)
    {
        var builder = new StringBuilder();
        
        for (int i = 0; i < layout.Floors.Count; i++)
        {
            if (i > 0)
            {
                builder.AppendLine();
            }
            
            builder.AppendLine($"=== Floor {i} ===");
            Render(layout.Floors[i], builder, options);
        }
        
        return builder.ToString();
    }
    
    /// <summary>
    /// Renders a single-floor layout to a StringBuilder.
    /// </summary>
    public void Render(FloorLayout<TRoomType> layout, StringBuilder builder, AsciiRenderOptions? options = null)
    {
        options ??= new AsciiRenderOptions();
        
        // Handle empty layout
        if (layout.Rooms.Count == 0)
        {
            builder.AppendLine("(Empty layout)");
            return;
        }
        
        // Calculate bounds
        var (min, max) = layout.GetBounds();
        
        // Apply viewport if specified
        if (options.Viewport.HasValue)
        {
            var viewport = options.Viewport.Value;
            min = new Cell(Math.Max(min.X, viewport.Min.X), Math.Max(min.Y, viewport.Min.Y));
            max = new Cell(Math.Min(max.X, viewport.Max.X), Math.Min(max.Y, viewport.Max.Y));
        }
        
        // Calculate grid size
        int width = max.X - min.X + 1;
        int height = max.Y - min.Y + 1;
        
        // Apply scale
        int scaledWidth = width * options.Scale;
        int scaledHeight = height * options.Scale;
        
        // Create grid
        var grid = new char[scaledHeight, scaledWidth];
        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                grid[y, x] = ' ';
            }
        }
        
        // Create sets for quick lookup
        var roomCells = new HashSet<Cell>(layout.GetAllRoomCells());
        var hallwayCells = new HashSet<Cell>(layout.GetAllHallwayCells());
        var doorPositions = new HashSet<Cell>(layout.Doors.Select(d => d.Position));
        var criticalPathRooms = new HashSet<int>(layout.CriticalPath);
        
        // Build room type to symbol mapping
        var roomTypeSymbols = BuildRoomTypeSymbolMap(options);
        
        // Render hallways first (lowest layer)
        if (options.ShowHallways)
        {
            foreach (var cell in hallwayCells)
            {
                if (IsInBounds(cell, min, max))
                {
                    RenderCell(grid, cell, min, options.Scale, '.');
                }
            }
        }
        
        // Render rooms
        foreach (var room in layout.Rooms)
        {
            var isCriticalPath = criticalPathRooms.Contains(room.NodeId);
            var symbol = GetRoomSymbol(room.RoomType, roomTypeSymbols);
            
            foreach (var cell in room.GetWorldCells())
            {
                if (IsInBounds(cell, min, max))
                {
                    char roomChar = symbol;
                    if (options.ShowRoomIds)
                    {
                        // For room IDs, we'll use the first digit or a marker
                        // Since we can't overlay, we'll use the symbol and add ID in legend
                        roomChar = symbol;
                    }
                    
                    if (isCriticalPath && options.HighlightCriticalPath)
                    {
                        // Use uppercase or special marker for critical path
                        roomChar = char.ToUpperInvariant(symbol);
                    }
                    
                    RenderCell(grid, cell, min, options.Scale, roomChar);
                }
            }
        }
        
        // Render interior features
        if (options.ShowInteriorFeatures)
        {
            foreach (var (cell, feature) in layout.InteriorFeatures)
            {
                if (IsInBounds(cell, min, max))
                {
                    char featureChar = GetInteriorFeatureSymbol(feature);
                    RenderCell(grid, cell, min, options.Scale, featureChar);
                }
            }
        }
        
        // Render doors
        if (options.ShowDoors)
        {
            foreach (var door in layout.Doors)
            {
                if (IsInBounds(door.Position, min, max))
                {
                    RenderCell(grid, door.Position, min, options.Scale, '+');
                }
            }
        }
        
        // Render secret passages
        if (options.ShowSecretPassages)
        {
            foreach (var secretPassage in layout.SecretPassages)
            {
                if (IsInBounds(secretPassage.DoorA.Position, min, max))
                {
                    RenderCell(grid, secretPassage.DoorA.Position, min, options.Scale, '~');
                }
                if (IsInBounds(secretPassage.DoorB.Position, min, max))
                {
                    RenderCell(grid, secretPassage.DoorB.Position, min, options.Scale, '~');
                }
                
                // Render secret passage hallway if it exists
                if (secretPassage.Hallway != null && options.ShowHallways)
                {
                    foreach (var segment in secretPassage.Hallway.Segments)
                    {
                        foreach (var cell in segment.GetCells())
                        {
                            if (IsInBounds(cell, min, max))
                            {
                                RenderCell(grid, cell, min, options.Scale, '~');
                            }
                        }
                    }
                }
            }
        }
        
        // Convert grid to string
        for (int y = 0; y < scaledHeight; y++)
        {
            for (int x = 0; x < scaledWidth; x++)
            {
                builder.Append(grid[y, x]);
            }
            builder.AppendLine();
        }
        
        // Add legend if requested
        if (options.IncludeLegend)
        {
            builder.AppendLine();
            builder.AppendLine("Legend:");
            AddLegend(builder, layout, options, roomTypeSymbols);
        }
    }
    
    private bool IsInBounds(Cell cell, Cell min, Cell max)
    {
        return cell.X >= min.X && cell.X <= max.X && cell.Y >= min.Y && cell.Y <= max.Y;
    }
    
    private void RenderCell(char[,] grid, Cell cell, Cell min, int scale, char ch)
    {
        int gridX = (cell.X - min.X) * scale;
        int gridY = (cell.Y - min.Y) * scale;
        
        for (int sy = 0; sy < scale; sy++)
        {
            for (int sx = 0; sx < scale; sx++)
            {
                int x = gridX + sx;
                int y = gridY + sy;
                
                if (x >= 0 && x < grid.GetLength(1) && y >= 0 && y < grid.GetLength(0))
                {
                    // Don't overwrite non-space characters unless it's a door or special feature
                    if (grid[y, x] == ' ' || ch == '+' || ch == '~' || ch == '.' || IsInteriorFeatureChar(ch))
                    {
                        grid[y, x] = ch;
                    }
                }
            }
        }
    }
    
    private bool IsInteriorFeatureChar(char ch)
    {
        return ch == '█' || ch == '○' || ch == '!' || ch == '◊';
    }
    
    private Dictionary<object, char> BuildRoomTypeSymbolMap(AsciiRenderOptions options)
    {
        var map = new Dictionary<object, char>();
        
        // If custom symbols provided, use them
        if (options.CustomRoomTypeSymbols != null)
        {
            foreach (var kvp in options.CustomRoomTypeSymbols)
            {
                map[kvp.Key] = kvp.Value;
            }
        }
        
        return map;
    }
    
    private char GetRoomSymbol(TRoomType roomType, Dictionary<object, char> customSymbols)
    {
        // Check custom symbols first
        if (customSymbols.TryGetValue(roomType, out char customSymbol))
        {
            return customSymbol;
        }
        
        // Default symbol mapping based on room type name
        string typeName = roomType.ToString();
        
        return typeName switch
        {
            var n when n.Contains("Spawn", StringComparison.OrdinalIgnoreCase) => 'S',
            var n when n.Contains("Boss", StringComparison.OrdinalIgnoreCase) => 'B',
            var n when n.Contains("Combat", StringComparison.OrdinalIgnoreCase) => 'C',
            var n when n.Contains("Shop", StringComparison.OrdinalIgnoreCase) => '$',
            var n when n.Contains("Treasure", StringComparison.OrdinalIgnoreCase) => 'T',
            var n when n.Contains("Secret", StringComparison.OrdinalIgnoreCase) => '?',
            _ => typeName[0] // Use first letter as fallback
        };
    }
    
    private char GetInteriorFeatureSymbol(InteriorFeature feature)
    {
        return feature switch
        {
            InteriorFeature.Pillar => '○',
            InteriorFeature.Wall => '█',
            InteriorFeature.Hazard => '!',
            InteriorFeature.Decorative => '◊',
            _ => '?'
        };
    }
    
    private void AddLegend(StringBuilder builder, FloorLayout<TRoomType> layout, AsciiRenderOptions options, Dictionary<object, char> roomTypeSymbols)
    {
        var usedSymbols = new HashSet<char>();
        var symbolDescriptions = new Dictionary<char, string>();
        
        // Collect room type symbols
        foreach (var room in layout.Rooms)
        {
            var symbol = GetRoomSymbol(room.RoomType, roomTypeSymbols);
            if (!usedSymbols.Contains(symbol))
            {
                usedSymbols.Add(symbol);
                symbolDescriptions[symbol] = $"{symbol} = {room.RoomType}";
            }
        }
        
        // Add hallway symbol if used
        if (options.ShowHallways && layout.Hallways.Count > 0)
        {
            usedSymbols.Add('.');
            symbolDescriptions['.'] = ". = Hallway";
        }
        
        // Add door symbol if used
        if (options.ShowDoors && layout.Doors.Count > 0)
        {
            usedSymbols.Add('+');
            symbolDescriptions['+'] = "+ = Door";
        }
        
        // Add secret passage symbol if used
        if (options.ShowSecretPassages && layout.SecretPassages.Count > 0)
        {
            usedSymbols.Add('~');
            symbolDescriptions['~'] = "~ = Secret Passage";
        }
        
        // Add interior feature symbols if used
        if (options.ShowInteriorFeatures)
        {
            var features = layout.InteriorFeatures.Select(f => f.Feature).Distinct().ToList();
            foreach (var feature in features)
            {
                var symbol = GetInteriorFeatureSymbol(feature);
                if (!usedSymbols.Contains(symbol))
                {
                    usedSymbols.Add(symbol);
                    symbolDescriptions[symbol] = $"{symbol} = {feature}";
                }
            }
        }
        
        // Output legend
        foreach (var kvp in symbolDescriptions.OrderBy(k => k.Key))
        {
            builder.AppendLine($"  {kvp.Value}");
        }
        
        // Add room IDs if showing them
        if (options.ShowRoomIds)
        {
            builder.AppendLine();
            builder.AppendLine("Room IDs:");
            foreach (var room in layout.Rooms.OrderBy(r => r.NodeId))
            {
                var symbol = GetRoomSymbol(room.RoomType, roomTypeSymbols);
                builder.AppendLine($"  {symbol} = Room {room.NodeId}");
            }
        }
    }
}

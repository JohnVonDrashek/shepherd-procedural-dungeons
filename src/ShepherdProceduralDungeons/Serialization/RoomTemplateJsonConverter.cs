using System.Text.Json;
using System.Text.Json.Serialization;
using ShepherdProceduralDungeons.Exceptions;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Serialization;

/// <summary>
/// JSON converter for RoomTemplate that handles different shape types (rectangle, L-shape, custom).
/// </summary>
public sealed class RoomTemplateJsonConverter<TRoomType> : JsonConverter<RoomTemplate<TRoomType>> where TRoomType : Enum
{
    public override RoomTemplate<TRoomType> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var id = root.GetProperty("id").GetString() ?? throw new JsonException("Template must have an 'id' property.");
        var validRoomTypes = root.GetProperty("validRoomTypes").Deserialize<TRoomType[]>(options) 
            ?? throw new JsonException("Template must have 'validRoomTypes' property.");
        var weight = root.TryGetProperty("weight", out var weightElement) ? weightElement.GetDouble() : 1.0;

        // Parse shape
        if (!root.TryGetProperty("shape", out var shapeElement))
        {
            throw new JsonException("Template must have a 'shape' property.");
        }

        var shapeType = shapeElement.GetProperty("type").GetString();
        var cells = ParseShape(shapeType, shapeElement);
        var doorEdges = ParseDoorEdges(root, cells, options);

        // Parse interior features if present
        var interiorFeatures = new Dictionary<Cell, InteriorFeature>();
        if (root.TryGetProperty("interiorFeatures", out var interiorFeaturesElement))
        {
            var featuresObj = interiorFeaturesElement.Deserialize<Dictionary<string, string>>(options);
            if (featuresObj != null)
            {
                foreach (var kvp in featuresObj)
                {
                    var cellParts = kvp.Key.Split(',');
                    if (cellParts.Length == 2 && int.TryParse(cellParts[0], out var x) && int.TryParse(cellParts[1], out var y))
                    {
                        var cell = new Cell(x, y);
                        if (cells.Contains(cell) && Enum.TryParse<InteriorFeature>(kvp.Value, true, out var feature))
                        {
                            interiorFeatures[cell] = feature;
                        }
                    }
                }
            }
        }

        return new RoomTemplate<TRoomType>
        {
            Id = id,
            ValidRoomTypes = new HashSet<TRoomType>(validRoomTypes),
            Cells = new HashSet<Cell>(cells),
            DoorEdges = doorEdges,
            Weight = weight,
            InteriorFeatures = interiorFeatures
        };
    }

    public override void Write(Utf8JsonWriter writer, RoomTemplate<TRoomType> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("id", value.Id);
        writer.WritePropertyName("validRoomTypes");
        JsonSerializer.Serialize(writer, value.ValidRoomTypes, options);
        writer.WriteNumber("weight", value.Weight);

        // Write shape
        writer.WritePropertyName("shape");
        WriteShape(writer, value);

        // Write door edges
        writer.WritePropertyName("doorEdges");
        WriteDoorEdges(writer, value, options);

        // Write interior features if any
        if (value.InteriorFeatures.Count > 0)
        {
            writer.WritePropertyName("interiorFeatures");
            writer.WriteStartObject();
            foreach (var kvp in value.InteriorFeatures)
            {
                var key = $"{kvp.Key.X},{kvp.Key.Y}";
                writer.WriteString(key, kvp.Value.ToString());
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private HashSet<Cell> ParseShape(string? shapeType, JsonElement shapeElement)
    {
        return shapeType switch
        {
            "rectangle" => ParseRectangleShape(shapeElement),
            "lShape" => ParseLShapeShape(shapeElement),
            "custom" => ParseCustomShape(shapeElement),
            _ => throw new JsonException($"Unknown shape type: {shapeType}")
        };
    }

    private HashSet<Cell> ParseRectangleShape(JsonElement shapeElement)
    {
        var width = shapeElement.GetProperty("width").GetInt32();
        var height = shapeElement.GetProperty("height").GetInt32();
        var cells = new HashSet<Cell>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells.Add(new Cell(x, y));
            }
        }
        return cells;
    }

    private HashSet<Cell> ParseLShapeShape(JsonElement shapeElement)
    {
        var width = shapeElement.GetProperty("width").GetInt32();
        var height = shapeElement.GetProperty("height").GetInt32();
        var cutoutWidth = shapeElement.GetProperty("cutoutWidth").GetInt32();
        var cutoutHeight = shapeElement.GetProperty("cutoutHeight").GetInt32();
        var cutoutCorner = shapeElement.GetProperty("cutoutCorner").GetString();
        
        var cells = new HashSet<Cell>();
        
        // Start with full rectangle
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                cells.Add(new Cell(x, y));
            }
        }

        // Remove cutout
        var corner = Enum.Parse<Corner>(cutoutCorner ?? "TopLeft");
        int startX = 0, startY = 0;
        switch (corner)
        {
            case Corner.TopLeft:
                startX = 0;
                startY = 0;
                break;
            case Corner.TopRight:
                startX = width - cutoutWidth;
                startY = 0;
                break;
            case Corner.BottomLeft:
                startX = 0;
                startY = height - cutoutHeight;
                break;
            case Corner.BottomRight:
                startX = width - cutoutWidth;
                startY = height - cutoutHeight;
                break;
        }

        for (int dy = 0; dy < cutoutHeight; dy++)
        {
            for (int dx = 0; dx < cutoutWidth; dx++)
            {
                cells.Remove(new Cell(startX + dx, startY + dy));
            }
        }

        return cells;
    }

    private HashSet<Cell> ParseCustomShape(JsonElement shapeElement)
    {
        var cellsArray = shapeElement.GetProperty("cells").Deserialize<Cell[]>(new JsonSerializerOptions
        {
            Converters = { new CellJsonConverter() }
        }) ?? throw new JsonException("Custom shape must have a 'cells' array.");
        return new HashSet<Cell>(cellsArray);
    }

    private Dictionary<Cell, Edge> ParseDoorEdges(JsonElement root, HashSet<Cell> cells, JsonSerializerOptions options)
    {
        if (!root.TryGetProperty("doorEdges", out var doorEdgesElement))
        {
            throw new JsonException("Template must have 'doorEdges' property.");
        }

        var strategy = doorEdgesElement.GetProperty("strategy").GetString();
        var doorEdges = new Dictionary<Cell, Edge>();

        switch (strategy)
        {
            case "allExteriorEdges":
                // Calculate all exterior edges
                foreach (var cell in cells)
                {
                    var exteriorEdges = Edge.None;
                    if (!cells.Contains(cell.North)) exteriorEdges |= Edge.North;
                    if (!cells.Contains(cell.South)) exteriorEdges |= Edge.South;
                    if (!cells.Contains(cell.East)) exteriorEdges |= Edge.East;
                    if (!cells.Contains(cell.West)) exteriorEdges |= Edge.West;
                    if (exteriorEdges != Edge.None)
                    {
                        doorEdges[cell] = exteriorEdges;
                    }
                }
                break;

            case "sides":
                var sides = doorEdgesElement.GetProperty("sides").Deserialize<string[]>(options) 
                    ?? throw new JsonException("Sides strategy must have 'sides' array.");
                var allowedSides = Edge.None;
                foreach (var side in sides)
                {
                    if (Enum.TryParse<Edge>(side, true, out var edge))
                    {
                        allowedSides |= edge;
                    }
                }
                // Apply to bounding box edges
                if (cells.Count > 0)
                {
                    int minX = cells.Min(c => c.X);
                    int maxX = cells.Max(c => c.X);
                    int minY = cells.Min(c => c.Y);
                    int maxY = cells.Max(c => c.Y);

                    foreach (var cell in cells)
                    {
                        var edges = Edge.None;
                        if (cell.Y == minY && allowedSides.HasFlag(Edge.North) && !cells.Contains(cell.North))
                            edges |= Edge.North;
                        if (cell.Y == maxY && allowedSides.HasFlag(Edge.South) && !cells.Contains(cell.South))
                            edges |= Edge.South;
                        if (cell.X == maxX && allowedSides.HasFlag(Edge.East) && !cells.Contains(cell.East))
                            edges |= Edge.East;
                        if (cell.X == minX && allowedSides.HasFlag(Edge.West) && !cells.Contains(cell.West))
                            edges |= Edge.West;
                        if (edges != Edge.None)
                        {
                            doorEdges[cell] = edges;
                        }
                    }
                }
                break;

            case "explicit":
                var edgesObj = doorEdgesElement.GetProperty("edges").Deserialize<Dictionary<string, string[]>>(options)
                    ?? throw new JsonException("Explicit strategy must have 'edges' object.");
                foreach (var kvp in edgesObj)
                {
                    var cellParts = kvp.Key.Split(',');
                    if (cellParts.Length == 2 && int.TryParse(cellParts[0], out var x) && int.TryParse(cellParts[1], out var y))
                    {
                        var cell = new Cell(x, y);
                        if (cells.Contains(cell))
                        {
                            var edges = Edge.None;
                            foreach (var edgeName in kvp.Value)
                            {
                                if (Enum.TryParse<Edge>(edgeName, true, out var edge))
                                {
                                    edges |= edge;
                                }
                            }
                            doorEdges[cell] = edges;
                        }
                    }
                }
                break;

            default:
                throw new JsonException($"Unknown door edges strategy: {strategy}");
        }

        return doorEdges;
    }

    private void WriteShape(Utf8JsonWriter writer, RoomTemplate<TRoomType> template)
    {
        writer.WriteStartObject();

        // Detect shape type
        if (IsRectangle(template.Cells, out var width, out var height))
        {
            writer.WriteString("type", "rectangle");
            writer.WriteNumber("width", width);
            writer.WriteNumber("height", height);
        }
        else if (IsLShape(template.Cells, out width, out height, out var cutoutWidth, out var cutoutHeight, out var cutoutCorner))
        {
            writer.WriteString("type", "lShape");
            writer.WriteNumber("width", width);
            writer.WriteNumber("height", height);
            writer.WriteNumber("cutoutWidth", cutoutWidth);
            writer.WriteNumber("cutoutHeight", cutoutHeight);
            writer.WriteString("cutoutCorner", cutoutCorner.ToString());
        }
        else
        {
            writer.WriteString("type", "custom");
            writer.WritePropertyName("cells");
            writer.WriteStartArray();
            foreach (var cell in template.Cells.OrderBy(c => c.Y).ThenBy(c => c.X))
            {
                JsonSerializer.Serialize(writer, cell, new JsonSerializerOptions { Converters = { new CellJsonConverter() } });
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private bool IsRectangle(IReadOnlySet<Cell> cells, out int width, out int height)
    {
        if (cells.Count == 0)
        {
            width = height = 0;
            return false;
        }

        int minX = cells.Min(c => c.X);
        int maxX = cells.Max(c => c.X);
        int minY = cells.Min(c => c.Y);
        int maxY = cells.Max(c => c.Y);
        width = maxX - minX + 1;
        height = maxY - minY + 1;

        // Check if all cells form a rectangle
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!cells.Contains(new Cell(x, y)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsLShape(IReadOnlySet<Cell> cells, out int width, out int height, out int cutoutWidth, out int cutoutHeight, out Corner cutoutCorner)
    {
        width = height = cutoutWidth = cutoutHeight = 0;
        cutoutCorner = Corner.TopLeft;

        if (cells.Count == 0)
        {
            return false;
        }

        int minX = cells.Min(c => c.X);
        int maxX = cells.Max(c => c.X);
        int minY = cells.Min(c => c.Y);
        int maxY = cells.Max(c => c.Y);
        width = maxX - minX + 1;
        height = maxY - minY + 1;

        // Check if it's a rectangle with one corner cut out
        var fullRectangle = new HashSet<Cell>();
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                fullRectangle.Add(new Cell(x, y));
            }
        }

        var missing = fullRectangle.Except(cells).ToList();
        if (missing.Count == 0)
        {
            return false; // It's a full rectangle, not an L-shape
        }

        // Check if missing cells form a rectangle in a corner
        int missingMinX = missing.Min(c => c.X);
        int missingMaxX = missing.Max(c => c.X);
        int missingMinY = missing.Min(c => c.Y);
        int missingMaxY = missing.Max(c => c.Y);
        cutoutWidth = missingMaxX - missingMinX + 1;
        cutoutHeight = missingMaxY - missingMinY + 1;

        // Require cutout to be at least 2x2 to be considered an L-shape
        // This prevents single-cell cutouts from being detected as L-shapes
        if (cutoutWidth < 2 || cutoutHeight < 2)
        {
            return false;
        }

        // Check if all missing cells form a rectangle
        for (int y = missingMinY; y <= missingMaxY; y++)
        {
            for (int x = missingMinX; x <= missingMaxX; x++)
            {
                if (!missing.Contains(new Cell(x, y)))
                {
                    return false;
                }
            }
        }

        // Determine corner
        if (missingMinX == minX && missingMinY == minY)
            cutoutCorner = Corner.TopLeft;
        else if (missingMaxX == maxX && missingMinY == minY)
            cutoutCorner = Corner.TopRight;
        else if (missingMinX == minX && missingMaxY == maxY)
            cutoutCorner = Corner.BottomLeft;
        else if (missingMaxX == maxX && missingMaxY == maxY)
            cutoutCorner = Corner.BottomRight;
        else
            return false;

        return true;
    }

    private void WriteDoorEdges(Utf8JsonWriter writer, RoomTemplate<TRoomType> template, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Detect strategy
        if (IsAllExteriorEdges(template))
        {
            writer.WriteString("strategy", "allExteriorEdges");
        }
        else if (IsSidesStrategy(template, out var sides))
        {
            writer.WriteString("strategy", "sides");
            writer.WritePropertyName("sides");
            JsonSerializer.Serialize(writer, sides, options);
        }
        else
        {
            writer.WriteString("strategy", "explicit");
            writer.WritePropertyName("edges");
            writer.WriteStartObject();
            foreach (var kvp in template.DoorEdges)
            {
                var key = $"{kvp.Key.X},{kvp.Key.Y}";
                var edges = new List<string>();
                if (kvp.Value.HasFlag(Edge.North)) edges.Add("North");
                if (kvp.Value.HasFlag(Edge.South)) edges.Add("South");
                if (kvp.Value.HasFlag(Edge.East)) edges.Add("East");
                if (kvp.Value.HasFlag(Edge.West)) edges.Add("West");
                writer.WritePropertyName(key);
                JsonSerializer.Serialize(writer, edges, options);
            }
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }

    private bool IsAllExteriorEdges(RoomTemplate<TRoomType> template)
    {
        foreach (var cell in template.Cells)
        {
            var expectedEdges = Edge.None;
            if (!template.Cells.Contains(cell.North)) expectedEdges |= Edge.North;
            if (!template.Cells.Contains(cell.South)) expectedEdges |= Edge.South;
            if (!template.Cells.Contains(cell.East)) expectedEdges |= Edge.East;
            if (!template.Cells.Contains(cell.West)) expectedEdges |= Edge.West;

            if (template.DoorEdges.TryGetValue(cell, out var actualEdges))
            {
                if (actualEdges != expectedEdges)
                {
                    return false;
                }
            }
            else if (expectedEdges != Edge.None)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsSidesStrategy(RoomTemplate<TRoomType> template, out List<string> sides)
    {
        sides = new List<string>();
        if (template.Cells.Count == 0) return false;

        int minX = template.Cells.Min(c => c.X);
        int maxX = template.Cells.Max(c => c.X);
        int minY = template.Cells.Min(c => c.Y);
        int maxY = template.Cells.Max(c => c.Y);

        var allowedSides = Edge.None;
        foreach (var kvp in template.DoorEdges)
        {
            var cell = kvp.Key;
            var edges = kvp.Value;
            if (cell.Y == minY && edges.HasFlag(Edge.North)) allowedSides |= Edge.North;
            if (cell.Y == maxY && edges.HasFlag(Edge.South)) allowedSides |= Edge.South;
            if (cell.X == maxX && edges.HasFlag(Edge.East)) allowedSides |= Edge.East;
            if (cell.X == minX && edges.HasFlag(Edge.West)) allowedSides |= Edge.West;
        }

        if (allowedSides.HasFlag(Edge.North)) sides.Add("North");
        if (allowedSides.HasFlag(Edge.South)) sides.Add("South");
        if (allowedSides.HasFlag(Edge.East)) sides.Add("East");
        if (allowedSides.HasFlag(Edge.West)) sides.Add("West");

        // Check if all door edges match the sides strategy
        foreach (var kvp in template.DoorEdges)
        {
            var cell = kvp.Key;
            var edges = kvp.Value;
            var expectedEdges = Edge.None;
            if (cell.Y == minY && allowedSides.HasFlag(Edge.North) && !template.Cells.Contains(cell.North))
                expectedEdges |= Edge.North;
            if (cell.Y == maxY && allowedSides.HasFlag(Edge.South) && !template.Cells.Contains(cell.South))
                expectedEdges |= Edge.South;
            if (cell.X == maxX && allowedSides.HasFlag(Edge.East) && !template.Cells.Contains(cell.East))
                expectedEdges |= Edge.East;
            if (cell.X == minX && allowedSides.HasFlag(Edge.West) && !template.Cells.Contains(cell.West))
                expectedEdges |= Edge.West;

            if (edges != expectedEdges)
            {
                return false;
            }
        }

        return sides.Count > 0;
    }
}

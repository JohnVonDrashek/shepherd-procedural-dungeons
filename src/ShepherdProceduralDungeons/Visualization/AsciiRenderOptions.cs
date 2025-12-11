using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Visualization;

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

    /// <summary>
    /// Whether to show cluster boundaries.
    /// </summary>
    public bool ShowClusterBoundaries { get; init; } = false;

    /// <summary>
    /// Whether to show cluster IDs.
    /// </summary>
    public bool ShowClusterIds { get; init; } = false;
}

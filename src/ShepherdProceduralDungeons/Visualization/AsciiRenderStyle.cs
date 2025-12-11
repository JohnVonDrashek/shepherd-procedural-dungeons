namespace ShepherdProceduralDungeons.Visualization;

/// <summary>
/// Rendering style presets for ASCII map visualization.
/// </summary>
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

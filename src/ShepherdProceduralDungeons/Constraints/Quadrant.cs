namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Represents quadrants of a dungeon layout.
/// Used by spatial constraints to specify where rooms can be placed.
/// </summary>
[Flags]
public enum Quadrant
{
    /// <summary>Top-left quadrant (negative X, negative Y relative to center).</summary>
    TopLeft = 1,

    /// <summary>Top-right quadrant (positive X, negative Y relative to center).</summary>
    TopRight = 2,

    /// <summary>Bottom-left quadrant (negative X, positive Y relative to center).</summary>
    BottomLeft = 4,

    /// <summary>Bottom-right quadrant (positive X, positive Y relative to center).</summary>
    BottomRight = 8,

    /// <summary>Center region (near the center of the dungeon).</summary>
    Center = 16
}

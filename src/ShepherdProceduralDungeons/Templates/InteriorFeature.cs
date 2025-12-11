namespace ShepherdProceduralDungeons.Templates;

/// <summary>
/// Types of interior features that can be placed within room templates.
/// </summary>
public enum InteriorFeature
{
    /// <summary>Pillar - Obstacle that blocks movement but not line of sight.</summary>
    Pillar,

    /// <summary>Wall - Interior wall that creates sub-areas within rooms.</summary>
    Wall,

    /// <summary>Hazard - Special cells that might contain traps, lava, spikes, etc.</summary>
    Hazard,

    /// <summary>Decorative - Visual markers for special areas (altars, fountains, etc.).</summary>
    Decorative
}

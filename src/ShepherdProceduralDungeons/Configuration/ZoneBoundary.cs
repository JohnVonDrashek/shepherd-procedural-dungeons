namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Base class for zone boundary definitions.
/// Boundaries determine which rooms belong to a zone.
/// </summary>
public abstract class ZoneBoundary
{
    /// <summary>
    /// Distance-based zone boundary. Rooms are assigned to the zone based on their distance from the start node.
    /// </summary>
    public sealed class DistanceBased : ZoneBoundary
    {
        /// <summary>
        /// Minimum distance from start (inclusive).
        /// </summary>
        public required int MinDistance { get; init; }

        /// <summary>
        /// Maximum distance from start (inclusive).
        /// </summary>
        public required int MaxDistance { get; init; }
    }

    /// <summary>
    /// Critical path-based zone boundary. Rooms are assigned to the zone based on their position along the critical path.
    /// </summary>
    public sealed class CriticalPathBased : ZoneBoundary
    {
        /// <summary>
        /// Start percentage along critical path (0.0 to 1.0).
        /// </summary>
        public required float StartPercent { get; init; }

        /// <summary>
        /// End percentage along critical path (0.0 to 1.0).
        /// </summary>
        public required float EndPercent { get; init; }
    }
}

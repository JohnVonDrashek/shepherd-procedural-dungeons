using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Configuration;

/// <summary>
/// Represents a biome or thematic zone within a dungeon floor.
/// Zones partition the dungeon into distinct regions with different generation rules.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public sealed class Zone<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Unique identifier for this zone.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name for this zone.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Defines the spatial boundaries of this zone.
    /// </summary>
    public required ZoneBoundary Boundary { get; init; }

    /// <summary>
    /// Zone-specific room type requirements (beyond global requirements).
    /// </summary>
    public IReadOnlyList<(TRoomType Type, int Count)>? RoomRequirements { get; init; }

    /// <summary>
    /// Zone-specific constraints for room type placement.
    /// </summary>
    public IReadOnlyList<IConstraint<TRoomType>>? Constraints { get; init; }

    /// <summary>
    /// Zone-specific template pool. If provided, these templates are preferred for rooms in this zone.
    /// Falls back to global templates if not provided or if no matching template exists.
    /// </summary>
    public IReadOnlyList<RoomTemplate<TRoomType>>? Templates { get; init; }
}

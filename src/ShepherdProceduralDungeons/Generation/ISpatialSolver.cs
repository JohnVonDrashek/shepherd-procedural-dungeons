using ShepherdProceduralDungeons.Configuration;
using ShepherdProceduralDungeons.Constraints;
using ShepherdProceduralDungeons.Graph;
using ShepherdProceduralDungeons.Layout;
using ShepherdProceduralDungeons.Templates;

namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Interface for spatial placement solvers that position rooms in 2D space.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public interface ISpatialSolver<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Places all rooms in 2D space.
    /// </summary>
    /// <param name="graph">The floor graph with room types assigned.</param>
    /// <param name="assignments">Room type for each node.</param>
    /// <param name="templates">Available templates keyed by room type.</param>
    /// <param name="hallwayMode">How to handle non-adjacent rooms.</param>
    /// <param name="rng">Random number generator.</param>
    /// <param name="constraints">Optional spatial constraints to validate placements.</param>
    /// <returns>List of placed rooms with positions.</returns>
    IReadOnlyList<PlacedRoom<TRoomType>> Solve(
        FloorGraph graph,
        IReadOnlyDictionary<int, TRoomType> assignments,
        IReadOnlyDictionary<TRoomType, IReadOnlyList<RoomTemplate<TRoomType>>> templates,
        HallwayMode hallwayMode,
        Random rng,
        IReadOnlyList<IConstraint<TRoomType>>? constraints = null);
}


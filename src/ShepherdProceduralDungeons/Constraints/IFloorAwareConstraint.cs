namespace ShepherdProceduralDungeons.Constraints;

/// <summary>
/// Interface for constraints that need to know which floor they're being evaluated on.
/// </summary>
/// <typeparam name="TRoomType">The enum type representing different room types.</typeparam>
public interface IFloorAwareConstraint<TRoomType> : IConstraint<TRoomType> where TRoomType : Enum
{
    /// <summary>
    /// Sets the current floor index for constraint evaluation.
    /// </summary>
    /// <param name="floorIndex">The 0-based index of the floor being generated.</param>
    void SetFloorIndex(int floorIndex);
}

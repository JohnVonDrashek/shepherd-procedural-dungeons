namespace ShepherdProceduralDungeons.Generation;

/// <summary>
/// Utility methods for collection operations.
/// </summary>
public static class CollectionUtilities
{
    /// <summary>
    /// Shuffles a list in-place using the Fisher-Yates shuffle algorithm.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to shuffle.</param>
    /// <param name="rng">The random number generator to use.</param>
    public static void Shuffle<T>(IList<T> list, Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}

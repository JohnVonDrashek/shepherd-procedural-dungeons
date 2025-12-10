namespace ShepherdProceduralDungeons.Exceptions;

/// <summary>
/// Base exception for all dungeon generation errors.
/// </summary>
public class GenerationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the GenerationException class with a specified error message.
    /// </summary>
    public GenerationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the GenerationException class with a specified error message and inner exception.
    /// </summary>
    public GenerationException(string message, Exception inner) : base(message, inner) { }
}

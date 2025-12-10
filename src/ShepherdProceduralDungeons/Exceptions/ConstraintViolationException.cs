namespace ShepherdProceduralDungeons.Exceptions;

/// <summary>
/// Exception thrown when room type constraints cannot be satisfied during generation.
/// </summary>
public class ConstraintViolationException : GenerationException
{
    /// <summary>
    /// Gets the type of constraint that was violated, if available.
    /// </summary>
    public string? ConstraintType { get; init; }

    /// <summary>
    /// Initializes a new instance of the ConstraintViolationException class with a specified error message.
    /// </summary>
    public ConstraintViolationException(string message) : base(message) { }
}

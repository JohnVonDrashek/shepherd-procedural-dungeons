namespace ShepherdProceduralDungeons.Exceptions;

/// <summary>
/// Exception thrown when the generation configuration is invalid.
/// This is detected before generation begins.
/// </summary>
public class InvalidConfigurationException : GenerationException
{
    /// <summary>
    /// Initializes a new instance of the InvalidConfigurationException class with a specified error message.
    /// </summary>
    public InvalidConfigurationException(string message) : base(message) { }
}

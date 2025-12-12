using System;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Attribute to mark integration tests with appropriate timeout settings.
/// Use this attribute in combination with [Fact] to mark tests as integration tests.
/// Example: [Fact(Timeout = IntegrationTestTimeout)] [IntegrationTest]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class IntegrationTestAttribute : Attribute
{
    /// <summary>
    /// Default timeout for integration tests (30 seconds).
    /// </summary>
    public const int DefaultTimeoutMs = 30000;
}

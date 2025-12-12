using System.Diagnostics;
using System.Reflection;
using System.Text;
using ShepherdProceduralDungeons;
using Xunit;

namespace ShepherdProceduralDungeons.Tests;

/// <summary>
/// Tests for the DEBUG Logging Optimization System feature.
/// These tests verify that the DebugLogger class provides configurable logging
/// with log levels, component filtering, and test context detection.
/// </summary>
public class DebugLoggingOptimizationTests
{
    /// <summary>
    /// Test Scenario 1: Verbose Log Suppression in Tests
    /// When running tests, VERBOSE level logs should be automatically suppressed,
    /// but INFO/WARN/ERROR logs should still appear.
    /// </summary>
    [Fact]
    public void VerboseLogSuppression_InTestContext_SuppressesVerboseButKeepsInfoWarnError()
    {
        // Arrange
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Log messages at different levels
            DebugLogger.LogVerbose(DebugLogger.Component.AStar, "This verbose message should be suppressed");
            DebugLogger.LogInfo(DebugLogger.Component.AStar, "This info message should appear");
            DebugLogger.LogWarn(DebugLogger.Component.AStar, "This warn message should appear");
            DebugLogger.LogError(DebugLogger.Component.AStar, "This error message should appear");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.DoesNotContain("This verbose message should be suppressed", outputText);
            Assert.Contains("This info message should appear", outputText);
            Assert.Contains("This warn message should appear", outputText);
            Assert.Contains("This error message should appear", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// Test Scenario 2: Environment Variable Override
    /// Setting SHEPHERD_DEBUG_LEVEL=VERBOSE should enable all log levels
    /// even during test execution.
    /// </summary>
    [Fact]
    public void EnvironmentVariableOverride_SetToVerbose_EnablesAllLogLevels()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("SHEPHERD_DEBUG_LEVEL");
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_LEVEL", "VERBOSE");
            
            // Reset logger to pick up environment variable
            ResetDebugLoggerConfiguration();
            
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act
            DebugLogger.LogVerbose(DebugLogger.Component.AStar, "Verbose message with VERBOSE level enabled");
            DebugLogger.LogInfo(DebugLogger.Component.AStar, "Info message");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.Contains("Verbose message with VERBOSE level enabled", outputText);
            Assert.Contains("Info message", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            if (originalEnv != null)
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_LEVEL", originalEnv);
            }
            else
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_LEVEL", null);
            }
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 3: Component Filtering
    /// Setting SHEPHERD_DEBUG_COMPONENTS=AStar should only log AStar component messages,
    /// filtering out other components.
    /// </summary>
    [Fact]
    public void ComponentFiltering_SetToAStar_OnlyLogsAStarComponent()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS");
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", "AStar");
            
            // Reset logger to pick up environment variable
            ResetDebugLoggerConfiguration();
            
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act
            DebugLogger.LogInfo(DebugLogger.Component.AStar, "AStar message should appear");
            DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, "RoomPlacement message should be filtered");
            DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, "HallwayGeneration message should be filtered");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.Contains("AStar message should appear", outputText);
            Assert.DoesNotContain("RoomPlacement message should be filtered", outputText);
            Assert.DoesNotContain("HallwayGeneration message should be filtered", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            if (originalEnv != null)
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", originalEnv);
            }
            else
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", null);
            }
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 4: Performance Impact
    /// Disabling verbose logging should have zero measurable performance impact
    /// (no string formatting overhead).
    /// </summary>
    [Fact]
    public void PerformanceImpact_VerboseDisabled_NoStringFormattingOverhead()
    {
        // Arrange - Ensure verbose logging is disabled (default in test context)
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            // Reset logger to ensure clean state
            ResetDebugLoggerConfiguration();
            
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Call LogVerbose many times (should be fast since it's disabled)
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                DebugLogger.LogVerbose(DebugLogger.Component.AStar, $"Message {i} with expensive formatting: {ComplexStringFormatting(i)}");
            }
            stopwatch.Stop();
            
            // Assert - Should complete very quickly (< 100ms) since verbose is disabled
            // If string formatting occurred in the logging method itself, this would take much longer
            // Note: String formatting in the test (before method call) still happens, but the method
            // should return immediately without doing any work, so overhead should be minimal
            Assert.True(stopwatch.ElapsedMilliseconds < 100, 
                $"Logging 10000 disabled verbose messages took {stopwatch.ElapsedMilliseconds}ms, expected < 100ms");
            
            // Flush and get output before disposing writer
            writer.Flush();
            var outputText = output.ToString();
            
            // Verify no VERBOSE output was generated (other log levels might have output from other code)
            Assert.DoesNotContain("[VERBOSE]", outputText);
            Assert.DoesNotContain("Message 0 with expensive formatting", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 5: Log Level Hierarchy
    /// Setting log level to INFO should suppress VERBOSE but show INFO/WARN/ERROR.
    /// Setting to WARN should only show WARN/ERROR.
    /// </summary>
    [Fact]
    public void LogLevelHierarchy_SetToInfo_SuppressesVerboseButShowsInfoWarnError()
    {
        // Arrange
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Set log level to INFO
            DebugLogger.SetLogLevel(DebugLogger.LogLevel.Info);
            
            DebugLogger.LogVerbose(DebugLogger.Component.General, "Verbose should be suppressed");
            DebugLogger.LogInfo(DebugLogger.Component.General, "Info should appear");
            DebugLogger.LogWarn(DebugLogger.Component.General, "Warn should appear");
            DebugLogger.LogError(DebugLogger.Component.General, "Error should appear");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.DoesNotContain("Verbose should be suppressed", outputText);
            Assert.Contains("Info should appear", outputText);
            Assert.Contains("Warn should appear", outputText);
            Assert.Contains("Error should appear", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 5: Log Level Hierarchy (WARN level)
    /// Setting log level to WARN should only show WARN/ERROR, suppressing VERBOSE/INFO.
    /// </summary>
    [Fact]
    public void LogLevelHierarchy_SetToWarn_OnlyShowsWarnAndError()
    {
        // Arrange
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Set log level to WARN
            DebugLogger.SetLogLevel(DebugLogger.LogLevel.Warn);
            
            DebugLogger.LogVerbose(DebugLogger.Component.General, "Verbose should be suppressed");
            DebugLogger.LogInfo(DebugLogger.Component.General, "Info should be suppressed");
            DebugLogger.LogWarn(DebugLogger.Component.General, "Warn should appear");
            DebugLogger.LogError(DebugLogger.Component.General, "Error should appear");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.DoesNotContain("Verbose should be suppressed", outputText);
            Assert.DoesNotContain("Info should be suppressed", outputText);
            Assert.Contains("Warn should appear", outputText);
            Assert.Contains("Error should appear", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 6: Test Context Detection
    /// DebugLogger should detect test context and automatically suppress VERBOSE logs.
    /// </summary>
    [Fact]
    public void TestContextDetection_IsTestContext_ReturnsTrue()
    {
        // Assert - In test context, IsTestContext should return true
        Assert.True(DebugLogger.IsTestContext, "IsTestContext should return true when running in test context");
    }

    /// <summary>
    /// Test Scenario 7: Component Enable/Disable
    /// Programmatically enabling/disabling components should work correctly.
    /// </summary>
    [Fact]
    public void ComponentEnableDisable_EnableAStar_OnlyAStarLogsAppear()
    {
        // Arrange
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Enable only AStar component
            DebugLogger.EnableComponent(DebugLogger.Component.AStar);
            DebugLogger.DisableComponent(DebugLogger.Component.RoomPlacement);
            DebugLogger.DisableComponent(DebugLogger.Component.HallwayGeneration);
            
            DebugLogger.LogInfo(DebugLogger.Component.AStar, "AStar message should appear");
            DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, "RoomPlacement message should be filtered");
            DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, "HallwayGeneration message should be filtered");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.Contains("AStar message should appear", outputText);
            Assert.DoesNotContain("RoomPlacement message should be filtered", outputText);
            Assert.DoesNotContain("HallwayGeneration message should be filtered", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 8: Conditional Compilation
    /// DebugLogger methods should be marked with [Conditional("DEBUG")] to ensure
    /// zero overhead in release builds.
    /// </summary>
    [Fact]
    public void ConditionalCompilation_MethodsMarkedWithConditionalDebug_ZeroOverheadInRelease()
    {
        // Arrange - Check that DebugLogger methods have Conditional attribute
        var logVerboseMethod = typeof(DebugLogger).GetMethod(nameof(DebugLogger.LogVerbose));
        var logInfoMethod = typeof(DebugLogger).GetMethod(nameof(DebugLogger.LogInfo));
        var logWarnMethod = typeof(DebugLogger).GetMethod(nameof(DebugLogger.LogWarn));
        var logErrorMethod = typeof(DebugLogger).GetMethod(nameof(DebugLogger.LogError));
        
        // Assert - All logging methods should have Conditional("DEBUG") attribute
        Assert.NotNull(logVerboseMethod);
        Assert.NotNull(logInfoMethod);
        Assert.NotNull(logWarnMethod);
        Assert.NotNull(logErrorMethod);
        
        var verboseConditional = logVerboseMethod!.GetCustomAttribute<ConditionalAttribute>();
        var infoConditional = logInfoMethod!.GetCustomAttribute<ConditionalAttribute>();
        var warnConditional = logWarnMethod!.GetCustomAttribute<ConditionalAttribute>();
        var errorConditional = logErrorMethod!.GetCustomAttribute<ConditionalAttribute>();
        
        Assert.NotNull(verboseConditional);
        Assert.NotNull(infoConditional);
        Assert.NotNull(warnConditional);
        Assert.NotNull(errorConditional);
        
        Assert.Equal("DEBUG", verboseConditional!.ConditionString);
        Assert.Equal("DEBUG", infoConditional!.ConditionString);
        Assert.Equal("DEBUG", warnConditional!.ConditionString);
        Assert.Equal("DEBUG", errorConditional!.ConditionString);
    }

    /// <summary>
    /// Test Scenario 9: All Components Exist
    /// Verify that all required component types exist in the Component enum.
    /// </summary>
    [Fact]
    public void AllComponentsExist_ComponentEnum_HasAllRequiredComponents()
    {
        // Assert - Verify all required components exist
        var componentType = typeof(DebugLogger.Component);
        var components = Enum.GetValues(componentType).Cast<DebugLogger.Component>().ToList();
        
        Assert.Contains(DebugLogger.Component.AStar, components);
        Assert.Contains(DebugLogger.Component.RoomPlacement, components);
        Assert.Contains(DebugLogger.Component.HallwayGeneration, components);
        Assert.Contains(DebugLogger.Component.GraphGeneration, components);
        Assert.Contains(DebugLogger.Component.ConstraintEvaluation, components);
        Assert.Contains(DebugLogger.Component.General, components);
    }

    /// <summary>
    /// Test Scenario 10: All Log Levels Exist
    /// Verify that all required log levels exist in the LogLevel enum.
    /// </summary>
    [Fact]
    public void AllLogLevelsExist_LogLevelEnum_HasAllRequiredLevels()
    {
        // Assert - Verify all required log levels exist
        var logLevelType = typeof(DebugLogger.LogLevel);
        var logLevels = Enum.GetValues(logLevelType).Cast<DebugLogger.LogLevel>().ToList();
        
        Assert.Contains(DebugLogger.LogLevel.Verbose, logLevels);
        Assert.Contains(DebugLogger.LogLevel.Info, logLevels);
        Assert.Contains(DebugLogger.LogLevel.Warn, logLevels);
        Assert.Contains(DebugLogger.LogLevel.Error, logLevels);
    }

    /// <summary>
    /// Test Scenario 11: Multiple Components in Environment Variable
    /// Setting SHEPHERD_DEBUG_COMPONENTS to multiple comma-separated values
    /// should enable all specified components.
    /// </summary>
    [Fact]
    public void MultipleComponentsInEnvironmentVariable_CommaSeparated_EnablesAllSpecifiedComponents()
    {
        // Arrange
        var originalEnv = Environment.GetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS");
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", "AStar,HallwayGeneration");
            
            // Reset logger to pick up environment variable
            ResetDebugLoggerConfiguration();
            
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act
            DebugLogger.LogInfo(DebugLogger.Component.AStar, "AStar message should appear");
            DebugLogger.LogInfo(DebugLogger.Component.HallwayGeneration, "HallwayGeneration message should appear");
            DebugLogger.LogInfo(DebugLogger.Component.RoomPlacement, "RoomPlacement message should be filtered");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.Contains("AStar message should appear", outputText);
            Assert.Contains("HallwayGeneration message should appear", outputText);
            Assert.DoesNotContain("RoomPlacement message should be filtered", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            if (originalEnv != null)
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", originalEnv);
            }
            else
            {
                Environment.SetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS", null);
            }
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Test Scenario 12: Error Level Always Logs
    /// ERROR level logs should always appear regardless of log level setting.
    /// </summary>
    [Fact]
    public void ErrorLevelAlwaysLogs_RegardlessOfLogLevel_AlwaysAppears()
    {
        // Arrange
        var output = new StringBuilder();
        var originalOut = Console.Out;
        
        try
        {
            using var writer = new StringWriter(output);
            Console.SetOut(writer);
            
            // Act - Set log level to ERROR (most restrictive)
            DebugLogger.SetLogLevel(DebugLogger.LogLevel.Error);
            
            DebugLogger.LogVerbose(DebugLogger.Component.General, "Verbose should be suppressed");
            DebugLogger.LogInfo(DebugLogger.Component.General, "Info should be suppressed");
            DebugLogger.LogWarn(DebugLogger.Component.General, "Warn should be suppressed");
            DebugLogger.LogError(DebugLogger.Component.General, "Error should always appear");
            
            // Flush before reading
            writer.Flush();
            
            // Assert
            var outputText = writer.ToString();
            Assert.DoesNotContain("Verbose should be suppressed", outputText);
            Assert.DoesNotContain("Info should be suppressed", outputText);
            Assert.DoesNotContain("Warn should be suppressed", outputText);
            Assert.Contains("Error should always appear", outputText);
        }
        finally
        {
            Console.SetOut(originalOut);
            ResetDebugLoggerConfiguration();
        }
    }

    /// <summary>
    /// Helper method to reset DebugLogger configuration to defaults.
    /// This simulates reinitializing the logger after environment variable changes.
    /// </summary>
    private void ResetDebugLoggerConfiguration()
    {
        // This will be implemented in DebugLogger - for now, this test will fail
        // which is expected in TDD red phase
        var resetMethod = typeof(DebugLogger).GetMethod("ResetConfiguration", 
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        
        if (resetMethod != null)
        {
            resetMethod.Invoke(null, null);
        }
    }

    /// <summary>
    /// Helper method to simulate expensive string formatting.
    /// Used to verify that disabled logging doesn't perform string formatting.
    /// </summary>
    private string ComplexStringFormatting(int value)
    {
        // Simulate expensive string operations
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append($"{value * i} ");
        }
        return sb.ToString();
    }
}

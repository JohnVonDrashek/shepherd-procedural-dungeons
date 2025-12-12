# Feature: DEBUG Logging Optimization System

**ID**: FEATURE-002
**Status**: complete
**Created**: 2025-01-27T05:00:00Z
**Priority**: high
**Complexity**: medium

## Description

Implement a configurable DEBUG logging optimization system to prevent test output spam from verbose debug statements. Currently, DEBUG builds log every single operation (e.g., "A* exploring node 10442") tens of thousands of times during test execution, creating massive log files, slowing test execution, and making it impossible to find meaningful debug information.

**Why this matters**: Debug logging is essential for development and troubleshooting, but unoptimized logging creates several critical problems:
- **Test performance degradation**: Writing tens of thousands of log lines to console significantly slows test execution
- **Log file bloat**: Test runs generate multi-megabyte log files that are difficult to parse
- **Signal-to-noise ratio**: Important debug information gets buried in repetitive low-level logs
- **CI/CD impact**: Excessive logging slows CI pipelines and can cause log size limits to be exceeded
- **Developer productivity**: Developers can't effectively use debug output when it's overwhelmed by noise

**Problems solved**:
- Excessive console output during test runs (tens of thousands of DEBUG lines per test)
- Inability to selectively enable/disable verbose logging for different components
- No way to detect test context and automatically suppress verbose logs
- Missing log level system (all DEBUG logs are treated equally)
- Performance overhead from unconditional string formatting and console I/O
- Difficulty finding meaningful debug information in massive log outputs

## Requirements

- [x] Log level system with VERBOSE, INFO, WARN, ERROR levels
- [x] Test context detection to automatically suppress VERBOSE logs during test execution
- [x] Environment variable configuration for log levels (e.g., `SHEPHERD_DEBUG_LEVEL=INFO`)
- [x] Per-component log level configuration (e.g., suppress A* verbose logs but keep room placement logs)
- [x] Conditional logging that avoids string formatting when logs are disabled
- [x] Backward compatibility: existing `#if DEBUG` blocks continue to work
- [x] Performance optimization: no overhead when verbose logging is disabled
- [x] Documentation for developers on how to configure and use the logging system

## Technical Details

**Implementation Approach**:

1. **DebugLogger Class**: Create a new `DebugLogger` class in `src/ShepherdProceduralDungeons/` that provides:
   - Log level enumeration: `Verbose`, `Info`, `Warn`, `Error`
   - Static methods: `LogVerbose()`, `LogInfo()`, `LogWarn()`, `LogError()`
   - Component-based filtering (e.g., "AStar", "RoomPlacement", "HallwayGeneration")
   - Test context detection via `System.Diagnostics.Debugger.IsAttached` or environment variable check
   - Configuration via environment variables: `SHEPHERD_DEBUG_LEVEL`, `SHEPHERD_DEBUG_COMPONENTS`

2. **Log Level Configuration**:
   - Default behavior: VERBOSE logs disabled during test execution (detected via test framework or environment)
   - Environment variable override: `SHEPHERD_DEBUG_LEVEL=VERBOSE` to enable all logs
   - Component-specific: `SHEPHERD_DEBUG_COMPONENTS=AStar,HallwayGeneration` to enable only specific components
   - Runtime configuration: Allow programmatic configuration for library consumers

3. **Performance Optimization**:
   - Use conditional compilation and method attributes to avoid string formatting overhead
   - Implement `[Conditional("DEBUG")]` attributes to ensure zero overhead in release builds
   - Lazy evaluation: Only format log messages when the log level is actually enabled
   - Consider using `StringBuilder` or structured logging for high-frequency logs

4. **Migration Strategy**:
   - Replace `Console.WriteLine($"[DEBUG] ...")` with `DebugLogger.LogVerbose("...")` or `DebugLogger.LogInfo("...")`
   - Keep `#if DEBUG` blocks for now, but migrate to DebugLogger calls
   - Update `DungeonDebugVisualizer` to use DebugLogger instead of direct Console.WriteLine
   - Update `HallwayGenerator` A* logging to use VERBOSE level (suppressed by default in tests)
   - Update other generators and solvers to use appropriate log levels

5. **Test Context Detection**:
   - Detect test execution via: `Assembly.GetCallingAssembly().GetName().Name.Contains("Test")`
   - Or use environment variable: `SHEPHERD_TEST_MODE=true`
   - Or check for xUnit test attributes in call stack
   - Default: Suppress VERBOSE logs in test context, keep INFO/WARN/ERROR

6. **Component Categories**:
   - `AStar`: A* pathfinding algorithm logs (most verbose)
   - `RoomPlacement`: Room placement and spatial solving logs
   - `HallwayGeneration`: Hallway generation and connection logs
   - `GraphGeneration`: Graph algorithm execution logs
   - `ConstraintEvaluation`: Constraint checking and validation logs
   - `General`: General debug information

**Architecture Considerations**:

- This feature touches multiple parts of the system: all generators, solvers, and debug visualizers
- Must integrate seamlessly with existing `#if DEBUG` conditional compilation
- Should have zero performance impact when verbose logging is disabled
- Configuration should be simple and intuitive for developers
- Must maintain backward compatibility with existing debug output expectations
- Consider future extensibility: structured logging, log file output, remote logging

**API Design**:

```csharp
public static class DebugLogger
{
    public enum LogLevel { Verbose, Info, Warn, Error }
    public enum Component { AStar, RoomPlacement, HallwayGeneration, GraphGeneration, ConstraintEvaluation, General }
    
    [Conditional("DEBUG")]
    public static void LogVerbose(Component component, string message);
    
    [Conditional("DEBUG")]
    public static void LogInfo(Component component, string message);
    
    [Conditional("DEBUG")]
    public static void LogWarn(Component component, string message);
    
    [Conditional("DEBUG")]
    public static void LogError(Component component, string message);
    
    public static void SetLogLevel(LogLevel level);
    public static void EnableComponent(Component component);
    public static void DisableComponent(Component component);
    public static bool IsTestContext { get; }
}
```

**Usage Example**:

```csharp
// Old way (logs every node, tens of thousands of times):
#if DEBUG
Console.WriteLine($"[DEBUG] A* exploring node {nodesExplored}: {current}...");
#endif

// New way (suppressed in test context, configurable):
DebugLogger.LogVerbose(Component.AStar, $"A* exploring node {nodesExplored}: {current}...");

// Important info (always logged):
DebugLogger.LogInfo(Component.AStar, $"A* found path: {pathLength} cells");
```

## Dependencies

- None

## Test Scenarios

1. **Verbose Log Suppression in Tests**: When running tests, VERBOSE level logs should be automatically suppressed, but INFO/WARN/ERROR logs should still appear.

2. **Environment Variable Override**: Setting `SHEPHERD_DEBUG_LEVEL=VERBOSE` should enable all log levels even during test execution.

3. **Component Filtering**: Setting `SHEPHERD_DEBUG_COMPONENTS=AStar` should only log AStar component messages, filtering out other components.

4. **Performance Impact**: Disabling verbose logging should have zero measurable performance impact (no string formatting overhead).

5. **Backward Compatibility**: Existing `#if DEBUG Console.WriteLine()` blocks should continue to work alongside the new logging system.

6. **Log Level Hierarchy**: Setting log level to INFO should suppress VERBOSE but show INFO/WARN/ERROR. Setting to WARN should only show WARN/ERROR.

7. **Test Output Verification**: Test runs should produce significantly fewer log lines (e.g., <100 lines instead of tens of thousands) while still showing important INFO/WARN/ERROR messages.

## Acceptance Criteria

- [x] `DebugLogger` class exists with log level and component support
- [x] Test context detection automatically suppresses VERBOSE logs during test execution
- [x] Environment variable configuration works (`SHEPHERD_DEBUG_LEVEL`, `SHEPHERD_DEBUG_COMPONENTS`)
- [x] A* pathfinding logs use VERBOSE level and are suppressed in tests by default
- [x] All existing DEBUG Console.WriteLine calls migrated to DebugLogger with appropriate levels
- [x] Performance benchmarks show no overhead when verbose logging is disabled
- [x] Test output reduced by >90% (from tens of thousands to hundreds of lines)
- [x] Documentation exists explaining log levels, configuration, and usage
- [x] All tests pass with new logging system in place
- [x] Developers can still enable verbose logging when needed for debugging

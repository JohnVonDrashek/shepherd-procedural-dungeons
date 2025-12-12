using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ShepherdProceduralDungeons;

/// <summary>
/// Configurable DEBUG logging system with log levels, component filtering, and test context detection.
/// Provides zero-overhead logging when verbose logs are disabled.
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// Log levels in order of verbosity: Verbose (most verbose) to Error (least verbose).
    /// </summary>
    public enum LogLevel
    {
        Verbose,
        Info,
        Warn,
        Error
    }

    /// <summary>
    /// Components that can be filtered for logging.
    /// </summary>
    public enum Component
    {
        AStar,
        RoomPlacement,
        HallwayGeneration,
        GraphGeneration,
        ConstraintEvaluation,
        General
    }

    private static LogLevel _currentLogLevel = LogLevel.Info;
    private static readonly HashSet<Component> _enabledComponents = new();
    private static bool _isInitialized = false;
    private static readonly object _lockObject = new();
    private static bool? _cachedIsTestContext = null;
    private static bool _verboseEnabled = false; // Cached flag for fast path

    /// <summary>
    /// Gets whether the current execution context is a test context.
    /// </summary>
    public static bool IsTestContext
    {
        get
        {
            if (_cachedIsTestContext.HasValue)
                return _cachedIsTestContext.Value;

            // Check for test assembly in call stack
            try
            {
                var stackTrace = new StackTrace();
                for (int i = 0; i < stackTrace.FrameCount; i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    if (frame == null) continue;
                    
                    var method = frame.GetMethod();
                    if (method == null) continue;
                    
                    var assembly = method.DeclaringType?.Assembly;
                    if (assembly == null) continue;
                    
                    var assemblyName = assembly.GetName().Name;
                    if (assemblyName != null && assemblyName.Contains("Test", StringComparison.OrdinalIgnoreCase))
                    {
                        _cachedIsTestContext = true;
                        return true;
                    }
                }
            }
            catch
            {
                // If we can't determine, assume not test context
            }

            // Check environment variable
            var testMode = Environment.GetEnvironmentVariable("SHEPHERD_TEST_MODE");
            if (!string.IsNullOrEmpty(testMode) && testMode.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                _cachedIsTestContext = true;
                return true;
            }

            _cachedIsTestContext = false;
            return false;
        }
    }

    static DebugLogger()
    {
        Initialize();
    }

    /// <summary>
    /// Initializes the logger configuration from environment variables.
    /// </summary>
    private static void Initialize()
    {
        lock (_lockObject)
        {
            if (_isInitialized)
                return;

            // Initialize enabled components - by default, all are enabled
            foreach (Component component in Enum.GetValues<Component>())
            {
                _enabledComponents.Add(component);
            }

            // Read log level from environment variable
            var logLevelEnv = Environment.GetEnvironmentVariable("SHEPHERD_DEBUG_LEVEL");
            if (!string.IsNullOrEmpty(logLevelEnv))
            {
                if (Enum.TryParse<LogLevel>(logLevelEnv, ignoreCase: true, out var parsedLevel))
                {
                    _currentLogLevel = parsedLevel;
                }
            }
            else
            {
                // Default: suppress VERBOSE in test context, otherwise INFO
                if (IsTestContext)
                {
                    _currentLogLevel = LogLevel.Info; // Suppress VERBOSE in tests
                }
                else
                {
                    _currentLogLevel = LogLevel.Info;
                }
            }

            // Cache whether verbose is enabled for fast path
            _verboseEnabled = _currentLogLevel <= LogLevel.Verbose;

            // Read component filter from environment variable
            var componentsEnv = Environment.GetEnvironmentVariable("SHEPHERD_DEBUG_COMPONENTS");
            if (!string.IsNullOrEmpty(componentsEnv))
            {
                _enabledComponents.Clear();
                var componentNames = componentsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var componentName in componentNames)
                {
                    if (Enum.TryParse<Component>(componentName, ignoreCase: true, out var component))
                    {
                        _enabledComponents.Add(component);
                    }
                }
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Resets the logger configuration to defaults. Used primarily for testing.
    /// </summary>
    public static void ResetConfiguration()
    {
        lock (_lockObject)
        {
            _isInitialized = false;
            _enabledComponents.Clear();
            _currentLogLevel = LogLevel.Info;
            _verboseEnabled = false;
            _cachedIsTestContext = null; // Reset cache
            Initialize();
        }
    }

    /// <summary>
    /// Sets the current log level.
    /// </summary>
    public static void SetLogLevel(LogLevel level)
    {
        lock (_lockObject)
        {
            _currentLogLevel = level;
            _verboseEnabled = level <= LogLevel.Verbose;
        }
    }

    /// <summary>
    /// Enables logging for the specified component.
    /// </summary>
    public static void EnableComponent(Component component)
    {
        lock (_lockObject)
        {
            _enabledComponents.Add(component);
        }
    }

    /// <summary>
    /// Disables logging for the specified component.
    /// </summary>
    public static void DisableComponent(Component component)
    {
        lock (_lockObject)
        {
            _enabledComponents.Remove(component);
        }
    }

    /// <summary>
    /// Checks if a log level should be output based on current configuration.
    /// Optimized to check log level first (most common early exit).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldLog(LogLevel level, Component component)
    {
        // Always log ERROR level
        if (level == LogLevel.Error)
        {
            // Still need to check component
            return _enabledComponents.Contains(component);
        }

        // Fast path: Check log level first (most common early exit)
        // If the requested level is below the current threshold, skip immediately
        if (level < _currentLogLevel)
            return false;

        // Check if component is enabled
        return _enabledComponents.Contains(component);
    }

    /// <summary>
    /// Logs a verbose message. Only outputs if VERBOSE level is enabled and component is enabled.
    /// </summary>
    [Conditional("DEBUG")]
    public static void LogVerbose(Component component, string message)
    {
        // Ultra-fast path: If verbose is disabled globally, skip immediately (no other checks needed)
        if (!_verboseEnabled)
            return;

        // Component check - only reached if verbose is enabled
        if (_enabledComponents.Contains(component))
        {
            try
            {
                Console.WriteLine($"[VERBOSE] [{component}] {message}");
            }
            catch (ObjectDisposedException)
            {
                // Console.Out was disposed (e.g., by test framework), ignore
            }
        }
    }

    /// <summary>
    /// Logs an info message. Only outputs if INFO level or higher is enabled and component is enabled.
    /// </summary>
    [Conditional("DEBUG")]
    public static void LogInfo(Component component, string message)
    {
        if (ShouldLog(LogLevel.Info, component))
        {
            try
            {
                Console.WriteLine($"[INFO] [{component}] {message}");
            }
            catch (ObjectDisposedException)
            {
                // Console.Out was disposed (e.g., by test framework), ignore
            }
        }
    }

    /// <summary>
    /// Logs a warning message. Only outputs if WARN level or higher is enabled and component is enabled.
    /// </summary>
    [Conditional("DEBUG")]
    public static void LogWarn(Component component, string message)
    {
        if (ShouldLog(LogLevel.Warn, component))
        {
            try
            {
                Console.WriteLine($"[WARN] [{component}] {message}");
            }
            catch (ObjectDisposedException)
            {
                // Console.Out was disposed (e.g., by test framework), ignore
            }
        }
    }

    /// <summary>
    /// Logs an error message. Always outputs if component is enabled (ERROR level cannot be suppressed).
    /// </summary>
    [Conditional("DEBUG")]
    public static void LogError(Component component, string message)
    {
        if (ShouldLog(LogLevel.Error, component))
        {
            try
            {
                Console.WriteLine($"[ERROR] [{component}] {message}");
            }
            catch (ObjectDisposedException)
            {
                // Console.Out was disposed (e.g., by test framework), ignore
            }
        }
    }
}

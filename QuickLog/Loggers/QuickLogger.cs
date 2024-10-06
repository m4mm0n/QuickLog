using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// A multi-purpose QuickLog
/// </summary>
public class QuickLogger : IQuickLog
{
    // Enable/disable flags for different logging mechanisms
    public bool EnableConsoleLogging { get; set; }
    public bool EnableFileLogging { get; set; }
    public bool EnableEventLogging { get; set; }
    public bool EnableTraceLogging { get; set; }

    // Internal loggers
    private readonly EventOnlyLogger? _eventLogger;
    private readonly ConsoleQuickLogger? _consoleLogger;
    private readonly FileQuickLogger? _fileLogger;
    private readonly TraceLogger? _traceLogger;

    public event EventHandler<LogEventArgs>? LogEvent;

    public QuickLogger(string? logFilePath = null)
    {
        _eventLogger = new EventOnlyLogger();
        _consoleLogger = new ConsoleQuickLogger();
        _traceLogger = new TraceLogger();
        _fileLogger = logFilePath != null ? new FileQuickLogger(logFilePath) : null;

        // Relay log events to QuickLogger's event
        _eventLogger.LogEvent += RelayLogEvent;
        _consoleLogger.LogEvent += RelayLogEvent;
        _traceLogger.LogEvent += RelayLogEvent;
        if (_fileLogger != null)
        {
            _fileLogger.LogEvent += RelayLogEvent;
        }
    }

    private void RelayLogEvent(object? sender, LogEventArgs e) => LogEvent?.Invoke(this, e);

    public void Log(LogType logType, string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (EnableConsoleLogging) _consoleLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
        if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, message, callerName, callerFilePath, callerLineNumber);
        if (EnableEventLogging) _eventLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
        if (EnableTraceLogging) _traceLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
    }

    public void Log(LogType logType, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (EnableConsoleLogging) _consoleLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableEventLogging) _eventLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableTraceLogging) _traceLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
    }

    public void Log(LogType logType, string message, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (EnableConsoleLogging) _consoleLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableEventLogging) _eventLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        if (EnableTraceLogging) _traceLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
    }

    public void TraceMethodEntry(
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            string message = $"Entering method: {method.DeclaringType?.Name}.{method.Name}";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }

    public void TraceMethodExit(Stopwatch stopwatch,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            stopwatch.Stop();
            string message = $"Exiting method: {method.DeclaringType?.Name}.{method.Name}. Execution time: {stopwatch.ElapsedMilliseconds} ms.";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }
}
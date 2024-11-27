using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// A multi-purpose logger that can log to various destinations such as Console, File, Event, and Trace.
/// </summary>
public class QuickLogger : IQuickLog, ICloneable
{
    /// <summary>
    /// The path of the log-files - used only internally!
    /// </summary>
    internal string LogPath { get; private set; }

    /// <summary>
    /// Enables or disables logging to the console.
    /// </summary>
    public bool EnableConsoleLogging { get; set; }

    /// <summary>
    /// Enables or disables logging to a file.
    /// </summary>
    public bool EnableFileLogging { get; set; }

    /// <summary>
    /// Enables or disables logging via event handlers.
    /// </summary>
    public bool EnableEventLogging { get; set; }

    /// <summary>
    /// Enables or disables logging to the system trace.
    /// </summary>
    public bool EnableTraceLogging { get; set; }

    #region Internal Loggers

    private EventOnlyLogger? _eventLogger;
    private ConsoleQuickLogger? _consoleLogger;
    private FileLogger? _fileLogger;
    private TraceLogger? _traceLogger;

    #endregion

    /// <summary>
    /// Occurs when a log event is triggered.
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickLogger"/> class with an optional file path for file logging.
    /// </summary>
    /// <param name="logFilePath">The file path for logging. If null, file logging is disabled.</param>
    public QuickLogger(string? logFilePath = null)
    {
        _eventLogger = new EventOnlyLogger();
        _consoleLogger = new ConsoleQuickLogger();
        _traceLogger = new TraceLogger();
        _fileLogger = logFilePath != null ? new FileLogger(logFilePath) : null;
        if (logFilePath != null)
            LogPath = Path.GetDirectoryName(logFilePath) ?? "logs";

        // Relay log events to QuickLogger's event
        _eventLogger.LogEvent += RelayLogEvent;
        _consoleLogger.LogEvent += RelayLogEvent;
        _traceLogger.LogEvent += RelayLogEvent;

        if (_fileLogger != null) _fileLogger.LogEvent += RelayLogEvent;
    }

    private QuickLogger()
    {
        //empty constructor for cloning
    }

    /// <summary>
    /// Relays log events from the internal loggers to the main logger event handler.
    /// </summary>
    /// <param name="sender">The sender of the log event.</param>
    /// <param name="e">The event arguments containing the log details.</param>
    private void RelayLogEvent(object? sender, LogEventArgs e) => LogEvent?.Invoke(this, e);

    /// <summary>
    /// Logs a message with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
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

    /// <summary>
    /// Logs an exception with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
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

    /// <summary>
    /// Logs a message and an exception with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
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

    /// <summary>
    /// Logs the entry of a method, capturing caller information.
    /// </summary>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void TraceMethodEntry(
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            var message = $"Entering method: {method.DeclaringType?.Name}.{method.Name}";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }

    /// <summary>
    /// Logs the exit of a method along with its execution time, capturing caller information.
    /// </summary>
    /// <param name="stopwatch">The <see cref="Stopwatch"/> used to measure the method's execution time.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void TraceMethodExit(Stopwatch stopwatch,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            stopwatch.Stop();
            var message = $"Exiting method: {method.DeclaringType?.Name}.{method.Name}. Execution time: {stopwatch.ElapsedMilliseconds} ms.";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }

    /// <summary>
    /// Clones the current instance of the <see cref="QuickLogger"/> class.
    /// </summary>
    /// <returns>A 1:1 clone of the current <see cref="QuickLogger"/></returns>
    public object Clone()
    {
        var x = new QuickLogger
        {
            EnableConsoleLogging = EnableConsoleLogging,
            EnableFileLogging = EnableFileLogging,
            EnableEventLogging = EnableEventLogging,
            EnableTraceLogging = EnableTraceLogging,
            LogPath = LogPath
        };
        x.LogEvent = LogEvent;
        x._consoleLogger = _consoleLogger;
        x._eventLogger = _eventLogger;
        x._fileLogger = _fileLogger;
        x._traceLogger = _traceLogger;
        return x;
    }

    /// <summary>
    /// Clones the current instance of the <see cref="QuickLogger"/> class, and optionally changes the log file path.
    /// </summary>
    /// <param name="fileName">The file path for logging. If null, file logging is disabled if not already set originally by current instance.</param>
    /// <returns>A 1:1 clone of the current <see cref="QuickLogger"/></returns>
    public QuickLogger CloneDeep(string? fileName = null)
    {
        var x = (QuickLogger)Clone();
        if (fileName != null && !fileName.EndsWith(x.LogPath.ToLower()))
        {
            _fileLogger = new FileLogger(fileName);
            x.LogPath = Path.GetDirectoryName(fileName) ?? "logs";
        }

        return x;
    }
}

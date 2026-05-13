/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : QuickLogger.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2024-10-06 09:02:53 +02:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : E6E76311
 *  
 *  Description    :
 *                   Provides a flexible, multi-sink logger that supports synchronous and asynchronous logging to console, file, event
 *                   handlers, and system trace, with filtering and batching capabilities.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: E6E76311

using QuickLog.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using QuickLog.Sinks;
using QuickLog.Utilities;

namespace QuickLog.Loggers;

/// <summary>
/// Provides a flexible, multi-sink logger that supports synchronous and asynchronous logging to console, file, event
/// handlers, and system trace, with filtering and batching capabilities.
/// </summary>
/// <remarks>QuickLogger enables logging to multiple destinations and supports both synchronous and asynchronous
/// modes. It allows fine-grained control over which sinks are enabled, batching behavior, and log filtering. The logger
/// can be configured to dispatch log events to custom handlers and supports capturing recent log entries for
/// diagnostics. Thread safety is maintained for asynchronous operations. Dispose the logger when no longer needed to
/// release resources and flush pending log entries.</remarks>
public class QuickLogger : IQuickLog, ICloneable
{
    /// <summary>
    /// The path of the log-files - used only internally!
    /// </summary>
    internal string LogPath { get; private set; }

    /// <summary>
    /// The collection of log sinks where log entries are dispatched.
    /// </summary>
    private readonly List<ILogSink> _sinks = new();

    /// <summary>
    /// The collection of asynchronous log sinks.
    /// </summary>
    private readonly List<ILogSink> _asyncSinks = new();

    /// <summary>
    /// Handles asynchronous log dispatching.
    /// </summary>
    private AsyncLogDispatcher? _asyncDispatcher;

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

    /// <summary>
    /// Enables or disables asynchronous logging.
    /// </summary>
    public bool EnableAsyncLogging { get; set; } = false;

    /// <summary>
    /// Enables or disables asynchronous file logging.
    /// </summary>
    public bool EnableAsyncFileLogging { get; set; }

    /// <summary>
    /// Enables or disables asynchronous binary logging.
    /// </summary>
    public bool EnableAsyncBinaryLogging { get; set; }

    /// <summary>
    /// When true, disables all synchronous loggers (console/file/event/trace)
    /// and routes logging exclusively through the async pipeline.
    /// </summary>
    public bool AsyncOnly { get; set; }

    /// <summary>
    /// When in AsyncOnly mode, controls whether LogEvent is still raised.
    /// </summary>
    public bool RaiseLogEventInAsyncOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets a filter function to determine which log events should be processed.
    /// </summary>
    public Func<LogEventArgs, bool>? Filter { get; set; }

    /// <summary>
    /// Creates a logging scope with the specified name.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IDisposable Scope(string name) => QuickLog.Core.LogScope.Push(name);

    /// <summary>
    /// Gets or sets the number of files to process in each batch operation.
    /// </summary>
    public int FileBatchSize { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of files to process in each batch operation for asynchronous logging.
    /// </summary>
    public int AsyncFileBatchSize { get; set; } = 16;

    /// <summary>
    /// Gets or sets the path for binary log files.
    /// </summary>
    public string? BinaryLogPath { get; set; }

    /// <summary>
    /// Gets or sets the path for JSON Lines output (one JSON object per entry).
    /// <see langword="null"/> disables the JSON sink.
    /// </summary>
    public string? JsonLogPath { get; set; }

    /// <summary>Route async-dispatched entries to <see cref="System.Diagnostics.Trace"/>.</summary>
    public bool EnableAsyncTraceLogging { get; set; }

    /// <summary>Drop policy applied when the async queue is full.</summary>
    public AsyncDropPolicy AsyncDropPolicy { get; set; } = AsyncDropPolicy.DropBelowLevel;

    /// <summary>Entries below this level may be dropped under <see cref="AsyncDropPolicy.DropBelowLevel"/>.</summary>
    public LogType AsyncMinimumLevel { get; set; } = LogType.Warn;

    /// <summary>Thread role that is shielded from dropping under <see cref="AsyncDropPolicy.DropByThreadRole"/>.</summary>
    public ThreadRole AsyncProtectedRole { get; set; } = ThreadRole.Audio;

    /// <summary>Optional size-based rotation settings for file-backed async sinks.</summary>
    public LogRotationOptions? Rotation { get; set; }

    /// <summary>
    /// Returns a snapshot of recent log entries captured by the async memory sink.
    /// Returns an empty list if async logging is disabled.
    /// </summary>
    public IReadOnlyList<LogEventArgs> GetRecentLogs()
        => _memoryLogger?.Snapshot() ?? Array.Empty<LogEventArgs>();

    #region Internal Loggers

    private EventOnlyLogger? _eventLogger;
    private ConsoleQuickLogger? _consoleLogger;
    private FileLogger? _fileLogger;
    private TraceLogger? _traceLogger;
    private MemoryQuickLogger? _memoryLogger = new(512);

    #endregion

    /// <summary>
    /// Occurs when a log event is triggered.
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickLogger"/> class with the specified log file path and logging options.
    /// </summary>
    /// <param name="logFilePath">The file path for logging. If null, file logging is automatically disabled.</param>
    /// <param name="eventLogging">Optional if Event Logging is wanted.</param>
    /// <param name="consoleLogging">Optional if user wants logging to be written to console.</param>
    /// <param name="fileLogging">Optional if user wants logging to be written to a log-file.</param>
    /// <param name="traceLogging">Optional if Trace Logging is wanted.</param>
    public QuickLogger(string? logFilePath = null, bool eventLogging = false, bool consoleLogging = false, bool fileLogging = false, bool traceLogging = false)
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

        EnableConsoleLogging = consoleLogging;
        EnableFileLogging = fileLogging;
        EnableEventLogging = eventLogging;
        EnableTraceLogging = traceLogging;
    }

    private QuickLogger()
    { }

    /// <summary>
    /// Relays log events from the internal loggers to the main logger event handler.
    /// </summary>
    /// <param name="sender">The sender of the log event.</param>
    /// <param name="e">The event arguments containing the log details.</param>
    private void RelayLogEvent(object? sender, LogEventArgs e) => LogEvent?.Invoke(this, e);

    private void Dispatch(LogEventArgs args)
    {
        if (Filter != null && !Filter(args))
            return;

        EnsureAsyncDispatcher();

        _asyncDispatcher?.Enqueue(new LogEntry(
            DateTime.UtcNow,
            args.LoggingType,
            args.Exception != null
                ? args.Exception.ToStringDemystified()
                : args.Message ?? string.Empty,
            "QuickLogger",
            QuickLog.Core.LogScope.Current,
            args.CallerName,
            args.CallerFilePath,
            args.CallerLineNumber,
            Environment.CurrentManagedThreadId,
            ThreadContext.Role,
            LogContext.CurrentCorrelationId,
            LogContext.CurrentTraceId,
            LogContext.CurrentSpanId
        ));
    }

    private void DispatchFast(
        LogType logType,
        string message,
        string callerName,
        string callerFilePath,
        int callerLineNumber)
    {
        if (!EnableAsyncLogging)
            return;

        EnsureAsyncDispatcher();

        _asyncDispatcher?.Enqueue(new LogEntry(
            DateTime.UtcNow,
            logType,
            message,
            "QuickLogger",
            QuickLog.Core.LogScope.Current,
            callerName,
            callerFilePath,
            callerLineNumber,
            Environment.CurrentManagedThreadId,
            ThreadContext.Role,
            LogContext.CurrentCorrelationId,
            LogContext.CurrentTraceId,
            LogContext.CurrentSpanId
        ));
    }

    private void EnsureAsyncDispatcher()
    {
        if (_asyncDispatcher != null || !EnableAsyncLogging)
            return;

        _asyncSinks.Clear();

        // Always keep memory sink (safe, diagnostic)
        _memoryLogger ??= new MemoryQuickLogger(512);
        _asyncSinks.Add(new MemorySink(_memoryLogger));

        // Optional async file sink (batched)
        if (EnableAsyncFileLogging && !string.IsNullOrWhiteSpace(LogPath))
            _asyncSinks.Add(new FileSink(Path.Combine(LogPath, "quicklog.async.log"), AsyncFileBatchSize, Rotation));

        // Optional async trace sink
        if (EnableAsyncTraceLogging)
            _asyncSinks.Add(new TraceSink());

        // Optional JSON Lines sink
        if (!string.IsNullOrWhiteSpace(JsonLogPath))
            _asyncSinks.Add(new JsonLinesSink(JsonLogPath, Rotation));

        // Optional compact binary sink
        if (EnableAsyncBinaryLogging && !string.IsNullOrWhiteSpace(BinaryLogPath))
            _asyncSinks.Add(new BinaryLogSink(BinaryLogPath, Rotation));

        _asyncDispatcher = new AsyncLogDispatcher(_asyncSinks)
        {
            DropPolicy    = this.AsyncDropPolicy,
            MinimumLevel  = AsyncMinimumLevel,
            ProtectedRole = AsyncProtectedRole
        };
    }

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
        if (!AsyncOnly)
        {
            if (EnableConsoleLogging) _consoleLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, message, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging) _eventLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging) _traceLogger?.Log(logType, message, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            LogEvent?.Invoke(this, new LogEventArgs(
                logType,
                message,
                callerName,
                callerFilePath,
                callerLineNumber));
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                message,
                callerName,
                callerFilePath,
                callerLineNumber);
        }
        else
        {
            Dispatch(new LogEventArgs(
                logType,
                message,
                callerName,
                callerFilePath,
                callerLineNumber));
        }
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
        if (!AsyncOnly)
        {
            if (EnableConsoleLogging) _consoleLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging) _eventLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging) _traceLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            LogEvent?.Invoke(this, new LogEventArgs(
                logType,
                exception,
                callerName,
                callerFilePath,
                callerLineNumber));
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                exception.ToStringDemystified(),
                callerName,
                callerFilePath,
                callerLineNumber);
        }
        else
        {
            Dispatch(new LogEventArgs(
                logType,
                exception,
                callerName,
                callerFilePath,
                callerLineNumber));
        }
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
        if (!AsyncOnly)
        {
            if (EnableConsoleLogging) _consoleLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null) _fileLogger.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging) _eventLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging) _traceLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            LogEvent?.Invoke(this, new LogEventArgs(
                logType,
                message,
                exception,
                callerName,
                callerFilePath,
                callerLineNumber));
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                $"Message: {message}\r\nException: {exception.ToStringDemystified()}",
                callerName,
                callerFilePath,
                callerLineNumber);
        }
        else
        {
            Dispatch(new LogEventArgs(
                logType,
                message,
                exception,
                callerName,
                callerFilePath,
                callerLineNumber));
        }
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
        Log(LogType.Trace,
            $"Entering method: {callerName}",
            callerName, callerFilePath, callerLineNumber);
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
        stopwatch.Stop();
        Log(LogType.Trace,
            $"Exiting method: {callerName}. Execution time: {stopwatch.ElapsedMilliseconds} ms.",
            callerName, callerFilePath, callerLineNumber);
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
    /// <summary>
    /// Flushes any pending asynchronous log entries.
    /// </summary>
    public void Flush() => _asyncDispatcher?.Flush();

    /// <summary>
    /// Shuts down asynchronous logging and releases resources.
    /// </summary>
    public void Shutdown()
    {
        _asyncDispatcher?.Flush();
        _asyncDispatcher?.Dispose();
        _asyncDispatcher = null;
    }
    /// <summary>
    /// Disposes of the internal loggers.
    /// </summary>
    ~QuickLogger()
    {
        _eventLogger?.Dispose();
        _consoleLogger?.Dispose();
        _fileLogger?.Dispose();
        _traceLogger?.Dispose();
    }
    /// <summary>
    /// Disposes of the internal loggers.
    /// </summary>
    public void Dispose()
    {
        Shutdown();

        _eventLogger?.Dispose();
        _consoleLogger?.Dispose();
        _fileLogger?.Dispose();
        _traceLogger?.Dispose();
        GC.SuppressFinalize(this);
    }
}

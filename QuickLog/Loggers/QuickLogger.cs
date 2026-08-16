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
    private sealed record StructuredRelayContext(
        string Message,
        LogEventId EventId,
        IReadOnlyDictionary<string, object?> Properties);

    /// <summary>
    /// The path of the log-files - used only internally!
    /// </summary>
    internal string LogPath { get; private set; } = "logs";

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
    private LogDispatcherStats? _lastStats;
    private LogSpamController? _spamController;
    private LogRateLimiter? _rateLimiter;
    private readonly DateTime _startedUtc = DateTime.UtcNow;
    private bool _startupEmitted;
    private bool _shutdownSummaryEmitted;
    private readonly AsyncLocal<StructuredRelayContext?> _structuredRelay = new();

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

    /// <summary>Optional rotation, retention, and compression settings for file-backed async sinks.</summary>
    public LogRotationOptions? Rotation { get; set; }

    /// <summary>Maximum number of entries buffered by the async dispatcher.</summary>
    public int AsyncQueueCapacity { get; set; } = 8192;

    /// <summary>Optional redaction settings applied before entries enter async sinks.</summary>
    public LogRedactionOptions? Redaction { get; set; }

    /// <summary>Optional duplicate coalescing settings for the async path.</summary>
    public LogSpamControlOptions? SpamControl { get; set; }

    /// <summary>Logical session name written in startup and shutdown markers.</summary>
    public string? SessionName { get; set; }

    /// <summary>Stable identifier for the current logging session.</summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Whether <see cref="EmitStartup"/> should be called during option-based configuration.</summary>
    public bool EmitStartupBanner { get; set; }

    /// <summary>Whether <see cref="Shutdown"/> should write a compact final summary.</summary>
    public bool EmitShutdownSummary { get; set; }

    /// <summary>Whether console output should include ANSI color escape sequences.</summary>
    public bool UseAnsiColor { get; set; }

    /// <summary>Whether console output should use compact formatting.</summary>
    public bool CompactText { get; set; }

    /// <summary>Whether console timestamps should use local time instead of UTC.</summary>
    public bool UseLocalTime { get; set; }

    /// <summary>Minimum level accepted before entries are dispatched.</summary>
    public LogType MinimumLevel { get; set; } = LogType.Trace;

    /// <summary>Minimum levels for named sinks such as <c>console</c>, <c>file</c>, or <c>trace</c>.</summary>
    public Dictionary<string, LogType> SinkMinimumLevels { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns a snapshot of recent log entries captured by the async memory sink.
    /// Returns an empty list if async logging is disabled.
    /// </summary>
    public IReadOnlyList<LogEventArgs> GetRecentLogs()
        => _memoryLogger?.Snapshot() ?? Array.Empty<LogEventArgs>();

    /// <summary>Returns a snapshot of async dispatcher health counters.</summary>
    public LogDispatcherStats GetStats()
        => _asyncDispatcher?.GetStats()
           ?? _lastStats
           ?? new LogDispatcherStats(AsyncQueueCapacity, 0, 0, 0, 0, 0, 0, 0, null);

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
    private void RelayLogEvent(object? sender, LogEventArgs e)
    {
        var structured = _structuredRelay.Value;
        if (structured is null)
        {
            LogEvent?.Invoke(this, e);
            return;
        }

        LogEvent?.Invoke(this, new LogEventArgs(
            e.LoggingType,
            structured.Message,
            e.Exception,
            e.CallerName,
            e.CallerFilePath,
            e.CallerLineNumber,
            e.Scope,
            e.CorrelationId,
            e.TraceId,
            e.SpanId,
            structured.EventId,
            structured.Properties));
    }

    /// <summary>Returns whether the logger accepts the supplied level.</summary>
    /// <param name="logType">The level to test.</param>
    /// <returns><see langword="true"/> when the level meets the configured minimum.</returns>
    public bool IsEnabled(LogType logType) => logType >= MinimumLevel;

    private bool ShouldLog(LogType logType) => IsEnabled(logType);

    private bool AllowsSink(string sinkName, LogType logType)
        => !SinkMinimumLevels.TryGetValue(sinkName, out var minimum) || logType >= minimum;

    private void ApplyConsoleOptions()
    {
        if (_consoleLogger is null)
            return;

        _consoleLogger.CompactText = CompactText;
        _consoleLogger.UseLocalTime = UseLocalTime;
        _consoleLogger.UseAnsiColor = UseAnsiColor;
    }

    /// <summary>
    /// Emits a compact startup banner for the current process and session.
    /// </summary>
    public void EmitStartup()
    {
        if (_startupEmitted)
            return;

        _startupEmitted = true;
        var sessionName = string.IsNullOrWhiteSpace(SessionName) ? "quicklog" : SessionName!;
        Log(LogType.Info, LogRuntimeSnapshot.Startup(sessionName, SessionId));
    }

    private void Dispatch(LogEventArgs args)
    {
        var structured = _structuredRelay.Value;
        var properties = LogProperties.Merge(LogContext.CurrentProperties, structured?.Properties ?? args.Properties);
        var filterArgs = structured is null && properties.Count == 0
            ? args
            : new LogEventArgs(
                args.LoggingType,
                structured?.Message ?? args.Message,
                args.Exception,
                args.CallerName,
                args.CallerFilePath,
                args.CallerLineNumber,
                LogScope.Current,
                LogContext.CurrentCorrelationId,
                LogContext.CurrentTraceId,
                LogContext.CurrentSpanId,
                structured?.EventId ?? args.EventId,
                properties);
        if (Filter != null && !Filter(filterArgs))
            return;

        EnsureAsyncDispatcher();

        EnqueueAsyncEntry(new LogEntry(
            DateTime.UtcNow,
            args.LoggingType,
            Redact(args.Exception != null
                ? string.IsNullOrWhiteSpace(args.Message)
                    ? args.Exception.ToStringDemystified()
                    : $"{args.Message}{Environment.NewLine}{args.Exception.ToStringDemystified()}"
                : args.Message ?? string.Empty),
            "QuickLogger",
            QuickLog.Core.LogScope.Current,
            args.CallerName,
            args.CallerFilePath,
            args.CallerLineNumber,
            Environment.CurrentManagedThreadId,
            ThreadContext.Role,
            LogContext.CurrentCorrelationId,
            LogContext.CurrentTraceId,
            LogContext.CurrentSpanId,
            structured?.EventId ?? args.EventId,
            RedactProperties(properties)
        ));
    }

    private void DispatchFast(
        LogType logType,
        string message,
        string callerName,
        string callerFilePath,
        int callerLineNumber,
        LogEventId eventId = default,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        if (!EnableAsyncLogging)
            return;

        EnsureAsyncDispatcher();

        var mergedProperties = LogProperties.Merge(LogContext.CurrentProperties, properties);
        EnqueueAsyncEntry(new LogEntry(
            DateTime.UtcNow,
            logType,
            Redact(message),
            "QuickLogger",
            QuickLog.Core.LogScope.Current,
            callerName,
            callerFilePath,
            callerLineNumber,
            Environment.CurrentManagedThreadId,
            ThreadContext.Role,
            LogContext.CurrentCorrelationId,
            LogContext.CurrentTraceId,
            LogContext.CurrentSpanId,
            eventId,
            RedactProperties(mergedProperties)
        ));
    }

    private string Redact(string value)
        => Redaction is { Enabled: true } options
            ? new LogRedactor(options).Redact(value)
            : value;

    private IReadOnlyDictionary<string, object?> RedactProperties(
        IReadOnlyDictionary<string, object?>? properties)
        => Redaction is { Enabled: true } options
            ? new LogRedactor(options).RedactProperties(properties)
            : LogProperties.Snapshot(properties);

    private string FormatForTextSink(string message)
    {
        var structured = _structuredRelay.Value;
        if (structured is null)
            return message;

        var eventText = structured.EventId == LogEventId.None ? string.Empty : $" [{structured.EventId}]";
        var properties = LogProperties.Format(structured.Properties);
        return properties.Length == 0
            ? $"{message}{eventText}"
            : $"{message}{eventText} {properties}";
    }

    private void RaiseAsyncOnlyEvent(
        LogType logType,
        string? message,
        Exception? exception,
        string callerName,
        string callerFilePath,
        int callerLineNumber)
    {
        var structured = _structuredRelay.Value;
        LogEvent?.Invoke(this, new LogEventArgs(
            logType,
            structured?.Message ?? message,
            exception,
            callerName,
            callerFilePath,
            callerLineNumber,
            LogScope.Current,
            LogContext.CurrentCorrelationId,
            LogContext.CurrentTraceId,
            LogContext.CurrentSpanId,
            structured?.EventId ?? default,
            LogProperties.Merge(LogContext.CurrentProperties, structured?.Properties)));
    }

    private void EnqueueAsyncEntry(in LogEntry entry)
    {
        EnsureAsyncDispatcher();
        if (_asyncDispatcher is null)
            return;

        if (SpamControl is { Enabled: true } options)
        {
            _spamController ??= new LogSpamController(options);
            foreach (var candidate in _spamController.Process(entry))
                _asyncDispatcher.Enqueue(candidate);
            return;
        }

        _asyncDispatcher.Enqueue(entry);
    }

    private void FlushSpamControl()
    {
        if (_asyncDispatcher is null || _spamController is null)
            return;

        foreach (var candidate in _spamController.Flush())
            _asyncDispatcher.Enqueue(candidate);
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

        _asyncDispatcher = new AsyncLogDispatcher(_asyncSinks, AsyncQueueCapacity)
        {
            DropPolicy = this.AsyncDropPolicy,
            MinimumLevel = AsyncMinimumLevel,
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
        if (!ShouldLog(logType))
            return;

        var textMessage = FormatForTextSink(message);
        if (!AsyncOnly)
        {
            ApplyConsoleOptions();
            if (EnableConsoleLogging && AllowsSink("console", logType)) _consoleLogger?.Log(logType, textMessage, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null && AllowsSink("file", logType)) _fileLogger.Log(logType, textMessage, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging && AllowsSink("event", logType)) _eventLogger?.Log(logType, textMessage, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging && AllowsSink("trace", logType)) _traceLogger?.Log(logType, textMessage, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            RaiseAsyncOnlyEvent(logType, message, null, callerName, callerFilePath, callerLineNumber);
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                message,
                callerName,
                callerFilePath,
                callerLineNumber,
                _structuredRelay.Value?.EventId ?? default,
                _structuredRelay.Value?.Properties);
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
    /// Logs a message with a stable event identifier and structured properties.
    /// </summary>
    /// <param name="logType">The type of the log entry.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="eventId">The stable event identifier.</param>
    /// <param name="properties">The structured properties.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    public void Log(
        LogType logType,
        string message,
        LogEventId eventId,
        IReadOnlyDictionary<string, object?>? properties = null,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!IsEnabled(logType))
            return;

        var previous = _structuredRelay.Value;
        var snapshot = LogProperties.Merge(LogContext.CurrentProperties, properties);
        _structuredRelay.Value = new StructuredRelayContext(message, eventId, snapshot);
        try
        {
            Log(logType, message, callerName, callerFilePath, callerLineNumber);
        }
        finally
        {
            _structuredRelay.Value = previous;
        }
    }

    /// <summary>Logs a message and exception with a stable event identifier and structured properties.</summary>
    /// <param name="logType">The type of the log entry.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="eventId">The stable event identifier.</param>
    /// <param name="properties">The structured properties.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    public void Log(
        LogType logType,
        string message,
        Exception exception,
        LogEventId eventId,
        IReadOnlyDictionary<string, object?>? properties = null,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (!IsEnabled(logType))
            return;

        var previous = _structuredRelay.Value;
        _structuredRelay.Value = new StructuredRelayContext(
            message,
            eventId,
            LogProperties.Merge(LogContext.CurrentProperties, properties));
        try
        {
            Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        }
        finally
        {
            _structuredRelay.Value = previous;
        }
    }

    /// <summary>
    /// Logs an interpolated message without evaluating formatted values when the level is disabled.
    /// </summary>
    /// <param name="logType">The type of the log entry.</param>
    /// <param name="message">The lazily built message.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    public void Log(
        LogType logType,
        [InterpolatedStringHandlerArgument("", "logType")] ref QuickLogInterpolatedStringHandler message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (IsEnabled(logType))
            Log(logType, message.GetFormattedText(), callerName, callerFilePath, callerLineNumber);
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
        if (!ShouldLog(logType))
            return;

        if (!AsyncOnly)
        {
            ApplyConsoleOptions();
            if (EnableConsoleLogging && AllowsSink("console", logType)) _consoleLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null && AllowsSink("file", logType)) _fileLogger.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging && AllowsSink("event", logType)) _eventLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging && AllowsSink("trace", logType)) _traceLogger?.Log(logType, exception, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            RaiseAsyncOnlyEvent(logType, null, exception, callerName, callerFilePath, callerLineNumber);
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                exception.ToStringDemystified(),
                callerName,
                callerFilePath,
                callerLineNumber,
                _structuredRelay.Value?.EventId ?? default,
                _structuredRelay.Value?.Properties);
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
        if (!ShouldLog(logType))
            return;

        if (!AsyncOnly)
        {
            ApplyConsoleOptions();
            if (EnableConsoleLogging && AllowsSink("console", logType)) _consoleLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableFileLogging && _fileLogger != null && AllowsSink("file", logType)) _fileLogger.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableEventLogging && AllowsSink("event", logType)) _eventLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
            if (EnableTraceLogging && AllowsSink("trace", logType)) _traceLogger?.Log(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        }
        else if (RaiseLogEventInAsyncOnly)
        {
            RaiseAsyncOnlyEvent(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        }

        if (AsyncOnly && EnableAsyncLogging)
        {
            DispatchFast(
                logType,
                $"Message: {message}\r\nException: {exception.ToStringDemystified()}",
                callerName,
                callerFilePath,
                callerLineNumber,
                _structuredRelay.Value?.EventId ?? default,
                _structuredRelay.Value?.Properties);
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
    /// Logs a message only once for a stable caller-supplied key.
    /// </summary>
    /// <param name="key">Stable key used to suppress duplicate entries.</param>
    /// <param name="level">Log level to write when accepted.</param>
    /// <param name="message">Message to write when accepted.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
    public void LogOnce(
        string key,
        LogType level,
        string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        _rateLimiter ??= new LogRateLimiter();
        if (_rateLimiter.ShouldLogOnce(key))
            Log(level, message, callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Logs a message at most once per interval for a stable caller-supplied key.
    /// </summary>
    /// <param name="key">Stable key used to rate-limit entries.</param>
    /// <param name="interval">Minimum time between accepted entries.</param>
    /// <param name="level">Log level to write when accepted.</param>
    /// <param name="message">Message to write when accepted.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
    public void LogEvery(
        string key,
        TimeSpan interval,
        LogType level,
        string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        _rateLimiter ??= new LogRateLimiter();
        if (_rateLimiter.ShouldLogEvery(key, interval))
            Log(level, message, callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Logs frame timing and marks frames at or above the hitch threshold as warnings.
    /// </summary>
    /// <param name="frame">Frame number.</param>
    /// <param name="elapsed">Measured frame duration.</param>
    /// <param name="hitchThreshold">Duration at which the frame becomes a hitch.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
    public void LogFrameTime(
        long frame,
        TimeSpan elapsed,
        TimeSpan hitchThreshold,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var hitch = elapsed >= hitchThreshold;
        var label = hitch ? "FRAME HITCH" : "FRAME";
        var level = hitch ? LogType.Warn : LogType.Trace;
        Log(level, $"{label} frame={frame} elapsedMs={(long)elapsed.TotalMilliseconds}", callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Begins an asset-loading marker that writes an end marker when disposed.
    /// </summary>
    /// <param name="assetName">Logical asset name.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
    public IDisposable BeginAssetLoad(
        string assetName,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
        => new AssetLoadMarker(this, assetName, callerName, callerFilePath, callerLineNumber);

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
            EnableAsyncLogging = EnableAsyncLogging,
            EnableAsyncFileLogging = EnableAsyncFileLogging,
            EnableAsyncBinaryLogging = EnableAsyncBinaryLogging,
            AsyncOnly = AsyncOnly,
            RaiseLogEventInAsyncOnly = RaiseLogEventInAsyncOnly,
            Filter = Filter,
            FileBatchSize = FileBatchSize,
            AsyncFileBatchSize = AsyncFileBatchSize,
            BinaryLogPath = BinaryLogPath,
            JsonLogPath = JsonLogPath,
            EnableAsyncTraceLogging = EnableAsyncTraceLogging,
            AsyncDropPolicy = AsyncDropPolicy,
            AsyncMinimumLevel = AsyncMinimumLevel,
            AsyncProtectedRole = AsyncProtectedRole,
            Rotation = Rotation,
            AsyncQueueCapacity = AsyncQueueCapacity,
            Redaction = Redaction,
            SpamControl = SpamControl,
            SessionName = SessionName,
            SessionId = SessionId,
            EmitStartupBanner = EmitStartupBanner,
            EmitShutdownSummary = EmitShutdownSummary,
            UseAnsiColor = UseAnsiColor,
            CompactText = CompactText,
            UseLocalTime = UseLocalTime,
            MinimumLevel = MinimumLevel,
            LogPath = LogPath
        };
        foreach (var pair in SinkMinimumLevels)
            x.SinkMinimumLevels[pair.Key] = pair.Value;

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
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            x._fileLogger = new FileLogger(fileName);
            x.LogPath = Path.GetDirectoryName(fileName) ?? "logs";
        }

        return x;
    }
    /// <summary>
    /// Flushes any pending asynchronous log entries.
    /// </summary>
    public void Flush()
    {
        FlushSpamControl();
        _asyncDispatcher?.Flush();
    }

    /// <summary>Asynchronously flushes pending entries without blocking the caller thread.</summary>
    /// <param name="cancellationToken">A token that can cancel the wait.</param>
    /// <returns>An operation that completes after pending entries are flushed.</returns>
    public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        FlushSpamControl();
        if (_asyncDispatcher is not null)
            await _asyncDispatcher.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Shuts down asynchronous logging and releases resources.
    /// </summary>
    public void Shutdown()
    {
        if (EmitShutdownSummary && !_shutdownSummaryEmitted)
        {
            _shutdownSummaryEmitted = true;
            Log(LogType.Info, LogRuntimeSnapshot.Shutdown(_startedUtc, GetStats(), SessionId));
        }

        Flush();
        _lastStats = _asyncDispatcher?.GetStats();
        _asyncDispatcher?.Dispose();
        _asyncDispatcher = null;
        _spamController = null;
    }

    /// <summary>
    /// Flushes and shuts down asynchronous logging with optional timeout and cancellation.
    /// </summary>
    /// <param name="timeout">The maximum graceful-flush duration, or <see langword="null"/> for no timeout.</param>
    /// <param name="cancellationToken">A token that can cancel shutdown.</param>
    /// <returns>An operation that completes after asynchronous resources are released.</returns>
    public async ValueTask ShutdownAsync(
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = timeout is { } value && value != Timeout.InfiniteTimeSpan
            ? new CancellationTokenSource(value)
            : null;
        using var linkedSource = timeoutSource is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        var token = linkedSource?.Token ?? cancellationToken;

        if (EmitShutdownSummary && !_shutdownSummaryEmitted)
        {
            _shutdownSummaryEmitted = true;
            Log(LogType.Info, LogRuntimeSnapshot.Shutdown(_startedUtc, GetStats(), SessionId));
        }

        await FlushAsync(token).ConfigureAwait(false);
        _lastStats = _asyncDispatcher?.GetStats();
        if (_asyncDispatcher is not null)
            await _asyncDispatcher.DisposeAsync().ConfigureAwait(false);
        _asyncDispatcher = null;
        _spamController = null;
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

    /// <summary>Asynchronously flushes and disposes the logger.</summary>
    /// <returns>An operation that completes after logger resources are released.</returns>
    public async ValueTask DisposeAsync()
    {
        await ShutdownAsync().ConfigureAwait(false);
        _eventLogger?.Dispose();
        _consoleLogger?.Dispose();
        _fileLogger?.Dispose();
        _traceLogger?.Dispose();
        GC.SuppressFinalize(this);
    }
}

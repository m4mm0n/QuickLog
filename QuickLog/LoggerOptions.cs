using QuickLog.Core;

namespace QuickLog;

/// <summary>
/// Configuration options for a <see cref="QuickLogger"/> instance.
/// All properties have sensible defaults (console enabled, everything else off).
/// Supports fluent chaining — pass to <see cref="LogManager.ConfigureDefault(LoggerOptions)"/>.
/// </summary>
/// <example>
/// <code>
/// LogManager.ConfigureDefault(
///     new LoggerOptions()
///         .WithFile("logs/app.log")
///         .WithConsole()
///         .WithAsync(AsyncDropPolicy.DropBelowLevel, LogType.Warn)
///         .WithJsonLog("logs/app.jsonl"));
/// </code>
/// </example>
public sealed class LoggerOptions
{
    // ── Sync sinks ────────────────────────────────────────────────────────────

    /// <summary>Path for the text log file. Setting this automatically enables <see cref="FileLogging"/>.</summary>
    public string? LogFilePath { get; set; }

    /// <summary>Write to the console. Default: <see langword="true"/>.</summary>
    public bool ConsoleLogging { get; set; } = true;

    /// <summary>Write to <see cref="LogFilePath"/>. Automatically set by <see cref="WithFile"/>.</summary>
    public bool FileLogging { get; set; }

    /// <summary>Raise <see cref="QuickLogger.LogEvent"/> on every entry.</summary>
    public bool EventLogging { get; set; }

    /// <summary>Write to <see cref="System.Diagnostics.Trace"/> on the sync path.</summary>
    public bool TraceLogging { get; set; }

    // ── Async pipeline ────────────────────────────────────────────────────────

    /// <summary>Enable the background async dispatcher.</summary>
    public bool AsyncLogging { get; set; }

    /// <summary>
    /// Suppress all sync IO and route everything through the async pipeline.
    /// Requires <see cref="AsyncLogging"/> = <see langword="true"/>.
    /// </summary>
    public bool AsyncOnly { get; set; }

    /// <summary>Route async entries to <see cref="System.Diagnostics.Trace"/>.</summary>
    public bool AsyncTraceLogging { get; set; }

    /// <summary>Path for JSON Lines output (one JSON object per line). <see langword="null"/> = disabled.</summary>
    public string? JsonLogPath { get; set; }

    /// <summary>Write compact binary log records on the async pipeline.</summary>
    public bool AsyncBinaryLogging { get; set; }

    /// <summary>Path for compact binary log output. <see langword="null"/> = disabled.</summary>
    public string? BinaryLogPath { get; set; }

    /// <summary>Drop policy when the async queue is full. Default: <see cref="AsyncDropPolicy.DropBelowLevel"/>.</summary>
    public AsyncDropPolicy AsyncDropPolicy { get; set; } = AsyncDropPolicy.DropBelowLevel;

    /// <summary>Entries below this level may be dropped when the queue is full (used with <see cref="AsyncDropPolicy.DropBelowLevel"/>).</summary>
    public LogType AsyncMinimumLevel { get; set; } = LogType.Warn;

    /// <summary>Thread role that is never dropped (used with <see cref="AsyncDropPolicy.DropByThreadRole"/>).</summary>
    public ThreadRole AsyncProtectedRole { get; set; } = ThreadRole.Audio;

    /// <summary>Optional size-based rotation settings for file-backed sinks.</summary>
    public LogRotationOptions? Rotation { get; set; }

    // ── Filtering ─────────────────────────────────────────────────────────────

    /// <summary>Optional predicate applied before dispatching. Return <see langword="false"/> to drop an entry.</summary>
    public Func<LogEventArgs, bool>? Filter { get; set; }

    // ── Fluent builder ────────────────────────────────────────────────────────

    /// <summary>Set the text log file path and enable file logging.</summary>
    public LoggerOptions WithFile(string path)
    {
        LogFilePath = path;
        FileLogging = true;
        return this;
    }

    /// <summary>Enable or disable console logging.</summary>
    public LoggerOptions WithConsole(bool enabled = true) { ConsoleLogging = enabled; return this; }

    /// <summary>Enable or disable the <see cref="QuickLogger.LogEvent"/> event.</summary>
    public LoggerOptions WithEvents(bool enabled = true) { EventLogging = enabled; return this; }

    /// <summary>Enable or disable <see cref="System.Diagnostics.Trace"/> on the sync path.</summary>
    public LoggerOptions WithTrace(bool enabled = true) { TraceLogging = enabled; return this; }

    /// <summary>Enable the async pipeline with optional drop policy and minimum level.</summary>
    public LoggerOptions WithAsync(
        AsyncDropPolicy policy = AsyncDropPolicy.DropBelowLevel,
        LogType minLevel = LogType.Warn)
    {
        AsyncLogging = true;
        AsyncDropPolicy = policy;
        AsyncMinimumLevel = minLevel;
        return this;
    }

    /// <summary>
    /// Enable async-only mode: all sync sinks are bypassed and every entry goes through the
    /// async pipeline. Automatically enables <see cref="AsyncLogging"/>.
    /// </summary>
    public LoggerOptions WithAsyncOnly(
        AsyncDropPolicy policy = AsyncDropPolicy.DropBelowLevel,
        LogType minLevel = LogType.Warn)
    {
        AsyncOnly = true;
        return WithAsync(policy, minLevel);
    }

    /// <summary>Route async entries to <see cref="System.Diagnostics.Trace"/>.</summary>
    public LoggerOptions WithAsyncTrace(bool enabled = true) { AsyncTraceLogging = enabled; return this; }

    /// <summary>Write one JSON object per log entry to the given file path (JSON Lines / NDJSON).</summary>
    public LoggerOptions WithJsonLog(string path) { JsonLogPath = path; return this; }

    /// <summary>Write compact binary log entries to the given file path on the async pipeline.</summary>
    public LoggerOptions WithBinaryLog(string path)
    {
        BinaryLogPath = path;
        AsyncBinaryLogging = true;
        AsyncLogging = true;
        return this;
    }

    /// <summary>Enable size-based rotation for file-backed sinks.</summary>
    public LoggerOptions WithRotation(long maxFileBytes, int maxFiles = 5, bool rotateOnStartup = false)
    {
        Rotation = new LogRotationOptions
        {
            MaxFileBytes = maxFileBytes,
            MaxFiles = maxFiles,
            RotateOnStartup = rotateOnStartup
        };
        return this;
    }

    /// <summary>Apply a filter predicate; entries for which the predicate returns <see langword="false"/> are dropped.</summary>
    public LoggerOptions WithFilter(Func<LogEventArgs, bool> filter) { Filter = filter; return this; }
}

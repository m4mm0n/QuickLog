using QuickLog.Core;
using QuickLog.Loggers;

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

    /// <summary>Maximum number of entries buffered by the async dispatcher.</summary>
    public int AsyncQueueCapacity { get; set; } = 8192;

    /// <summary>Optional sensitive value redaction settings.</summary>
    public LogRedactionOptions? Redaction { get; set; }

    /// <summary>Optional duplicate coalescing settings for the async path.</summary>
    public LogSpamControlOptions? SpamControl { get; set; }

    /// <summary>Logical session name included in startup/shutdown summaries and session folders.</summary>
    public string? SessionName { get; set; }

    /// <summary>Whether a session identifier should be generated automatically.</summary>
    public bool AutoSessionId { get; set; }

    /// <summary>Whether the logger should emit a compact startup banner when configured through <see cref="LogManager"/>.</summary>
    public bool EmitStartupBanner { get; set; }

    /// <summary>Whether the logger should emit a compact shutdown summary.</summary>
    public bool EmitShutdownSummary { get; set; }

    /// <summary>Whether console output should include ANSI color escape sequences.</summary>
    public bool UseAnsiColor { get; set; }

    /// <summary>Whether console output should use compact single-line formatting.</summary>
    public bool CompactText { get; set; }

    /// <summary>Whether console timestamps should be written in local time instead of UTC.</summary>
    public bool UseLocalTime { get; set; }

    /// <summary>Minimum level accepted by the logger before dispatching to any sink.</summary>
    public LogType MinimumLevel { get; set; } = LogType.Trace;

    /// <summary>Per-sink minimum levels keyed by sink name such as <c>console</c>, <c>file</c>, or <c>binary</c>.</summary>
    public Dictionary<string, LogType> SinkMinimumLevels { get; } = new(StringComparer.OrdinalIgnoreCase);

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

    /// <summary>Set the async dispatcher queue capacity.</summary>
    public LoggerOptions WithAsyncQueueCapacity(int capacity)
    {
        AsyncQueueCapacity = Math.Max(1, capacity);
        return this;
    }

    /// <summary>Enable sensitive value redaction with the default rules.</summary>
    public LoggerOptions WithRedaction(Action<LogRedactionOptions>? configure = null)
    {
        Redaction = new LogRedactionOptions();
        configure?.Invoke(Redaction);
        return this;
    }

    /// <summary>Enable duplicate message coalescing on the async path.</summary>
    public LoggerOptions WithSpamControl(int duplicateThreshold = 8)
    {
        SpamControl = new LogSpamControlOptions
        {
            Enabled = true,
            DuplicateThreshold = duplicateThreshold
        };
        return this;
    }

    /// <summary>Apply a filter predicate; entries for which the predicate returns <see langword="false"/> are dropped.</summary>
    public LoggerOptions WithFilter(Func<LogEventArgs, bool> filter) { Filter = filter; return this; }

    /// <summary>
    /// Creates a dependency-free, async-only profile for games, demo engines, and other frame-sensitive apps.
    /// </summary>
    /// <param name="logDirectory">Directory where JSON and binary logs should be written.</param>
    public static LoggerOptions ForEngine(string logDirectory = "logs") => new LoggerOptions()
        .WithConsole(false)
        .WithAsyncOnly()
        .WithJsonLog(CombineLogPath(logDirectory, "quicklog.jsonl"))
        .WithBinaryLog(CombineLogPath(logDirectory, "quicklog.qlog"))
        .WithRotation(16 * 1024 * 1024, maxFiles: 5)
        .WithRedaction(LogRedactionOptions.UseCrashSafePreset)
        .WithSpamControl(8)
        .WithSession("engine", autoId: true)
        .WithStartupBanner()
        .WithShutdownSummary();

    /// <summary>
    /// Creates a dependency-free async profile for long-running services.
    /// </summary>
    /// <param name="logDirectory">Directory where service logs should be written.</param>
    public static LoggerOptions ForService(string logDirectory = "logs") => ForEngine(logDirectory)
        .WithConsole(false)
        .WithSession("service", autoId: true);

    /// <summary>
    /// Creates a compact profile for command-line tools.
    /// </summary>
    /// <param name="sessionName">Logical tool session name.</param>
    public static LoggerOptions ForTool(string sessionName = "tool") => new LoggerOptions()
        .WithConsole()
        .WithAsync()
        .WithSession(sessionName, autoId: true)
        .WithCompactText()
        .WithAnsiColor();

    /// <summary>
    /// Creates a dependency-free async profile for Godot projects.
    /// </summary>
    /// <param name="logDirectory">Godot-style or filesystem log directory.</param>
    public static LoggerOptions ForGodot(string logDirectory = "user://logs") => ForEngine(logDirectory)
        .WithSession("godot", autoId: true);

    /// <summary>
    /// Sets session metadata used by startup/shutdown summaries.
    /// </summary>
    /// <param name="name">Logical session name.</param>
    /// <param name="autoId">Whether to generate a session identifier automatically.</param>
    public LoggerOptions WithSession(string? name = null, bool autoId = true)
    {
        SessionName = name;
        AutoSessionId = autoId;
        return this;
    }

    /// <summary>Enables or disables startup banner emission.</summary>
    public LoggerOptions WithStartupBanner(bool enabled = true) { EmitStartupBanner = enabled; return this; }

    /// <summary>Enables or disables shutdown summary emission.</summary>
    public LoggerOptions WithShutdownSummary(bool enabled = true) { EmitShutdownSummary = enabled; return this; }

    /// <summary>Enables or disables ANSI color in console output.</summary>
    public LoggerOptions WithAnsiColor(bool enabled = true) { UseAnsiColor = enabled; return this; }

    /// <summary>Enables or disables compact console text formatting.</summary>
    public LoggerOptions WithCompactText(bool enabled = true) { CompactText = enabled; return this; }

    /// <summary>Uses local-time console timestamps when enabled.</summary>
    public LoggerOptions WithLocalTime(bool enabled = true) { UseLocalTime = enabled; return this; }

    /// <summary>Sets the global minimum log level.</summary>
    public LoggerOptions WithMinimumLevel(LogType level) { MinimumLevel = level; return this; }

    /// <summary>Sets the minimum level for a named sink.</summary>
    public LoggerOptions WithSinkMinimumLevel(string sink, LogType level)
    {
        SinkMinimumLevels[sink] = level;
        return this;
    }

    /// <summary>
    /// Validates the option set for contradictory or lossy settings.
    /// </summary>
    public LoggerOptionsValidationResult Validate()
    {
        var issues = new List<LoggerOptionsIssue>();

        if (AsyncOnly && !AsyncLogging)
            issues.Add(new("QL001", "AsyncOnly requires AsyncLogging.", LoggerOptionsIssueSeverity.Error));

        if (AsyncOnly && string.IsNullOrWhiteSpace(JsonLogPath) && !AsyncBinaryLogging && !AsyncTraceLogging)
            issues.Add(new("QL001", "AsyncOnly should have at least one durable async sink.", LoggerOptionsIssueSeverity.Error));

        if (Rotation is not null && (Rotation.MaxFileBytes <= 0 || Rotation.MaxFiles <= 0))
            issues.Add(new("QL002", "Rotation requires MaxFileBytes and MaxFiles greater than zero.", LoggerOptionsIssueSeverity.Error));

        return new LoggerOptionsValidationResult(issues);
    }

    private static string CombineLogPath(string directory, string fileName)
        => directory.Contains("://", StringComparison.Ordinal)
            ? $"{directory.TrimEnd('/', '\\')}/{fileName}"
            : Path.Combine(directory, fileName);
}

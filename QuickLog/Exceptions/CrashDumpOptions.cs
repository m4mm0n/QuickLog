/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : CrashDumpOptions.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

namespace QuickLog.Exceptions;

using QuickLog.Core;

/// <summary>
/// Controls the structured crash-dump report written by <see cref="ExceptionHookManager"/>
/// when an unhandled exception is captured.
/// </summary>
public sealed class CrashDumpOptions
{
    /// <summary>
    /// Whether to write a crash dump file. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Directory where crash dump files are written.
    /// When <see langword="null"/> defaults to <c>%TEMP%\QuickLogCrashDumps</c>.
    /// The directory is created automatically if it does not exist.
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Maximum number of dump files to keep in <see cref="OutputDirectory"/>.
    /// Oldest files are deleted when the limit is exceeded. Defaults to <c>10</c>.
    /// </summary>
    public int MaxDumpFiles { get; set; } = 10;

    /// <summary>
    /// When <see langword="true"/>, the process's environment variables are included in
    /// the crash report. Defaults to <see langword="false"/> because env vars often
    /// contain secrets (API keys, connection strings, etc.).
    /// </summary>
    public bool IncludeEnvironmentVariables { get; set; } = false;

    /// <summary>
    /// When <see langword="true"/>, the crash report includes a tail of recent in-memory log entries.
    /// </summary>
    public bool IncludeRecentLogs { get; set; } = true;

    /// <summary>
    /// Maximum number of recent log entries to include when <see cref="IncludeRecentLogs"/> is enabled.
    /// </summary>
    public int RecentLogCount { get; set; } = 128;

    /// <summary>
    /// When <see langword="true"/>, the crash report includes async dispatcher health counters.
    /// </summary>
    public bool IncludeDispatcherStats { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, the crash report includes the current <see cref="LogStateSnapshot"/>.
    /// </summary>
    public bool IncludeStateSnapshot { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, the crash report includes a stable exception fingerprint.
    /// </summary>
    public bool IncludeFingerprint { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, crash reports include the one-based repeat count for the fingerprint.
    /// </summary>
    public bool CountDuplicateFingerprints { get; set; } = true;

    /// <summary>
    /// Sensitive value redaction applied to crash report text fields.
    /// </summary>
    public LogRedactionOptions Redaction { get; set; } = new();

    internal string ResolvedOutputDirectory =>
        OutputDirectory ?? Path.Combine(Path.GetTempPath(), "QuickLogCrashDumps");
}

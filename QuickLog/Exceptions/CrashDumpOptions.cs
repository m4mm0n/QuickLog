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

    internal string ResolvedOutputDirectory =>
        OutputDirectory ?? Path.Combine(Path.GetTempPath(), "QuickLogCrashDumps");
}

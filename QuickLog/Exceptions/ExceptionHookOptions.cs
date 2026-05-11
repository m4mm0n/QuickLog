/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ExceptionHookOptions.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

namespace QuickLog.Exceptions;

/// <summary>
/// Controls the behaviour of <see cref="ExceptionHookManager"/> when an unhandled exception is captured.
/// </summary>
public sealed class ExceptionHookOptions
{
    /// <summary>
    /// Whether to display a popup dialog when an exception is caught.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool ShowPopup { get; set; } = true;

    /// <summary>
    /// <see cref="LogType"/> used when logging the captured exception.
    /// Defaults to <see cref="LogType.Crit"/> because unhandled exceptions are always critical.
    /// </summary>
    public LogType ExceptionLogType { get; set; } = LogType.Crit;

    /// <summary>
    /// Title shown in the popup dialog.
    /// </summary>
    public string PopupTitle { get; set; } = "Unhandled Exception";

    /// <summary>
    /// When <see langword="true"/>, unobserved <see cref="Task"/> exceptions are marked as
    /// observed, preventing the process from being torn down by the finalizer thread.
    /// <para>
    /// Use with care: suppressing task exceptions can hide bugs. Only set this if you are
    /// certain every unobserved task exception is benign in your application.
    /// </para>
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool MarkTaskExceptionsObserved { get; set; } = false;

    /// <summary>
    /// Optional delegate that receives each exception before any other processing.
    /// Return <see langword="false"/> to completely ignore that exception (no log, no popup, no event).
    /// Return <see langword="true"/> (or leave <see langword="null"/>) to process it normally.
    /// </summary>
    public Func<Exception, ExceptionSource, bool>? ExceptionFilter { get; set; }

    /// <summary>
    /// Custom popup implementation. When <see langword="null"/> the built-in
    /// <see cref="DefaultExceptionPopup"/> (<c>MessageBoxW</c> on Windows, console fallback elsewhere) is used.
    /// </summary>
    public IExceptionPopup? CustomPopup { get; set; }

    /// <summary>
    /// When <see langword="true"/>, the full demystified stack trace is included in the popup message body.
    /// When <see langword="false"/>, only the exception type and message are shown.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool ShowStackTraceInPopup { get; set; } = true;

    /// <summary>
    /// Crash-dump report settings. When <see langword="null"/> or
    /// <see cref="CrashDumpOptions.Enabled"/> is <see langword="false"/>, no dump is written.
    /// </summary>
    public CrashDumpOptions? CrashDump { get; set; }

    /// <summary>
    /// Automatic restart / recovery settings. When <see langword="null"/> no restart or
    /// recovery logic is applied.
    /// </summary>
    public RestartOptions? Restart { get; set; }
}

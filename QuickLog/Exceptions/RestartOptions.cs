/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : RestartOptions.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

namespace QuickLog.Exceptions;

/// <summary>
/// Controls automatic process restart and non-fatal exception recovery behaviour inside
/// <see cref="ExceptionHookManager"/>.
/// </summary>
public sealed class RestartOptions
{
    internal const string RestartCountEnvVar = "QUICKLOG_RESTART_COUNT";

    /// <summary>
    /// When <see langword="true"/>, a new instance of the current process is spawned automatically
    /// after a fatal <see cref="AppDomain.UnhandledException"/> (one where
    /// <see cref="UnhandledExceptionEventArgs.IsTerminating"/> is <see langword="true"/>).
    /// The current process then exits normally.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool EnableAutoRestart { get; set; } = false;

    /// <summary>
    /// Maximum number of consecutive crash-restarts allowed before giving up.
    /// Prevents infinite restart loops. The count is tracked via the
    /// <c>QUICKLOG_RESTART_COUNT</c> environment variable passed to each child process.
    /// Defaults to <c>3</c>.
    /// </summary>
    public int MaxRestartCount { get; set; } = 3;

    /// <summary>
    /// Additional command-line arguments appended when spawning the restarted process.
    /// The original process arguments are always forwarded first.
    /// </summary>
    public string[]? ExtraArguments { get; set; }

    /// <summary>
    /// How long to wait before spawning the restarted process. Gives the crash dump
    /// writer and popup time to finish. Defaults to 500 ms.
    /// </summary>
    public TimeSpan DelayBeforeRestart { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Optional recovery delegate invoked for <em>non-terminating</em> exceptions
    /// (i.e. <see cref="ExceptionSource.UnobservedTask"/>).
    /// <para>
    /// Return <see langword="true"/> if your code successfully recovered from the exception
    /// (e.g. reset a connection pool). Returning <see langword="true"/> sets
    /// <see cref="ExceptionHookedEventArgs.SuppressDefaultHandling"/> so no further
    /// popup or log entry is produced.
    /// </para>
    /// Return <see langword="false"/> to let normal handling continue.
    /// </summary>
    public Func<Exception, bool>? RecoveryAction { get; set; }

    /// <summary>
    /// How many times the current process has been restarted by <see cref="ExceptionHookManager"/>.
    /// Reads the <c>QUICKLOG_RESTART_COUNT</c> environment variable set by the parent crash.
    /// Returns <c>0</c> when the process was started normally (not via auto-restart).
    /// </summary>
    public static int CurrentRestartCount =>
        int.TryParse(Environment.GetEnvironmentVariable(RestartCountEnvVar), out var n) ? n : 0;
}

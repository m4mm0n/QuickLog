using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Exceptions;

/// <summary>
/// Hooks into the two primary unhandled-exception surfaces exposed by the .NET runtime and takes
/// ownership over them: every captured exception is logged via QuickLog and (optionally) shown in
/// a modal popup dialog before the process continues or terminates.
/// <para>
/// Hooks registered:
/// <list type="bullet">
///   <item><see cref="AppDomain.UnhandledException"/> — exceptions that escape all try/catch on any thread.</item>
///   <item><see cref="TaskScheduler.UnobservedTaskException"/> — faulted <see cref="Task"/>s whose
///         exceptions were never observed before GC-finalization.</item>
/// </list>
/// </para>
/// <para>
/// Call <see cref="Attach(IQuickLog, ExceptionHookOptions?)"/> once at application startup and
/// <see cref="Detach"/> on shutdown. Multiple calls to <see cref="Attach"/> replace the logger and
/// options but do not register duplicate event handlers.
/// </para>
/// </summary>
public static class ExceptionHookManager
{
    private static IQuickLog? _logger;
    private static ExceptionHookOptions _options = new();
    private static bool _attached;
    private static readonly object _lock = new();
    private static readonly DefaultExceptionPopup _defaultPopup = new();

    /// <summary>
    /// Raised immediately when an unhandled exception is captured, before logging and the popup.
    /// Set <see cref="ExceptionHookedEventArgs.SuppressDefaultHandling"/> to <see langword="true"/>
    /// to take full control and skip the built-in log + popup.
    /// </summary>
    public static event EventHandler<ExceptionHookedEventArgs>? ExceptionCaught;

    /// <summary>
    /// Whether the hooks are currently registered.
    /// </summary>
    public static bool IsAttached
    {
        get { lock (_lock) return _attached; }
    }

    /// <summary>
    /// Registers the exception hooks and binds them to <paramref name="logger"/>.
    /// If already attached, replaces the logger and options without re-registering the handlers.
    /// </summary>
    /// <param name="logger">The logger that will receive captured exceptions.</param>
    /// <param name="options">
    /// Behaviour options. When <see langword="null"/> defaults are used
    /// (popup enabled, <see cref="LogType.Crit"/>, task exceptions <em>not</em> marked observed).
    /// </param>
    public static void Attach(IQuickLog logger, ExceptionHookOptions? options = null)
    {
        lock (_lock)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? new ExceptionHookOptions();

            if (!_attached)
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                _attached = true;
            }
        }
    }

    /// <summary>
    /// Removes the exception hooks. Safe to call even if not currently attached.
    /// </summary>
    public static void Detach()
    {
        lock (_lock)
        {
            if (!_attached) return;
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            _attached = false;
            _logger = null;
        }
    }

    // -----------------------------------------------------------------------------------------
    //  AppDomain.UnhandledException
    // -----------------------------------------------------------------------------------------

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception
                        ?? new Exception($"Non-CLR exception object: {e.ExceptionObject}");

        Handle(exception, ExceptionSource.AppDomain, e.IsTerminating);
    }

    // -----------------------------------------------------------------------------------------
    //  TaskScheduler.UnobservedTaskException
    // -----------------------------------------------------------------------------------------

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // Unwrap AggregateException so we log the real root causes individually when possible.
        var root = e.Exception.InnerExceptions.Count == 1
            ? e.Exception.InnerExceptions[0]
            : e.Exception;

        Handle(root, ExceptionSource.UnobservedTask, isTerminating: false);

        // Capture options snapshot so we don't hold the lock across the event call.
        bool markObserved;
        lock (_lock) markObserved = _options.MarkTaskExceptionsObserved;

        if (markObserved)
            e.SetObserved();
    }

    // -----------------------------------------------------------------------------------------
    //  Core handling pipeline
    // -----------------------------------------------------------------------------------------

    private static void Handle(Exception exception, ExceptionSource source, bool isTerminating)
    {
        // Capture consistent snapshots before entering the pipeline.
        IQuickLog? logger;
        ExceptionHookOptions opts;
        lock (_lock)
        {
            logger = _logger;
            opts = _options;
        }

        // 1. User filter — if it returns false, drop the exception entirely.
        try
        {
            if (opts.ExceptionFilter != null && !opts.ExceptionFilter(exception, source))
                return;
        }
        catch { /* never let the filter itself crash the handler */ }

        // 2. Fire the ExceptionCaught event so subscribers can inspect / suppress.
        var args = new ExceptionHookedEventArgs(exception, source, isTerminating);
        try
        {
            ExceptionCaught?.Invoke(null, args);
        }
        catch { /* event handler errors must not abort the pipeline */ }

        if (args.SuppressDefaultHandling)
            return;

        // 3. Attempt non-fatal recovery (unobserved tasks only).
        if (source == ExceptionSource.UnobservedTask && opts.Restart?.RecoveryAction != null)
        {
            try
            {
                if (opts.Restart.RecoveryAction(exception))
                    return; // recovered — skip log, dump, popup
            }
            catch { /* recovery delegate must not abort the pipeline */ }
        }

        // 4. Log the exception.
        if (logger != null)
        {
            try
            {
                var prefix = source == ExceptionSource.AppDomain
                    ? isTerminating
                        ? "[FATAL — process terminating]"
                        : "[FATAL — unhandled]"
                    : "[UNOBSERVED TASK]";

                logger.Log(opts.ExceptionLogType, $"⚠ Your exception has been hijacked by QuickLog! {prefix} {exception.GetType().Name}: {exception.Message}", exception);
            }
            catch { /* logging must never throw from within the exception handler */ }
        }

        // 5. Write crash dump.
        string? dumpPath = null;
        if (opts.CrashDump is { Enabled: true })
        {
            try
            {
                IReadOnlyList<LogEventArgs>? recentLogs = null;
                LogDispatcherStats? dispatcherStats = null;

                if (logger is QuickLog.Loggers.QuickLogger quickLogger)
                {
                    recentLogs = quickLogger.GetRecentLogs();
                    dispatcherStats = quickLogger.GetStats();
                }

                dumpPath = CrashDumpWriter.Write(
                    exception,
                    source,
                    isTerminating,
                    opts.CrashDump,
                    recentLogs,
                    dispatcherStats);
            }
            catch { /* dump errors must not abort the handler */ }
        }

        // 6. Show popup (include dump path when available).
        if (opts.ShowPopup)
        {
            try
            {
                var popup = opts.CustomPopup ?? _defaultPopup;
                var body = BuildPopupBody(exception, opts.ShowStackTraceInPopup, source, isTerminating, dumpPath);
                popup.Show(opts.PopupTitle, body, exception, source);
            }
            catch { /* popup errors must not abort the handler */ }
        }

        // 7. Auto-restart (fatal AppDomain exceptions only).
        if (isTerminating && source == ExceptionSource.AppDomain && opts.Restart is { EnableAutoRestart: true })
            TryRestart(opts.Restart);
    }

    private static void TryRestart(RestartOptions restart)
    {
        try
        {
            if (!RestartOptions.IsAutoRestartSupported)
                return;

            var currentCount = RestartOptions.CurrentRestartCount;
            if (currentCount >= restart.MaxRestartCount)
                return;

            if (restart.DelayBeforeRestart > TimeSpan.Zero)
                Thread.Sleep(restart.DelayBeforeRestart);

            var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (exe is null) return;

            var originalArgs = Environment.GetCommandLineArgs().Skip(1); // skip exe name
            var extraArgs = restart.ExtraArguments ?? [];
            var allArgs = string.Join(" ", originalArgs.Concat(extraArgs).Select(a => $"\"{a}\""));

            var psi = new System.Diagnostics.ProcessStartInfo(exe, allArgs)
            {
                UseShellExecute = false
            };
            psi.Environment[RestartOptions.RestartCountEnvVar] = (currentCount + 1).ToString();
            System.Diagnostics.Process.Start(psi);
        }
        catch { /* restart failure must not itself crash the handler */ }
    }

    private static string BuildPopupBody(Exception exception, bool includeStack, ExceptionSource source, bool isTerminating, string? dumpPath)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("⚠ Your exception has been hijacked by QuickLog!");
        sb.AppendLine();
        sb.AppendLine(source == ExceptionSource.AppDomain
            ? isTerminating
                ? "A fatal unhandled exception occurred. The application must terminate."
                : "An unhandled exception was caught."
            : "An unobserved Task exception was captured by the finalizer thread.");

        sb.AppendLine();
        sb.Append("Type:    ").AppendLine(exception.GetType().FullName ?? exception.GetType().Name);
        sb.Append("Message: ").AppendLine(exception.Message);

        if (dumpPath != null)
        {
            sb.AppendLine();
            sb.Append("Crash dump: ").AppendLine(dumpPath);
        }

        if (includeStack)
        {
            sb.AppendLine();
            sb.AppendLine("Stack trace:");
            sb.AppendLine(exception.ToStringDemystified());
        }

        return sb.ToString().TrimEnd();
    }
}

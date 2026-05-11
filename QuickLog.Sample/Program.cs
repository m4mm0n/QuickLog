/*
 * ====================================================================================================
 *  QuickLog.Sample — ExceptionHookManager demonstration
 * ====================================================================================================
 *  Demonstrates all three exception surfaces that ExceptionHookManager owns:
 *
 *    1. Manually caught exception  — logged via IQuickLog directly, no hook involved
 *    2. Unobserved Task exception  — TaskScheduler.UnobservedTaskException hook fires
 *    3. Unhandled AppDomain exception — AppDomain.UnhandledException fires, process terminates
 *
 *  Run the program and observe:
 *    - Console output (QuickLog)
 *    - Log file written next to the exe  (sample.log)
 *    - MessageBoxW popup for hook-captured exceptions (Windows)
 * ====================================================================================================
 */

using QuickLog;
using QuickLog.Exceptions;

// ── 1. Configure QuickLog ────────────────────────────────────────────────────
LogManager.ConfigureDefault("sample.log");

// ── 2. Attach exception hooks ────────────────────────────────────────────────
LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup             = true,
    ShowStackTraceInPopup = true,
    ExceptionLogType      = LogType.Crit,
    PopupTitle            = "QuickLog Sample — Unhandled Exception",
    MarkTaskExceptionsObserved = false,
});

// Subscribe to the raw event for any extra custom logic (e.g. crash-report upload).
ExceptionHookManager.ExceptionCaught += (_, args) =>
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"[ExceptionCaught event]  Source={args.Source}  Terminating={args.IsTerminating}");
    Console.WriteLine($"                         Type={args.Exception.GetType().Name}");
    Console.ResetColor();
};

var logger = LogManager.GetDefaultLogger();

Console.WriteLine("=== QuickLog ExceptionHookManager Sample ===");
Console.WriteLine();

// ── Demo 1: Manually caught — logger used directly, no hook involved ──────────
Console.WriteLine("--- Demo 1: Manually caught exception ---");
try
{
    ThrowArgumentException();
}
catch (ArgumentException ex)
{
    logger.Log(LogType.Error, "Caught in user code — handled gracefully.", ex);
    Console.WriteLine("  (exception was caught and logged manually — no popup)");
}

Console.WriteLine();
Pause("Press ENTER to trigger an unobserved Task exception...");

// ── Demo 2: Unobserved Task exception ────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("--- Demo 2: Unobserved Task exception ---");
Console.WriteLine("  (fire-and-forget task — never awaited, exception goes unobserved)");
FireAndForgetFaultedTask();

// Force GC so the faulted task's finalizer runs and raises UnobservedTaskException.
for (var i = 3; i > 0; i--)
{
    Console.WriteLine($"  Triggering GC to surface unobserved task... {i}");
    await Task.Delay(400);
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

Console.WriteLine();
Pause("Press ENTER to trigger an unhandled AppDomain exception (PROCESS WILL TERMINATE)...");

// ── Demo 3: Unhandled — escapes all catch blocks, AppDomain hook fires ────────
Console.WriteLine();
Console.WriteLine("--- Demo 3: Unhandled exception on background thread ---");
Console.WriteLine("  Watch for the MessageBoxW popup — then the process exits.");
Console.WriteLine();

ThrowUnhandledOnBackground();

// Keep the main thread alive long enough for the background thread to throw.
await Task.Delay(2000);

// ── Helpers ──────────────────────────────────────────────────────────────────

static void ThrowArgumentException()
    => throw new ArgumentException("'userId' must be a positive integer.", "userId");

static void FireAndForgetFaultedTask()
{
    // Intentionally not awaited — classic unobserved-task pattern.
    _ = Task.Run(async () =>
    {
        await Task.Delay(50);
        throw new InvalidOperationException("Background task failed: connection pool exhausted.");
    });
}

static void ThrowUnhandledOnBackground()
{
    var thread = new Thread(() =>
    {
        throw new AccessViolationException(
            "Critical failure: memory corruption detected in render pipeline.");
    })
    {
        IsBackground = false,   // non-background so the process stays alive until it throws
        Name = "Demo-Unhandled-Thread"
    };
    thread.Start();
}

static void Pause(string prompt)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write(prompt);
    Console.ResetColor();
    Console.ReadLine();
}

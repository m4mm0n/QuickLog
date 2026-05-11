/*
 * ====================================================================================================
 *  QuickLog.Sample — ExceptionHookManager demonstration
 * ====================================================================================================
 *  Demonstrates all exception ownership features:
 *
 *    1. Manually caught exception  — logged directly, no hook involved
 *    2. Unobserved Task exception  — TaskScheduler hook, recovery action fires first
 *    3. Unobserved Task exception  — TaskScheduler hook, recovery fails, crash dump written
 *    4. Unhandled AppDomain exception — fatal, crash dump written, process auto-restarts (once)
 *
 *  Output:
 *    - Console (QuickLog)
 *    - sample.log  (file next to the exe)
 *    - crash_*.json  (in %TEMP%\QuickLogCrashDumps)
 *    - MessageBoxW popup (Windows) for hook-captured exceptions
 * ====================================================================================================
 */

using QuickLog;
using QuickLog.Exceptions;

// ── Show restart context ──────────────────────────────────────────────────────
var restartCount = RestartOptions.CurrentRestartCount;
if (restartCount > 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"*** Process restarted by QuickLog (restart #{restartCount}) ***");
    Console.ResetColor();
    Console.WriteLine();
}

// ── 1. Configure QuickLog ────────────────────────────────────────────────────
LogManager.ConfigureDefault("sample.log");

// ── 2. Attach exception hooks ────────────────────────────────────────────────
LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup             = true,
    ShowStackTraceInPopup = true,
    ExceptionLogType      = LogType.Crit,
    PopupTitle            = "QuickLog Sample — Unhandled Exception",
    MarkTaskExceptionsObserved = true,  // keep the process alive for task exceptions

    CrashDump = new CrashDumpOptions
    {
        Enabled   = true,
        MaxDumpFiles = 20,
        // OutputDirectory defaults to %TEMP%\QuickLogCrashDumps
    },

    Restart = new RestartOptions
    {
        EnableAutoRestart = true,
        MaxRestartCount   = 1,   // restart at most once
        DelayBeforeRestart = TimeSpan.FromMilliseconds(800),

        // Recovery delegate: called for unobserved task exceptions before log/dump/popup.
        // Return true to silently swallow; return false to let normal handling continue.
        RecoveryAction = ex =>
        {
            if (ex is InvalidOperationException ioe && ioe.Message.Contains("recoverable"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [Recovery] Caught '{ex.Message}' — recovered silently, no dump.");
                Console.ResetColor();
                return true; // handled — skip log + dump + popup
            }
            return false; // not handled — proceed normally
        }
    }
});

// Subscribe to the raw event for any extra custom logic.
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

// ── Demo 1: Manually caught — no hook ────────────────────────────────────────
Console.WriteLine("--- Demo 1: Manually caught exception (no hook, no dump) ---");
try
{
    ThrowArgumentException();
}
catch (ArgumentException ex)
{
    logger.Log(LogType.Error, "Caught in user code — handled gracefully.", ex);
    Console.WriteLine("  (logged manually, no popup, no dump)");
}

Console.WriteLine();
Pause("Press ENTER to trigger a recoverable unobserved Task exception...");

// ── Demo 2: Unobserved Task — recovery succeeds ───────────────────────────────
Console.WriteLine();
Console.WriteLine("--- Demo 2: Unobserved Task — RecoveryAction handles it silently ---");
FireAndForgetFaultedTask("This is recoverable — pool reset succeeded.");
await DrainGC();

Console.WriteLine();
Pause("Press ENTER to trigger an unrecoverable unobserved Task exception (crash dump written)...");

// ── Demo 3: Unobserved Task — recovery fails, dump written ───────────────────
Console.WriteLine();
Console.WriteLine("--- Demo 3: Unobserved Task — not recoverable, crash dump written ---");
FireAndForgetFaultedTask("Catastrophic failure: disk full.");
await DrainGC();

Console.WriteLine();
Pause("Press ENTER to trigger a fatal AppDomain exception (crash dump + auto-restart)...");

// ── Demo 4: Fatal AppDomain — crash dump + auto-restart ───────────────────────
Console.WriteLine();
Console.WriteLine("--- Demo 4: Fatal AppDomain exception — crash dump written, process restarts ---");
if (restartCount >= 1)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  (max restarts reached — this time we let it die cleanly)");
    Console.ResetColor();
}
else
{
    Console.WriteLine("  Watch: crash dump written, MessageBoxW popup, then a new process spawns.");
}
Console.WriteLine();

ThrowUnhandledOnBackground();
await Task.Delay(3000);

// ── Helpers ──────────────────────────────────────────────────────────────────

static void ThrowArgumentException()
    => throw new ArgumentException("'userId' must be a positive integer.", "userId");

static void FireAndForgetFaultedTask(string message)
{
    _ = Task.Run(async () =>
    {
        await Task.Delay(50);
        throw new InvalidOperationException(message);
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
        IsBackground = false,
        Name = "Demo-Unhandled-Thread"
    };
    thread.Start();
}

static async Task DrainGC()
{
    for (var i = 3; i > 0; i--)
    {
        Console.WriteLine($"  GC pass {4 - i}/3...");
        await Task.Delay(400);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}

static void Pause(string prompt)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write(prompt);
    Console.ResetColor();
    Console.ReadLine();
}

/*
 * ====================================================================================================
 *  QuickLog.Sample — ExceptionHookManager + Godot integration demonstration
 * ====================================================================================================
 *  Demos:
 *    1.  Manual catch  — logged directly, no hook
 *    2.  Unobserved Task (recoverable) — RecoveryAction silences it
 *    3.  Unobserved Task (unrecoverable) — crash dump written
 *    4.  Fatal AppDomain exception — crash dump, popup, auto-restart
 *    5.  Godot integration — shows setup pattern and GodotBridge simulation
 * ====================================================================================================
 */

using QuickLog;
using QuickLog.Exceptions;
using QuickLog.Godot;

// ── Show restart context ──────────────────────────────────────────────────────
var restartCount = RestartOptions.CurrentRestartCount;
if (restartCount > 0)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"*** Process restarted by QuickLog (restart #{restartCount}) ***");
    Console.ResetColor();
    Console.WriteLine();
}

// ── Configure QuickLog ────────────────────────────────────────────────────────
LogManager.ConfigureDefault("sample.log");

// ── Attach exception hooks ────────────────────────────────────────────────────
LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup             = true,
    ShowStackTraceInPopup = true,
    ExceptionLogType      = LogType.Crit,
    PopupTitle            = "QuickLog Sample — Unhandled Exception",
    MarkTaskExceptionsObserved = true,
    CrashDump = new CrashDumpOptions { Enabled = true, MaxDumpFiles = 20 },
    Restart   = new RestartOptions
    {
        EnableAutoRestart  = true,
        MaxRestartCount    = 1,
        DelayBeforeRestart = TimeSpan.FromMilliseconds(800),
        RecoveryAction     = ex =>
        {
            if (ex is InvalidOperationException ioe && ioe.Message.Contains("recoverable"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"  [Recovery] '{ex.Message}' — recovered silently.");
                Console.ResetColor();
                return true;
            }
            return false;
        }
    }
});

ExceptionHookManager.ExceptionCaught += (_, args) =>
{
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"[ExceptionCaught]  Source={args.Source}  Terminating={args.IsTerminating}  Type={args.Exception.GetType().Name}");
    Console.ResetColor();
};

var logger = LogManager.GetDefaultLogger();

Console.WriteLine("=== QuickLog Sample ===");
Console.WriteLine();

// ─────────────────────────────────────────────────────────────────────────────
//  Exception demos (same as before)
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine("--- Demo 1: Manually caught exception ---");
try { ThrowArgumentException(); }
catch (ArgumentException ex)
{
    logger.Log(LogType.Error, "Caught in user code — handled gracefully.", ex);
    Console.WriteLine("  (logged manually, no popup, no dump)");
}

Console.WriteLine();
Pause("Press ENTER → recoverable unobserved Task...");
Console.WriteLine();
Console.WriteLine("--- Demo 2: Unobserved Task — RecoveryAction handles it ---");
FireAndForget("This is recoverable — pool reset succeeded.");
await DrainGC();

Console.WriteLine();
Pause("Press ENTER → unrecoverable unobserved Task (crash dump)...");
Console.WriteLine();
Console.WriteLine("--- Demo 3: Unobserved Task — crash dump written ---");
FireAndForget("Catastrophic failure: disk full.");
await DrainGC();

Console.WriteLine();
Pause("Press ENTER → fatal exception (crash dump + restart)...");
Console.WriteLine();
Console.WriteLine("--- Demo 4: Fatal AppDomain exception ---");
if (restartCount >= 1)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  (max restarts reached — dying cleanly this time)");
    Console.ResetColor();
}
ThrowUnhandledOnBackground();
await Task.Delay(3000);

// ─────────────────────────────────────────────────────────────────────────────
//  Godot integration demo (this only runs if restarted; in a real Godot project
//  you'd call this at _Ready() or Main() before anything else)
// ─────────────────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine("--- Demo 5: Godot integration setup ---");
Console.WriteLine();

DemoGodotIntegration(logger);

// ── Helpers ──────────────────────────────────────────────────────────────────

static void ThrowArgumentException()
    => throw new ArgumentException("'userId' must be a positive integer.", "userId");

static void FireAndForget(string message)
{
    _ = Task.Run(async () => { await Task.Delay(50); throw new InvalidOperationException(message); });
}

static void ThrowUnhandledOnBackground()
{
    new Thread(() => throw new AccessViolationException(
        "Critical failure: memory corruption detected in render pipeline."))
    { IsBackground = false, Name = "Demo-Unhandled-Thread" }.Start();
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

static void DemoGodotIntegration(IQuickLog logger)
{
    // ── Show whether Godot runtime is detected ──────────────────────────────
    var isGodot = GodotLogInterceptor.IsGodotPresent;
    Console.WriteLine($"  Godot runtime detected: {isGodot}");
    Console.WriteLine($"  (Not running inside Godot editor/export — expected false in this console app)");
    Console.WriteLine();

    // ── Show the one-liner that goes in any Godot project ──────────────────
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  In a real Godot project, add ONE line to your AutoLoad or _Ready():");
    Console.ResetColor();
    Console.WriteLine("    LogManager.AttachGodotHooks();");
    Console.WriteLine();
    Console.WriteLine("  This configures:");
    Console.WriteLine("    • GodotLogInterceptor  — routes GD.Print/PushError/PushWarning to QuickLog");
    Console.WriteLine("    • GodotBridge          — static callbacks for Godot.Logger subclasses");
    Console.WriteLine("    • ExceptionHookManager — hijacks all .NET exceptions in your Godot game");
    Console.WriteLine("    • GodotAlertPopup      — uses OS.Alert() for native Godot error dialogs");
    Console.WriteLine("    • CrashDumpWriter      — writes crash_*.json on every fatal exception");
    Console.WriteLine();

    // ── Simulate what GodotBridge would receive from the engine ────────────
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  Simulating Godot engine callbacks through GodotBridge:");
    Console.ResetColor();
    Console.WriteLine();

    // Attach Godot interceptor so GodotBridge is wired up for the simulation below.
    GodotLogInterceptor.Attach(logger, new GodotLogOptions
    {
        HijackExceptions = false,       // already hooked above via AttachExceptionHooks
        TryDynamicLoggerRegistration = false  // no Godot runtime present in this console app
    });

    GodotBridge.GodotLogReceived += (_, args) =>
    {
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine($"  [GodotLogReceived event] Source={args.Source}  LogType={args.LoggingType}");
        Console.ResetColor();
    };

    Console.WriteLine("  → GD.Print(\"Player spawned at (10, 0, 5)\")");
    GodotBridge.HandleMessage("Player spawned at (10, 0, 5)", error: false);

    Console.WriteLine("  → GD.PrintErr(\"Texture atlas rebuild failed\")");
    GodotBridge.HandleMessage("Texture atlas rebuild failed", error: true);

    Console.WriteLine("  → GD.PushWarning(\"Physics body sleeping unexpectedly\")");
    GodotBridge.HandleError("_physics_process", "player.gd", 42,
        "PHYSICS_SLEEP", "Physics body sleeping unexpectedly", errorTypeValue: 1);

    Console.WriteLine("  → GD.PushError(\"Null reference in AI state machine\")");
    GodotBridge.HandleError("_process", "ai_controller.gd", 88,
        "NULL_REF", "Null reference in AI state machine", errorTypeValue: 0);

    Console.WriteLine("  → GDScript runtime error");
    GodotBridge.HandleError("update_health", "hud.gd", 17,
        "SCRIPT_ERROR", "Variable 'hp_bar' is null.", errorTypeValue: 2);

    Console.WriteLine();

    // ── Show the manual bridge template ────────────────────────────────────
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("  If dynamic OS.AddLogger registration doesn't work in your project,");
    Console.WriteLine("  add this two-file bridge (copy-paste ready):");
    Console.ResetColor();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  // QuickLogSink.cs  (inside YOUR Godot project)");
    Console.WriteLine("  public partial class QuickLogSink : Godot.Logger");
    Console.WriteLine("  {");
    Console.WriteLine("      public override void _LogMessage(string message, bool error)");
    Console.WriteLine("          => GodotBridge.HandleMessage(message, error);");
    Console.WriteLine();
    Console.WriteLine("      public override void _LogError(string function, string file, int line,");
    Console.WriteLine("          string code, string rationale, bool errorType, int errorTypeValue,");
    Console.WriteLine("          Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)");
    Console.WriteLine("          => GodotBridge.HandleError(function, file, line, code, rationale, errorTypeValue);");
    Console.WriteLine("  }");
    Console.WriteLine();
    Console.WriteLine("  // In your AutoLoad _Ready():  OS.AddLogger(new QuickLogSink());");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("  Everything else (routing, log levels, crash dumps, popups) is handled");
    Console.WriteLine("  automatically by QuickLog — no other code needed.");
}

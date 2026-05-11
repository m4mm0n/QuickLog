# QuickLog

QuickLog is a **high-performance, engine-grade logging system** written in C#.
It is designed for **deterministic behavior**, **low allocation**, and **post-mortem analysis**,
making it especially suitable for **game engines, demo engines, tools, and services**.

QuickLog deliberately avoids heavy abstractions, reflection, DI containers,
and message-template complexity. What you get instead is **clarity, control, and speed**.

---

## Core Principles

- **Deterministic behavior**
- **Async-first design**
- **Bounded memory usage**
- **No hidden allocations**
- **Crash-safe logging**
- **Offline analysis tooling**
- **Explicit lifecycle control**
- **Zero external dependencies**

---

## Features

### Logging Core
- `IQuickLog` clean interface
- Strongly typed `LogType`
- Caller info via compiler services
- Exception demystification (stack trace clean-up, zero deps)
- CRC32 integrity checks
- Scopes (`LogScope`)
- Thread roles (`ThreadContext`)

### Sinks
- Console
- File (text)
- Trace
- Event-only
- Memory (circular buffer)
- Binary (CRC protected)

### Async Pipeline
- Dedicated background dispatcher
- Bounded queue
- Configurable drop policies
- Severity-aware dropping
- Thread-role-aware dropping
- Async-only mode (no sync IO)
- Deterministic flush & shutdown

### Exception Ownership *(v2.0)*
- Hook `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException`
- Log every captured exception automatically
- Modal popup (native `MessageBoxW` — zero deps)
- Structured JSON crash dump (`crash_*.json`)
- Auto-restart on fatal exceptions (with loop guard)
- Recovery delegate for non-fatal task exceptions
- `ExceptionCaught` event for custom side-effects
- Per-exception filter delegate

### Godot Integration *(v2.0)*
- Route `GD.Print`, `GD.PrintErr`, `GD.PushError`, `GD.PushWarning`, GDScript/shader errors through QuickLog
- Dynamic `Godot.Logger` subclass via `Reflection.Emit` — zero compile-time Godot dependency
- Manual bridge template for guaranteed Godot 4 C# compatibility
- Native `OS.Alert()` popup for exception dialogs inside Godot
- One-liner setup via `LogManager.AttachGodotHooks()`

### Tooling
- Binary log reader
- Binary log exporter
- Binary log query/filtering
- Timeline TUI viewer
- Colorized output
- Search + highlighting
- Level / role toggles
- Grouping by time slices
- Filter presets (save/load)

---

## Basic Usage

```csharp
var logger = new QuickLogger(
    logFilePath: "logs/app.log",
    consoleLogging: true,
    fileLogging: true);

logger.Log(LogType.Info,  "Hello QuickLog");
logger.Log(LogType.Warn,  "Something might be wrong");
logger.Log(LogType.Error, new Exception("Boom"));
```

---

## LogManager — Centralized Setup

```csharp
// Configure once at startup
LogManager.ConfigureDefault("app.log");

// Get a named logger anywhere in the codebase
var logger = LogManager.GetLogger("Database");
var logger = LogManager.GetLogger(typeof(MyClass));

// Access the default logger
var log = LogManager.GetDefaultLogger();
```

---

## Async-Only Mode (Recommended for Engines)

```csharp
logger.AsyncOnly = true;
logger.EnableAsyncLogging = true;

logger.AsyncDropPolicy = AsyncDropPolicy.DropBelowLevel;
logger.AsyncMinimumLevel = LogType.Error;
```

This ensures:
- No blocking IO on the game thread
- No frame hitches
- Critical logs are never dropped

---

## Thread Roles

Assign once per thread:

```csharp
ThreadContext.Set(ThreadRole.Render);
ThreadContext.Set(ThreadRole.Audio);
ThreadContext.Set(ThreadRole.Network);
```

All logs from that thread are tagged accordingly, and the async drop policy can
protect or deprioritize specific roles.

---

## Scopes

```csharp
using (LogScope.Begin("Frame", frameId))
{
    logger.Log(LogType.Trace, "Rendering frame");
}
```

Scopes are propagated into async and binary logs.

---

## Exception Ownership *(v2.0)*

QuickLog can take **full ownership** of every unhandled exception in your process —
logging it, writing a crash dump, showing a popup, and optionally restarting.

### One-liner setup

```csharp
LogManager.ConfigureDefault("app.log");
LogManager.AttachExceptionHooks();          // owns all unhandled exceptions from here
```

### Full options

```csharp
LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup             = true,
    ShowStackTraceInPopup = true,
    ExceptionLogType      = LogType.Crit,
    PopupTitle            = "My App — Unhandled Exception",

    // Crash dump — written to %TEMP%\QuickLogCrashDumps\crash_*.json
    CrashDump = new CrashDumpOptions
    {
        Enabled      = true,
        MaxDumpFiles = 10
    },

    // Auto-restart on fatal AppDomain exceptions
    Restart = new RestartOptions
    {
        EnableAutoRestart  = true,
        MaxRestartCount    = 3,
        DelayBeforeRestart = TimeSpan.FromMilliseconds(500),

        // Recovery delegate for non-fatal unobserved task exceptions
        RecoveryAction = ex =>
        {
            if (ex is InvalidOperationException && ex.Message.Contains("connection"))
            {
                ResetConnectionPool();
                return true;   // recovered — skip log/dump/popup
            }
            return false;      // not recovered — proceed normally
        }
    },

    // Filter: ignore specific exceptions entirely
    ExceptionFilter = (ex, source) => ex is not OperationCanceledException
});
```

### Crash dump format

Each crash is written as a structured JSON file:

```json
{
  "Timestamp": "2026-05-11T07:05:42Z",
  "Source": "AppDomain",
  "IsTerminating": true,
  "RestartCount": 0,
  "Exception": {
    "Type": "System.AccessViolationException",
    "Message": "Critical failure: memory corruption detected.",
    "StackTrace": "..."
  },
  "Process": {
    "Id": 1234,
    "Name": "MyApp",
    "Executable": "C:\\MyApp\\MyApp.exe",
    "MemoryBytes": 47259648
  },
  "Environment": {
    "MachineName": "WORKSTATION-01",
    "OsVersion": "Microsoft Windows NT 10.0.26200.0",
    "RuntimeVersion": "8.0.22"
  }
}
```

### Subscribe to the raw event

```csharp
ExceptionHookManager.ExceptionCaught += (_, args) =>
{
    // args.Exception, args.Source, args.IsTerminating
    // Set args.SuppressDefaultHandling = true to skip log + popup
    UploadCrashReport(args.Exception);
};
```

### Check restart count

```csharp
// At startup — know if the process was restarted after a crash
if (RestartOptions.CurrentRestartCount > 0)
    logger.Log(LogType.Warn, $"Restarted after crash (attempt #{RestartOptions.CurrentRestartCount})");
```

---

## Godot Integration *(v2.0)*

QuickLog integrates directly with the Godot 4 C# engine — routing all engine output
through QuickLog and hijacking unhandled exceptions with native `OS.Alert()` dialogs.

### One-liner setup (in your AutoLoad or `_Ready()`)

```csharp
LogManager.ConfigureDefault("user://logs/game.log");
LogManager.AttachGodotHooks();
```

This automatically:
- Intercepts `GD.Print`, `GD.PrintErr`, `GD.PushError`, `GD.PushWarning`, GDScript errors
- Hooks all unhandled .NET exceptions with a native `OS.Alert()` popup
- Writes crash dumps to `%TEMP%\QuickLogCrashDumps` on every fatal exception

### Full options

```csharp
LogManager.AttachGodotHooks(new GodotLogOptions
{
    InterceptPrint       = true,
    InterceptPrintError  = true,
    InterceptErrors      = true,
    InterceptWarnings    = true,
    InterceptScriptErrors = true,

    PrintLogType        = LogType.Info,
    PrintErrorLogType   = LogType.Error,
    ErrorLogType        = LogType.Error,
    WarningLogType      = LogType.Warn,
    ScriptErrorLogType  = LogType.Crit,

    HijackExceptions    = true,   // wraps ExceptionHookManager with OS.Alert popup
    ExceptionOptions    = new ExceptionHookOptions
    {
        CrashDump = new CrashDumpOptions { Enabled = true }
    }
});
```

### Check if dynamic Logger registration succeeded

```csharp
LogManager.AttachGodotHooks();

if (!GodotLogInterceptor.IsDynamicSinkRegistered)
    GD.Print("QuickLog: manual bridge needed — see GodotBridge docs");
```

### Manual bridge (guaranteed to work in all Godot 4 C# setups)

If `IsDynamicSinkRegistered` is `false`, add these two files to your **Godot project**:

```csharp
// QuickLogSink.cs  (inside your Godot project, NOT in QuickLog)
public partial class QuickLogSink : Godot.Logger
{
    public override void _LogMessage(string message, bool error)
        => GodotBridge.HandleMessage(message, error);

    public override void _LogError(string function, string file, int line,
        string code, string rationale, bool errorType, int errorTypeValue,
        Godot.Collections.Array<ScriptBacktrace> scriptBacktraces)
        => GodotBridge.HandleError(function, file, line, code, rationale, errorTypeValue);
}
```

```csharp
// In your AutoLoad _Ready():
OS.AddLogger(new QuickLogSink());
```

Everything else — routing, log levels, crash dumps, popups — is handled automatically.

### Subscribe to Godot log events

```csharp
GodotLogInterceptor.GodotLogReceived += (_, args) =>
{
    // args.Source, args.Message, args.Function, args.File, args.Line
    // Set args.SuppressLogging = true to skip the QuickLog forward
};
```

### Godot file logger

```csharp
// Writes to user:// when running under Godot, falls back to %LOCALAPPDATA%\GodotUser
var logger = new GodotFileLogger("game.log", subfolder: "logs");
Console.WriteLine(logger.IsUsingGodotPath);  // true when inside Godot
Console.WriteLine(logger.FullPath);
```

---

## Binary Logs & Analysis

### Export to text

```csharp
BinaryLogExporter.ExportToText("quicklog.bin", "recovered.log");
```

### Query

```csharp
var errors = BinaryLogQuery.WithLevel(
    "quicklog.bin",
    LogType.Error | LogType.Crit);
```

### Timeline Viewer

```csharp
BinaryLogTimelineViewer.Run("quicklog.bin");
```

Controls:
```
↑ ↓        Navigate
PgUp/PgDn  Jump
G          Group by time
/          Search (highlighted)
L          Toggle log levels
R          Toggle thread roles
F5         Save filter preset
F9         Load filter preset
Esc        Exit
```

---

## Shutdown

Always shut down explicitly — this flushes async queues, detaches all hooks, and
ensures no logs are lost:

```csharp
LogManager.Shutdown();
```

---

## What QuickLog Is NOT

- Not a DI-based framework
- Not a message-template logger
- Not reflection-heavy at runtime
- Not opinionated about formatting
- Not hiding behavior behind magic

QuickLog is **infrastructure**, not ceremony.

---

## License

MIT

---

## Status

| Component | Status |
|---|---|
| Core logging / sinks | Production-ready |
| Async pipeline | Production-ready |
| Binary logs & tooling | Production-ready |
| Exception ownership | Stable (v2.0) |
| Crash dump writer | Stable (v2.0) |
| Godot integration | Experimental (v2.0) |

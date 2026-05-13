# QuickLog

[![NuGet](https://img.shields.io/nuget/v/ZLS.QuickLog.svg)](https://www.nuget.org/packages/ZLS.QuickLog)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ZLS.QuickLog.svg)](https://www.nuget.org/packages/ZLS.QuickLog)

QuickLog is a **high-performance, engine-grade logging system** written in C#.
It is designed for **deterministic behavior**, **low allocation**, and **post-mortem analysis**,
making it especially suitable for **game engines, demo engines, tools, and services**.

QuickLog deliberately avoids heavy abstractions, reflection, DI containers,
and message-template complexity. What you get instead is **clarity, control, and speed**.

---

## Install

```powershell
dotnet add package ZLS.QuickLog --version 2.2.0
```

QuickLog targets `net8.0` and `net10.0`, ships with XML documentation, and has
no external package dependencies.

---

## Quick Start

```csharp
using QuickLog;
using QuickLog.Loggers;

var logger = new QuickLogger(
    logFilePath: "logs/app.log",
    consoleLogging: true,
    fileLogging: true);

logger.Log(LogType.Info, "Hello QuickLog");
logger.Log(LogType.Warn, "Something might be wrong");
logger.Log(LogType.Error, new Exception("Boom"));

logger.Dispose();
```

For application-wide setup:

```csharp
LogManager.ConfigureDefault(
    new LoggerOptions()
        .WithAsyncOnly()
        .WithJsonLog("logs/app.jsonl")
        .WithBinaryLog("logs/app.qlog")
        .WithRotation(maxFileBytes: 16 * 1024 * 1024, maxFiles: 5)
        .WithRedaction()
        .WithSpamControl(duplicateThreshold: 8));

var log = LogManager.GetDefaultLogger();
log.Log(LogType.Info, "QuickLog is online");

LogManager.Shutdown();
```

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
- Async-flowing scopes (`LogScope`)
- Correlation ids plus `Activity` trace/span capture (`LogContext`)
- Built-in sensitive value redaction
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
- Dispatcher health counters
- Duplicate message coalescing
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
- Binary log query/filtering by level, time, correlation, or text
- Zero-dependency `QuickLog.Tools` CLI
- Doctor / inspect / replay / benchmark / bundle commands
- Source-less launch and passive observe helpers
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

## v2.2 Engine Mode

```csharp
LogManager.ConfigureDefault(
    new LoggerOptions()
        .WithAsyncOnly()
        .WithJsonLog("logs/app.jsonl")
        .WithBinaryLog("logs/app.qlog")
        .WithRotation(maxFileBytes: 16 * 1024 * 1024, maxFiles: 5)
        .WithAsyncQueueCapacity(8192)
        .WithRedaction()
        .WithSpamControl(duplicateThreshold: 8));

var logger = LogManager.GetDefaultLogger();

using (LogContext.BeginCorrelation(Guid.NewGuid().ToString("N")))
using (LogScope.Begin("Startup"))
{
    logger.Log(LogType.Info, "Game boot sequence started");
}
```

This enables the dependency-free v2.2 path: async-only dispatch, JSON Lines,
CRC-protected binary logs, size-based rotation, redaction, duplicate coalescing,
and async-safe scope/correlation context.

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

## Dispatcher Health

```csharp
if (LogManager.GetDefaultLogger() is QuickLog.Loggers.QuickLogger quickLogger)
{
    var stats = quickLogger.GetStats();
    Console.WriteLine($"written={stats.Written} dropped={stats.DroppedTotal}");
}
```

The dispatcher tracks queue depth, capacity, written entries, dropped entries,
sink failures, and the last sink error without adding a metrics dependency.

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
using (LogContext.BeginCorrelation(matchId))
{
    logger.Log(LogType.Trace, "Rendering frame");
}
```

Scopes and correlation ids flow through async continuations and are written to
JSON Lines, binary logs, crash dump log tails, and text exports.

---

## Redaction & Duplicate Control

```csharp
LogManager.ConfigureDefault(
    new LoggerOptions()
        .WithAsyncOnly()
        .WithBinaryLog("logs/app.qlog")
        .WithRedaction(options => options.SensitiveKeys.Add("session"))
        .WithSpamControl(duplicateThreshold: 8));
```

Redaction masks common secrets before async sinks and crash dumps see them.
Duplicate control keeps hot repeated messages from flooding disk by emitting a
summary entry after the threshold is crossed.

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
  },
  "RecentLogs": [
    {
      "Level": "Error",
      "Message": "Lost connection to asset server",
      "Scope": "Frame:18442",
      "CorrelationId": "match-7"
    }
  ],
  "Dispatcher": {
    "Written": 128,
    "DroppedTotal": 0,
    "SinkFailures": 0
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

Text exports include the source location, scope, correlation id, trace id, and
span id when those fields are present.

### Query

```csharp
var errors = BinaryLogQuery.WithLevel(
    "quicklog.bin",
    LogType.Error | LogType.Crit);

var bootLogs = BinaryLogQuery.ContainingText("quicklog.bin", "boot");
var matchLogs = BinaryLogQuery.WithCorrelation("quicklog.bin", "match-7");
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

## QuickLog.Tools *(v2.2 Experimental)*

`QuickLog.Tools` is a zero-external-dependency companion CLI. It references
QuickLog, uses only the .NET runtime libraries, and keeps the core logger clean.

Run it from the repo:

```powershell
dotnet run --project QuickLog.Tools -- doctor logs --recursive
dotnet run --project QuickLog.Tools -- inspect logs/app.qlog --level Error --correlation match-7
dotnet run --project QuickLog.Tools -- replay logs/app.qlog --to jsonl --out logs/app.replay.jsonl
dotnet run --project QuickLog.Tools -- benchmark --iterations 10000 --mode binary
dotnet run --project QuickLog.Tools -- bundle --out support.zip --logs logs --crashes crashes --include-env --include-exports
```

### Source-less Diagnostics

Use `launch` when QuickLog should start the selected app and capture stdout,
stderr, process lifetime, and QuickLog session artifacts:

```powershell
dotnet run --project QuickLog.Tools -- launch --out sessions/app --diagnostic-env --wait-for-exit -- dotnet --info
```

Use `observe` when the process is already running:

```powershell
dotnet run --project QuickLog.Tools -- observe --pid 1234 --duration 10 --out sessions/observe-1234
```

`observe` is passive. It records process metadata, memory/thread samples, and a
best-effort diagnostic-port probe. It does not inject code into the process and
does not claim deep EventPipe capture without the diagnostics client dependency
that QuickLog intentionally avoids.

### Profiler Helper

The profiler command is an experimental helper for CLR profiler environment
blocks. It does not ship a native profiler DLL.

```powershell
dotnet run --project QuickLog.Tools -- profiler explain
dotnet run --project QuickLog.Tools -- profiler env --clsid 00000000-0000-0000-0000-000000000000 --path C:\Tools\Profiler.dll
```

Only use launch, observe, or profiler settings on applications you own or are
authorized to inspect.

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
| Async pipeline | Production-ready (v2.2 health stats) |
| Binary logs & tooling | Production-ready (v2.2 context-aware format) |
| Exception ownership | Stable (v2.0) |
| Crash dump writer | Stable (v2.2 log tail + dispatcher stats) |
| Godot integration | Experimental (v2.0) |

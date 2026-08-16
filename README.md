# QuickLog 3

[![NuGet](https://img.shields.io/nuget/v/ZLS.QuickLog.svg)](https://www.nuget.org/packages/ZLS.QuickLog)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ZLS.QuickLog.svg)](https://www.nuget.org/packages/ZLS.QuickLog)
[![CI](https://github.com/m4mm0n/QuickLog/actions/workflows/ci.yml/badge.svg)](https://github.com/m4mm0n/QuickLog/actions/workflows/ci.yml)

QuickLog is a dependency-free logging and diagnostics runtime for games, tools,
desktop applications, and services. Version 3 adds typed structured events, the
QLOG v3 binary format, allocation-aware call sites, asynchronous lifecycle APIs,
compressed retention, richer offline tooling, Native AOT verification, and an
optional Microsoft logging adapter.

The core package targets `net8.0` and `net10.0`. It has no runtime package
dependencies and is continuously built on Windows, Linux, and macOS. Android and
iOS consumers are compile-verified from dedicated mobile target frameworks.

## Install

Install the standalone core:

```powershell
dotnet add package ZLS.QuickLog --version 3.0.0
```

For hosts that already use `Microsoft.Extensions.Logging`, add the separate
adapter package:

```powershell
dotnet add package ZLS.QuickLog.Extensions.Logging --version 3.0.0
```

`ZLS.QuickLog.Extensions.Logging` depends on the Microsoft logging abstractions;
`ZLS.QuickLog` itself remains dependency-free.

## Quick start

```csharp
using QuickLog;
using QuickLog.Loggers;

await using var logger = new QuickLogger
{
    EnableConsoleLogging = true,
    EnableAsyncLogging = true,
    AsyncOnly = true,
    JsonLogPath = "logs/app.jsonl",
    EnableAsyncBinaryLogging = true,
    BinaryLogPath = "logs/app.qlog"
};

logger.Log(LogType.Info, "QuickLog is online");
logger.Log(
    LogType.Info,
    "Player connected",
    new LogEventId(1001, "PlayerConnected"),
    LogProperties.Create(
        new LogProperty("playerId", 42),
        new LogProperty("region", "eu-north")));

await logger.ShutdownAsync(TimeSpan.FromSeconds(5));
```

For application-wide setup:

```csharp
LogManager.ConfigureDefault(
    LoggerOptions.ForEngine("logs")
        .WithMinimumLevel(LogType.Info)
        .WithRotation(
            maxFileBytes: 16 * 1024 * 1024,
            maxFiles: 8,
            maxAge: TimeSpan.FromDays(14),
            maxTotalBytes: 128 * 1024 * 1024,
            compressRotatedFiles: true));

var log = LogManager.GetDefaultLogger();
log.Log(LogType.Info, "Engine startup complete");
LogManager.Shutdown();
```

`LoggerOptions.CreateLogger()` creates an independently owned logger without
changing global `LogManager` state.

## What ships in v3

- Typed event identifiers and immutable structured properties.
- QLOG v3 records with CRC32 integrity and typed property values.
- Backward reads for QLOG v1 and v2 files.
- JSON Lines, text, console, trace, memory, event, and binary sinks.
- `IsEnabled(LogType)` and a custom interpolated-string handler that avoids
  evaluating disabled messages.
- Async-only dispatch with bounded queues and explicit drop policies.
- `FlushAsync`, `ShutdownAsync`, `IAsyncDisposable`, timeout, and cancellation.
- Rotation by active-file size, retained-file count, age, and total byte budget.
- Optional GZip compression for rotated files.
- Async-flowing scopes, correlations, properties, and `Activity` trace/span IDs.
- Secret redaction for messages, structured properties, crash reports, and bundles.
- Crash ownership, fingerprints, recent log tails, state snapshots, and restart guards.
- Offline inspect, search, replay, repair, merge, report, bundle, and timeline tools.
- Native AOT and trimming-compatible core paths with explicit reflection boundaries.
- Windows, Linux, macOS, Android, iOS, tool, service, engine, and Godot profiles.
- A separate `Microsoft.Extensions.Logging` provider package.
- Deterministic NuGet packages, XML documentation, portable symbols, package
  consumer validation, and release checksums.

## Structured events

Use a stable numeric identifier for machine queries and an optional name for
human diagnostics. Values are snapshotted before asynchronous dispatch, so later
dictionary changes cannot alter an already accepted event.

```csharp
var properties = LogProperties.Create(
    new LogProperty("asset", "models/ship.glb"),
    new LogProperty("attempt", 3),
    new LogProperty("cached", true),
    new LogProperty("elapsedMs", 12.5));

logger.Log(
    LogType.Info,
    "Asset loaded",
    new LogEventId(1201, "AssetLoaded"),
    properties);
```

Supported typed QLOG values include strings, booleans, signed and unsigned
integers, floating-point numbers, decimals, `DateTime`, `DateTimeOffset`, and
`Guid`. Other values are captured using invariant text.

### Structured scopes

```csharp
using (LogContext.BeginCorrelation(matchId))
using (LogScope.Begin(
    new LogProperty("matchId", matchId),
    new LogProperty("map", "arena-7")))
{
    logger.Log(LogType.Info, "Round started", new LogEventId(1300, "RoundStarted"));
}
```

Event properties override properties with the same name inherited from the
current scope. Scope and correlation state flow through async continuations.

### Allocation-aware interpolation

Interpolated expressions are not evaluated when the level is disabled:

```csharp
if (logger.IsEnabled(LogType.Debug))
{
    // Useful for non-interpolated expensive work.
}

logger.Log(LogType.Debug, $"Visible chunks: {world.GetVisibleChunkCount()}");
```

The second form uses QuickLog's interpolated-string handler automatically. This
is formatting, not message-template parsing; use structured properties when a
value must remain independently queryable.

## Profiles and platform paths

```csharp
var engine = LoggerOptions.ForEngine("logs");
var service = LoggerOptions.ForService("logs");
var tool = LoggerOptions.ForTool("asset-packer");
var godot = LoggerOptions.ForGodot("user://logs");
var linux = LoggerOptions.ForLinux("my-game");
var macos = LoggerOptions.ForMacOS("my-game");
var android = LoggerOptions.ForAndroid("my-game", logDirectory: appFilesDirectory);
var ios = LoggerOptions.ForIOS("my-game", logDirectory: appSupportDirectory);
```

`ForLinux` prefers `$XDG_STATE_HOME/<app>/logs`, then
`~/.local/state/<app>/logs`. `ForMacOS` uses `~/Library/Logs/<app>`. Mobile
profiles use application local data unless the platform host supplies a writable
directory. Mobile profiles disable console output and process auto-restart is
reported as unsupported on Android and iOS.

All profiles can be validated before use:

```csharp
var validation = options.Validate();
foreach (var issue in validation.Issues)
    Console.WriteLine($"{issue.Severity} {issue.Code}: {issue.Message}");
```

## Async pipeline and lifecycle

The dispatcher owns a bounded queue and a dedicated background thread. Available
full-queue policies are `DropNewest`, `DropOldest`, `DropBelowLevel`, and
`DropByThreadRole`.

```csharp
var logger = LoggerOptions.ForEngine("logs")
    .WithAsyncQueueCapacity(16_384)
    .CreateLogger();

logger.AsyncDropPolicy = AsyncDropPolicy.DropBelowLevel;
logger.AsyncMinimumLevel = LogType.Warn;
logger.AsyncProtectedRole = ThreadRole.Audio;

await logger.FlushAsync(cancellationToken);
await logger.ShutdownAsync(TimeSpan.FromSeconds(5), cancellationToken);
```

`GetStats()` reports capacity, current depth, accepted and written entries,
drops by reason, sink failures, and the last sink error. Shutdown summaries can
include these counters.

Assign a role once per specialized thread:

```csharp
ThreadContext.Set(ThreadRole.Render);
ThreadContext.Set(ThreadRole.Audio);
ThreadContext.Set(ThreadRole.Network);
```

## Rotation, retention, and compression

```csharp
var rotation = new LogRotationOptions
{
    MaxFileBytes = 32 * 1024 * 1024,
    MaxFiles = 10,
    MaxAge = TimeSpan.FromDays(30),
    MaxTotalBytes = 256 * 1024 * 1024,
    RotateOnStartup = false,
    CompressRotatedFiles = true
};
```

Retention applies to text, JSON Lines, and QLOG sinks. The active file is never
deleted to satisfy a budget; oldest rotations are removed first. Compression is
performed after a successful rotation and produces `.gz` files.

## QLOG v3

Each QLOG record is independently framed:

1. `QLOG` magic and format version.
2. UTC timestamp, severity, thread, role, and caller metadata.
3. Scope, correlation, trace, span, message, and source fields.
4. Numeric event ID, optional event name, and typed property collection.
5. CRC32 over the complete record payload.

The reader accepts versions 1, 2, and 3. Merge and repair output always uses
version 3. Corrupt lengths, excessive property counts, unsupported versions,
truncated records, bad magic, and CRC failures produce diagnostics instead of
unbounded allocations.

```csharp
var entries = BinaryLogReader.Read("logs/app.qlog", stopOnCrcError: false);
var result = BinaryLogReader.ReadWithDiagnostics("logs/app.qlog");

var retries = BinaryLogQuery.WithEventId("logs/app.qlog", 401);
var edgeOne = BinaryLogQuery.WithProperty("logs/app.qlog", "host", "edge-1");
var match = BinaryLogQuery.WithCorrelation("logs/app.qlog", "match-7");
```

Summaries include levels, top messages, correlations, event counts, and property
key counts. Text exports and the timeline viewer include structured data.

## Microsoft.Extensions.Logging

The adapter preserves category, `EventId`, message-template state, exceptions,
and external scopes:

```csharp
using Microsoft.Extensions.Logging;
using QuickLog.Extensions.Logging;

await using var quickLogger = LoggerOptions.ForService("logs").CreateLogger();
using var factory = LoggerFactory.Create(builder =>
    builder.ClearProviders().AddQuickLog(quickLogger));

var log = factory.CreateLogger("Game.Network");
using (log.BeginScope(new Dictionary<string, object?> { ["session"] = "alpha" }))
{
    log.LogWarning(
        new EventId(301, "PacketRetry"),
        "Retrying packet {PacketId} after {DelayMs} ms",
        17,
        250);
}
```

QuickLog remains the lifecycle owner by default. Pass `disposeLogger: true` only
when the Microsoft provider should own and dispose the supplied instance.

## Redaction and duplicate control

```csharp
var options = LoggerOptions.ForEngine("logs")
    .WithRedaction(redaction => redaction.SensitiveKeys.Add("sessionSecret"))
    .WithSpamControl(duplicateThreshold: 8);
```

Built-in presets are `Secrets`, `Network`, `UserData`, and `CrashSafe`.
Redaction runs before asynchronous sinks and masks configured structured keys as
well as matching message fragments. Duplicate control coalesces hot repeated
messages and emits a summary entry after the configured threshold.

## Exception ownership and crash reports

```csharp
LogStateSnapshot.Set("map", "e1m1");
LogStateSnapshot.Set("phase", "loading");

LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup = true,
    MarkTaskExceptionsObserved = true,
    CrashDump = new CrashDumpOptions
    {
        Enabled = true,
        MaxDumpFiles = 10,
        IncludeRecentLogs = true,
        IncludeDispatcherStats = true,
        IncludeStateSnapshot = true,
        Redaction = LogRedactionOptions.CrashSafe()
    }
});
```

Crash JSON includes exception trees, process and runtime facts, fingerprints,
duplicate counts, recent messages, event IDs, structured properties, trace
context, dispatcher health, and an application state snapshot. Windows uses a
native popup when enabled; other platforms use a safe stderr fallback or a
caller-provided `IExceptionPopup`.

Auto-restart has a loop guard and is limited to supported desktop/server hosts.
Use `RestartOptions.IsSupportedOnCurrentPlatform` before presenting restart as
an available recovery action.

## Godot integration

```csharp
LogManager.ConfigureDefault(LoggerOptions.ForGodot("user://logs"));
LogManager.AttachGodotHooks();
```

QuickLog can route Godot output and unhandled exceptions without a compile-time
Godot dependency. Dynamic `Godot.Logger` registration is attempted only when
runtime code generation is available. Check
`GodotLogInterceptor.IsDynamicSinkRegistered`; if it is false, implement a small
`Godot.Logger` subclass that forwards `_LogMessage` to
`GodotBridge.HandleMessage` and `_LogError` to `GodotBridge.HandleError`.

This reflection/emission path is explicitly optional for trimmed and Native AOT
applications. The direct bridge remains the deterministic integration boundary.

## QLOG attributes

`[QLOG]` provides explicit entry, exit, timing, and exception markers without
weaving, proxies, or runtime dependencies:

```csharp
public sealed class AssetCompiler
{
    [QLOG(QLogOption.Entry | QLogOption.Exit | QLogOption.Timing)]
    public void BuildAtlas(IQuickLog logger)
    {
        using var scope = QLogScope.Enter(logger);
        // work
    }
}
```

`QLogDiscovery.Scan(Type)` is trimming-annotated. Assembly-wide discovery needs
the application to preserve the marker metadata it intends to scan.

## QuickLog.Tools

Run the companion CLI directly from the repository:

```powershell
dotnet run --project QuickLog.Tools -- doctor logs --recursive
```

| Command | Purpose |
|---|---|
| `doctor <path> [--recursive]` | Validate QLOG, JSONL, crash, and rotation artifacts. |
| `inspect <file>` | Filter and summarize by level, text, correlation, event, property, and time. |
| `replay <file> --to console|text|jsonl` | Replay or convert QLOG entries. |
| `tail <file> [--follow]` | Read the end of an active text log. |
| `grep <pattern> <path>` | Search messages, event IDs, and structured values. |
| `stats <file>` | Show level, event, property, correlation, and message counts. |
| `summarize <file> --out summary.json` | Write a machine-readable summary. |
| `report --out report.html` | Build a static single-file diagnostics report. |
| `repair <file> --out fixed.qlog` | Salvage valid records from a damaged QLOG. |
| `merge <a> <b> --out merged.qlog` | Merge logs in timestamp order as QLOG v3. |
| `timeline <file>` | Open the interactive console timeline viewer. |
| `redact <input> --out <output>` | Write a masked text-log copy. |
| `bundle --out support.zip` | Build a bounded support bundle with a manifest. |
| `benchmark` | Run dependency-free pipeline microbenchmarks. |
| `launch` / `observe` | Capture owned process sessions or passive metadata samples. |
| `doctor-config <file>` | Validate serialized logger options. |

Examples with v3 filters:

```powershell
dotnet run --project QuickLog.Tools -- inspect logs/app.qlog --event PacketRetry --property attempt=3
dotnet run --project QuickLog.Tools -- grep edge-1 logs --recursive --property host=edge-1
dotnet run --project QuickLog.Tools -- replay logs/app.qlog --to jsonl --out logs/export.jsonl
```

Only inspect or launch applications you own or are authorized to diagnose.

## Native AOT, trimming, and mobile builds

The repository includes executable and compile-time consumer gates:

```powershell
dotnet publish samples/QuickLog.AotSmoke -c Release -r win-x64 --self-contained true
dotnet build samples/QuickLog.MobileSmoke -f net10.0-android -c Release
dotnet build samples/QuickLog.MobileSmoke -f net10.0-ios -c Release
```

Core JSON sinks use source-generated metadata. Reflection-based discovery and
optional Godot integration expose or document their trimming boundaries. CI
publishes and runs the Native AOT smoke on Windows, Linux, and macOS and compiles
the Android/iOS consumer on macOS.

These gates verify package consumption and compilation. They do not replace
simulator, emulator, physical-device, or live Godot project integration tests.

## Building and validating a release

```powershell
dotnet restore QuickLog.sln
dotnet build QuickLog.sln -c Release --no-restore
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --no-build
dotnet pack QuickLog/QuickLog.csproj -c Release --no-build -o artifacts/nupkgs
dotnet pack QuickLog.Extensions.Logging/QuickLog.Extensions.Logging.csproj -c Release --no-build -o artifacts/nupkgs
./scripts/Test-PackageConsumer.ps1 -PackageDirectory ./artifacts/nupkgs
```

The package validation script checks both target frameworks, XML docs, README,
changelog, license, portable symbol packages, core dependency policy, adapter
dependencies, restore from the produced packages, a clean consumer build, and a
structured runtime round-trip.

## Compatibility and migration

- Existing `IQuickLog.Log(LogType, string|Exception, ...)` calls remain valid.
- `IQuickLog` now also implements `IAsyncDisposable` and exposes default v3 APIs.
- New binary writes use QLOG v3; the reader continues to accept QLOG v1 and v2.
- Existing text and JSONL consumers can ignore the added event/property fields.
- `LogEntry` and `LogEventArgs` add event and property data at the end of their
  public construction surface.
- The core remains `net8.0;net10.0` and dependency-free.
- Microsoft host integration moved into an explicitly optional package boundary.

## Project status

| Area | v3 status |
|---|---|
| Core logging and sinks | Release-gated on .NET 8 and .NET 10 |
| Structured events / QLOG v3 | CRC round-trip and v1/v2 compatibility tested |
| Async lifecycle | Flush, shutdown, timeout, cancellation, and disposal tested |
| Rotation and retention | Size, count, age, byte budget, and compression tested |
| Diagnostics tools | Inspect, replay, query, summary, repair, merge, and report tested |
| Crash reporting | Structured tails, redaction, fingerprints, and state tested |
| Native AOT / trimming | Warning-free build plus native process smoke |
| Windows / Linux / macOS | CI build, test, and native smoke |
| Android / iOS | Dedicated target-framework consumer compile smoke |
| Microsoft logging adapter | Separate package and clean consumer smoke |
| Godot | Direct bridge stable; dynamic registration remains best-effort |

## License

QuickLog is licensed under the [MIT License](LICENSE).

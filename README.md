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

---

## Features

### Logging Core
- `IQuickLog` clean interface
- Strongly typed `LogType`
- Caller info via compiler services
- Exception demystification
- CRC32 integrity checks
- Scopes (`LogScope`)
- Thread roles (`ThreadContext`)

### Sinks
- Console
- File (text)
- Trace
- Event-only
- Memory
- Binary (CRC protected)

### Async Pipeline
- Dedicated background dispatcher
- Bounded queue
- Configurable drop policies
- Severity-aware dropping
- Thread-role-aware dropping
- Async-only mode (no sync IO)
- Deterministic flush & shutdown

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

logger.Log(LogType.Info, "Hello QuickLog");
logger.Log(LogType.Warn, "Something might be wrong");
logger.Log(LogType.Error, new Exception("Boom"));
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
- No blocking IO
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

All logs from that thread are tagged accordingly.

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

## Binary Logs & Analysis

### Export to text

```csharp
BinaryLogExporter.ExportToText(
    "quicklog.bin",
    "recovered.log");
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

Always shut down explicitly:

```csharp
LogManager.Shutdown();
```

This guarantees:
- Async queues are flushed
- No logs are lost
- Clean termination

---

## What QuickLog Is NOT

- Not a DI-based framework
- Not a message-template logger
- Not reflection-heavy
- Not opinionated about formatting
- Not hiding behavior behind magic

QuickLog is **infrastructure**, not ceremony.

---

## License

MIT

---

## Status

QuickLog is **production-ready**.
The architecture is stable, the async pipeline is deterministic,
and the tooling enables serious post-mortem analysis.
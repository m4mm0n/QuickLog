# QuickLog Zero-Dependency Diagnostics Tools Design

Date: 2026-05-13
Branch: v2.2-Exp

## Goal

Add source-less diagnostics tooling around QuickLog while keeping the entire repository zero external dependencies. The core `QuickLog` library remains a dependency-free logging library, and new tooling lives in separate projects so diagnostic experiments do not destabilize core logging.

The tools must help an authorized user inspect, launch, observe, package, and replay diagnostics for .NET applications, including applications where the user does not have source code.

## Non-Goals

- Do not add external NuGet packages anywhere in the repository.
- Do not use `Microsoft.Diagnostics.NETCore.Client`, `TraceEvent`, Serilog, OpenTelemetry, or native package dependencies.
- Do not inject into arbitrary third-party processes without an explicit user-selected process id or executable path.
- Do not make the core `QuickLog` package depend on tools or profiler code.
- Do not implement a full production CLR profiler in this v2.2 tooling pass.

## Constraints

Microsoft's .NET diagnostics stack exposes diagnostic ports and EventPipe tracing through IPC. The recommended custom-tool path is `Microsoft.Diagnostics.NETCore.Client`, but this project must remain zero external dependencies. Therefore v2.2 tooling will implement safe, dependency-free capabilities first, and will treat direct diagnostic IPC/EventPipe and native profiling as experimental tracks with explicit boundaries.

All new projects must use the .NET SDK and built-in framework libraries only.

## Project Layout

Add one managed console tool project:

- `QuickLog.Tools`
  - References `QuickLog`.
  - Targets `net10.0` initially to match the sample/test host.
  - Contains command parsing, command handlers, process observation, support bundling, benchmarking, and log inspection.
  - Has no `PackageReference` items.

Add tests to the existing `QuickLog.Tests` project where possible:

- Pure command parsing tests.
- Binary log doctor/inspect/replay tests using temporary `.qlog` files.
- Bundle creation tests using temporary directories.
- Benchmark smoke tests with reduced iterations.
- Launch tests against a small generated command only if reliable in CI/local runs.

## Commands

### `quicklog doctor`

Validate logs and diagnostic artifacts.

Inputs:

- `quicklog doctor <path>`
- `quicklog doctor <directory> --recursive`

Behavior:

- For `.qlog`, read with CRC checking and report entries, time range, levels, and first corruption point when detectable.
- For `.jsonl`, count lines and malformed JSON-looking lines with a lightweight structural check.
- For crash dump `.json`, verify required top-level fields by string/JSON document parsing using `System.Text.Json`.
- For rotation sets, detect sibling files such as `.1`, `.2`, and report sizes.

Output:

- Human-readable console summary.
- Non-zero exit code when artifacts are missing or invalid.

### `quicklog inspect`

Summarize binary logs.

Inputs:

- `quicklog inspect <path>`
- Optional filters: `--level`, `--contains`, `--correlation`, `--from`, `--to`, `--limit`.

Behavior:

- Uses `BinaryLogReader` and `BinaryLogQuery`.
- Shows count, first/last timestamp, level counts, top scopes, top correlations, and sample entries.

### `quicklog replay`

Replay `.qlog` logs into another format.

Inputs:

- `quicklog replay <path> --to console`
- `quicklog replay <path> --to text --out <file>`
- `quicklog replay <path> --to jsonl --out <file>`

Behavior:

- Console output uses simple level labels.
- Text output uses `BinaryLogExporter`.
- JSON Lines output uses `System.Text.Json` directly in the tool.

### `quicklog benchmark`

Measure QuickLog throughput without BenchmarkDotNet or other packages.

Inputs:

- `quicklog benchmark`
- Optional: `--iterations`, `--mode sync|async|binary|json|redaction|spam`.

Behavior:

- Uses `Stopwatch`, `GC.GetAllocatedBytesForCurrentThread`, and temporary files.
- Reports elapsed time, logs/sec, approximate allocated bytes, output bytes, drops, and dispatcher stats.
- Uses conservative default iteration counts suitable for local runs.

### `quicklog bundle`

Create a support bundle.

Inputs:

- `quicklog bundle --out <zip> --logs <dir> --crashes <dir>`
- Optional: `--include-env`, `--include-exports`, `--max-file-bytes`, `--redact`.

Behavior:

- Uses `System.IO.Compression.ZipArchive`.
- Includes selected log files, crash dumps, generated text exports for `.qlog`, a manifest JSON, and a system summary.
- Applies QuickLog redaction to manifest text and generated exports when requested.

### `quicklog launch`

Start a selected .NET application with QuickLog observation around it.

Inputs:

- `quicklog launch -- <app> [args...]`
- Optional: `--out <dir>`, `--name <session-name>`, `--diagnostic-env`, `--wait-for-exit`.

Behavior:

- Starts the process with `System.Diagnostics.Process`.
- Captures stdout and stderr asynchronously.
- Logs process start, exit code, duration, stdout, stderr, and observer metadata to `.qlog` and `.jsonl`.
- When `--diagnostic-env` is set, adds documented .NET diagnostic environment variables without requiring external packages.
- Does not claim deep EventPipe capture unless that experimental mode is implemented and verified.

### `quicklog observe`

Observe an already-running process without source.

Inputs:

- `quicklog observe --pid <pid> --duration <seconds> --out <dir>`

Behavior:

- Validates the process exists and records metadata: process name, id, start time, working set, thread count, module names where accessible, and periodic CPU/memory samples.
- Checks whether a default .NET diagnostic IPC endpoint appears to exist on the current platform.
- Logs observations to QuickLog artifacts.
- Does not inject code into the process.
- Does not require administrator privileges beyond what the OS already requires for process metadata.

### `quicklog profiler`

Expose an experimental profiler command group without adding a production native profiler.

Inputs:

- `quicklog profiler explain`
- `quicklog profiler env --clsid <guid> --path <profiler-path>`

Behavior:

- Documents the CLR profiler environment variables and risk model.
- Can print or write launch environment blocks for an external native profiler implementation.
- Does not ship an unmanaged profiler DLL in this implementation pass.

## Data Flow

Tool commands write through normal QuickLog APIs:

1. Parse command line into a typed command model.
2. Create a session output directory.
3. Configure a `QuickLogger` with async-only JSON Lines and binary logging.
4. Run command-specific work.
5. Flush and shut down.
6. Return exit code based on command result.

Existing binary readers/exporters remain the source of truth for `.qlog` interpretation.

## Error Handling

- Commands return non-zero exit codes for invalid input, missing paths, unreadable processes, malformed logs, or failed bundle creation.
- Errors are written to stderr and to the session log when a session exists.
- Tool errors must not throw raw stack traces for expected user mistakes.
- Unexpected exceptions are caught at the CLI boundary, logged, and reported with a concise message.

## Testing

Required tests:

- Command parser accepts each command and rejects invalid combinations.
- `doctor` detects valid and corrupted `.qlog` files.
- `inspect` summarizes level counts and correlation filters.
- `replay` writes text and JSON Lines outputs.
- `bundle` creates a zip with manifest and generated exports.
- `benchmark` smoke test completes with small iteration count.
- `launch` can run a local deterministic command and capture stdout/stderr when available on the host OS.
- `observe` handles the current test process or a short-lived child process without crashing.

Verification commands:

- `dotnet build QuickLog.sln`
- `dotnet test QuickLog.sln`
- `dotnet pack QuickLog/QuickLog.csproj -c Release`

## Documentation

Update `README.md` with a `QuickLog.Tools` section covering:

- Zero-dependency guarantee.
- Source-less diagnostics limits.
- Command examples.
- Safety/authorization note.
- Difference between observe, launch, and profiler.

## Acceptance Criteria

- The solution builds with `QuickLog.Tools` included.
- No new external package references exist outside the existing test packages already present.
- `QuickLog` core remains decoupled from tools.
- All tests pass.
- CLI smoke commands work locally.
- The profiler command is clearly marked experimental and does not imply deep tracing has shipped.

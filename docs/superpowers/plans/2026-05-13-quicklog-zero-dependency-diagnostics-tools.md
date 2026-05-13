# QuickLog Zero-Dependency Diagnostics Tools Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a zero-external-dependency `QuickLog.Tools` CLI with doctor, inspect, replay, benchmark, bundle, launch, observe, and profiler helper commands.

**Architecture:** Keep `QuickLog` core untouched except for using its existing public utility surface. Add a separate managed console project that references `QuickLog`, with focused command classes and pure command parsing that can be tested from `QuickLog.Tests`. The profiler command is an experimental environment/helper surface only, not a native profiler DLL.

**Tech Stack:** C#/.NET SDK only, `System.Diagnostics`, `System.Text.Json`, `System.IO.Compression`, existing QuickLog binary reader/exporter APIs, xUnit tests already present in `QuickLog.Tests`.

---

### Task 1: CLI Project And Command Parser

**Files:**
- Create: `QuickLog.Tools/QuickLog.Tools.csproj`
- Create: `QuickLog.Tools/Program.cs`
- Create: `QuickLog.Tools/ToolApplication.cs`
- Create: `QuickLog.Tools/ToolCommand.cs`
- Create: `QuickLog.Tools/ToolConsole.cs`
- Modify: `QuickLog.sln`
- Modify: `QuickLog.Tests/QuickLog.Tests.csproj`
- Test: `QuickLog.Tests/ToolCommandParserTests.cs`

- [ ] **Step 1: Write parser tests**

Create tests proving `doctor`, `inspect`, `replay`, `benchmark`, `bundle`, `launch`, `observe`, and `profiler` parse into typed commands and invalid input returns a parse error.

- [ ] **Step 2: Run parser tests to verify RED**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolCommandParserTests --no-restore`
Expected: compilation fails because `QuickLog.Tools` does not exist.

- [ ] **Step 3: Add `QuickLog.Tools` project and parser**

Implement a small zero-dependency parser that recognizes command names, positional values, options, and `launch -- <app> [args...]`.

- [ ] **Step 4: Add project references**

Add `QuickLog.Tools` to `QuickLog.sln` and reference it from `QuickLog.Tests`.

- [ ] **Step 5: Run parser tests to verify GREEN**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolCommandParserTests --no-restore`
Expected: parser tests pass.

- [ ] **Step 6: Commit**

Commit message: `feat: add QuickLog tools command parser`

### Task 2: Doctor, Inspect, And Replay

**Files:**
- Create: `QuickLog.Tools/Commands/DoctorCommand.cs`
- Create: `QuickLog.Tools/Commands/InspectCommand.cs`
- Create: `QuickLog.Tools/Commands/ReplayCommand.cs`
- Create: `QuickLog.Tools/Commands/CommandResult.cs`
- Modify: `QuickLog.Tools/ToolApplication.cs`
- Test: `QuickLog.Tests/ToolLogCommandTests.cs`

- [ ] **Step 1: Write failing log command tests**

Tests create temporary `.qlog` files using `BinaryLogSink`, then assert:
- `doctor` reports valid entries.
- `doctor` returns failure for corrupted CRC.
- `inspect` reports level counts and correlation matches.
- `replay --to text` writes context-aware text.
- `replay --to jsonl` writes JSON Lines.

- [ ] **Step 2: Run log command tests to verify RED**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolLogCommandTests --no-restore`
Expected: compilation fails because command classes do not exist.

- [ ] **Step 3: Implement command classes**

Use `BinaryLogReader`, `BinaryLogQuery`, `BinaryLogExporter`, `System.Text.Json`, and plain console output.

- [ ] **Step 4: Run log command tests to verify GREEN**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolLogCommandTests --no-restore`
Expected: tests pass.

- [ ] **Step 5: Commit**

Commit message: `feat: add log doctor inspect and replay commands`

### Task 3: Bundle And Benchmark

**Files:**
- Create: `QuickLog.Tools/Commands/BundleCommand.cs`
- Create: `QuickLog.Tools/Commands/BenchmarkCommand.cs`
- Modify: `QuickLog.Tools/ToolApplication.cs`
- Test: `QuickLog.Tests/ToolBundleBenchmarkTests.cs`

- [ ] **Step 1: Write failing bundle/benchmark tests**

Tests assert:
- `bundle` creates a zip containing `manifest.json`.
- `bundle --include-exports` adds text exports for `.qlog` files.
- `benchmark --iterations 10 --mode binary` returns success and reports logs/sec.

- [ ] **Step 2: Run tests to verify RED**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolBundleBenchmarkTests --no-restore`
Expected: compilation fails because commands do not exist.

- [ ] **Step 3: Implement bundle and benchmark**

Use `ZipArchive`, `Stopwatch`, `GC.GetAllocatedBytesForCurrentThread`, temporary files, and `QuickLogger` configured with local output paths.

- [ ] **Step 4: Run tests to verify GREEN**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolBundleBenchmarkTests --no-restore`
Expected: tests pass.

- [ ] **Step 5: Commit**

Commit message: `feat: add support bundle and benchmark tools`

### Task 4: Launch, Observe, And Profiler Helpers

**Files:**
- Create: `QuickLog.Tools/Commands/LaunchCommand.cs`
- Create: `QuickLog.Tools/Commands/ObserveCommand.cs`
- Create: `QuickLog.Tools/Commands/ProfilerCommand.cs`
- Create: `QuickLog.Tools/Diagnostics/DiagnosticPortProbe.cs`
- Modify: `QuickLog.Tools/ToolApplication.cs`
- Test: `QuickLog.Tests/ToolProcessCommandTests.cs`

- [ ] **Step 1: Write failing process command tests**

Tests assert:
- `observe --pid <current-pid> --duration 0` logs current process metadata.
- `launch --out <dir> -- dotnet --info` captures stdout and exit code when `dotnet` is available.
- `profiler explain` marks profiler support experimental.
- `profiler env --clsid <guid> --path <path>` prints CLR profiler environment variables.

- [ ] **Step 2: Run tests to verify RED**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolProcessCommandTests --no-restore`
Expected: compilation fails because commands do not exist.

- [ ] **Step 3: Implement process commands**

Use `Process`, periodic metadata sampling, diagnostic port existence checks, stdout/stderr capture, and no process injection.

- [ ] **Step 4: Run tests to verify GREEN**

Run: `dotnet test QuickLog.Tests/QuickLog.Tests.csproj --filter ToolProcessCommandTests --no-restore`
Expected: tests pass.

- [ ] **Step 5: Commit**

Commit message: `feat: add process launch observe and profiler helpers`

### Task 5: README And Full Verification

**Files:**
- Modify: `README.md`
- Test: full solution

- [ ] **Step 1: Update README**

Document `QuickLog.Tools`, zero-dependency constraints, command examples, source-less diagnostics limits, and authorization/safety note.

- [ ] **Step 2: Verify no new external package references**

Run: `rg -n "<PackageReference" QuickLog.Tools QuickLog QuickLog.Sample`
Expected: no output.

- [ ] **Step 3: Build solution**

Run: `dotnet build QuickLog.sln`
Expected: build succeeds.

- [ ] **Step 4: Test solution**

Run: `dotnet test QuickLog.sln`
Expected: all tests pass.

- [ ] **Step 5: Pack library**

Run: `dotnet pack QuickLog/QuickLog.csproj -c Release -o artifacts/packages`
Expected: package builds.

- [ ] **Step 6: Commit**

Commit message: `docs: document QuickLog tools`

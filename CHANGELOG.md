# Changelog

All notable changes to QuickLog are documented here.

This changelog follows Semantic Versioning and Keep a Changelog style sections.

## Version Provenance

- `2.2.0` is the current local `v2.2-Exp` development line. It is reflected in package metadata, but it is not tagged or pushed.
- `2.1.0` is tagged as `v2.1.0`.
- `2.0.0` is tagged as `v2.0.0`.
- Versions before `2.0.0` are retrospective changelog labels. The repository history before `2.0.0` did not record package version metadata or release tags, so those entries describe historical milestones rather than published tags.

## [2.2.0] - 2026-05-13

### Added

- Added public async binary logging configuration through `LoggerOptions.WithBinaryLog`, `QuickLogger.EnableAsyncBinaryLogging`, and `QuickLogger.BinaryLogPath`.
- Added async-safe logging context with `LogContext`, public `LogScope`, correlation ids, and `Activity` trace/span capture.
- Added binary log format v2 context fields for correlation id, trace id, and span id while preserving v1 reader compatibility.
- Added context-aware binary log query helpers:
  - `BinaryLogQuery.WithCorrelation`.
  - `BinaryLogQuery.ContainingText`.
- Added context-aware text exports for binary logs, including scope, correlation id, trace id, and span id.
- Added dependency-free size-based log rotation through `LogRotationOptions` and `RotatingFileWriter`.
- Added rotation support for text file, JSON Lines, and binary sinks.
- Added async dispatcher health statistics through `LogDispatcherStats` and `QuickLogger.GetStats`.
- Added async dispatcher queue capacity configuration.
- Added sink-failure accounting and last sink error tracking in the async dispatcher.
- Added crash dump enrichment with recent log tails and dispatcher stats.
- Added crash dump redaction configuration.
- Added built-in sensitive value redaction with `LogRedactionOptions` and `LogRedactor`.
- Added fluent redaction setup through `LoggerOptions.WithRedaction`.
- Added duplicate message coalescing through `LogSpamControlOptions` and `LogSpamController`.
- Added fluent spam-control setup through `LoggerOptions.WithSpamControl`.
- Added `QuickLog.Tools`, a zero-external-dependency companion CLI.
- Added `quicklog doctor` for validating `.qlog`, `.jsonl`, crash JSON, and rotation artifacts.
- Added `quicklog inspect` for binary log summaries, filters, level counts, scopes, correlations, and sample entries.
- Added `quicklog replay` for replaying `.qlog` files to console, text, or JSON Lines.
- Added `quicklog benchmark` for simple built-in throughput checks without BenchmarkDotNet.
- Added `quicklog bundle` for support bundle ZIP creation with manifests, crash dumps, logs, environment data, and optional binary exports.
- Added `quicklog launch` for starting selected applications and capturing stdout, stderr, process lifetime, and QuickLog session artifacts.
- Added `quicklog observe` for passive, source-less process metadata sampling.
- Added `quicklog profiler explain` and `quicklog profiler env` as experimental CLR profiler environment helpers.
- Added design and implementation planning docs for the zero-dependency diagnostics tooling.
- Added tests for parser behavior, binary utility behavior, rotation, dispatcher stats, crash tails, redaction, spam control, tool commands, process launch/observe, profiler helpers, and packaging-sensitive flows.

### Changed

- Changed QuickLog package metadata from `2.1.0` to `2.2.0`.
- Changed the core library to multi-target `net8.0` and `net10.0`.
- Changed `QuickLogger` async flow to preserve scope, correlation id, trace id, and span id.
- Changed JSON Lines output to include scope, correlation id, trace id, and span id.
- Changed crash dumps to include recent logs and dispatcher health when available.
- Changed async dispatcher behavior to survive sink failures instead of letting a sink exception stop dispatch.
- Changed samples and README to demonstrate v2.2 engine-mode configuration.
- Changed NuGet package README metadata to include an install command, target framework information, zero-dependency note, and quick-start examples.
- Changed local repository hygiene to ignore `.worktrees/`.

### Fixed

- Fixed binary utility coverage gaps by adding tests for context-preserving query and export behavior.
- Fixed async dispatcher flush/shutdown behavior around spam-control summary flushing.
- Fixed support for binary reader/exporter utilities after context fields were added.

### Notes

- `QuickLog.Tools` intentionally does not use `Microsoft.Diagnostics.NETCore.Client`, `TraceEvent`, OpenTelemetry, Serilog, or any external NuGet package.
- `quicklog observe` is passive and does not inject code.
- `quicklog profiler` does not ship a native profiler DLL.

## [2.1.0] - 2026-05-12

### Added

- Added `TraceSink`.
- Added `JsonLinesSink`.
- Added `LoggerOptions` fluent builder configuration.
- Added `LogManager.ConfigureDefault(LoggerOptions)`.
- Added JSON Lines sink tests.
- Added trace sink tests.
- Added logger options tests.
- Added NuGet version and download badges to README.
- Added package metadata for NuGet distribution under `ZLS.QuickLog`.
- Added README inclusion in the NuGet package.

### Changed

- Changed `QuickLog.Sample` to target `net10.0`.
- Changed package id to `ZLS.QuickLog`.
- Changed README badges to point at the NuGet package.
- Changed release workflow behavior for NuGet publishing.
- Changed release workflow globbing to work correctly on Windows runners.

### Fixed

- Fixed a broken NuGet push secret guard in the release workflow.
- Fixed NuGet package globbing in the release workflow.
- Fixed NuGet.org listing support by including `README.md` in the package.

## [2.0.0] - 2026-05-11

### Added

- Added `ExceptionHookManager` for full unhandled exception ownership.
- Added handling for `AppDomain.UnhandledException`.
- Added handling for `TaskScheduler.UnobservedTaskException`.
- Added native popup support for fatal exception reporting.
- Added `ExceptionCaught` event for custom exception side effects.
- Added exception filtering support.
- Added crash dump writer for structured JSON crash reports.
- Added auto-restart support for fatal exceptions.
- Added restart loop guard.
- Added recovery delegate support for non-fatal task exceptions.
- Added `QuickLog.Sample` console project demonstrating exception ownership.
- Added hijack branding to exception log prefixes and popup headers.
- Added full Godot integration:
  - `GodotLogInterceptor`.
  - `GodotBridge`.
  - `GodotAlertPopup`.
- Added one-line Godot setup through `LogManager.AttachGodotHooks`.
- Added GitHub Actions release workflow for DLL build, NuGet packing, binary zips, and GitHub Release uploads.
- Added XML documentation generation to release output.
- Added `QuickLog.Tests` with 42 xUnit tests covering CRC32, binary log roundtrip, `MemoryQuickLogger`, async dispatcher, `LogScope`, `CrashDumpWriter`, and `ExceptionHookManager`.
- Added `InternalsVisibleTo(QuickLog.Tests)` so internal sinks and helpers can be tested directly.
- Added CI workflow for master, Experimental, and pull requests.

### Changed

- Changed target framework to `net10.0`.
- Changed package metadata to `2.0.0`.
- Changed release workflow so tests must pass before publishing.
- Changed NuGet publishing to use `--skip-duplicate` for safer re-runs.

### Fixed

- Fixed a `BinaryLogReader` CRC-position bug where the stream position was not advanced past the CRC field after payload re-read, which truncated multi-entry binary logs to one entry.

## [1.9.0] - 2026-01-18

### Added

- Added internal async overloads and a more complete async logging pipeline.
- Added `AsyncDropPolicy`.
- Added `AsyncLogDispatcher`.
- Added `ILogSink`.
- Added `LogEntry`.
- Added internal `LogScope`.
- Added `ThreadContext`.
- Added `ThreadRole`.
- Added `MemoryQuickLogger`.
- Added sink abstractions:
  - `BinaryLogSink`.
  - `ConsoleSink`.
  - `FileSink`.
  - `MemorySink`.
- Added binary log tooling:
  - `BinaryLogReader`.
  - `BinaryLogExporter`.
  - `BinaryLogQuery`.
  - `BinaryLogTimelineViewer`.
- Added file header update script.
- Added `.headerconfig.json`.

### Changed

- Changed `QuickLogger` and `LogManager` to support the expanded async and sink architecture.
- Changed `LogEventArgs` to carry richer event data.
- Changed README to document the engine-grade logging direction.
- Changed CRC32 and utility code as part of the expanded logging infrastructure.

### Fixed

- Fixed several smaller async and utility issues as part of the engine-grade refactor.

## [1.8.0] - 2025-09-18

### Removed

- Removed the `Ben.Demystifier` dependency.

### Changed

- Moved extension helpers into the `Utilities` folder.
- Applied minor code fixes across the project.

## [1.7.0] - 2025-09-17

### Added

- Added `GodotFileLogger`.
- Added `GodotUserPathResolver`.

### Changed

- Updated `LogManager` for Godot-aware file logging.
- Updated README documentation for Godot-related logging.

## [1.6.0] - 2025-03-13

### Fixed

- Fixed logger disposal issues across the project.
- Fixed `FileLogger` static-state behavior that could cause multiple logger instances to write to the same log file.

## [1.5.0] - 2025-03-12

### Added

- Added friendlier `QuickLogger` construction patterns.
- Added `ReplaceInvalidPathChars` for sanitizing invalid path characters.

### Fixed

- Fixed log files being created incorrectly.
- Fixed missing directory creation for file-backed loggers.
- Fixed `CheckFileWritePermissions` behavior that could leave the log file missing.
- Fixed upcoming file-write issues in `FileLogger`.

## [1.4.0] - 2024-11-27

### Added

- Added `LogManager.GetLogger(Type type)`.
- Added automatic file/no-file selection support for `LogManager.GetLogger` methods.
- Added `LogManager.ConfigureDefault` overload for default file output.
- Added `ICloneable` support to `QuickLogger`.
- Added `QuickLogger.CloneDeep` for deep cloning with optional log file changes.
- Added an additional security-related extension helper.

## [1.3.0] - 2024-11-20

### Fixed

- Fixed files not being saved to disk because of erroneous filenames and file paths.

## [1.2.0] - 2024-10-21

### Added

- Added a custom CRC32 hasher for `LogEventArgs`.

### Changed

- Replaced standard `GetHashCode()` behavior in `LogEventArgs` with the custom CRC32-based hash.

## [1.1.0] - 2024-10-10

### Added

- Added timestamps to each log entry.

### Fixed

- Fixed logger output not being written to the specified log path.
- Fixed named loggers not getting their own log files correctly.

## [1.0.0] - 2024-10-09

### Added

- Added the first public README.
- Added the public repository baseline.
- Added the first feature-complete logger baseline.

### Changed

- Reworked logger implementations around `QuickLogger`, `ConsoleQuickLogger`, `EventOnlyLogger`, `FileLogger`, and `TraceLogger`.
- Replaced the earlier `FileQuickLogger` direction with the newer file logging implementation.
- Expanded `LogManager` and `LogType` for the public baseline.

### Fixed

- Fixed early README/repository setup mistakes after the project was made public.

## [0.1.0] - 2024-10-06

### Added

- Added the initial QuickLog project.
- Added the original .NET 8 library scaffold.
- Added the original external `Ben.Demystifier` package reference, which was later removed in `1.8.0`.

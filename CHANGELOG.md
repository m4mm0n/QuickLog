# Changelog

All notable changes to QuickLog are documented here.

This changelog follows Semantic Versioning and Keep a Changelog style sections.

## Version Provenance

- `3.0.0` is the current major release line.
- `2.4.0` is tagged as `v2.4.0`.
- `2.3.1` is tagged as `v2.3.1`.
- `2.3.0` is tagged as `v2.3.0`.
- `2.2.0` is tagged as `v2.2.0`.
- `2.1.0` is tagged as `v2.1.0`.
- `2.0.0` is tagged as `v2.0.0`.
- Versions before `2.0.0` are retrospective changelog labels. The repository history before `2.0.0` did not record package version metadata or release tags, so those entries describe historical milestones rather than published tags.

## [Unreleased]

No unreleased changes.

## [3.0.0] - 2026-08-16

### Added

- Added stable `LogEventId` values and immutable structured properties across log entries, events, scopes, sinks, crash reports, queries, summaries, exports, replay, and reports.
- Added `LogProperty` and `LogProperties` helpers for typed value capture, immutable snapshots, deterministic merges, and invariant text formatting.
- Added async-flowing structured property scopes through `LogScope.Begin(...)` and `LogContext.BeginProperties(...)`.
- Added QLOG format version 3 with typed property encoding, event identity, record-level CRC32 protection, bounded string lengths, and bounded property counts.
- Added `BinaryLogQuery.WithEventId(...)` and `BinaryLogQuery.WithProperty(...)`.
- Added event and property counts to `BinaryLogSummary` and all summary/report surfaces.
- Added `IQuickLog.IsEnabled(...)` and a custom interpolated-string handler that avoids evaluating disabled interpolated messages.
- Added structured message and structured exception overloads while preserving the original `IQuickLog.Log(...)` calls.
- Added `IQuickLog.FlushAsync(...)`, `QuickLogger.ShutdownAsync(...)`, timeout and cancellation support, and asynchronous disposal.
- Added retention by rotated-file age and total byte budget.
- Added optional GZip compression for newly rotated text, JSON Lines, and QLOG files.
- Added unique high-resolution rotation names to prevent same-timestamp overwrite collisions.
- Added `LoggerOptions.CreateLogger()` for independently owned, fully configured instances.
- Added portable macOS, Android, and iOS profiles with deterministic writable-path resolution.
- Added explicit current-platform auto-restart support reporting and a mobile restart guard.
- Added runtime code-generation guards for optional dynamic Godot logger registration.
- Added source-generated JSON metadata for trimming-safe JSON Lines and crash report serialization.
- Added trim annotations and documented reflection boundaries for discovery, stack formatting, and optional Godot integration.
- Added `ZLS.QuickLog.Extensions.Logging`, a separate Microsoft logging provider that forwards categories, event identifiers, structured state, exceptions, and external scopes.
- Added executable Native AOT consumer coverage and Android/iOS target-framework consumer projects.
- Added deterministic portable symbol packages for both NuGet packages.
- Added clean-package layout, dependency, restore, build, and runtime consumer validation.
- Added cross-platform Native AOT release gates, mobile compile gates, curated release notes, release checksums, and both NuGet packages to the release workflow.
- Added a repository-level MIT license file.

### Changed

- Changed package, assembly, file, and release metadata to `3.0.0`.
- Changed new binary writes, merge output, and repair output to QLOG v3 while retaining QLOG v1 and v2 reads.
- Changed JSON Lines output to include `eventId`, `eventName`, and typed `properties` fields.
- Changed console, file, trace, text export, timeline, and replay output to include event and property data when present.
- Changed structured redaction to mask sensitive property names before entries reach asynchronous sinks or crash artifacts.
- Changed crash dump recent-log entries to include event identity and redacted structured properties.
- Changed async dispatcher completion accounting to track accepted-but-not-finished entries directly.
- Changed drop-policy accounting to include entries removed while trimming a full queue.
- Changed stack fingerprints to use available exception stack text without requiring reflected method metadata.
- Changed method entry/exit tracing to use compiler-provided caller names instead of runtime stack reflection.
- Changed logger option construction so global and independently owned loggers share one configuration path.
- Changed package discovery tags and platform documentation for Windows, Linux, macOS, Android, and iOS.
- Changed CI to test Windows, Linux, and macOS and to validate packages, Native AOT executables, Android consumers, and iOS consumers.
- Changed release artifacts to include core and adapter packages, portable symbols, combined binaries, curated notes, and SHA-256 hashes.
- Kept Android and iOS support on the portable `net8.0` and `net10.0` core assets; mobile target frameworks validate package consumption without adding mobile runtime dependencies to the core.

### Fixed

- Fixed a flush race where the consumer could remove an entry from the queue immediately before marking it in flight, allowing shutdown to return before sink accounting completed.
- Fixed flush completion after `DropOldest` or queue trimming removed an already accepted entry.
- Fixed structured exception events in async-only mode so event identifiers and properties are retained.
- Fixed async-only `LogEvent` callbacks so structured metadata, scope, correlation, trace, and span data are preserved.
- Fixed filters so they can inspect the fully merged structured event instead of an incomplete pre-dispatch view.
- Fixed rotated-file naming so rapid consecutive rotations cannot overwrite an earlier rotation.
- Fixed QLOG readers so corrupt string lengths and property counts fail with format diagnostics instead of requesting unbounded buffers.

### Compatibility

- Existing message and exception logging calls remain source-compatible.
- Existing QLOG v1 and v2 files remain readable.
- Existing JSONL consumers can ignore the additional v3 fields.
- The core package continues to target `net8.0` and `net10.0` with no runtime package dependencies.
- Optional Microsoft host integration is isolated in `ZLS.QuickLog.Extensions.Logging`.

### Verification

- Release builds require zero compiler and trim-analyzer warnings.
- The repository validates QLOG v1/v2/v3 compatibility, structured redaction, async shutdown, retention/compression, tooling filters, crash propagation, and adapter behavior.
- Native AOT executables are published and run on Windows, Linux, and macOS.
- Android and iOS consumers are compiled from `net10.0-android` and `net10.0-ios`; simulator, emulator, physical-device, and live Godot project checks remain integration responsibilities.

## [2.4.0] - 2026-05-23

### Added

- Added explicit Linux platform facts and capabilities through `QuickLog.Platform`.
- Added XDG-aware Linux log directory resolution through `QuickLogPathResolver`.
- Added `LoggerOptions.ForLinux(...)` for dependency-free durable JSON Lines and QLOG output under `$XDG_STATE_HOME/<app>/logs` or `~/.local/state/<app>/logs`.
- Added `samples/QuickLog.LinuxSmoke` and Ubuntu CI/release smoke coverage.
- Added regression coverage for Linux profiles, Linux smoke project wiring, Ubuntu workflows, active log reads, and sample reruns.

### Changed

- Changed package metadata from `2.3.1` to `2.4.0`.
- Changed tool text and binary readers to open active log files with read/write sharing so diagnostics can inspect logs while applications are still running.
- Changed the sample app to replace `quicklog.jsonl` as well as QLOG and text export outputs on each run.
- Changed CLI parsing so unknown options fail with a parse error instead of being silently ignored.
- Changed non-Windows exception popup fallback to suppress stderr failures during exception handling.

### Notes

- Linux support does not add runtime dependencies and keeps the package targeting `net8.0` and `net10.0`.
- Native modal popup support remains Windows-only; Linux exception reporting uses a safe stderr fallback or caller-provided `IExceptionPopup`.

## [2.3.1] - 2026-05-19

### Fixed

- Fixed Godot runtime type resolution so optional Godot types are found from already loaded assemblies, not only hard-coded assembly-qualified names.
- Fixed dynamic `Godot.Logger` registration so a later attach can retry registration if the first attach ran before Godot types were available or disabled dynamic registration.
- Fixed Godot-owned exception hook lifecycle so reattaching with `HijackExceptions = false` detaches hooks previously attached by `GodotLogInterceptor`.
- Fixed dynamic logger IL emission to tolerate Godot `ErrorType` enum parameters and preserve base method visibility.
- Fixed `GodotFileLogger`, `ConfigureDefaultGodotLogger`, and `GetGodotLogger` to use safe default filenames when sanitization removes every character.

### Added

- Added focused Godot regression tests for loaded-assembly path resolution, dynamic logger routing, exception hook ownership, and safe filename fallback.

### Changed

- Changed package metadata from `2.3.0` to `2.3.1`.
- Changed NuGet release notes to describe the Godot hardening release.

## [2.3.0] - 2026-05-17

### Added

- Added lean `LoggerOptions` profiles for engine, service, tool, and Godot-style usage.
- Added `LoggerOptions.Validate()` with structured validation issues for lossy or contradictory settings.
- Added startup banners, shutdown summaries, auto session ids, session markers, checkpoints, and bookmarks.
- Added runtime minimum log level controls and per-sink minimum level overrides.
- Added dependency-free `[QLOG(...)]` attribute helpers with explicit runner, scope, and discovery APIs.
- Added `LogOnce`, `LogEvery`, frame hitch markers, and asset load markers for low-noise operational diagnostics.
- Added crash fingerprints, duplicate fingerprint counts, and crash state snapshots.
- Added redaction presets for secrets, network values, user-data paths, and crash-safe support output.
- Added safe log filename and session directory helpers.
- Added console formatting controls for compact text, ANSI color, and local timestamps.
- Added QLOG diagnostics, repair, merge, and summary utilities.
- Added `quicklog tail`, `grep`, `diff`, `stats`, `redact`, `summarize`, `report`, `repair`, `merge`, `timeline`, and `doctor-config`.
- Added static single-file HTML report generation for offline support bundles.
- Added README command example parsing tests and runtime project dependency policy tests.

### Changed

- Changed package metadata from `2.2.0` to `2.3.0`.
- Changed the sample app to demonstrate the v2.3 lean diagnostics path without requiring extra dependencies or destructive exception demos.
- Changed crash dumps so state snapshot values are redacted before they are written.

### Notes

- QuickLog, QuickLog.Tools, and QuickLog.Sample remain dependency-free at runtime.
- The new report command writes a static HTML file only; it does not start a server or add a web dashboard runtime.

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

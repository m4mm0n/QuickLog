# QuickLog Linux And Android Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the next major QuickLog revision with verified Linux and Android support while keeping the runtime package dependency-free.

**Architecture:** Treat Linux as a first-class runtime for the existing `net8.0` and `net10.0` targets, then add Android-specific target-framework builds for mobile packaging and app-data path safety. Keep all platform decisions behind small internal helpers so sinks, crash dumps, profiles, and tools do not grow platform-specific branches everywhere.

**Tech Stack:** .NET SDK 10, `net8.0`, `net10.0`, `net10.0-android`, xUnit, GitHub Actions `ubuntu-latest`, .NET Android workload, BCL-only APIs.

---

## Non-Negotiable Guardrails

- Do not add runtime `PackageReference` entries to `QuickLog`, `QuickLog.Tools`, or `QuickLog.Sample`.
- Do not add Serilog, OpenTelemetry, Microsoft.Extensions.Logging, AndroidX, MAUI, Xamarin helpers, or any dependency-backed abstraction.
- Do not require Android runtime APIs from QuickLog core; use BCL paths and optional user-provided log roots.
- Do not make console or popup behavior fatal on Android where no traditional console/popup surface exists.
- Keep desktop Linux behavior identical to Windows unless a path, environment, or process boundary differs.
- Keep `QuickLog.Tools` desktop-only for this revision unless a later task explicitly decides to package tools for Android.
- Every public class, method, enum, and property added in this revision must have XML documentation.

## Current Portability Notes

- `QuickLog/QuickLog.csproj` currently targets `net8.0;net10.0`.
- `DefaultExceptionPopup` already uses `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)` before calling `user32.dll`, so Linux and Android should use fallback behavior.
- `GodotLogInterceptor` uses `System.Reflection.Emit`; Android may compile this under normal JIT-capable runtimes, but this path must be kept optional and non-fatal.
- `RotatingFileWriter`, JSON Lines, binary logs, and crash dumps assume the supplied path is writable. Android needs a safe default root for relative paths.
- `GodotUserPathResolver` uses `Environment.SpecialFolder.LocalApplicationData`, which needs explicit Android coverage.

## File Structure

- Modify `QuickLog/QuickLog.csproj`: add Android target framework and platform compile constants.
- Create `QuickLog/Platform/QuickLogPlatform.cs`: internal runtime/target-framework facts.
- Create `QuickLog/Platform/QuickLogPathResolver.cs`: relative path and default-root resolution for Linux and Android.
- Create `QuickLog/Platform/QuickLogConsole.cs`: safe console/error output wrapper for exception popup fallback.
- Modify `QuickLog/Sinks/RotatingFileWriter.cs`: resolve paths before opening files.
- Modify `QuickLog/Exceptions/CrashDumpOptions.cs`: resolve crash dump output through the platform path helper.
- Modify `QuickLog/Exceptions/DefaultExceptionPopup.cs`: use safe console fallback and suppress unsupported popup failures.
- Modify `QuickLog/LoggerOptions.cs`: add `ForLinux` and `ForAndroid` profiles.
- Modify `QuickLog/LogManager.cs`: preserve new profile settings in named loggers.
- Create `QuickLog.Tests/PlatformPathResolverTests.cs`: deterministic path-resolution coverage.
- Create `QuickLog.Tests/PlatformProfileTests.cs`: profile and validation coverage.
- Create `QuickLog.Tests/PlatformPopupTests.cs`: fallback popup does not throw.
- Create `samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj`: compile-only Android smoke app.
- Create `samples/QuickLog.AndroidSmoke/Program.cs`: writes a JSONL and QLOG into a caller-supplied root.
- Modify `QuickLog.sln`: include Android smoke project.
- Modify `.github/workflows/ci.yml`: add Linux test job and Android build job.
- Modify `.github/workflows/release.yml`: pack all supported TFMs and keep NuGet publishing unchanged.
- Modify `README.md` and `CHANGELOG.md`: document Linux/Android support and limitations.

---

### Task 1: Add Platform Dependency Policy Tests

**Files:**
- Create: `QuickLog.Tests/PlatformDependencyPolicyTests.cs`

- [ ] **Step 1: Write the failing test**

Create `QuickLog.Tests/PlatformDependencyPolicyTests.cs`:

```csharp
using Xunit;

namespace QuickLog.Tests;

public sealed class PlatformDependencyPolicyTests
{
    [Fact]
    public void RuntimeAndSmokeProjects_DoNotUsePackageReferences()
    {
        var root = FindRepoRoot();
        var projects = new[]
        {
            Path.Combine(root, "QuickLog", "QuickLog.csproj"),
            Path.Combine(root, "QuickLog.Tools", "QuickLog.Tools.csproj"),
            Path.Combine(root, "QuickLog.Sample", "QuickLog.Sample.csproj"),
            Path.Combine(root, "samples", "QuickLog.AndroidSmoke", "QuickLog.AndroidSmoke.csproj")
        };

        foreach (var project in projects.Where(File.Exists))
        {
            var text = File.ReadAllText(project);
            Assert.DoesNotContain("<PackageReference", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }
}
```

- [ ] **Step 2: Run test to verify it passes before platform work**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter PlatformDependencyPolicyTests
```

Expected: pass. The Android smoke project does not exist yet, so the test checks existing runtime projects and becomes a guard once the smoke project is added.

- [ ] **Step 3: Commit**

```powershell
git add QuickLog.Tests/PlatformDependencyPolicyTests.cs
git commit -m "test: guard platform runtime dependency policy"
```

---

### Task 2: Add Linux And Android Targeting

**Files:**
- Modify: `QuickLog/QuickLog.csproj`
- Test: `QuickLog.Tests/PlatformProfileTests.cs`

- [ ] **Step 1: Write the failing TFM test**

Create `QuickLog.Tests/PlatformProfileTests.cs`:

```csharp
using Xunit;

namespace QuickLog.Tests;

public sealed class PlatformProfileTests
{
    [Fact]
    public void QuickLogProject_TargetsAndroidInAdditionToPortableFrameworks()
    {
        var root = FindRepoRoot();
        var project = File.ReadAllText(Path.Combine(root, "QuickLog", "QuickLog.csproj"));

        Assert.Contains("<TargetFrameworks>net8.0;net10.0;net10.0-android</TargetFrameworks>", project);
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogProject_TargetsAndroidInAdditionToPortableFrameworks
```

Expected: fail because `net10.0-android` is not in `TargetFrameworks`.

- [ ] **Step 3: Add Android target framework**

Change `QuickLog/QuickLog.csproj`:

```xml
<TargetFrameworks>net8.0;net10.0;net10.0-android</TargetFrameworks>
```

Add this property group below the existing package metadata group:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-android'">
  <SupportedOSPlatformVersion>23.0</SupportedOSPlatformVersion>
  <DefineConstants>$(DefineConstants);QUICKLOG_ANDROID</DefineConstants>
</PropertyGroup>
```

- [ ] **Step 4: Run the TFM test**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogProject_TargetsAndroidInAdditionToPortableFrameworks
```

Expected: pass.

- [ ] **Step 5: Compile portable frameworks**

Run:

```powershell
dotnet build QuickLog/QuickLog.csproj -c Release -f net8.0
dotnet build QuickLog/QuickLog.csproj -c Release -f net10.0
```

Expected: both builds pass with 0 warnings.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/QuickLog.csproj QuickLog.Tests/PlatformProfileTests.cs
git commit -m "build: add Android target framework"
```

---

### Task 3: Centralize Platform Facts

**Files:**
- Create: `QuickLog/Platform/QuickLogPlatform.cs`
- Test: `QuickLog.Tests/PlatformPathResolverTests.cs`

- [ ] **Step 1: Write platform fact tests**

Create `QuickLog.Tests/PlatformPathResolverTests.cs`:

```csharp
using QuickLog.Platform;
using Xunit;

namespace QuickLog.Tests;

public sealed class PlatformPathResolverTests
{
    [Fact]
    public void QuickLogPlatform_ExposesRuntimeFacts()
    {
        Assert.False(string.IsNullOrWhiteSpace(QuickLogPlatform.RuntimeName));
        Assert.Contains(QuickLogPlatform.CurrentKind, new[]
        {
            QuickLogPlatformKind.Windows,
            QuickLogPlatformKind.Linux,
            QuickLogPlatformKind.Android,
            QuickLogPlatformKind.MacOS,
            QuickLogPlatformKind.Ios,
            QuickLogPlatformKind.Unknown
        });
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogPlatform_ExposesRuntimeFacts
```

Expected: compile failure for missing `QuickLog.Platform`.

- [ ] **Step 3: Implement platform facts**

Create `QuickLog/Platform/QuickLogPlatform.cs`:

```csharp
using System.Runtime.InteropServices;

namespace QuickLog.Platform;

public enum QuickLogPlatformKind
{
    Unknown = 0,
    Windows,
    Linux,
    Android,
    MacOS,
    Ios
}

public static class QuickLogPlatform
{
    public static QuickLogPlatformKind CurrentKind
    {
        get
        {
#if QUICKLOG_ANDROID
            return QuickLogPlatformKind.Android;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return QuickLogPlatformKind.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return QuickLogPlatformKind.Linux;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return QuickLogPlatformKind.MacOS;
            return QuickLogPlatformKind.Unknown;
#endif
        }
    }

    public static string RuntimeName
        => $"{CurrentKind}; {RuntimeInformation.FrameworkDescription}; {RuntimeInformation.OSDescription}";

    public static bool IsMobile
        => CurrentKind is QuickLogPlatformKind.Android or QuickLogPlatformKind.Ios;
}
```

- [ ] **Step 4: Run test**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogPlatform_ExposesRuntimeFacts
```

Expected: pass.

- [ ] **Step 5: Commit**

```powershell
git add QuickLog/Platform/QuickLogPlatform.cs QuickLog.Tests/PlatformPathResolverTests.cs
git commit -m "feat: add platform runtime facts"
```

---

### Task 4: Add Writable Path Resolution

**Files:**
- Create: `QuickLog/Platform/QuickLogPathResolver.cs`
- Modify: `QuickLog/Sinks/RotatingFileWriter.cs`
- Modify: `QuickLog/Exceptions/CrashDumpOptions.cs`
- Test: `QuickLog.Tests/PlatformPathResolverTests.cs`

- [ ] **Step 1: Add path-resolution tests**

Append to `PlatformPathResolverTests`:

```csharp
[Fact]
public void ResolveLogFilePath_RootsRelativePathsUnderWritableRoot()
{
    var root = Path.Combine(Path.GetTempPath(), $"ql_platform_{Guid.NewGuid():N}");

    var path = QuickLogPathResolver.ResolveLogFilePath("logs/app.qlog", root);

    Assert.Equal(Path.Combine(root, "logs", "app.qlog"), path);
}

[Fact]
public void ResolveLogFilePath_PreservesAbsolutePaths()
{
    var absolute = Path.Combine(Path.GetTempPath(), $"ql_abs_{Guid.NewGuid():N}", "app.log");

    var path = QuickLogPathResolver.ResolveLogFilePath(absolute, Path.Combine(Path.GetTempPath(), "unused"));

    Assert.Equal(absolute, path);
}

[Fact]
public void ResolveCrashDumpDirectory_UsesWritableRootWhenNoDirectoryConfigured()
{
    var root = Path.Combine(Path.GetTempPath(), $"ql_crash_{Guid.NewGuid():N}");

    var path = QuickLogPathResolver.ResolveCrashDumpDirectory(null, root);

    Assert.Equal(Path.Combine(root, "crashes"), path);
}
```

- [ ] **Step 2: Run tests to verify compile failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "ResolveLogFilePath|ResolveCrashDumpDirectory"
```

Expected: compile failure for missing `QuickLogPathResolver`.

- [ ] **Step 3: Implement path resolver**

Create `QuickLog/Platform/QuickLogPathResolver.cs`:

```csharp
namespace QuickLog.Platform;

public static class QuickLogPathResolver
{
    public static string DefaultWritableRoot
    {
        get
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(local))
                return Path.Combine(local, "QuickLog");

            var personal = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            if (!string.IsNullOrWhiteSpace(personal))
                return Path.Combine(personal, ".quicklog");

            return Path.Combine(Path.GetTempPath(), "QuickLog");
        }
    }

    public static string ResolveLogFilePath(string path, string? writableRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Log path cannot be empty.", nameof(path));

        if (Path.IsPathRooted(path))
            return path;

        return Path.Combine(writableRoot ?? DefaultWritableRoot, path);
    }

    public static string ResolveCrashDumpDirectory(string? configuredDirectory, string? writableRoot = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
            return Path.IsPathRooted(configuredDirectory)
                ? configuredDirectory
                : Path.Combine(writableRoot ?? DefaultWritableRoot, configuredDirectory);

        return Path.Combine(writableRoot ?? DefaultWritableRoot, "crashes");
    }
}
```

- [ ] **Step 4: Wire rotating writer**

Modify `QuickLog/Sinks/RotatingFileWriter.cs` constructor:

```csharp
using QuickLog.Platform;

public RotatingFileWriter(string path, LogRotationOptions? options = null)
{
    _path = QuickLogPathResolver.ResolveLogFilePath(path);
    _options = options?.IsEnabled == true ? options : null;

    Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
    _stream = OpenAppend();

    if (_options?.RotateOnStartup == true && _stream.Length > 0)
        Rotate();
}
```

- [ ] **Step 5: Wire crash dump directory**

Modify `CrashDumpOptions.ResolvedOutputDirectory`:

```csharp
using QuickLog.Platform;

internal string ResolvedOutputDirectory =>
    QuickLogPathResolver.ResolveCrashDumpDirectory(OutputDirectory);
```

- [ ] **Step 6: Run path tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter PlatformPathResolverTests
```

Expected: pass.

- [ ] **Step 7: Run existing sink tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "BinaryLogRoundtripTests|JsonLinesSinkTests|LogRotationTests|CrashDumpTests"
```

Expected: pass. If tests assert exact temp paths, adjust tests to use absolute temp paths so path resolution is explicit.

- [ ] **Step 8: Commit**

```powershell
git add QuickLog/Platform/QuickLogPathResolver.cs QuickLog/Sinks/RotatingFileWriter.cs QuickLog/Exceptions/CrashDumpOptions.cs QuickLog.Tests/PlatformPathResolverTests.cs QuickLog.Tests
git commit -m "feat: resolve writable log paths for Linux and Android"
```

---

### Task 5: Harden Console And Popup Fallbacks

**Files:**
- Create: `QuickLog/Platform/QuickLogConsole.cs`
- Modify: `QuickLog/Exceptions/DefaultExceptionPopup.cs`
- Test: `QuickLog.Tests/PlatformPopupTests.cs`

- [ ] **Step 1: Write popup fallback tests**

Create `QuickLog.Tests/PlatformPopupTests.cs`:

```csharp
using QuickLog.Exceptions;
using Xunit;

namespace QuickLog.Tests;

public sealed class PlatformPopupTests
{
    [Fact]
    public void DefaultPopup_DoesNotThrow_WhenConsoleFallbackIsUsed()
    {
        var popup = new DefaultExceptionPopup();
        var ex = new InvalidOperationException("sample");

        var thrown = Record.Exception(() =>
            popup.Show("QuickLog test", "message", ex, ExceptionSource.UnobservedTask));

        Assert.Null(thrown);
    }
}
```

- [ ] **Step 2: Run test**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter PlatformPopupTests
```

Expected: pass on Windows and Linux. Keep this as a regression guard before refactor.

- [ ] **Step 3: Implement console wrapper**

Create `QuickLog/Platform/QuickLogConsole.cs`:

```csharp
namespace QuickLog.Platform;

public static class QuickLogConsole
{
    public static void WriteErrorLine(string value)
    {
        try
        {
            Console.Error.WriteLine(value);
        }
        catch
        {
            System.Diagnostics.Trace.WriteLine(value);
        }
    }
}
```

- [ ] **Step 4: Use wrapper in popup fallback**

Modify `DefaultExceptionPopup.ShowConsoleFallback`:

```csharp
QuickLogConsole.WriteErrorLine(separator);
QuickLogConsole.WriteErrorLine($"  {title}");
QuickLogConsole.WriteErrorLine(separator);
QuickLogConsole.WriteErrorLine(message);
QuickLogConsole.WriteErrorLine(separator);
```

Add `using QuickLog.Platform;`.

- [ ] **Step 5: Run popup tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter PlatformPopupTests
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/Platform/QuickLogConsole.cs QuickLog/Exceptions/DefaultExceptionPopup.cs QuickLog.Tests/PlatformPopupTests.cs
git commit -m "feat: harden platform console popup fallback"
```

---

### Task 6: Add Linux And Android Profiles

**Files:**
- Modify: `QuickLog/LoggerOptions.cs`
- Test: `QuickLog.Tests/PlatformProfileTests.cs`

- [ ] **Step 1: Add profile tests**

Append to `PlatformProfileTests`:

```csharp
[Fact]
public void ForLinux_UsesDurableAsyncLogsAndConsole()
{
    var options = LoggerOptions.ForLinux("logs");

    Assert.True(options.ConsoleLogging);
    Assert.True(options.AsyncLogging);
    Assert.True(options.AsyncOnly);
    Assert.True(options.AsyncBinaryLogging);
    Assert.Equal(Path.Combine("logs", "quicklog.qlog"), options.BinaryLogPath);
    Assert.Equal(Path.Combine("logs", "quicklog.jsonl"), options.JsonLogPath);
    Assert.True(options.EmitStartupBanner);
    Assert.True(options.EmitShutdownSummary);
}

[Fact]
public void ForAndroid_DisablesConsoleAndUsesCrashSafeRedaction()
{
    var options = LoggerOptions.ForAndroid("logs");

    Assert.False(options.ConsoleLogging);
    Assert.True(options.AsyncLogging);
    Assert.True(options.AsyncOnly);
    Assert.True(options.AsyncBinaryLogging);
    Assert.NotNull(options.Redaction);
    Assert.True(options.Redaction.RedactUserProfilePaths);
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "ForLinux|ForAndroid"
```

Expected: compile failure for missing profiles.

- [ ] **Step 3: Implement profiles**

Add to `LoggerOptions`:

```csharp
public static LoggerOptions ForLinux(string logDirectory = "logs") => ForEngine(logDirectory)
    .WithConsole(true)
    .WithAnsiColor(true)
    .WithSession("linux", autoId: true);

public static LoggerOptions ForAndroid(string logDirectory = "logs") => ForEngine(logDirectory)
    .WithConsole(false)
    .WithSession("android", autoId: true);
```

- [ ] **Step 4: Run profile tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "PlatformProfileTests|LoggerOptionsValidationTests"
```

Expected: pass.

- [ ] **Step 5: Commit**

```powershell
git add QuickLog/LoggerOptions.cs QuickLog.Tests/PlatformProfileTests.cs
git commit -m "feat: add Linux and Android logger profiles"
```

---

### Task 7: Add Android Smoke Project

**Files:**
- Create: `samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj`
- Create: `samples/QuickLog.AndroidSmoke/Program.cs`
- Modify: `QuickLog.sln`

- [ ] **Step 1: Create Android smoke project**

Create `samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-android</TargetFramework>
    <SupportedOSPlatformVersion>23.0</SupportedOSPlatformVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\QuickLog\QuickLog.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create smoke program**

Create `samples/QuickLog.AndroidSmoke/Program.cs`:

```csharp
using QuickLog;
using QuickLog.Core;
using QuickLog.Loggers;

var root = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuickLogAndroidSmoke");

Directory.CreateDirectory(root);

LogManager.ConfigureDefault(LoggerOptions.ForAndroid(root));
var logger = (QuickLogger)LogManager.GetDefaultLogger();

LogStateSnapshot.Set("platform", "android");
logger.Log(LogType.Info, "Android smoke started");
logger.LogFrameTime(1, TimeSpan.FromMilliseconds(12), TimeSpan.FromMilliseconds(16));
logger.Shutdown();

Console.WriteLine(Path.Combine(root, "quicklog.qlog"));
LogManager.Shutdown();
```

- [ ] **Step 3: Add project to solution**

Run:

```powershell
dotnet sln QuickLog.sln add samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj
```

Expected: project added to the solution.

- [ ] **Step 4: Build smoke project with Android workload installed**

Run:

```powershell
dotnet workload restore samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj
dotnet build samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj -c Release
```

Expected: build succeeds. If the local machine lacks Android workload installation permissions, run this in CI after Task 8 and record the local limitation in the commit message body.

- [ ] **Step 5: Commit**

```powershell
git add QuickLog.sln samples/QuickLog.AndroidSmoke
git commit -m "test: add Android smoke project"
```

---

### Task 8: Add Linux And Android CI Gates

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1: Update CI workflow with Linux test job**

Add this job to `.github/workflows/ci.yml`:

```yaml
  linux-test:
    name: Linux Test
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        run: dotnet restore QuickLog.sln

      - name: Build
        run: dotnet build QuickLog.sln --configuration Release --no-restore

      - name: Test
        run: dotnet test QuickLog.Tests/QuickLog.Tests.csproj --configuration Release --no-build --logger "console;verbosity=normal"
```

- [ ] **Step 2: Update CI workflow with Android compile job**

Add this job to `.github/workflows/ci.yml`:

```yaml
  android-build:
    name: Android Build
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install Android workload
        run: dotnet workload install android

      - name: Restore Android Smoke
        run: dotnet restore samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj

      - name: Build Android Smoke
        run: dotnet build samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj --configuration Release --no-restore
```

- [ ] **Step 3: Update release workflow restore/build**

In `.github/workflows/release.yml`, keep packing `QuickLog/QuickLog.csproj`, but make sure the release job installs Android workload before restore:

```yaml
      - name: Install Android workload
        run: dotnet workload install android
```

Place it after setup-dotnet and before restore.

- [ ] **Step 4: Commit**

```powershell
git add .github/workflows/ci.yml .github/workflows/release.yml
git commit -m "ci: verify Linux tests and Android build"
```

---

### Task 9: Documentation And Versioning

**Files:**
- Modify: `QuickLog/QuickLog.csproj`
- Modify: `README.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump package metadata for next major prerelease**

Change in `QuickLog/QuickLog.csproj`:

```xml
<Version>3.0.0-linux-android.1</Version>
<AssemblyVersion>3.0.0.0</AssemblyVersion>
<FileVersion>3.0.0.0</FileVersion>
<PackageVersion>3.0.0-linux-android.1</PackageVersion>
<PackageReleaseNotes>QuickLog 3.0 preview adds verified Linux and Android support, mobile-safe writable paths, Linux/Android profiles, and CI gates while keeping runtime projects dependency-free.</PackageReleaseNotes>
```

- [ ] **Step 2: Update README platform section**

Add this section to `README.md`:

```markdown
## Platform Support

QuickLog 3.0 adds verified Linux and Android support.

| Platform | Status | Notes |
|---|---|---|
| Windows | Supported | Existing desktop path |
| Linux | Supported | Verified by CI on ubuntu-latest |
| Android | Preview supported | `net10.0-android`; writes relative logs under app-local storage |

Use `LoggerOptions.ForLinux("logs")` for Linux console/tool/server apps.
Use `LoggerOptions.ForAndroid("logs")` for Android apps where console output should be quiet and file-backed logs should land in a writable app-local location.
```

- [ ] **Step 3: Update changelog**

Add to `CHANGELOG.md`:

```markdown
## [3.0.0-linux-android.1] - Unreleased

### Added

- Added Linux CI verification for the existing portable targets.
- Added Android target-framework build support.
- Added platform path resolution for mobile-safe writable log roots.
- Added Linux and Android logger profiles.
- Added Android smoke project.

### Notes

- Runtime projects remain dependency-free.
- `QuickLog.Tools` remains desktop/server-oriented and is not packaged as an Android application.
```

- [ ] **Step 4: Commit**

```powershell
git add QuickLog/QuickLog.csproj README.md CHANGELOG.md
git commit -m "docs: document Linux and Android support preview"
```

---

### Task 10: Full Verification

**Files:**
- No planned source changes.

- [ ] **Step 1: Check dependency policy**

Run:

```powershell
rg -n "<PackageReference" QuickLog QuickLog.Tools QuickLog.Sample samples/QuickLog.AndroidSmoke
```

Expected: no output.

- [ ] **Step 2: Build portable frameworks**

Run:

```powershell
dotnet build QuickLog.sln -c Release
```

Expected: build succeeds with 0 warnings. If Android workload is not installed locally and the solution build attempts the Android project, run:

```powershell
dotnet build QuickLog/QuickLog.csproj -c Release -f net8.0
dotnet build QuickLog/QuickLog.csproj -c Release -f net10.0
dotnet build QuickLog.Tests/QuickLog.Tests.csproj -c Release
```

Expected: all portable builds pass with 0 warnings.

- [ ] **Step 3: Run tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --no-build
```

Expected: all tests pass.

- [ ] **Step 4: Build Android smoke**

Run:

```powershell
dotnet workload restore samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj
dotnet build samples/QuickLog.AndroidSmoke/QuickLog.AndroidSmoke.csproj -c Release
```

Expected: build succeeds. If local workload installation is unavailable, verify this through the Android CI job before tagging or releasing.

- [ ] **Step 5: Pack**

Run:

```powershell
dotnet pack QuickLog/QuickLog.csproj -c Release --no-build -o artifacts/packages
```

Expected: package contains `lib/net8.0`, `lib/net10.0`, and `lib/net10.0-android`.

- [ ] **Step 6: Commit verification note only if files changed**

Run:

```powershell
git status --short
```

Expected: no tracked source changes from verification. Do not commit generated `artifacts`, `bin`, `obj`, or logs.

---

## Release Criteria

- Linux CI job passes on `ubuntu-latest`.
- Android build job passes with the .NET Android workload.
- Full Windows Release build remains 0-warning.
- Runtime projects still have no `PackageReference`.
- NuGet package includes the Android TFM assembly.
- README states Android support is preview until a real device/emulator smoke has been run.

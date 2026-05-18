# QuickLog IOS And OSX Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add verified iOS and OSX/macOS support to QuickLog without adding runtime dependencies or hiding unsupported Apple-platform behavior.

**Architecture:** Keep macOS support on the existing portable `net8.0` and `net10.0` targets, then add an iOS target-framework build so NuGet resolves cleanly in iOS apps. Isolate Apple-specific constraints behind small platform and capability helpers, especially for writable paths, process restart, popup fallback, trimming, and `Reflection.Emit` restrictions.

**Tech Stack:** .NET SDK 10, `net8.0`, `net10.0`, `net10.0-ios`, xUnit, macOS GitHub Actions runners, .NET iOS workload, Xcode simulator build tooling, BCL-only APIs.

---

## Non-Negotiable Guardrails

- Do not add runtime `PackageReference` entries to `QuickLog`, `QuickLog.Tools`, `QuickLog.Sample`, or Apple smoke projects.
- Do not add MAUI, Xamarin.Forms, AppKit bindings, UIKit wrappers, Swift interop, or any external Apple helper library.
- Do not pretend iOS can support process restart, traditional console popups, or dynamic IL emission.
- Keep `QuickLog.Tools` desktop/server-oriented; do not package it as an iOS app.
- Keep `QLOG` attribute helpers explicit; do not introduce source generators, IL weaving, or proxies.
- Every new public class, enum, property, and method must have XML documentation.
- The iOS branch may depend conceptually on the Linux/Android platform-helper work, but this plan must remain executable from current `master`.

## Current Apple Portability Notes

- `QuickLog/QuickLog.csproj` currently targets `net8.0;net10.0`.
- macOS desktop already uses those portable TFMs, but it is not verified by CI.
- iOS apps require an iOS TFM such as `net10.0-ios` for clean NuGet asset selection.
- `GodotLogInterceptor` uses `System.Reflection.Emit`, which is not compatible with iOS AOT. Dynamic Godot sink registration must be compiled out or replaced with a no-op registrar on iOS.
- `QLogDiscovery.Scan(Assembly)` uses reflection over assemblies. That is acceptable as an explicit diagnostic helper, but trimming/AOT warnings must be documented and reduced with BCL annotations where possible.
- `RestartOptions` attempts to start the current process, which is invalid on iOS and should be disabled by platform capability checks.

## File Structure

- Modify `QuickLog/QuickLog.csproj`: add `net10.0-ios`, Apple compile constants, and trim/AOT analyzer settings.
- Create `QuickLog/Platform/QuickLogPlatform.cs`: current platform facts for Windows, Linux, Android, macOS, and iOS.
- Create `QuickLog/Platform/QuickLogPlatformCapabilities.cs`: booleans for restart, popup, dynamic code, and interactive console support.
- Create `QuickLog/Platform/QuickLogPathResolver.cs`: app-local writable path selection for iOS and macOS.
- Modify `QuickLog/Exceptions/RestartOptions.cs`: expose platform support check for restart.
- Modify `QuickLog/Exceptions/ExceptionHookManager.cs`: skip auto-restart when the platform cannot restart.
- Modify `QuickLog/Exceptions/DefaultExceptionPopup.cs`: use a safe Apple fallback that never throws when no console is available.
- Refactor `QuickLog/Godot/GodotLogInterceptor.cs`: delegate dynamic registration to a platform-specific registrar.
- Create `QuickLog/Godot/GodotDynamicSinkRegistrar.cs`: Reflection.Emit registrar for non-iOS targets.
- Create `QuickLog/Godot/GodotDynamicSinkRegistrar.Unsupported.cs`: iOS no-op registrar.
- Modify `QuickLog/QLogDiscovery.cs`: add trimming annotations and docs for reflection-based discovery.
- Modify `QuickLog/LoggerOptions.cs`: add `ForMacOS` and `ForIOS` profiles.
- Create `QuickLog.Tests/ApplePlatformProfileTests.cs`: profile and project metadata coverage.
- Create `QuickLog.Tests/ApplePlatformCapabilityTests.cs`: capability decisions and restart guard coverage.
- Create `QuickLog.Tests/QLogDiscoveryTrimTests.cs`: documents the reflection-discovery annotations.
- Create `samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj`: compile-only iOS smoke app.
- Create `samples/QuickLog.IosSmoke/Program.cs`: writes startup markers and shuts down cleanly.
- Create `samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj`: macOS desktop console smoke app.
- Create `samples/QuickLog.MacSmoke/Program.cs`: writes JSONL/QLOG and crash-safe state.
- Modify `QuickLog.sln`: include Apple smoke projects.
- Modify `.github/workflows/ci.yml`: add macOS test and iOS build jobs.
- Modify `.github/workflows/release.yml`: install iOS workload before packing when the iOS TFM is included.
- Modify `README.md` and `CHANGELOG.md`: document Apple support and limitations.

---

### Task 1: Add Apple Dependency Policy Tests

**Files:**
- Create: `QuickLog.Tests/AppleDependencyPolicyTests.cs`

- [ ] **Step 1: Write the dependency guard**

Create `QuickLog.Tests/AppleDependencyPolicyTests.cs`:

```csharp
using Xunit;

namespace QuickLog.Tests;

public sealed class AppleDependencyPolicyTests
{
    [Fact]
    public void RuntimeAndAppleSmokeProjects_DoNotUsePackageReferences()
    {
        var root = FindRepoRoot();
        var projects = new[]
        {
            Path.Combine(root, "QuickLog", "QuickLog.csproj"),
            Path.Combine(root, "QuickLog.Tools", "QuickLog.Tools.csproj"),
            Path.Combine(root, "QuickLog.Sample", "QuickLog.Sample.csproj"),
            Path.Combine(root, "samples", "QuickLog.IosSmoke", "QuickLog.IosSmoke.csproj"),
            Path.Combine(root, "samples", "QuickLog.MacSmoke", "QuickLog.MacSmoke.csproj")
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

- [ ] **Step 2: Run dependency test**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter AppleDependencyPolicyTests
```

Expected: pass. The smoke projects do not exist yet, so this becomes a guard as soon as they are added.

- [ ] **Step 3: Commit**

```powershell
git add QuickLog.Tests/AppleDependencyPolicyTests.cs
git commit -m "test: guard Apple runtime dependency policy"
```

---

### Task 2: Add iOS Target Framework

**Files:**
- Modify: `QuickLog/QuickLog.csproj`
- Create: `QuickLog.Tests/ApplePlatformProfileTests.cs`

- [ ] **Step 1: Write the failing TFM test**

Create `QuickLog.Tests/ApplePlatformProfileTests.cs`:

```csharp
using Xunit;

namespace QuickLog.Tests;

public sealed class ApplePlatformProfileTests
{
    [Fact]
    public void QuickLogProject_TargetsIosInAdditionToPortableFrameworks()
    {
        var root = FindRepoRoot();
        var project = File.ReadAllText(Path.Combine(root, "QuickLog", "QuickLog.csproj"));

        Assert.Contains("net10.0-ios", project);
        Assert.Contains("QUICKLOG_IOS", project);
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

- [ ] **Step 2: Run test to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogProject_TargetsIosInAdditionToPortableFrameworks
```

Expected: fail because the iOS TFM and constant do not exist.

- [ ] **Step 3: Add iOS TFM**

Change `QuickLog/QuickLog.csproj`:

```xml
<TargetFrameworks>net8.0;net10.0;net10.0-ios</TargetFrameworks>
```

Add:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-ios'">
  <SupportedOSPlatformVersion>13.0</SupportedOSPlatformVersion>
  <DefineConstants>$(DefineConstants);QUICKLOG_IOS</DefineConstants>
</PropertyGroup>
```

When this is merged with the Linux/Android branch, the final combined TFM list should be:

```xml
<TargetFrameworks>net8.0;net10.0;net10.0-android;net10.0-ios</TargetFrameworks>
```

- [ ] **Step 4: Run TFM test**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QuickLogProject_TargetsIosInAdditionToPortableFrameworks
```

Expected: pass.

- [ ] **Step 5: Compile portable frameworks locally**

Run:

```powershell
dotnet build QuickLog/QuickLog.csproj -c Release -f net8.0
dotnet build QuickLog/QuickLog.csproj -c Release -f net10.0
```

Expected: both builds pass with 0 warnings.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/QuickLog.csproj QuickLog.Tests/ApplePlatformProfileTests.cs
git commit -m "build: add iOS target framework"
```

---

### Task 3: Add Apple Platform Facts And Capabilities

**Files:**
- Create: `QuickLog/Platform/QuickLogPlatform.cs`
- Create: `QuickLog/Platform/QuickLogPlatformCapabilities.cs`
- Create: `QuickLog.Tests/ApplePlatformCapabilityTests.cs`

- [ ] **Step 1: Write capability tests**

Create `QuickLog.Tests/ApplePlatformCapabilityTests.cs`:

```csharp
using QuickLog.Platform;
using Xunit;

namespace QuickLog.Tests;

public sealed class ApplePlatformCapabilityTests
{
    [Fact]
    public void PlatformCapabilities_DisableRestartOnIos()
    {
        var caps = QuickLogPlatformCapabilities.For(QuickLogPlatformKind.Ios);

        Assert.False(caps.CanRestartProcess);
        Assert.False(caps.CanUseReflectionEmit);
        Assert.False(caps.HasInteractiveConsole);
    }

    [Fact]
    public void PlatformCapabilities_EnableDesktopFeaturesOnMacOS()
    {
        var caps = QuickLogPlatformCapabilities.For(QuickLogPlatformKind.MacOS);

        Assert.True(caps.CanRestartProcess);
        Assert.True(caps.HasInteractiveConsole);
    }
}
```

- [ ] **Step 2: Run tests to verify compile failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter ApplePlatformCapabilityTests
```

Expected: compile failure for missing platform types.

- [ ] **Step 3: Implement platform kind**

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
#if QUICKLOG_IOS
            return QuickLogPlatformKind.Ios;
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
}
```

- [ ] **Step 4: Implement capabilities**

Create `QuickLog/Platform/QuickLogPlatformCapabilities.cs`:

```csharp
namespace QuickLog.Platform;

public sealed record QuickLogPlatformCapabilities(
    bool CanRestartProcess,
    bool CanUseReflectionEmit,
    bool HasInteractiveConsole,
    bool SupportsNativePopup)
{
    public static QuickLogPlatformCapabilities Current => For(QuickLogPlatform.CurrentKind);

    public static QuickLogPlatformCapabilities For(QuickLogPlatformKind platform)
        => platform switch
        {
            QuickLogPlatformKind.Ios => new(
                CanRestartProcess: false,
                CanUseReflectionEmit: false,
                HasInteractiveConsole: false,
                SupportsNativePopup: false),
            QuickLogPlatformKind.MacOS => new(
                CanRestartProcess: true,
                CanUseReflectionEmit: true,
                HasInteractiveConsole: true,
                SupportsNativePopup: false),
            _ => new(
                CanRestartProcess: true,
                CanUseReflectionEmit: true,
                HasInteractiveConsole: true,
                SupportsNativePopup: platform == QuickLogPlatformKind.Windows)
        };
}
```

- [ ] **Step 5: Run capability tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter ApplePlatformCapabilityTests
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/Platform QuickLog.Tests/ApplePlatformCapabilityTests.cs
git commit -m "feat: add Apple platform capabilities"
```

---

### Task 4: Make Restart Options Platform-Safe

**Files:**
- Modify: `QuickLog/Exceptions/RestartOptions.cs`
- Modify: `QuickLog/Exceptions/ExceptionHookManager.cs`
- Test: `QuickLog.Tests/ApplePlatformCapabilityTests.cs`

- [ ] **Step 1: Add restart support test**

Append to `ApplePlatformCapabilityTests`:

```csharp
[Fact]
public void RestartOptions_ReportUnsupportedOnIos()
{
    var options = new RestartOptions { EnableAutoRestart = true };

    Assert.False(options.IsSupportedOn(QuickLogPlatformKind.Ios));
    Assert.True(options.IsSupportedOn(QuickLogPlatformKind.MacOS));
}
```

- [ ] **Step 2: Run test to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter RestartOptions_ReportUnsupportedOnIos
```

Expected: compile failure for missing `IsSupportedOn`.

- [ ] **Step 3: Implement restart support helper**

Add to `RestartOptions`:

```csharp
using QuickLog.Platform;

public bool IsSupportedOn(QuickLogPlatformKind platform)
    => !EnableAutoRestart || QuickLogPlatformCapabilities.For(platform).CanRestartProcess;
```

- [ ] **Step 4: Guard restart in exception manager**

Change the restart condition in `ExceptionHookManager.Handle`:

```csharp
if (isTerminating
    && source == ExceptionSource.AppDomain
    && opts.Restart is { EnableAutoRestart: true }
    && opts.Restart.IsSupportedOn(QuickLogPlatform.CurrentKind))
{
    TryRestart(opts.Restart);
}
```

Add `using QuickLog.Platform;`.

- [ ] **Step 5: Run focused tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "RestartOptions_ReportUnsupportedOnIos|ExceptionHookTests"
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/Exceptions/RestartOptions.cs QuickLog/Exceptions/ExceptionHookManager.cs QuickLog.Tests/ApplePlatformCapabilityTests.cs
git commit -m "fix: guard restart on unsupported Apple platforms"
```

---

### Task 5: Add Apple Writable Path Resolution

**Files:**
- Create: `QuickLog/Platform/QuickLogPathResolver.cs`
- Modify: `QuickLog/Sinks/RotatingFileWriter.cs`
- Modify: `QuickLog/Exceptions/CrashDumpOptions.cs`
- Test: `QuickLog.Tests/ApplePlatformCapabilityTests.cs`

- [ ] **Step 1: Add path tests**

Append:

```csharp
[Fact]
public void ApplePathResolver_RootsRelativeIosPathUnderWritableRoot()
{
    var root = Path.Combine(Path.GetTempPath(), $"ql_ios_{Guid.NewGuid():N}");

    var path = QuickLogPathResolver.ResolveLogFilePath("logs/app.qlog", root);

    Assert.Equal(Path.Combine(root, "logs", "app.qlog"), path);
}

[Fact]
public void ApplePathResolver_PreservesAbsoluteMacPath()
{
    var absolute = Path.Combine(Path.GetTempPath(), $"ql_mac_{Guid.NewGuid():N}", "app.log");

    var path = QuickLogPathResolver.ResolveLogFilePath(absolute, Path.Combine(Path.GetTempPath(), "unused"));

    Assert.Equal(absolute, path);
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "ApplePathResolver"
```

Expected: compile failure for missing resolver.

- [ ] **Step 3: Implement resolver**

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

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrWhiteSpace(documents))
                return Path.Combine(documents, "QuickLog");

            return Path.Combine(Path.GetTempPath(), "QuickLog");
        }
    }

    public static string ResolveLogFilePath(string path, string? writableRoot = null)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Log path cannot be empty.", nameof(path));

        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(writableRoot ?? DefaultWritableRoot, path);
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

- [ ] **Step 4: Wire file and crash paths**

Update `RotatingFileWriter` constructor:

```csharp
using QuickLog.Platform;

_path = QuickLogPathResolver.ResolveLogFilePath(path);
```

Update `CrashDumpOptions.ResolvedOutputDirectory`:

```csharp
using QuickLog.Platform;

internal string ResolvedOutputDirectory =>
    QuickLogPathResolver.ResolveCrashDumpDirectory(OutputDirectory);
```

- [ ] **Step 5: Run path and sink tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "ApplePathResolver|BinaryLogRoundtripTests|JsonLinesSinkTests|CrashDumpTests"
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/Platform/QuickLogPathResolver.cs QuickLog/Sinks/RotatingFileWriter.cs QuickLog/Exceptions/CrashDumpOptions.cs QuickLog.Tests/ApplePlatformCapabilityTests.cs
git commit -m "feat: resolve writable Apple log paths"
```

---

### Task 6: Split Godot Dynamic Registration For iOS AOT

**Files:**
- Modify: `QuickLog/Godot/GodotLogInterceptor.cs`
- Create: `QuickLog/Godot/GodotDynamicSinkRegistrar.cs`
- Create: `QuickLog/Godot/GodotDynamicSinkRegistrar.Unsupported.cs`
- Test: `QuickLog.Tests/ApplePlatformCapabilityTests.cs`

- [ ] **Step 1: Add registrar capability test**

Append:

```csharp
[Fact]
public void PlatformCapabilities_DisableDynamicCodeOnIos()
{
    Assert.False(QuickLogPlatformCapabilities.For(QuickLogPlatformKind.Ios).CanUseReflectionEmit);
    Assert.True(QuickLogPlatformCapabilities.For(QuickLogPlatformKind.MacOS).CanUseReflectionEmit);
}
```

- [ ] **Step 2: Extract non-iOS registrar**

Move the Reflection.Emit-specific methods from `GodotLogInterceptor` into `QuickLog/Godot/GodotDynamicSinkRegistrar.cs`:

```csharp
#if !QUICKLOG_IOS
using System.Reflection;
using System.Reflection.Emit;

namespace QuickLog.Godot;

internal static class GodotDynamicSinkRegistrar
{
    public static object? DynamicSinkInstance { get; private set; }
    private static MethodInfo? _removeLoggerMethod;

    public static void TryRegister()
    {
        // Move the existing TryRegisterDynamicSink body here, assigning DynamicSinkInstance.
    }

    public static void TryUnregister()
    {
        // Move the existing TryUnregisterDynamicSink body here.
    }
}
#endif
```

Use the exact IL bodies from the current `GodotLogInterceptor` methods so behavior does not change on Windows, Linux, or macOS.

- [ ] **Step 3: Add iOS no-op registrar**

Create `QuickLog/Godot/GodotDynamicSinkRegistrar.Unsupported.cs`:

```csharp
#if QUICKLOG_IOS
namespace QuickLog.Godot;

internal static class GodotDynamicSinkRegistrar
{
    public static object? DynamicSinkInstance => null;

    public static void TryRegister()
    {
    }

    public static void TryUnregister()
    {
    }
}
#endif
```

- [ ] **Step 4: Update interceptor**

In `GodotLogInterceptor`:

```csharp
public static bool IsDynamicSinkRegistered
{
    get { lock (_lock) return GodotDynamicSinkRegistrar.DynamicSinkInstance != null; }
}
```

Replace dynamic registration calls:

```csharp
if (!_attached && options.TryDynamicLoggerRegistration)
    GodotDynamicSinkRegistrar.TryRegister();
```

Replace unregister:

```csharp
GodotDynamicSinkRegistrar.TryUnregister();
```

Remove `using System.Reflection.Emit;` from `GodotLogInterceptor.cs`.

- [ ] **Step 5: Run Godot tests and build**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "Godot|ApplePlatformCapabilityTests"
dotnet build QuickLog/QuickLog.csproj -c Release -f net10.0
```

Expected: pass with 0 warnings.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/Godot/GodotLogInterceptor.cs QuickLog/Godot/GodotDynamicSinkRegistrar.cs QuickLog/Godot/GodotDynamicSinkRegistrar.Unsupported.cs QuickLog.Tests/ApplePlatformCapabilityTests.cs
git commit -m "refactor: isolate dynamic Godot registration for iOS"
```

---

### Task 7: Add QLOG Discovery Trim Annotations

**Files:**
- Modify: `QuickLog/QLogDiscovery.cs`
- Test: `QuickLog.Tests/QLogDiscoveryTrimTests.cs`

- [ ] **Step 1: Add source text tests for annotations**

Create `QuickLog.Tests/QLogDiscoveryTrimTests.cs`:

```csharp
using Xunit;

namespace QuickLog.Tests;

public sealed class QLogDiscoveryTrimTests
{
    [Fact]
    public void QLogDiscovery_DocumentsAssemblyScanTrimBehavior()
    {
        var root = FindRepoRoot();
        var source = File.ReadAllText(Path.Combine(root, "QuickLog", "QLogDiscovery.cs"));

        Assert.Contains("RequiresUnreferencedCode", source);
        Assert.Contains("DynamicallyAccessedMembers", source);
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

- [ ] **Step 2: Run test to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QLogDiscoveryTrimTests
```

Expected: fail because annotations are absent.

- [ ] **Step 3: Add annotations**

Modify `QLogDiscovery.cs`:

```csharp
using System.Diagnostics.CodeAnalysis;

[RequiresUnreferencedCode("Assembly-wide QLOG discovery uses reflection and may miss members removed by trimming. Prefer Scan(Type) for AOT-sensitive apps.")]
public static IReadOnlyList<QLogTarget> Scan(Assembly assembly)
```

Change `Scan(Type type)` signature:

```csharp
public static IReadOnlyList<QLogTarget> Scan(
    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
    Type type)
```

- [ ] **Step 4: Run trim annotation tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QLogDiscoveryTrimTests
```

Expected: pass.

- [ ] **Step 5: Run QLOG behavior tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter QLogAttributeTests
```

Expected: pass.

- [ ] **Step 6: Commit**

```powershell
git add QuickLog/QLogDiscovery.cs QuickLog.Tests/QLogDiscoveryTrimTests.cs
git commit -m "docs: mark QLOG discovery trimming behavior"
```

---

### Task 8: Add Apple Profiles

**Files:**
- Modify: `QuickLog/LoggerOptions.cs`
- Test: `QuickLog.Tests/ApplePlatformProfileTests.cs`

- [ ] **Step 1: Add profile tests**

Append to `ApplePlatformProfileTests`:

```csharp
[Fact]
public void ForMacOS_UsesConsoleAndDurableAsyncLogs()
{
    var options = LoggerOptions.ForMacOS("logs");

    Assert.True(options.ConsoleLogging);
    Assert.True(options.AsyncLogging);
    Assert.True(options.AsyncOnly);
    Assert.True(options.AsyncBinaryLogging);
    Assert.Equal(Path.Combine("logs", "quicklog.qlog"), options.BinaryLogPath);
    Assert.Equal("macos", options.SessionName);
}

[Fact]
public void ForIOS_DisablesConsoleAndUsesDurableAsyncLogs()
{
    var options = LoggerOptions.ForIOS("logs");

    Assert.False(options.ConsoleLogging);
    Assert.True(options.AsyncLogging);
    Assert.True(options.AsyncOnly);
    Assert.True(options.AsyncBinaryLogging);
    Assert.Equal("ios", options.SessionName);
}
```

- [ ] **Step 2: Run tests to verify failure**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter "ForMacOS|ForIOS"
```

Expected: compile failure.

- [ ] **Step 3: Implement profiles**

Add to `LoggerOptions`:

```csharp
public static LoggerOptions ForMacOS(string logDirectory = "logs") => ForEngine(logDirectory)
    .WithConsole(true)
    .WithAnsiColor(true)
    .WithSession("macos", autoId: true);

public static LoggerOptions ForIOS(string logDirectory = "logs") => ForEngine(logDirectory)
    .WithConsole(false)
    .WithSession("ios", autoId: true);
```

- [ ] **Step 4: Run profile tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release --filter ApplePlatformProfileTests
```

Expected: pass.

- [ ] **Step 5: Commit**

```powershell
git add QuickLog/LoggerOptions.cs QuickLog.Tests/ApplePlatformProfileTests.cs
git commit -m "feat: add iOS and macOS logger profiles"
```

---

### Task 9: Add iOS And macOS Smoke Projects

**Files:**
- Create: `samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj`
- Create: `samples/QuickLog.IosSmoke/Program.cs`
- Create: `samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj`
- Create: `samples/QuickLog.MacSmoke/Program.cs`
- Modify: `QuickLog.sln`

- [ ] **Step 1: Create iOS smoke project**

Create `samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0-ios</TargetFramework>
    <SupportedOSPlatformVersion>13.0</SupportedOSPlatformVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\QuickLog\QuickLog.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create iOS smoke program**

Create `samples/QuickLog.IosSmoke/Program.cs`:

```csharp
using QuickLog;
using QuickLog.Core;
using QuickLog.Loggers;

var root = args.Length > 0
    ? args[0]
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "QuickLogIosSmoke");

Directory.CreateDirectory(root);

LogManager.ConfigureDefault(LoggerOptions.ForIOS(root));
var logger = (QuickLogger)LogManager.GetDefaultLogger();

LogStateSnapshot.Set("platform", "ios");
logger.Log(LogType.Info, "iOS smoke started");
logger.LogFrameTime(1, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(16));
logger.Shutdown();
LogManager.Shutdown();
```

- [ ] **Step 3: Create macOS smoke project**

Create `samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\QuickLog\QuickLog.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Create macOS smoke program**

Create `samples/QuickLog.MacSmoke/Program.cs`:

```csharp
using QuickLog;
using QuickLog.Core;
using QuickLog.Loggers;
using QuickLog.Utilities;

var root = args.Length > 0
    ? args[0]
    : Path.Combine(Path.GetTempPath(), "QuickLogMacSmoke");

Directory.CreateDirectory(root);

LogManager.ConfigureDefault(LoggerOptions.ForMacOS(root));
var logger = (QuickLogger)LogManager.GetDefaultLogger();

LogStateSnapshot.Set("platform", "macos");
logger.Log(LogType.Info, "macOS smoke started");
logger.LogOnce("mac-smoke", LogType.Info, "logged once");
logger.Shutdown();

var qlog = Path.Combine(root, "quicklog.qlog");
Console.WriteLine(BinaryLogSummary.FromFile(qlog).EntryCount);
LogManager.Shutdown();
```

- [ ] **Step 5: Add projects to solution**

Run:

```powershell
dotnet sln QuickLog.sln add samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj
dotnet sln QuickLog.sln add samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj
```

Expected: both projects are added.

- [ ] **Step 6: Build macOS smoke locally**

Run:

```powershell
dotnet build samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj -c Release
```

Expected: pass with 0 warnings.

- [ ] **Step 7: Commit**

```powershell
git add QuickLog.sln samples/QuickLog.IosSmoke samples/QuickLog.MacSmoke
git commit -m "test: add Apple smoke projects"
```

---

### Task 10: Add macOS And iOS CI Gates

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/release.yml`

- [ ] **Step 1: Add macOS test job**

Add to `.github/workflows/ci.yml`:

```yaml
  macos-test:
    name: macOS Test
    runs-on: macos-latest
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

      - name: Run macOS smoke
        run: dotnet run --project samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj --configuration Release -- "$RUNNER_TEMP/quicklog-mac-smoke"
```

- [ ] **Step 2: Add iOS build job**

Add to `.github/workflows/ci.yml`:

```yaml
  ios-build:
    name: iOS Build
    runs-on: macos-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Install iOS workload
        run: dotnet workload install ios

      - name: Restore iOS Smoke
        run: dotnet restore samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj

      - name: Build iOS Smoke
        run: dotnet build samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj --configuration Release --no-restore -p:RuntimeIdentifier=iossimulator-x64
```

- [ ] **Step 3: Update release workflow**

In `.github/workflows/release.yml`, add after setup-dotnet:

```yaml
      - name: Install iOS workload
        run: dotnet workload install ios
```

Run the release job on `macos-latest` if packing `net10.0-ios` fails on `windows-latest`. The preferred final release job header is:

```yaml
  release:
    name: Release
    needs: test
    runs-on: macos-latest
```

- [ ] **Step 4: Commit**

```powershell
git add .github/workflows/ci.yml .github/workflows/release.yml
git commit -m "ci: verify macOS tests and iOS build"
```

---

### Task 11: Documentation And Versioning

**Files:**
- Modify: `QuickLog/QuickLog.csproj`
- Modify: `README.md`
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Bump package metadata for Apple preview**

Change in `QuickLog/QuickLog.csproj`:

```xml
<Version>3.0.0-ios-osx.1</Version>
<AssemblyVersion>3.0.0.0</AssemblyVersion>
<FileVersion>3.0.0.0</FileVersion>
<PackageVersion>3.0.0-ios-osx.1</PackageVersion>
<PackageReleaseNotes>QuickLog 3.0 preview adds verified iOS and macOS support, Apple-safe writable paths, iOS/macOS profiles, iOS AOT guards, and macOS CI while keeping runtime projects dependency-free.</PackageReleaseNotes>
```

- [ ] **Step 2: Update README platform section**

Add:

```markdown
## Apple Platform Support

QuickLog supports macOS through the portable `net8.0` and `net10.0` targets and iOS through `net10.0-ios`.

| Platform | Status | Notes |
|---|---|---|
| macOS / OSX | Supported | Verified by CI on `macos-latest` |
| iOS | Preview supported | Build verified through `net10.0-ios`; process restart and dynamic IL emission are disabled |

Use `LoggerOptions.ForMacOS("logs")` for desktop Mac apps.
Use `LoggerOptions.ForIOS("logs")` for iOS apps where console output should be quiet and file-backed logs should land in writable app-local storage.

`QLogDiscovery.Scan(Assembly)` is a reflection-heavy diagnostic helper. In trimmed/AOT iOS apps, prefer scanning explicit types with `QLogDiscovery.Scan(typeof(MyType))`.
```

- [ ] **Step 3: Update changelog**

Add:

```markdown
## [3.0.0-ios-osx.1] - Unreleased

### Added

- Added iOS target-framework build support.
- Added macOS CI verification and smoke app.
- Added iOS smoke build project.
- Added Apple platform capability checks for restart and dynamic code.
- Added iOS/macOS logger profiles.

### Changed

- Isolated Reflection.Emit-based Godot dynamic registration behind platform-specific code.
- Documented QLOG discovery trimming behavior for AOT-sensitive apps.

### Notes

- Runtime projects remain dependency-free.
- iOS process restart is intentionally unsupported.
```

- [ ] **Step 4: Commit**

```powershell
git add QuickLog/QuickLog.csproj README.md CHANGELOG.md
git commit -m "docs: document iOS and OSX support preview"
```

---

### Task 12: Full Apple Verification

**Files:**
- No planned source changes.

- [ ] **Step 1: Check dependency policy**

Run:

```powershell
rg -n "<PackageReference" QuickLog QuickLog.Tools QuickLog.Sample samples/QuickLog.IosSmoke samples/QuickLog.MacSmoke
```

Expected: no output.

- [ ] **Step 2: Build portable frameworks**

Run:

```powershell
dotnet build QuickLog/QuickLog.csproj -c Release -f net8.0
dotnet build QuickLog/QuickLog.csproj -c Release -f net10.0
```

Expected: both pass with 0 warnings.

- [ ] **Step 3: Run tests**

Run:

```powershell
dotnet test QuickLog.Tests/QuickLog.Tests.csproj -c Release
```

Expected: all tests pass.

- [ ] **Step 4: Build macOS smoke on macOS**

Run on a macOS runner or local Mac:

```bash
dotnet build samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj -c Release
dotnet run --project samples/QuickLog.MacSmoke/QuickLog.MacSmoke.csproj -c Release -- "$TMPDIR/quicklog-mac-smoke"
```

Expected: build passes and smoke prints an entry count greater than zero.

- [ ] **Step 5: Build iOS smoke on macOS**

Run on a macOS runner with Xcode:

```bash
dotnet workload install ios
dotnet build samples/QuickLog.IosSmoke/QuickLog.IosSmoke.csproj -c Release -p:RuntimeIdentifier=iossimulator-x64
```

Expected: iOS app builds. Running on a simulator is optional for this preview branch, but build success is required before release.

- [ ] **Step 6: Pack**

Run on a macOS runner if local Windows cannot pack iOS assets:

```bash
dotnet pack QuickLog/QuickLog.csproj -c Release --no-build -o artifacts/packages
```

Expected: package includes `lib/net8.0`, `lib/net10.0`, and `lib/net10.0-ios`.

- [ ] **Step 7: Confirm clean tree**

Run:

```bash
git status --short
```

Expected: no tracked source changes. Do not commit generated `artifacts`, `bin`, `obj`, or smoke logs.

---

## Release Criteria

- macOS CI job passes on `macos-latest`.
- iOS build job passes on `macos-latest`.
- Full Windows portable build remains 0-warning.
- Runtime projects still have no `PackageReference`.
- NuGet package includes the iOS TFM assembly.
- README states iOS limitations clearly: no process restart, no dynamic IL emission, and explicit QLOG discovery is preferred for trimmed/AOT apps.

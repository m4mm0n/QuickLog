using QuickLog.Platform;
using Xunit;

namespace QuickLog.Tests;

/// <summary>
/// Verifies the explicit Linux support surface and release workflow coverage.
/// </summary>
public sealed class LinuxSupportTests
{
    /// <summary>
    /// Verifies that Linux has desktop/server-friendly capabilities without claiming native popup support.
    /// </summary>
    [Fact]
    public void LinuxCapabilities_AreDesktopAndServerSafe()
    {
        var capabilities = QuickLogPlatform.GetCapabilities(QuickLogPlatformKind.Linux);

        Assert.True(capabilities.SupportsInteractiveConsole);
        Assert.True(capabilities.SupportsProcessRestart);
        Assert.False(capabilities.SupportsNativePopup);
        Assert.True(capabilities.UsesXdgDirectories);
    }

    /// <summary>
    /// Verifies that Linux log paths prefer the XDG state directory when it is supplied.
    /// </summary>
    [Fact]
    public void LinuxLogDirectory_UsesXdgStateHomeWhenAvailable()
    {
        var directory = QuickLogPathResolver.GetLinuxLogDirectory(
            applicationName: "QuickLog.Tests",
            stateHome: "/tmp/xdg-state",
            homeDirectory: "/home/tester");

        Assert.Equal(Path.Combine("/tmp/xdg-state", "QuickLog.Tests", "logs"), directory);
    }

    /// <summary>
    /// Verifies that Linux log paths fall back to the standard per-user state directory.
    /// </summary>
    [Fact]
    public void LinuxLogDirectory_FallsBackToLocalStateUnderHome()
    {
        var directory = QuickLogPathResolver.GetLinuxLogDirectory(
            applicationName: "QuickLog.Tests",
            stateHome: "",
            homeDirectory: "/home/tester");

        Assert.Equal(Path.Combine("/home/tester", ".local", "state", "QuickLog.Tests", "logs"), directory);
    }

    /// <summary>
    /// Verifies that the Linux profile produces durable async JSON and QLOG sinks.
    /// </summary>
    [Fact]
    public void ForLinux_ConfiguresXdgDurableAsyncProfile()
    {
        var options = LoggerOptions.ForLinux(
            applicationName: "QuickLog.Tests",
            stateHome: "/tmp/xdg-state",
            homeDirectory: "/home/tester");

        var expectedRoot = Path.Combine("/tmp/xdg-state", "QuickLog.Tests", "logs");
        Assert.False(options.ConsoleLogging);
        Assert.True(options.AsyncOnly);
        Assert.True(options.AsyncBinaryLogging);
        Assert.Equal(Path.Combine(expectedRoot, "quicklog.jsonl"), options.JsonLogPath);
        Assert.Equal(Path.Combine(expectedRoot, "quicklog.qlog"), options.BinaryLogPath);
        Assert.Equal("linux", options.SessionName);
    }

    /// <summary>
    /// Verifies that the Linux smoke project is part of the solution and remains dependency-free.
    /// </summary>
    [Fact]
    public void LinuxSmokeProject_IsInSolutionAndHasNoPackageReferences()
    {
        var root = FindRepoRoot();
        var solution = File.ReadAllText(Path.Combine(root, "QuickLog.sln"));
        var projectPath = Path.Combine(root, "samples", "QuickLog.LinuxSmoke", "QuickLog.LinuxSmoke.csproj");

        Assert.Contains("samples\\QuickLog.LinuxSmoke\\QuickLog.LinuxSmoke.csproj", solution);
        Assert.True(File.Exists(projectPath), $"Missing Linux smoke project: {projectPath}");
        Assert.DoesNotContain("<PackageReference", File.ReadAllText(projectPath), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that CI runs the core build and tests on Ubuntu.
    /// </summary>
    [Fact]
    public void CiWorkflow_IncludesUbuntuValidation()
    {
        var root = FindRepoRoot();
        var ci = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));

        Assert.Contains("ubuntu-latest", ci);
        Assert.Contains("QuickLog.LinuxSmoke", ci);
    }

    /// <summary>
    /// Verifies that the release workflow validates Linux before publishing packages.
    /// </summary>
    [Fact]
    public void ReleaseWorkflow_IncludesUbuntuValidation()
    {
        var root = FindRepoRoot();
        var release = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));

        Assert.Contains("ubuntu-latest", release);
        Assert.Contains("QuickLog.LinuxSmoke", release);
        Assert.Contains("QuickLog v${{ env.VERSION }}", release);
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

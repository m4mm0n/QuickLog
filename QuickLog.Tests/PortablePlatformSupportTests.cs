using QuickLog.Exceptions;
using QuickLog.Godot;
using QuickLog.Platform;
using Xunit;

namespace QuickLog.Tests;

public sealed class PortablePlatformSupportTests
{
    [Fact]
    public void MacOSCapabilities_AreDesktopSafe()
    {
        var capabilities = QuickLogPlatform.GetCapabilities(QuickLogPlatformKind.MacOS);

        Assert.True(capabilities.SupportsInteractiveConsole);
        Assert.True(capabilities.SupportsProcessRestart);
        Assert.False(capabilities.SupportsNativePopup);
        Assert.False(capabilities.UsesXdgDirectories);
    }

    [Theory]
    [InlineData(QuickLogPlatformKind.Android)]
    [InlineData(QuickLogPlatformKind.IOS)]
    public void MobileCapabilities_DisableDesktopOnlyBehavior(QuickLogPlatformKind kind)
    {
        var capabilities = QuickLogPlatform.GetCapabilities(kind);

        Assert.False(capabilities.SupportsInteractiveConsole);
        Assert.False(capabilities.SupportsProcessRestart);
        Assert.False(capabilities.SupportsNativePopup);
        Assert.False(RestartOptions.SupportsAutoRestart(kind));
    }

    [Fact]
    public void MacOSLogDirectory_UsesLibraryLogsBelowHome()
    {
        var directory = QuickLogPathResolver.GetMacOSLogDirectory(
            applicationName: "QuickLog.Tests",
            homeDirectory: "/Users/tester");

        Assert.Equal(Path.Combine("/Users/tester", "Library", "Logs", "QuickLog.Tests"), directory);
    }

    [Fact]
    public void MobileLogDirectories_UseApplicationLocalData()
    {
        const string localData = "/app/local-data";
        var expected = Path.Combine(localData, "QuickLog.Tests", "logs");

        Assert.Equal(expected, QuickLogPathResolver.GetAndroidLogDirectory("QuickLog.Tests", localData));
        Assert.Equal(expected, QuickLogPathResolver.GetIOSLogDirectory("QuickLog.Tests", localData));
    }

    [Theory]
    [InlineData("macos")]
    [InlineData("android")]
    [InlineData("ios")]
    public void PortableProfiles_AreDurableAsyncAndConsoleFree(string platform)
    {
        var root = Path.Combine(Path.GetTempPath(), "quicklog-portable-tests", platform);
        var options = platform switch
        {
            "macos" => LoggerOptions.ForMacOS(logDirectory: root),
            "android" => LoggerOptions.ForAndroid(logDirectory: root),
            "ios" => LoggerOptions.ForIOS(logDirectory: root),
            _ => throw new ArgumentOutOfRangeException(nameof(platform))
        };

        Assert.False(options.ConsoleLogging);
        Assert.True(options.AsyncOnly);
        Assert.True(options.AsyncBinaryLogging);
        Assert.Equal(Path.Combine(root, "quicklog.jsonl"), options.JsonLogPath);
        Assert.Equal(Path.Combine(root, "quicklog.qlog"), options.BinaryLogPath);
        Assert.Equal(platform, options.SessionName);
        Assert.True(options.Validate().IsValid);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void GodotDynamicRegistration_RequiresSupportedCompiledDynamicCode(
        bool supported,
        bool compiled,
        bool expected)
    {
        Assert.Equal(expected, GodotLogInterceptor.CanEmitDynamicSink(supported, compiled));
    }

    [Fact]
    public void ContinuousIntegration_IncludesMacOSValidation()
    {
        var root = FindRepoRoot();
        var ci = File.ReadAllText(Path.Combine(root, ".github", "workflows", "ci.yml"));
        var release = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));

        Assert.Contains("macos-latest", ci, StringComparison.Ordinal);
        Assert.Contains("macos-latest", release, StringComparison.Ordinal);
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

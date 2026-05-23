using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

/// <summary>
/// Exercises the Linux smoke app as CI and package users run it.
/// </summary>
public sealed class LinuxSmokeUsageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_linux_smoke_{Guid.NewGuid():N}");

    /// <summary>
    /// Creates an isolated smoke output directory.
    /// </summary>
    public LinuxSmokeUsageTests()
    {
        Directory.CreateDirectory(_dir);
    }

    /// <summary>
    /// Verifies that repeated smoke runs replace their advertised outputs.
    /// </summary>
    [Fact]
    public async Task LinuxSmokeRun_ReplacesPreviousJsonAndBinaryOutputs()
    {
        var jsonPath = Path.Combine(_dir, "quicklog.jsonl");
        await File.WriteAllTextAsync(jsonPath, "stale-jsonl-marker" + Environment.NewLine);

        var result = await RunSmokeAsync();

        Assert.True(result.ExitCode == 0, result.StandardError);
        Assert.DoesNotContain("stale-jsonl-marker", await File.ReadAllTextAsync(jsonPath));
        Assert.Equal(5, BinaryLogReader.Read(Path.Combine(_dir, "quicklog.qlog"), stopOnCrcError: false).Count());
        Assert.Contains("QuickLog Linux smoke", result.StandardOutput);
    }

    private async Task<ProcessResult> RunSmokeAsync()
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo("dotnet", $"\"{GetSmokeAssemblyPath()}\" \"{_dir}\"")
        {
            WorkingDirectory = _dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        Assert.NotNull(process);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, output, error);
    }

    private static string GetSmokeAssemblyPath()
    {
        var root = FindRepoRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var assembly = Path.Combine(root, "samples", "QuickLog.LinuxSmoke", "bin", configuration, "net10.0", "QuickLog.LinuxSmoke.dll");
        Assert.True(File.Exists(assembly), $"Linux smoke assembly was not found: {assembly}");
        return assembly;
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }

    /// <summary>
    /// Removes the isolated smoke output directory.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

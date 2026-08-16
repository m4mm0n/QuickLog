using System.Diagnostics;
using Xunit;

namespace QuickLog.Tests;

/// <summary>
/// Exercises the published sample as a consumer would run it from a clean working directory.
/// </summary>
public sealed class SampleUsageTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_sample_{Guid.NewGuid():N}");

    /// <summary>
    /// Creates the isolated sample working directory.
    /// </summary>
    public SampleUsageTests()
    {
        Directory.CreateDirectory(_dir);
    }

    /// <summary>
    /// Verifies that sample reruns replace every advertised output rather than mixing stale and fresh entries.
    /// </summary>
    [Fact]
    public async Task SampleRun_ReplacesJsonLinesOutputFromPreviousRuns()
    {
        var logDirectory = Path.Combine(_dir, "logs");
        Directory.CreateDirectory(logDirectory);
        var jsonPath = Path.Combine(logDirectory, "quicklog.jsonl");
        await File.WriteAllTextAsync(jsonPath, "stale-jsonl-marker" + Environment.NewLine);

        var result = await RunSampleAsync();

        Assert.True(result.ExitCode == 0, result.StandardError);
        Assert.DoesNotContain("stale-jsonl-marker", await File.ReadAllTextAsync(jsonPath));
        Assert.True(File.Exists(Path.Combine(logDirectory, "quicklog.qlog")));
        Assert.Contains("QuickLog v3.0 sample", result.StandardOutput);
    }

    /// <summary>
    /// Runs the compiled sample application and captures its process output.
    /// </summary>
    private async Task<ProcessResult> RunSampleAsync()
    {
        var startInfo = new ProcessStartInfo("dotnet", Quote(GetSampleAssemblyPath()))
        {
            WorkingDirectory = _dir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ProcessResult(process.ExitCode, output, error);
    }

    /// <summary>
    /// Locates the sample assembly built for the same configuration as the active test run.
    /// </summary>
    private static string GetSampleAssemblyPath()
    {
        var root = FindRepoRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var sampleAssembly = Path.Combine(root, "QuickLog.Sample", "bin", configuration, "net10.0", "QuickLog.Sample.dll");
        Assert.True(File.Exists(sampleAssembly), $"Sample assembly was not found: {sampleAssembly}");
        return sampleAssembly;
    }

    /// <summary>
    /// Quotes a command-line value for use in a single process argument string.
    /// </summary>
    private static string Quote(string value) => $"\"{value}\"";

    /// <summary>
    /// Finds the repository root from the test assembly output directory.
    /// </summary>
    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "QuickLog.sln")))
            dir = Directory.GetParent(dir)?.FullName;

        Assert.NotNull(dir);
        return dir!;
    }

    /// <summary>
    /// Removes the isolated sample working directory.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    /// <summary>
    /// Captured output from a sample process run.
    /// </summary>
    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

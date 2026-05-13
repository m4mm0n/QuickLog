using System.IO.Compression;
using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Tools;
using QuickLog.Tools.Commands;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolBundleBenchmarkTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_tools_bundle_{Guid.NewGuid():N}");

    public ToolBundleBenchmarkTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Bundle_CreatesZipWithManifestAndBinaryTextExport()
    {
        var logs = Path.Combine(_dir, "logs");
        var crashes = Path.Combine(_dir, "crashes");
        Directory.CreateDirectory(logs);
        Directory.CreateDirectory(crashes);

        var qlog = Path.Combine(logs, "app.qlog");
        using (var sink = new BinaryLogSink(qlog))
        {
            var entry = new LogEntry(DateTime.UtcNow, LogType.Error, "bundle me",
                "L", "ScopeBundle", "M", "f.cs", 4, 1, ThreadRole.Unknown, "corr-bundle");
            sink.Write(in entry);
        }

        File.WriteAllText(Path.Combine(crashes, "crash.json"), """{"Timestamp":"2026-05-13T00:00:00Z","Source":"Test","Exception":{}}""");

        var zipPath = Path.Combine(_dir, "support.zip");
        var console = new BufferToolConsole();

        var result = await BundleCommand.ExecuteAsync(
            new BundleToolCommand(zipPath, logs, crashes, IncludeEnv: true, IncludeExports: true, MaxFileBytes: null, Redact: false),
            console);

        Assert.True(result.Success, console.ErrorText);
        Assert.True(File.Exists(zipPath));

        using var zip = ZipFile.OpenRead(zipPath);
        Assert.Contains(zip.Entries, e => e.FullName == "manifest.json");
        Assert.Contains(zip.Entries, e => e.FullName == "logs/app.qlog");
        Assert.Contains(zip.Entries, e => e.FullName == "crashes/crash.json");
        Assert.Contains(zip.Entries, e => e.FullName == "exports/app.qlog.txt");
        Assert.Contains("Wrote", console.OutputText);
    }

    [Fact]
    public async Task Benchmark_BinaryMode_CompletesSmallRun()
    {
        var console = new BufferToolConsole();

        var result = await BenchmarkCommand.ExecuteAsync(
            new BenchmarkToolCommand(10, "binary"),
            console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("Mode: binary", console.OutputText);
        Assert.Contains("logs/sec", console.OutputText);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

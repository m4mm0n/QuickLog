using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Tools;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolMaintenanceCommandTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_maint_{Guid.NewGuid():N}");

    public ToolMaintenanceCommandTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Redact_WritesMaskedTextLog()
    {
        var input = Path.Combine(_dir, "input.log");
        var output = Path.Combine(_dir, "clean.log");
        File.WriteAllText(input, "api_key=secret");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["redact", input, "--out", output], console);

        Assert.Equal(0, code);
        Assert.Equal("api_key=***", File.ReadAllText(output));
    }

    [Fact]
    public async Task Repair_WritesReadableBinaryLog()
    {
        var input = Path.Combine(_dir, "bad.qlog");
        var output = Path.Combine(_dir, "fixed.qlog");
        File.WriteAllBytes(input, [0, 1, 2, 3, 4]);
        using (var sink = new BinaryLogSink(input))
            sink.Write(Entry(DateTime.UtcNow, LogType.Error, "after garbage"));
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["repair", input, "--out", output], console);

        Assert.Equal(0, code);
        Assert.Contains(BinaryLogReader.Read(output), e => e.Message == "after garbage");
    }

    [Fact]
    public async Task Merge_WritesEntriesSortedByTimestamp()
    {
        var later = DateTime.UtcNow.AddSeconds(5);
        var earlier = DateTime.UtcNow;
        var first = CreateBinaryLog("a.qlog", Entry(later, LogType.Info, "later"));
        var second = CreateBinaryLog("b.qlog", Entry(earlier, LogType.Info, "earlier"));
        var output = Path.Combine(_dir, "merged.qlog");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["merge", first, second, "--out", output], console);

        Assert.Equal(0, code);
        Assert.Equal(["earlier", "later"], BinaryLogReader.Read(output).Select(e => e.Message).ToArray());
    }

    [Fact]
    public async Task Timeline_PrintsNonInteractiveSummary()
    {
        var path = CreateBinaryLog("timeline.qlog", Entry(DateTime.UtcNow, LogType.Warn, "marker"));
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["timeline", path], console);

        Assert.Equal(0, code);
        Assert.Contains("Entries: 1", console.OutputText);
        Assert.Contains("Warn: 1", console.OutputText);
    }

    [Fact]
    public async Task DoctorConfig_ReportsValidationErrors()
    {
        var path = Path.Combine(_dir, "config.json");
        File.WriteAllText(path, "{\"AsyncOnly\":true}");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["doctor-config", path], console);

        Assert.Equal(1, code);
        Assert.Contains("QL001", console.OutputText);
    }

    private string CreateBinaryLog(string fileName, params LogEntry[] entries)
    {
        var path = Path.Combine(_dir, fileName);
        using var sink = new BinaryLogSink(path);
        foreach (var entry in entries)
            sink.Write(in entry);
        return path;
    }

    private static LogEntry Entry(DateTime timestamp, LogType level, string message)
        => new(timestamp, level, message, "test", null, "M", "", 0, 1, ThreadRole.Main);

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

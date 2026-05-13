using System.Text.Json;
using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Tools;
using QuickLog.Tools.Commands;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolLogCommandTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_tools_{Guid.NewGuid():N}");

    public ToolLogCommandTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Doctor_ReportsValidBinaryLogEntries()
    {
        var path = CreateBinaryLog("valid.qlog",
            new LogEntry(DateTime.UtcNow, LogType.Info, "hello", "L", "ScopeA", "M", "f.cs", 1, 1, ThreadRole.Unknown, "corr-1"));
        var console = new BufferToolConsole();

        var result = await DoctorCommand.ExecuteAsync(new DoctorToolCommand(path, Recursive: false), console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("valid.qlog", console.OutputText);
        Assert.Contains("Entries: 1", console.OutputText);
        Assert.Contains("Info: 1", console.OutputText);
    }

    [Fact]
    public async Task Doctor_FailsForCorruptedBinaryLog()
    {
        var path = CreateBinaryLog("corrupt.qlog",
            new LogEntry(DateTime.UtcNow, LogType.Info, "hello", "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown));
        var data = File.ReadAllBytes(path);
        data[^1] ^= 0xFF;
        File.WriteAllBytes(path, data);
        var console = new BufferToolConsole();

        var result = await DoctorCommand.ExecuteAsync(new DoctorToolCommand(path, Recursive: false), console);

        Assert.False(result.Success);
        Assert.Contains("invalid or corrupted", console.ErrorText);
    }

    [Fact]
    public async Task Inspect_ReportsLevelCountsAndCorrelationMatches()
    {
        var path = CreateBinaryLog("inspect.qlog",
            new LogEntry(DateTime.UtcNow, LogType.Info, "first", "L", "ScopeA", "M", "f.cs", 1, 1, ThreadRole.Unknown, "corr-a"),
            new LogEntry(DateTime.UtcNow, LogType.Error, "needle", "L", "ScopeB", "M", "f.cs", 2, 1, ThreadRole.Unknown, "corr-b"));
        var console = new BufferToolConsole();

        var result = await InspectCommand.ExecuteAsync(
            new InspectToolCommand(path, "Error", "needle", "corr-b", null, null, null),
            console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("Entries: 1", console.OutputText);
        Assert.Contains("Error: 1", console.OutputText);
        Assert.Contains("corr-b", console.OutputText);
        Assert.DoesNotContain("corr-a", console.OutputText);
    }

    [Fact]
    public async Task Replay_Text_WritesContextAwareTextExport()
    {
        var path = CreateBinaryLog("replay-text.qlog",
            new LogEntry(DateTime.UtcNow, LogType.Warn, "export me", "L", "ScopeText", "M", "f.cs", 7, 1, ThreadRole.Unknown, "corr-text"));
        var output = Path.Combine(_dir, "replay.txt");
        var console = new BufferToolConsole();

        var result = await ReplayCommand.ExecuteAsync(new ReplayToolCommand(path, "text", output), console);

        Assert.True(result.Success, console.ErrorText);
        var text = File.ReadAllText(output);
        Assert.Contains("export me", text);
        Assert.Contains("ScopeText", text);
        Assert.Contains("corr-text", text);
    }

    [Fact]
    public async Task Replay_JsonLines_WritesJsonPerEntry()
    {
        var path = CreateBinaryLog("replay-json.qlog",
            new LogEntry(DateTime.UtcNow, LogType.Crit, "json me", "L", "ScopeJson", "M", "f.cs", 9, 1, ThreadRole.Unknown, "corr-json"));
        var output = Path.Combine(_dir, "replay.jsonl");
        var console = new BufferToolConsole();

        var result = await ReplayCommand.ExecuteAsync(new ReplayToolCommand(path, "jsonl", output), console);

        Assert.True(result.Success, console.ErrorText);
        var line = File.ReadLines(output).Single();
        using var doc = JsonDocument.Parse(line);
        Assert.Equal("Crit", doc.RootElement.GetProperty("level").GetString());
        Assert.Equal("json me", doc.RootElement.GetProperty("message").GetString());
        Assert.Equal("corr-json", doc.RootElement.GetProperty("correlation").GetString());
    }

    private string CreateBinaryLog(string fileName, params LogEntry[] entries)
    {
        var path = Path.Combine(_dir, fileName);
        using var sink = new BinaryLogSink(path);
        foreach (var entry in entries)
            sink.Write(in entry);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

using System.Text.Json;
using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Tools;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolInspectionCommandTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_inspect_{Guid.NewGuid():N}");

    public ToolInspectionCommandTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Stats_PrintsLevelCountsForBinaryLog()
    {
        var path = CreateBinaryLog("stats.qlog",
            Entry(DateTime.UtcNow, LogType.Error, "boom"),
            Entry(DateTime.UtcNow.AddMilliseconds(1), LogType.Warn, "careful"));
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["stats", path], console);

        Assert.Equal(0, code);
        Assert.Contains("Entries: 2", console.OutputText);
        Assert.Contains("Error: 1", console.OutputText);
        Assert.Contains("Warn: 1", console.OutputText);
    }

    [Fact]
    public async Task Grep_FindsTextInBinaryJsonLinesAndPlainTextLogs()
    {
        var binary = CreateBinaryLog("grep.qlog", Entry(DateTime.UtcNow, LogType.Error, "binary boom"));
        var json = Path.Combine(_dir, "grep.jsonl");
        File.WriteAllText(json, "{\"ts\":\"2026-01-01T00:00:00.0000000Z\",\"level\":\"Info\",\"msg\":\"json boom\"}");
        var text = Path.Combine(_dir, "grep.log");
        File.WriteAllText(text, "plain boom");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["grep", "boom", _dir, "--recursive"], console);

        Assert.Equal(0, code);
        Assert.Contains(Path.GetFileName(binary), console.OutputText);
        Assert.Contains(Path.GetFileName(json), console.OutputText);
        Assert.Contains(Path.GetFileName(text), console.OutputText);
    }

    [Fact]
    public async Task Diff_PrintsMessagesPresentOnlyInRightLog()
    {
        var left = CreateBinaryLog("left.qlog", Entry(DateTime.UtcNow, LogType.Info, "same"));
        var right = CreateBinaryLog("right.qlog",
            Entry(DateTime.UtcNow, LogType.Info, "same"),
            Entry(DateTime.UtcNow.AddMilliseconds(1), LogType.Error, "new failure"));
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["diff", left, right], console);

        Assert.Equal(0, code);
        Assert.Contains("new failure", console.OutputText);
        Assert.DoesNotContain("same", console.OutputText);
    }

    [Fact]
    public async Task Summarize_WritesJsonSummaryWithCounts()
    {
        var path = CreateBinaryLog("summary.qlog",
            Entry(DateTime.UtcNow, LogType.Error, "boom"),
            Entry(DateTime.UtcNow.AddMilliseconds(1), LogType.Error, "boom"));
        var output = Path.Combine(_dir, "summary.json");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["summarize", path, "--out", output], console);

        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(File.ReadAllText(output));
        Assert.Equal(2, doc.RootElement.GetProperty("entryCount").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("levelCounts").GetProperty("Error").GetInt32());
        Assert.Equal("boom", doc.RootElement.GetProperty("topMessages")[0].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Tail_PrintsLastRequestedTextLines()
    {
        var path = Path.Combine(_dir, "tail.log");
        File.WriteAllLines(path, ["first", "second", "third"]);
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["tail", path, "--lines", "1"], console);

        Assert.Equal(0, code);
        Assert.Equal("third", Assert.Single(console.Output));
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

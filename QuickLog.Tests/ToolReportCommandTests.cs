using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Tools;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolReportCommandTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_report_{Guid.NewGuid():N}");

    public ToolReportCommandTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Report_WritesSingleStaticHtmlFile()
    {
        var logs = Path.Combine(_dir, "logs");
        Directory.CreateDirectory(logs);
        var qlog = Path.Combine(logs, "app.qlog");

        using (var sink = new BinaryLogSink(qlog))
            sink.Write(new LogEntry(DateTime.UtcNow, LogType.Error, "boom", "test", null, "m", "", 0, 1, ThreadRole.Main));

        var html = Path.Combine(_dir, "report.html");
        var console = new BufferToolConsole();

        var code = await ToolApplication.RunAsync(["report", "--out", html, "--logs", logs], console);

        Assert.Equal(0, code);
        var text = File.ReadAllText(html);
        Assert.Contains("<!doctype html>", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("boom", text);
        Assert.DoesNotContain("https://", text);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

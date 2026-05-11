using System.Text.Json;
using QuickLog.Core;
using QuickLog.Sinks;
using Xunit;

namespace QuickLog.Tests;

public sealed class JsonLinesSinkTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"ql_json_{Guid.NewGuid():N}.jsonl");

    [Fact]
    public void Write_ProducesOneLinePerEntry()
    {
        using (var sink = new JsonLinesSink(_path))
        {
            var e1 = MakeEntry(LogType.Info, "first");
            var e2 = MakeEntry(LogType.Warn, "second");
            sink.Write(in e1);
            sink.Write(in e2);
        }

        Assert.Equal(2, File.ReadAllLines(_path).Length);
    }

    [Fact]
    public void Write_EachLine_IsValidJson()
    {
        using (var sink = new JsonLinesSink(_path))
        {
            var e = MakeEntry(LogType.Error, "json check");
            sink.Write(in e);
        }

        var line = File.ReadAllLines(_path)[0];
        var doc = JsonDocument.Parse(line); // throws if invalid
        Assert.NotNull(doc);
    }

    [Fact]
    public void Write_ContainsExpectedFields()
    {
        var entry = new LogEntry(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LogType.Warn, "field check",
            "TestLogger", "MyScope", "MyMethod", "test.cs", 99,
            42, ThreadRole.Render);

        using (var sink = new JsonLinesSink(_path))
            sink.Write(in entry);

        using var doc = JsonDocument.Parse(File.ReadAllLines(_path)[0]);
        var root = doc.RootElement;
        Assert.Equal("Warn",       root.GetProperty("level").GetString());
        Assert.Equal("field check",root.GetProperty("msg").GetString());
        Assert.Equal("MyMethod",   root.GetProperty("member").GetString());
        Assert.Equal(99,           root.GetProperty("line").GetInt32());
        Assert.Equal(42,           root.GetProperty("thread").GetInt32());
        Assert.Equal("Render",     root.GetProperty("role").GetString());
        Assert.Equal("MyScope",    root.GetProperty("scope").GetString());
    }

    [Fact]
    public void Write_NullScope_OmittedFromJson()
    {
        using (var sink = new JsonLinesSink(_path))
        {
            var e = MakeEntry(LogType.Debug, "no scope");
            sink.Write(in e);
        }

        using var doc = JsonDocument.Parse(File.ReadAllLines(_path)[0]);
        Assert.False(doc.RootElement.TryGetProperty("scope", out _));
    }

    [Fact]
    public void Write_TimestampIsIso8601Utc()
    {
        using (var sink = new JsonLinesSink(_path))
        {
            var e = MakeEntry(LogType.Info, "ts test");
            sink.Write(in e);
        }

        using var doc = JsonDocument.Parse(File.ReadAllLines(_path)[0]);
        var ts = doc.RootElement.GetProperty("ts").GetString()!;
        Assert.True(DateTime.TryParse(ts, out _));
        Assert.EndsWith("Z", ts);
    }

    [Fact]
    public void Append_PreservesExistingContent()
    {
        using (var sink = new JsonLinesSink(_path))
        {
            var e1 = MakeEntry(LogType.Info, "first session");
            sink.Write(in e1);
        }

        using (var sink = new JsonLinesSink(_path))
        {
            var e2 = MakeEntry(LogType.Info, "second session");
            sink.Write(in e2);
        }

        Assert.Equal(2, File.ReadAllLines(_path).Length);
    }

    private static LogEntry MakeEntry(LogType level, string msg) =>
        new(DateTime.UtcNow, level, msg, "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}

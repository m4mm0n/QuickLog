using System.Diagnostics;
using QuickLog.Core;
using QuickLog.Sinks;
using Xunit;

namespace QuickLog.Tests;

public sealed class TraceSinkTests : IDisposable
{
    private readonly CapturingTraceListener _listener = new();

    public TraceSinkTests() => Trace.Listeners.Add(_listener);
    public void Dispose() => Trace.Listeners.Remove(_listener);

    [Fact]
    public void Write_InfoLevel_EmitsTraceInformation()
    {
        using var sink = new TraceSink();
        var e = MakeEntry(LogType.Info, "info message");
        sink.Write(in e);
        Assert.Contains(_listener.Messages, m => m.Contains("info message"));
    }

    [Fact]
    public void Write_WarnLevel_EmitsTraceWarning()
    {
        using var sink = new TraceSink();
        var e = MakeEntry(LogType.Warn, "warning message");
        sink.Write(in e);
        Assert.Contains(_listener.Messages, m => m.Contains("warning message"));
    }

    [Fact]
    public void Write_ErrorLevel_EmitsTraceError()
    {
        using var sink = new TraceSink();
        var e = MakeEntry(LogType.Error, "error message");
        sink.Write(in e);
        Assert.Contains(_listener.Messages, m => m.Contains("error message"));
    }

    [Fact]
    public void Write_CritLevel_EmitsTraceError()
    {
        using var sink = new TraceSink();
        var e = MakeEntry(LogType.Crit, "critical message");
        sink.Write(in e);
        Assert.Contains(_listener.Messages, m => m.Contains("critical message"));
    }

    [Fact]
    public void Write_IncludesMemberName_InOutput()
    {
        using var sink = new TraceSink();
        var entry = new LogEntry(DateTime.UtcNow, LogType.Debug, "msg",
            "L", null, "MyTestMethod", "f.cs", 1, 1, ThreadRole.Unknown);
        sink.Write(in entry);
        Assert.Contains(_listener.Messages, m => m.Contains("MyTestMethod"));
    }

    private static LogEntry MakeEntry(LogType level, string msg) =>
        new(DateTime.UtcNow, level, msg, "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);
}

/// <summary>Captures Trace output for assertion in tests.</summary>
internal sealed class CapturingTraceListener : TraceListener
{
    public List<string> Messages { get; } = new();

    public override void Write(string? message) { }
    public override void WriteLine(string? message) => Messages.Add(message ?? "");

    public override void TraceEvent(TraceEventCache? cache, string source,
        TraceEventType eventType, int id, string? message)
        => Messages.Add(message ?? "");

    public override void TraceEvent(TraceEventCache? cache, string source,
        TraceEventType eventType, int id, string? format, params object?[]? args)
        => Messages.Add(args is { Length: > 0 } ? string.Format(format ?? "", args) : (format ?? ""));
}

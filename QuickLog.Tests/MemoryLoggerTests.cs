using QuickLog.Loggers;
using Xunit;

namespace QuickLog.Tests;

public sealed class MemoryLoggerTests
{
    [Fact]
    public void Log_AddsEntry_ToSnapshot()
    {
        var ml = new MemoryQuickLogger();
        ml.Log(LogType.Info, "hello");

        var snap = ml.Snapshot();
        Assert.Single(snap);
        Assert.Equal("hello", snap[0].Message);
        Assert.Equal(LogType.Info, snap[0].LoggingType);
    }

    [Fact]
    public void LogException_StoresException_InSnapshot()
    {
        var ml = new MemoryQuickLogger();
        var ex = new InvalidOperationException("boom");
        ml.Log(LogType.Error, ex);

        var snap = ml.Snapshot();
        Assert.Single(snap);
        Assert.Same(ex, snap[0].Exception);
        Assert.Equal(LogType.Error, snap[0].LoggingType);
    }

    [Fact]
    public void LogMessageAndException_StoresBoth()
    {
        var ml = new MemoryQuickLogger();
        var ex = new ArgumentException("bad arg");
        ml.Log(LogType.Warn, "context message", ex);

        var snap = ml.Snapshot();
        Assert.Single(snap);
        Assert.Equal("context message", snap[0].Message);
        Assert.Same(ex, snap[0].Exception);
    }

    [Fact]
    public void CircularBuffer_DropsOldestWhenCapacityExceeded()
    {
        var ml = new MemoryQuickLogger(3);
        ml.Log(LogType.Info, "first");
        ml.Log(LogType.Info, "second");
        ml.Log(LogType.Info, "third");
        ml.Log(LogType.Info, "fourth");

        var snap = ml.Snapshot();
        Assert.Equal(3, snap.Count);
        Assert.DoesNotContain(snap, e => e.Message == "first");
        Assert.Contains(snap, e => e.Message == "fourth");
    }

    [Fact]
    public void CapacityOne_AlwaysKeepsLatestEntry()
    {
        var ml = new MemoryQuickLogger(1);
        ml.Log(LogType.Info, "old");
        ml.Log(LogType.Info, "new");

        Assert.Single(ml.Snapshot());
        Assert.Equal("new", ml.Snapshot()[0].Message);
    }

    [Fact]
    public void Dispose_ClearsAllEntries()
    {
        var ml = new MemoryQuickLogger();
        ml.Log(LogType.Info, "entry");
        ml.Dispose();
        Assert.Empty(ml.Snapshot());
    }

    [Fact]
    public void Log_AfterDispose_IsIgnored()
    {
        var ml = new MemoryQuickLogger();
        ml.Dispose();
        ml.Log(LogType.Crit, "post-dispose");
        Assert.Empty(ml.Snapshot());
    }

    [Fact]
    public void LogEvent_FiresForEachEntry()
    {
        var ml = new MemoryQuickLogger();
        var fired = 0;
        ml.LogEvent += (_, _) => fired++;

        ml.Log(LogType.Info, "a");
        ml.Log(LogType.Info, "b");

        Assert.Equal(2, fired);
    }

    [Fact]
    public void QuickLogger_RecentLogs_CarryScopeAndCorrelation()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;

        using (QuickLog.LogContext.BeginCorrelation("corr-memory"))
        using (QuickLog.LogScope.Begin("Frame", 7))
        {
            logger.Log(LogType.Info, "memory context");
        }

        logger.Shutdown();

        var entry = logger.GetRecentLogs().Single();
        Assert.Equal("Frame:7", entry.Scope);
        Assert.Equal("corr-memory", entry.CorrelationId);
    }
}

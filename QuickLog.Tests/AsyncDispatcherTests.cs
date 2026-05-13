using QuickLog.Loggers;
using QuickLog.Core;
using Xunit;

namespace QuickLog.Tests;

public sealed class AsyncDispatcherTests
{
    [Fact]
    public async Task AsyncLogging_EntriesReachMemorySink()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;

        for (var i = 0; i < 5; i++)
            logger.Log(LogType.Info, $"entry {i}");

        logger.Flush();
        await WaitForLogsAsync(logger, expectedCount: 5);

        Assert.Equal(5, logger.GetRecentLogs().Count);
    }

    [Fact]
    public async Task AsyncOnly_NoSyncIO_EventStillFires()
    {
        using var logger = new QuickLogger();
        logger.AsyncOnly = true;
        logger.EnableAsyncLogging = true;
        logger.RaiseLogEventInAsyncOnly = true;

        var events = new List<LogEventArgs>();
        logger.LogEvent += (_, e) => events.Add(e);

        logger.Log(LogType.Warn, "async only test");
        logger.Flush();
        await WaitForLogsAsync(logger, expectedCount: 1);

        Assert.Single(events);
        Assert.Equal(LogType.Warn, events[0].LoggingType);
    }

    [Fact]
    public async Task Filter_DropsEntriesBelowWarn_KeepsWarnAndAbove()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.Filter = e => e.LoggingType >= LogType.Warn;

        logger.Log(LogType.Info, "below threshold");
        logger.Log(LogType.Error, "above threshold");

        logger.Flush();
        await WaitForLogsAsync(logger, expectedCount: 1);

        var logs = logger.GetRecentLogs();
        Assert.DoesNotContain(logs, e => e.Message == "below threshold");
        Assert.Contains(logs, e => e.Message == "above threshold");
    }

    [Fact]
    public async Task Shutdown_FlushesAllPendingEntries()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;

        for (var i = 0; i < 20; i++)
            logger.Log(LogType.Debug, $"msg {i}");

        logger.Shutdown();
        await Task.Delay(50);

        Assert.Equal(20, logger.GetRecentLogs().Count);
    }

    [Fact]
    public void LogEvent_FiresOnSyncPath_WhenAsyncOnlyFalse()
    {
        using var logger = new QuickLogger(eventLogging: true);

        var received = new List<LogEventArgs>();
        logger.LogEvent += (_, e) => received.Add(e);

        logger.Log(LogType.Info, "sync event test");

        Assert.Single(received);
        Assert.Equal("sync event test", received[0].Message);
    }

    [Fact]
    public void GetStats_ReportsWrittenEntriesAndQueueCapacity()
    {
        using var logger = new QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.AsyncQueueCapacity = 32;

        logger.Log(LogType.Info, "stats one");
        logger.Shutdown();

        var stats = logger.GetStats();
        Assert.Equal(32, stats.QueueCapacity);
        Assert.True(stats.Written >= 1);
        Assert.Equal(0, stats.SinkFailures);
    }

    [Fact]
    public void Dispatcher_ContinuesAfterSinkFailure_AndReportsFailure()
    {
        using var good = new RecordingSink();
        using var dispatcher = new AsyncLogDispatcher(
            [new ThrowingSink(), good],
            queueCapacity: 8);

        var entry = new LogEntry(DateTime.UtcNow, LogType.Info, "survives",
            "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);

        dispatcher.Enqueue(in entry);
        dispatcher.Flush();

        var stats = dispatcher.GetStats();
        Assert.Equal(1, good.Count);
        Assert.Equal(1, stats.SinkFailures);
        Assert.Contains(nameof(InvalidOperationException), stats.LastSinkError);
    }

    private static async Task WaitForLogsAsync(QuickLogger logger, int expectedCount, int timeoutMs = 3000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (logger.GetRecentLogs().Count < expectedCount && DateTime.UtcNow < deadline)
            await Task.Delay(10);
    }

    private sealed class ThrowingSink : ILogSink
    {
        public void Write(in LogEntry entry) => throw new InvalidOperationException("sink down");
        public void Flush() { }
        public void Dispose() { }
    }

    private sealed class RecordingSink : ILogSink
    {
        public int Count { get; private set; }
        public void Write(in LogEntry entry) => Count++;
        public void Flush() { }
        public void Dispose() { }
    }
}

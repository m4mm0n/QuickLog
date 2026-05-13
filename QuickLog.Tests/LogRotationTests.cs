using QuickLog.Core;
using QuickLog.Sinks;
using Xunit;

namespace QuickLog.Tests;

public sealed class LogRotationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_rotate_{Guid.NewGuid():N}");

    [Fact]
    public void JsonLinesSink_RotatesWhenMaxBytesExceeded()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "app.jsonl");
        var options = new LogRotationOptions { MaxFileBytes = 160, MaxFiles = 3 };

        using (var sink = new JsonLinesSink(path, options))
        {
            for (var i = 0; i < 20; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogType.Info, new string('x', 30),
                    "L", null, "M", "f.cs", i, 1, ThreadRole.Unknown);
                sink.Write(in entry);
            }
        }

        Assert.True(Directory.GetFiles(_dir, "app*.jsonl").Length > 1);
        Assert.True(Directory.GetFiles(_dir, "app*.jsonl").Length <= 3);
    }

    [Fact]
    public void BinaryLogSink_RotatesWhenMaxBytesExceeded()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "app.qlog");
        var options = new LogRotationOptions { MaxFileBytes = 192, MaxFiles = 4 };

        using (var sink = new BinaryLogSink(path, options))
        {
            for (var i = 0; i < 20; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogType.Warn, new string('b', 50),
                    "L", null, "M", "f.cs", i, 1, ThreadRole.Unknown);
                sink.Write(in entry);
            }
        }

        Assert.True(Directory.GetFiles(_dir, "app*.qlog").Length > 1);
        Assert.True(Directory.GetFiles(_dir, "app*.qlog").Length <= 4);
    }

    [Fact]
    public void FileSink_RotatesWhenMaxBytesExceeded()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "app.log");
        var options = new LogRotationOptions { MaxFileBytes = 96, MaxFiles = 2 };

        using (var sink = new FileSink(path, 1, options))
        {
            for (var i = 0; i < 10; i++)
            {
                var entry = new LogEntry(DateTime.UtcNow, LogType.Error, new string('t', 40),
                    "L", null, "M", "f.cs", i, 1, ThreadRole.Unknown);
                sink.Write(in entry);
            }
        }

        Assert.True(Directory.GetFiles(_dir, "app*.log").Length > 1);
        Assert.True(Directory.GetFiles(_dir, "app*.log").Length <= 2);
    }

    [Fact]
    public void QuickLogger_AppliesRotationToAsyncJsonAndBinarySinks()
    {
        Directory.CreateDirectory(_dir);
        var jsonPath = Path.Combine(_dir, "pipe.jsonl");
        var binaryPath = Path.Combine(_dir, "pipe.qlog");

        using var logger = new QuickLog.Loggers.QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.JsonLogPath = jsonPath;
        logger.EnableAsyncBinaryLogging = true;
        logger.BinaryLogPath = binaryPath;
        logger.Rotation = new LogRotationOptions { MaxFileBytes = 180, MaxFiles = 3 };

        for (var i = 0; i < 20; i++)
            logger.Log(LogType.Warn, new string('p', 60));

        logger.Shutdown();

        Assert.True(Directory.GetFiles(_dir, "pipe*.jsonl").Length > 1);
        Assert.True(Directory.GetFiles(_dir, "pipe*.qlog").Length > 1);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}

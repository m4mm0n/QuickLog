using QuickLog.Core;
using QuickLog.Sinks;
using System.IO.Compression;
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

    [Fact]
    public void FileSink_CompressesRotationsAndPreservesReadableContent()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "compressed.log");
        var options = new LogRotationOptions
        {
            MaxFileBytes = 100,
            MaxFiles = 4,
            CompressRotatedFiles = true
        };

        using (var sink = new FileSink(path, 1, options))
        {
            for (var index = 0; index < 8; index++)
                sink.Write(new LogEntry(DateTime.UtcNow, LogType.Info, $"entry-{index}-{new string('x', 48)}",
                    "test", null, "M", "f.cs", index, 1, ThreadRole.IO));
        }

        var compressed = Assert.Single(Directory.GetFiles(_dir, "compressed*.log.gz").Take(1));
        using var file = File.OpenRead(compressed);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        Assert.Contains("entry-", reader.ReadToEnd());
        Assert.Empty(Directory.GetFiles(_dir, "compressed.*.log"));
    }

    [Fact]
    public void FileSink_PrunesExpiredRotationsOnStartup()
    {
        Directory.CreateDirectory(_dir);
        var active = Path.Combine(_dir, "aged.log");
        var old = Path.Combine(_dir, "aged.20200101_000000.log");
        File.WriteAllText(old, "old");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddDays(-10));

        using (new FileSink(active, 1, new LogRotationOptions
        {
            MaxFileBytes = 1024,
            MaxFiles = 5,
            MaxAge = TimeSpan.FromDays(1)
        }))
        { }

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(active));
    }

    [Fact]
    public void FileSink_EnforcesTotalByteBudgetAcrossRotations()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "budget.log");
        var options = new LogRotationOptions
        {
            MaxFileBytes = 140,
            MaxFiles = 20,
            MaxTotalBytes = 360
        };

        using (var sink = new FileSink(path, 1, options))
        {
            for (var index = 0; index < 30; index++)
                sink.Write(new LogEntry(DateTime.UtcNow, LogType.Info, new string('z', 70),
                    "test", null, "M", "f.cs", index, 1, ThreadRole.IO));
        }

        var total = Directory.GetFiles(_dir, "budget*").Sum(file => new FileInfo(file).Length);
        Assert.True(total <= options.MaxTotalBytes);
        Assert.True(Directory.GetFiles(_dir, "budget*.log").Length < options.MaxFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }
}

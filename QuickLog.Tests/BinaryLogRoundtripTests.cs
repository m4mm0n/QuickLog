using QuickLog.Core;
using QuickLog.Sinks;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class BinaryLogRoundtripTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"ql_test_{Guid.NewGuid():N}.bin");

    [Fact]
    public void SingleEntry_RoundtripsLevel_Message_MemberName_LineNumber_Timestamp()
    {
        var ts = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var entry = new LogEntry(ts, LogType.Info, "Hello roundtrip",
            "TestLogger", null, "TestMethod", "test.cs", 42,
            Environment.CurrentManagedThreadId, ThreadRole.Unknown);

        using (var sink = new BinaryLogSink(_path))
            sink.Write(in entry);

        var entries = BinaryLogReader.Read(_path).ToList();

        Assert.Single(entries);
        Assert.Equal(LogType.Info, entries[0].Level);
        Assert.Equal("Hello roundtrip", entries[0].Message);
        Assert.Equal("TestMethod", entries[0].MemberName);
        Assert.Equal(42, entries[0].LineNumber);
        Assert.Equal(ts.Ticks, entries[0].Timestamp.Ticks);
    }

    [Fact]
    public void MultipleEntries_AllPreservedInOrder()
    {
        using (var sink = new BinaryLogSink(_path))
        {
            for (var i = 0; i < 10; i++)
            {
                var e = new LogEntry(DateTime.UtcNow, LogType.Info, $"Message {i}",
                    "L", null, "M", "f.cs", i, 1, ThreadRole.Unknown);
                sink.Write(in e);
            }
        }

        var entries = BinaryLogReader.Read(_path).ToList();

        Assert.Equal(10, entries.Count);
        for (var i = 0; i < 10; i++)
            Assert.Equal($"Message {i}", entries[i].Message);
    }

    [Fact]
    public void VariousLogLevels_RoundtripCorrectly()
    {
        var levels = new[] { LogType.Trace, LogType.Debug, LogType.Info, LogType.Warn, LogType.Error, LogType.Crit };

        using (var sink = new BinaryLogSink(_path))
        {
            foreach (var level in levels)
            {
                var e = new LogEntry(DateTime.UtcNow, level, level.ToString(),
                    "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);
                sink.Write(in e);
            }
        }

        var read = BinaryLogReader.Read(_path).Select(e => e.Level).ToArray();
        Assert.Equal(levels, read);
    }

    [Fact]
    public void CorruptedCrc_StopsReading_WhenStopOnCrcErrorIsTrue()
    {
        var entry = new LogEntry(DateTime.UtcNow, LogType.Info, "ok",
            "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);

        using (var sink = new BinaryLogSink(_path))
            sink.Write(in entry);

        var data = File.ReadAllBytes(_path);
        data[^1] ^= 0xFF;
        data[^2] ^= 0xFF;
        File.WriteAllBytes(_path, data);

        Assert.Empty(BinaryLogReader.Read(_path, stopOnCrcError: true));
    }

    [Fact]
    public void EmptyCategory_RoundtripsAsEmptyString()
    {
        var entry = new LogEntry(DateTime.UtcNow, LogType.Debug, "cat test",
            "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown);

        using (var sink = new BinaryLogSink(_path))
            sink.Write(in entry);

        var e = BinaryLogReader.Read(_path).Single();
        Assert.Equal(string.Empty, e.Category);
    }

    [Fact]
    public void QuickLogger_WithAsyncBinaryLogging_WritesReadableBinaryLog()
    {
        using var logger = new QuickLog.Loggers.QuickLogger();
        logger.EnableAsyncLogging = true;
        logger.EnableAsyncBinaryLogging = true;
        logger.BinaryLogPath = _path;

        logger.Log(LogType.Warn, "binary through logger");
        logger.Shutdown();

        var entry = BinaryLogReader.Read(_path).Single();
        Assert.Equal(LogType.Warn, entry.Level);
        Assert.Equal("binary through logger", entry.Message);
    }

    [Fact]
    public void ContextFields_RoundtripThroughBinaryLog()
    {
        var entry = new LogEntry(
            DateTime.UtcNow, LogType.Info, "context binary",
            "L", "ScopeA", "M", "f.cs", 1, 1, ThreadRole.Unknown,
            "corr-1", "trace-1", "span-1");

        using (var sink = new BinaryLogSink(_path))
            sink.Write(in entry);

        var read = BinaryLogReader.Read(_path).Single();
        Assert.Equal("corr-1", read.CorrelationId);
        Assert.Equal("trace-1", read.TraceId);
        Assert.Equal("span-1", read.SpanId);
    }

    [Fact]
    public void Query_WithCorrelation_ReturnsMatchingEntries()
    {
        using (var sink = new BinaryLogSink(_path))
        {
            var first = new LogEntry(DateTime.UtcNow, LogType.Info, "first",
                "L", null, "M", "f.cs", 1, 1, ThreadRole.Unknown, "corr-a");
            var second = new LogEntry(DateTime.UtcNow, LogType.Info, "second needle",
                "L", null, "M", "f.cs", 2, 1, ThreadRole.Unknown, "corr-b");
            sink.Write(in first);
            sink.Write(in second);
        }

        var byCorrelation = BinaryLogQuery.WithCorrelation(_path, "corr-b").Single();
        var byText = BinaryLogQuery.ContainingText(_path, "needle").Single();

        Assert.Equal("second needle", byCorrelation.Message);
        Assert.Equal("corr-b", byText.CorrelationId);
    }

    [Fact]
    public void ExportToText_IncludesContextFields()
    {
        var textPath = Path.Combine(Path.GetTempPath(), $"ql_export_{Guid.NewGuid():N}.txt");
        try
        {
            var entry = new LogEntry(DateTime.UtcNow, LogType.Info, "export context",
                "L", "ScopeA", "M", "f.cs", 1, 1, ThreadRole.Unknown,
                "corr-export", "trace-export", "span-export");

            using (var sink = new BinaryLogSink(_path))
                sink.Write(in entry);

            BinaryLogExporter.ExportToText(_path, textPath);
            var text = File.ReadAllText(textPath);

            Assert.Contains("ScopeA", text);
            Assert.Contains("corr-export", text);
            Assert.Contains("trace-export", text);
            Assert.Contains("span-export", text);
        }
        finally
        {
            if (File.Exists(textPath)) File.Delete(textPath);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}

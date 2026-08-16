using System.Text;
using System.Text.Json;
using QuickLog.Core;
using QuickLog.Loggers;
using QuickLog.Sinks;
using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class StructuredLoggingTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"quicklog-v3-{Guid.NewGuid():N}.qlog");

    [Fact]
    public void BinaryV3_RoundtripsEventIdentityAndTypedProperties()
    {
        var properties = LogProperties.Create(
            new LogProperty("asset", "ship.glb"),
            new LogProperty("attempt", 3),
            new LogProperty("cached", true),
            new LogProperty("elapsed", 12.5));
        var entry = new LogEntry(
            DateTime.UtcNow,
            LogType.Info,
            "asset loaded",
            "test",
            "loading",
            "Load",
            "asset.cs",
            42,
            1,
            ThreadRole.IO,
            EventId: new LogEventId(1201, "AssetLoaded"),
            Properties: properties);

        using (var sink = new BinaryLogSink(_path))
            sink.Write(entry);

        var actual = BinaryLogReader.Read(_path).Single();
        Assert.Equal(new LogEventId(1201, "AssetLoaded"), actual.EventId);
        Assert.Equal("ship.glb", actual.Properties!["asset"]);
        Assert.Equal(3L, actual.Properties["attempt"]);
        Assert.Equal(true, actual.Properties["cached"]);
        Assert.Equal(12.5, actual.Properties["elapsed"]);
    }

    [Fact]
    public void BinaryReader_ContinuesToReadVersionTwoRecords()
    {
        WriteVersionTwoRecord(_path, "legacy-v2");

        var actual = BinaryLogReader.Read(_path).Single();

        Assert.Equal("legacy-v2", actual.Message);
        Assert.Equal(LogEventId.None, actual.EventId);
        Assert.Empty(actual.Properties!);
    }

    [Fact]
    public void StructuredLog_MergesContextAndRedactsSensitiveProperties()
    {
        using var logger = new QuickLogger
        {
            EnableAsyncLogging = true,
            AsyncOnly = true,
            Redaction = new LogRedactionOptions()
        };
        using var context = LogScope.Begin(
            new LogProperty("session", "play-1"),
            new LogProperty("password", "context-secret"));

        logger.Log(
            LogType.Info,
            "connected",
            new LogEventId(7, "Connected"),
            LogProperties.Create(
                new LogProperty("host", "game.example"),
                new LogProperty("token", "event-secret")));
        logger.Shutdown();

        var entry = logger.GetRecentLogs().Single();
        Assert.Equal(new LogEventId(7, "Connected"), entry.EventId);
        Assert.Equal("play-1", entry.Properties["session"]);
        Assert.Equal("***", entry.Properties["password"]);
        Assert.Equal("***", entry.Properties["token"]);
        Assert.Equal("game.example", entry.Properties["host"]);
    }

    [Fact]
    public void JsonLines_EmitsStructuredPayload()
    {
        var jsonPath = Path.ChangeExtension(_path, ".jsonl");
        try
        {
            var entry = new LogEntry(
                DateTime.UtcNow, LogType.Warn, "slow", "test", null, "Tick", "game.cs", 9, 1,
                ThreadRole.Main,
                EventId: new LogEventId(88, "SlowFrame"),
                Properties: LogProperties.Create(new LogProperty("milliseconds", 31)));
            using (var sink = new JsonLinesSink(jsonPath))
                sink.Write(entry);

            using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
            Assert.Equal(88, document.RootElement.GetProperty("eventId").GetInt32());
            Assert.Equal("SlowFrame", document.RootElement.GetProperty("eventName").GetString());
            Assert.Equal(31, document.RootElement.GetProperty("properties").GetProperty("milliseconds").GetInt32());
        }
        finally
        {
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
        }
    }

    [Fact]
    public void InterpolatedHandler_DoesNotEvaluateDisabledMessage()
    {
        using var logger = new QuickLogger { MinimumLevel = LogType.Warn };
        var evaluations = 0;

        int Evaluate()
        {
            evaluations++;
            return 42;
        }

        logger.Log(LogType.Debug, $"value={Evaluate()}");

        Assert.False(logger.IsEnabled(LogType.Debug));
        Assert.True(logger.IsEnabled(LogType.Error));
        Assert.Equal(0, evaluations);
    }

    private static void WriteVersionTwoRecord(string path, string message)
    {
        using var buffer = new MemoryStream();
        using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("QLOG"u8);
            writer.Write(2);
            writer.Write(DateTime.UnixEpoch.Ticks);
            writer.Write((int)LogType.Info);
            writer.Write(1);
            writer.Write((byte)ThreadRole.Main);
            writer.Write(1);
            WriteString(writer, "Member");
            WriteString(writer, "file.cs");
            WriteString(writer, null);
            WriteString(writer, message);
            WriteString(writer, "correlation");
            WriteString(writer, null);
            WriteString(writer, null);
        }

        var record = buffer.ToArray();
        using var crc = new Crc32();
        using var file = File.Create(path);
        file.Write(record);
        using var writerWithCrc = new BinaryWriter(file, Encoding.UTF8, leaveOpen: true);
        writerWithCrc.Write(crc.CalculateChecksum(record));
    }

    private static void WriteString(BinaryWriter writer, string? value)
    {
        var bytes = string.IsNullOrEmpty(value) ? [] : Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}

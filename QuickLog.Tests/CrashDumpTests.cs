using System.Text.Json;
using QuickLog.Exceptions;
using Xunit;

namespace QuickLog.Tests;

public sealed class CrashDumpTests : IDisposable
{
    private readonly string _dumpDir =
        Path.Combine(Path.GetTempPath(), $"ql_dumps_{Guid.NewGuid():N}");

    private CrashDumpOptions Opts(int maxFiles = 10) => new()
    {
        Enabled = true,
        OutputDirectory = _dumpDir,
        MaxDumpFiles = maxFiles
    };

    [Fact]
    public void Write_CreatesFile_InOutputDirectory()
    {
        var path = CrashDumpWriter.Write(
            new InvalidOperationException("test"),
            ExceptionSource.UnobservedTask,
            isTerminating: false,
            Opts());

        Assert.NotNull(path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Write_FileNameContains_ExceptionTypeName()
    {
        var path = CrashDumpWriter.Write(
            new ArgumentNullException("param"),
            ExceptionSource.UnobservedTask,
            isTerminating: false,
            Opts());

        Assert.Contains("ArgumentNullException", Path.GetFileName(path));
    }

    [Fact]
    public void Write_ProducesValidJson()
    {
        var path = CrashDumpWriter.Write(
            new Exception("json test"),
            ExceptionSource.AppDomain,
            isTerminating: true,
            Opts());

        var json = File.ReadAllText(path!);
        var doc = JsonDocument.Parse(json); // throws if invalid JSON
        Assert.NotNull(doc);
    }

    [Fact]
    public void Write_JsonContains_ExceptionTypeAndMessage()
    {
        var path = CrashDumpWriter.Write(
            new ArgumentException("bad argument"),
            ExceptionSource.UnobservedTask,
            isTerminating: false,
            Opts());

        using var doc = JsonDocument.Parse(File.ReadAllText(path!));
        var exNode = doc.RootElement.GetProperty("Exception");
        Assert.Contains("ArgumentException", exNode.GetProperty("Type").GetString());
        Assert.Equal("bad argument", exNode.GetProperty("Message").GetString());
    }

    [Fact]
    public void Write_JsonContains_CorrectSource()
    {
        var path = CrashDumpWriter.Write(
            new Exception("src test"),
            ExceptionSource.AppDomain,
            isTerminating: true,
            Opts());

        using var doc = JsonDocument.Parse(File.ReadAllText(path!));
        Assert.Equal("AppDomain", doc.RootElement.GetProperty("Source").GetString());
        Assert.True(doc.RootElement.GetProperty("IsTerminating").GetBoolean());
    }

    [Fact]
    public void Rotation_DeletesOldestFiles_WhenLimitExceeded()
    {
        var opts = Opts(maxFiles: 3);

        for (var i = 0; i < 5; i++)
        {
            CrashDumpWriter.Write(new Exception($"crash {i}"), ExceptionSource.AppDomain, true, opts);
            Thread.Sleep(5);
        }

        Assert.Equal(3, Directory.GetFiles(_dumpDir, "crash_*.json").Length);
    }

    [Fact]
    public void Write_AggregateException_IncludesInnerExceptions()
    {
        var agg = new AggregateException(
            new InvalidOperationException("inner1"),
            new ArgumentException("inner2"));

        var path = CrashDumpWriter.Write(agg, ExceptionSource.UnobservedTask, false, Opts());

        using var doc = JsonDocument.Parse(File.ReadAllText(path!));
        var inners = doc.RootElement.GetProperty("Exception").GetProperty("InnerExceptions");
        Assert.Equal(2, inners.GetArrayLength());
    }

    [Fact]
    public void Write_IncludesRecentLogsAndDispatcherStats_WhenSnapshotProvided()
    {
        using var logger = new QuickLog.Loggers.QuickLogger();
        logger.EnableAsyncLogging = true;

        using (QuickLog.LogContext.BeginCorrelation("crash-corr"))
        using (QuickLog.LogScope.Begin("CrashScope"))
        {
            logger.Log(LogType.Info, "before crash");
        }

        logger.Shutdown();

        var options = Opts();
        options.IncludeRecentLogs = true;
        options.RecentLogCount = 8;
        options.IncludeDispatcherStats = true;

        var path = CrashDumpWriter.Write(
            new InvalidOperationException("boom"),
            ExceptionSource.AppDomain,
            isTerminating: true,
            options,
            logger.GetRecentLogs(),
            logger.GetStats());

        using var doc = JsonDocument.Parse(File.ReadAllText(path!));
        var root = doc.RootElement;
        var recent = root.GetProperty("RecentLogs");
        Assert.Contains("before crash", recent[0].GetProperty("Message").GetString());
        Assert.Equal("CrashScope", recent[0].GetProperty("Scope").GetString());
        Assert.Equal("crash-corr", recent[0].GetProperty("CorrelationId").GetString());
        Assert.True(root.GetProperty("Dispatcher").GetProperty("Written").GetInt64() >= 1);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dumpDir))
            Directory.Delete(_dumpDir, recursive: true);
    }
}

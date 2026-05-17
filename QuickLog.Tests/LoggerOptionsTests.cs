using QuickLog.Core;
using Xunit;

namespace QuickLog.Tests;

[Collection("Sequential")]
public sealed class LoggerOptionsTests : IDisposable
{
    public void Dispose() => LogManager.Shutdown();

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void Defaults_ConsoleEnabled_EverythingElseOff()
    {
        var opts = new LoggerOptions();

        Assert.True(opts.ConsoleLogging);
        Assert.False(opts.FileLogging);
        Assert.False(opts.EventLogging);
        Assert.False(opts.TraceLogging);
        Assert.False(opts.AsyncLogging);
        Assert.False(opts.AsyncOnly);
        Assert.False(opts.AsyncTraceLogging);
        Assert.Null(opts.LogFilePath);
        Assert.Null(opts.JsonLogPath);
        Assert.Null(opts.Filter);
    }

    // ── Fluent builder ────────────────────────────────────────────────────────

    [Fact]
    public void WithFile_SetsPathAndEnablesFileLogging()
    {
        var opts = new LoggerOptions().WithFile("app.log");

        Assert.Equal("app.log", opts.LogFilePath);
        Assert.True(opts.FileLogging);
    }

    [Fact]
    public void WithConsole_False_DisablesConsole()
    {
        var opts = new LoggerOptions().WithConsole(false);
        Assert.False(opts.ConsoleLogging);
    }

    [Fact]
    public void WithAsync_SetsPolicy_AndMinLevel()
    {
        var opts = new LoggerOptions()
            .WithAsync(AsyncDropPolicy.DropNewest, LogType.Error);

        Assert.True(opts.AsyncLogging);
        Assert.Equal(AsyncDropPolicy.DropNewest, opts.AsyncDropPolicy);
        Assert.Equal(LogType.Error, opts.AsyncMinimumLevel);
    }

    [Fact]
    public void WithAsyncOnly_EnablesAsyncAndSetsFlag()
    {
        var opts = new LoggerOptions().WithAsyncOnly();

        Assert.True(opts.AsyncOnly);
        Assert.True(opts.AsyncLogging);
    }

    [Fact]
    public void WithJsonLog_SetsPath()
    {
        var opts = new LoggerOptions().WithJsonLog("logs/app.jsonl");
        Assert.Equal("logs/app.jsonl", opts.JsonLogPath);
    }

    [Fact]
    public void WithBinaryLog_SetsPathAndEnablesAsyncBinaryLogging()
    {
        var opts = new LoggerOptions().WithBinaryLog("logs/app.qlog");

        Assert.Equal("logs/app.qlog", opts.BinaryLogPath);
        Assert.True(opts.AsyncBinaryLogging);
        Assert.True(opts.AsyncLogging);
    }

    [Fact]
    public void WithRotation_ConfiguresRotationOptions()
    {
        var opts = new LoggerOptions().WithRotation(1024, maxFiles: 7, rotateOnStartup: true);

        Assert.NotNull(opts.Rotation);
        Assert.Equal(1024, opts.Rotation.MaxFileBytes);
        Assert.Equal(7, opts.Rotation.MaxFiles);
        Assert.True(opts.Rotation.RotateOnStartup);
    }

    [Fact]
    public void WithAsyncQueueCapacity_ClampsAndSetsCapacity()
    {
        var opts = new LoggerOptions().WithAsyncQueueCapacity(0);

        Assert.Equal(1, opts.AsyncQueueCapacity);
    }

    [Fact]
    public void WithRedaction_CreatesConfigurableRedactionOptions()
    {
        var opts = new LoggerOptions().WithRedaction(r => r.SensitiveKeys.Add("session"));

        Assert.NotNull(opts.Redaction);
        Assert.Contains("session", opts.Redaction.SensitiveKeys);
    }

    [Fact]
    public void WithSpamControl_ConfiguresDuplicateThreshold()
    {
        var opts = new LoggerOptions().WithSpamControl(5);

        Assert.NotNull(opts.SpamControl);
        Assert.True(opts.SpamControl.Enabled);
        Assert.Equal(5, opts.SpamControl.DuplicateThreshold);
    }

    [Fact]
    public void WithFilter_SetsDelegate()
    {
        Func<LogEventArgs, bool> f = _ => true;
        var opts = new LoggerOptions().WithFilter(f);
        Assert.Same(f, opts.Filter);
    }

    [Fact]
    public void FluentChain_AllOptions_ConfiguredCorrectly()
    {
        var opts = new LoggerOptions()
            .WithFile("app.log")
            .WithConsole()
            .WithEvents()
            .WithTrace()
            .WithAsync(AsyncDropPolicy.DropOldest, LogType.Crit)
            .WithAsyncTrace()
            .WithJsonLog("app.jsonl");

        Assert.Equal("app.log", opts.LogFilePath);
        Assert.True(opts.FileLogging);
        Assert.True(opts.ConsoleLogging);
        Assert.True(opts.EventLogging);
        Assert.True(opts.TraceLogging);
        Assert.True(opts.AsyncLogging);
        Assert.True(opts.AsyncTraceLogging);
        Assert.Equal(AsyncDropPolicy.DropOldest, opts.AsyncDropPolicy);
        Assert.Equal(LogType.Crit, opts.AsyncMinimumLevel);
        Assert.Equal("app.jsonl", opts.JsonLogPath);
    }

    [Fact]
    public void ForEngine_ConfiguresAsyncBinaryJsonRotationAndNoiseControl()
    {
        var opts = LoggerOptions.ForEngine("logs");

        Assert.False(opts.ConsoleLogging);
        Assert.True(opts.AsyncLogging);
        Assert.True(opts.AsyncOnly);
        Assert.True(opts.AsyncBinaryLogging);
        Assert.Equal(Path.Combine("logs", "quicklog.qlog"), opts.BinaryLogPath);
        Assert.Equal(Path.Combine("logs", "quicklog.jsonl"), opts.JsonLogPath);
        Assert.NotNull(opts.Rotation);
        Assert.NotNull(opts.Redaction);
        Assert.NotNull(opts.SpamControl);
    }

    [Fact]
    public void ForTool_KeepsConsoleOnAndUsesCompactFormatting()
    {
        var opts = LoggerOptions.ForTool("tool");

        Assert.True(opts.ConsoleLogging);
        Assert.True(opts.CompactText);
        Assert.Equal("tool", opts.SessionName);
    }

    // ── LogManager.ConfigureDefault(LoggerOptions) ────────────────────────────

    [Fact]
    public void ConfigureDefault_WithOptions_CreatesLogger()
    {
        LogManager.ConfigureDefault(new LoggerOptions().WithConsole(false));

        var logger = LogManager.GetDefaultLogger();
        Assert.NotNull(logger);
    }

    [Fact]
    public async Task ConfigureDefault_WithAsyncAndJsonLog_WritesJsonEntries()
    {
        var jsonPath = Path.Combine(Path.GetTempPath(), $"ql_opts_{Guid.NewGuid():N}.jsonl");
        try
        {
            LogManager.ConfigureDefault(new LoggerOptions()
                .WithConsole(false)
                .WithAsync()
                .WithJsonLog(jsonPath));

            var logger = LogManager.GetDefaultLogger();
            logger.Log(LogType.Info, "options json test");

            // Shutdown async dispatcher (Join on background thread) so file is closed before reading.
            if (logger is QuickLog.Loggers.QuickLogger ql)
                ql.Shutdown();

            Assert.True(File.Exists(jsonPath));
            var lines = File.ReadAllLines(jsonPath);
            Assert.NotEmpty(lines);
        }
        finally
        {
            LogManager.Shutdown();
            if (File.Exists(jsonPath)) File.Delete(jsonPath);
        }
    }
}

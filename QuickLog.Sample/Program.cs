using QuickLog;
using QuickLog.Core;
using QuickLog.Exceptions;
using QuickLog.Loggers;
using QuickLog.Utilities;
using QuickLog.Extensions.Logging;
using Microsoft.Extensions.Logging;

const string LogRoot = "logs";
const string BinaryLogPath = "logs/quicklog.qlog";
const string JsonLogPath = "logs/quicklog.jsonl";
const string ExportPath = "logs/quicklog.export.log";

Directory.CreateDirectory(LogRoot);
foreach (var path in new[] { BinaryLogPath, JsonLogPath, ExportPath })
{
    if (File.Exists(path))
        File.Delete(path);
}

LogStateSnapshot.Clear();
LogStateSnapshot.Set("phase", "startup");
LogStateSnapshot.Set("asset", "none");

LogManager.ConfigureDefault(
    LoggerOptions.ForEngine(LogRoot)
        .WithMinimumLevel(LogType.Trace)
        .WithRotation(
            maxFileBytes: 16 * 1024 * 1024,
            maxFiles: 8,
            maxAge: TimeSpan.FromDays(14),
            maxTotalBytes: 128 * 1024 * 1024,
            compressRotatedFiles: true));

LogManager.AttachExceptionHooks(new ExceptionHookOptions
{
    ShowPopup = false,
    MarkTaskExceptionsObserved = true,
    CrashDump = new CrashDumpOptions
    {
        Enabled = true,
        OutputDirectory = Path.Combine(LogRoot, "crashes"),
        MaxDumpFiles = 5,
        Redaction = LogRedactionOptions.CrashSafe()
    }
});

var logger = LogManager.GetDefaultLogger();
var quickLogger = (QuickLogger)logger;

using (LogContext.BeginCorrelation($"sample-{Guid.NewGuid():N}"))
using (var session = LogSession.Begin(logger, "sample", quickLogger.SessionId))
{
    logger.Log(LogType.Info, "QuickLog v3 structured diagnostics sample started");
    logger.Log(
        LogType.Info,
        "Sample session initialized",
        new LogEventId(3001, "SampleInitialized"),
        LogProperties.Create(
            new LogProperty("sessionId", quickLogger.SessionId),
            new LogProperty("platform", Environment.OSVersion.Platform.ToString())));
    session.Checkpoint("configured");

    using var factory = LoggerFactory.Create(builder =>
        builder.ClearProviders().AddQuickLog(logger));
    factory.CreateLogger("QuickLog.Sample").LogInformation(
        new EventId(3002, "AdapterReady"),
        "Microsoft logging bridge ready for session {SessionId}",
        quickLogger.SessionId);

    quickLogger.LogOnce("startup.notice", LogType.Info, "This startup notice is written once");
    quickLogger.LogEvery("network.retry", TimeSpan.FromSeconds(30), LogType.Warn, "Retrying matchmaking endpoint");
    quickLogger.LogEvery("network.retry", TimeSpan.FromSeconds(30), LogType.Warn, "This duplicate retry is suppressed");

    LogStateSnapshot.Set("phase", "loading-assets");
    using (quickLogger.BeginAssetLoad("textures/player-atlas.png"))
    {
        await Task.Delay(25);
    }

    quickLogger.LogFrameTime(42, TimeSpan.FromMilliseconds(18), TimeSpan.FromMilliseconds(16));
    session.Bookmark("first-hitch");

    var markedWork = new MarkedSampleWork();
    QLogRunner.Invoke(logger, markedWork.BuildIndex);

    try
    {
        throw new InvalidOperationException("Synthetic recoverable sample failure token=sample-secret");
    }
    catch (Exception ex)
    {
        LogStateSnapshot.Set("phase", "recoverable-error");
        logger.Log(LogType.Error, "Captured a recoverable sample exception", ex);
    }

    session.Checkpoint("done");
}

var sessionId = quickLogger.SessionId;
await quickLogger.FlushAsync();
LogManager.Shutdown();

var entries = BinaryLogReader.Read(BinaryLogPath, stopOnCrcError: false).ToArray();
BinaryLogExporter.ExportToText(BinaryLogPath, ExportPath);
var summary = BinaryLogSummary.FromEntries(entries);

Console.WriteLine("=== QuickLog v3.0 sample ===");
Console.WriteLine($"Session: {sessionId}");
Console.WriteLine($"Entries: {summary.EntryCount}");
Console.WriteLine($"Events:  {summary.EventCounts.Count}");
Console.WriteLine($"Binary:  {Path.GetFullPath(BinaryLogPath)}");
Console.WriteLine($"Export:  {Path.GetFullPath(ExportPath)}");

/// <summary>
/// Demonstrates attribute-marked work that can be invoked through <see cref="QLogRunner"/>.
/// </summary>
[QLOG(LoggingOption.Default, Name = "sample-build-index")]
internal sealed class MarkedSampleWork
{
    /// <summary>
    /// Simulates a small unit of work that emits QLOG entry, exit, timing, and exception markers.
    /// </summary>
    public void BuildIndex()
    {
        Thread.Sleep(5);
    }
}

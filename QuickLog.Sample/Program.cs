/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : Program.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11 06:58:20 +02:00
 *  Last Modified  : 2026-05-17 20:35:51 +02:00
 *  CRC32          : E4B9E2E8
 *  
 *  Description    :
 *                   Demonstrates attribute-marked work that can be invoked through <see cref="QLogRunner"/>.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: E4B9E2E8

using QuickLog;
using QuickLog.Core;
using QuickLog.Exceptions;
using QuickLog.Loggers;
using QuickLog.Utilities;

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
        .WithMinimumLevel(LogType.Trace));

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
    logger.Log(LogType.Info, "QuickLog v2.4 Linux-ready diagnostics sample started");
    session.Checkpoint("configured");

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
quickLogger.Flush();
LogManager.Shutdown();

var entries = BinaryLogReader.Read(BinaryLogPath, stopOnCrcError: false).ToArray();
BinaryLogExporter.ExportToText(BinaryLogPath, ExportPath);
var summary = BinaryLogSummary.FromEntries(entries);

Console.WriteLine("=== QuickLog v2.4 sample ===");
Console.WriteLine($"Session: {sessionId}");
Console.WriteLine($"Entries: {summary.EntryCount}");
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

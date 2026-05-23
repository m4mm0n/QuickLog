using QuickLog;
using QuickLog.Core;
using QuickLog.Loggers;
using QuickLog.Platform;
using QuickLog.Utilities;

var logRoot = args.Length > 0
    ? args[0]
    : QuickLogPathResolver.GetLinuxLogDirectory("QuickLog.LinuxSmoke");

Directory.CreateDirectory(logRoot);
foreach (var path in new[]
{
    Path.Combine(logRoot, "quicklog.jsonl"),
    Path.Combine(logRoot, "quicklog.qlog")
})
{
    if (File.Exists(path))
        File.Delete(path);
}

LogManager.ConfigureDefault(
    LoggerOptions.ForLinux("QuickLog.LinuxSmoke", logDirectory: logRoot)
        .WithMinimumLevel(LogType.Trace));

var logger = LogManager.GetDefaultLogger();
var quickLogger = (QuickLogger)logger;

logger.Log(LogType.Info, "QuickLog Linux smoke started");
logger.Log(LogType.Warn, "QuickLog Linux smoke warning marker");
quickLogger.LogOnce("linux-smoke-once", LogType.Info, "QuickLog Linux smoke once marker");

quickLogger.Flush();
LogManager.Shutdown();

var binaryPath = Path.Combine(logRoot, "quicklog.qlog");
var jsonPath = Path.Combine(logRoot, "quicklog.jsonl");
var entries = BinaryLogReader.Read(binaryPath, stopOnCrcError: false).ToArray();

Console.WriteLine("=== QuickLog Linux smoke ===");
Console.WriteLine($"Platform: {QuickLogPlatform.CurrentKind}");
Console.WriteLine($"LogRoot:  {Path.GetFullPath(logRoot)}");
Console.WriteLine($"Json:     {Path.GetFullPath(jsonPath)}");
Console.WriteLine($"Binary:   {Path.GetFullPath(binaryPath)}");
Console.WriteLine($"Entries:  {entries.Length}");

return entries.Length > 0 && File.Exists(jsonPath) ? 0 : 1;

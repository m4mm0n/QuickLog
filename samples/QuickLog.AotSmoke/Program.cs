using Microsoft.Extensions.Logging;
using QuickLog;
using QuickLog.Extensions.Logging;
using QuickLog.Loggers;
using QuickLog.Utilities;

var output = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(Path.GetTempPath(), $"quicklog-aot-{Guid.NewGuid():N}");
Directory.CreateDirectory(output);
var binaryPath = Path.Combine(output, "aot.qlog");
var jsonPath = Path.Combine(output, "aot.jsonl");

await using var quickLogger = new QuickLogger
{
    EnableAsyncLogging = true,
    AsyncOnly = true,
    EnableAsyncBinaryLogging = true,
    BinaryLogPath = binaryPath,
    JsonLogPath = jsonPath
};
using var factory = LoggerFactory.Create(builder => builder.ClearProviders().AddQuickLog(quickLogger));
var logger = factory.CreateLogger("AotSmoke");
logger.LogInformation(new EventId(3000, "NativeEvent"), "AOT value {Value}", 42);
await quickLogger.ShutdownAsync(TimeSpan.FromSeconds(10));

var entry = BinaryLogReader.Read(binaryPath).Single();
if (entry.EventId.Id != 3000 || !entry.Properties!.ContainsKey("Value") || !File.Exists(jsonPath))
    throw new InvalidOperationException("AOT smoke output did not preserve structured data.");

Console.WriteLine($"AOT_SMOKE_OK {entry.EventId} {output}");

using QuickLog.Loggers;

namespace QuickLog.Tools.Diagnostics;

internal sealed class ToolSessionLogger : IDisposable
{
    public string OutputDirectory { get; }
    public string BinaryLogPath { get; }
    public string JsonLogPath { get; }
    public QuickLogger Logger { get; }

    public ToolSessionLogger(string outputDirectory)
    {
        OutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(OutputDirectory);

        BinaryLogPath = Path.Combine(OutputDirectory, "session.qlog");
        JsonLogPath = Path.Combine(OutputDirectory, "session.jsonl");

        Logger = new QuickLogger
        {
            EnableConsoleLogging = false,
            EnableFileLogging = false,
            EnableTraceLogging = false,
            EnableEventLogging = false,
            EnableAsyncLogging = true,
            AsyncOnly = true,
            EnableAsyncBinaryLogging = true,
            BinaryLogPath = BinaryLogPath,
            JsonLogPath = JsonLogPath,
            AsyncQueueCapacity = 8192,
            Redaction = new QuickLog.Core.LogRedactionOptions()
        };
    }

    public void Dispose()
    {
        Logger.Shutdown();
    }
}

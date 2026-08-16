using QuickLog.Core;
using System.Collections.Concurrent;

namespace QuickLog.Sinks;

/// <summary>
/// Provides a log sink that writes log entries to a file in batches for improved performance.
/// </summary>
/// <remarks>FileSink buffers log entries and writes them to the specified file in batches, reducing the number of
/// disk writes. The sink is not thread-safe; concurrent access should be externally synchronized if used from multiple
/// threads. The file is opened in append mode, and log entries are formatted with timestamp, log level, and message.
/// Call Dispose to ensure all buffered entries are flushed and resources are released.</remarks>
internal sealed class FileSink : ILogSink
{
    private readonly RotatingFileWriter _writer;
    private readonly ConcurrentQueue<LogEntry> _queue = new();
    private readonly int _batchSize;

    public FileSink(string path, int batchSize, LogRotationOptions? rotation = null)
    {
        _writer = new RotatingFileWriter(path, rotation);
        _batchSize = Math.Max(1, batchSize);
    }

    public void Write(in LogEntry entry)
    {
        _queue.Enqueue(entry);
        if (_queue.Count >= _batchSize)
            Flush();
    }

    public void Flush()
    {
        while (_queue.TryDequeue(out var e))
        {
            var context = string.IsNullOrWhiteSpace(e.CorrelationId)
                ? string.Empty
                : $" [{e.CorrelationId}]";
            var eventText = e.EventId == LogEventId.None ? string.Empty : $" [{e.EventId}]";
            var properties = LogProperties.Format(e.Properties);
            if (properties.Length > 0)
                properties = $" {properties}";

            _writer.WriteLine(
                $"[{e.Timestamp:O}] [{e.Level}] [{e.ThreadRole}]{context}{eventText} {e.Message}{properties}");
        }

        _writer.Flush();
    }

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }
}

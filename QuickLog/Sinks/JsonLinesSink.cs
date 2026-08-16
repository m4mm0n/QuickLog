using System.Text.Json;
using QuickLog.Core;

namespace QuickLog.Sinks;

/// <summary>
/// A log sink that appends one JSON object per log entry to a file (JSON Lines / NDJSON format).
/// Compatible with Seq, Elastic, Loki, and any log aggregator that accepts JSON Lines.
/// </summary>
internal sealed class JsonLinesSink : ILogSink
{
    private readonly RotatingFileWriter _writer;

    public JsonLinesSink(string path, LogRotationOptions? rotation = null)
    {
        _writer = new RotatingFileWriter(path, rotation);
    }

    public void Write(in LogEntry entry)
    {
        var line = new JsonLogLine(
            entry.Timestamp.ToString("O"),
            entry.Level.ToString(),
            entry.Message,
            entry.MemberName,
            entry.FilePath,
            entry.LineNumber,
            entry.ThreadId,
            entry.ThreadRole.ToString(),
            string.IsNullOrEmpty(entry.Category) ? null : entry.Category,
            string.IsNullOrEmpty(entry.CorrelationId) ? null : entry.CorrelationId,
            string.IsNullOrEmpty(entry.TraceId) ? null : entry.TraceId,
            string.IsNullOrEmpty(entry.SpanId) ? null : entry.SpanId,
            entry.EventId == LogEventId.None ? null : entry.EventId.Id,
            entry.EventId.Name,
            entry.Properties is { Count: > 0 } ? entry.Properties : null);

        _writer.WriteLine(JsonSerializer.Serialize(line, JsonLinesSerializationContext.Default.JsonLogLine));
    }

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }
}

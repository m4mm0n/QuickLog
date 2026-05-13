using System.Text.Json;
using System.Text.Json.Serialization;
using QuickLog.Core;

namespace QuickLog.Sinks;

/// <summary>
/// A log sink that appends one JSON object per log entry to a file (JSON Lines / NDJSON format).
/// Compatible with Seq, Elastic, Loki, and any log aggregator that accepts JSON Lines.
/// </summary>
internal sealed class JsonLinesSink : ILogSink
{
    private readonly RotatingFileWriter _writer;

    private static readonly JsonSerializerOptions _json = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonLinesSink(string path, LogRotationOptions? rotation = null)
    {
        _writer = new RotatingFileWriter(path, rotation);
    }

    public void Write(in LogEntry entry)
    {
        var line = new LogLine(
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
            string.IsNullOrEmpty(entry.SpanId) ? null : entry.SpanId);

        _writer.WriteLine(JsonSerializer.Serialize(line, _json));
    }

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }

    // ── JSON model ────────────────────────────────────────────────────────────

    private sealed record LogLine(
        [property: JsonPropertyName("ts")]     string Ts,
        [property: JsonPropertyName("level")]  string Level,
        [property: JsonPropertyName("msg")]    string Msg,
        [property: JsonPropertyName("member")] string Member,
        [property: JsonPropertyName("file")]   string File,
        [property: JsonPropertyName("line")]   int Line,
        [property: JsonPropertyName("thread")] int Thread,
        [property: JsonPropertyName("role")]   string Role,
        [property: JsonPropertyName("scope")]  string? Scope,
        [property: JsonPropertyName("correlation")] string? Correlation,
        [property: JsonPropertyName("trace")] string? Trace,
        [property: JsonPropertyName("span")] string? Span);
}

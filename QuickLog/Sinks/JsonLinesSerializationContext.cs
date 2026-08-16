using System.Text.Json.Serialization;

namespace QuickLog.Sinks;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(JsonLogLine))]
internal sealed partial class JsonLinesSerializationContext : JsonSerializerContext;

internal sealed record JsonLogLine(
    [property: JsonPropertyName("ts")] string Ts,
    [property: JsonPropertyName("level")] string Level,
    [property: JsonPropertyName("msg")] string Msg,
    [property: JsonPropertyName("member")] string Member,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("line")] int Line,
    [property: JsonPropertyName("thread")] int Thread,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("correlation")] string? Correlation,
    [property: JsonPropertyName("trace")] string? Trace,
    [property: JsonPropertyName("span")] string? Span,
    [property: JsonPropertyName("eventId")] int? EventId,
    [property: JsonPropertyName("eventName")] string? EventName,
    [property: JsonPropertyName("properties")] IReadOnlyDictionary<string, object?>? Properties);

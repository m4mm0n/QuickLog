using System.Text.Json.Serialization;

namespace QuickLog.Exceptions;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CrashReport))]
internal sealed partial class CrashReportSerializationContext : JsonSerializerContext;

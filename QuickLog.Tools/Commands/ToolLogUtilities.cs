using System.Text.Json;
using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>
/// Provides shared dependency-free log reading helpers for CLI commands.
/// </summary>
internal static class ToolLogUtilities
{
    /// <summary>
    /// Reads entries from a supported log file.
    /// </summary>
    /// <param name="path">Path to a QLOG or JSON Lines file.</param>
    public static IReadOnlyList<LogEntry> ReadEntries(string path)
    {
        if (IsBinaryLog(path))
            return BinaryLogReader.Read(path, stopOnCrcError: false).ToList();

        if (Path.GetExtension(path).Equals(".jsonl", StringComparison.OrdinalIgnoreCase))
            return ReadJsonLines(path);

        return [];
    }

    /// <summary>
    /// Writes a compact summary to the supplied console.
    /// </summary>
    /// <param name="summary">Summary to write.</param>
    /// <param name="console">Console abstraction receiving output.</param>
    public static void WriteSummary(BinaryLogSummary summary, IToolConsole console)
    {
        console.WriteLine($"Entries: {summary.EntryCount}");
        if (summary.FirstTimestampUtc is not null)
            console.WriteLine($"First: {summary.FirstTimestampUtc:O}");
        if (summary.LastTimestampUtc is not null)
            console.WriteLine($"Last: {summary.LastTimestampUtc:O}");

        foreach (var pair in summary.LevelCounts.OrderBy(pair => pair.Key.ToString()))
            console.WriteLine($"{pair.Key}: {pair.Value}");

        foreach (var message in summary.TopMessages.Take(5))
            console.WriteLine($"Top: {message.Count} x {message.Message}");

        foreach (var pair in summary.EventCounts.OrderByDescending(pair => pair.Value).Take(5))
            console.WriteLine($"Event: {pair.Value} x {pair.Key}");

        foreach (var pair in summary.PropertyCounts.OrderByDescending(pair => pair.Value).Take(5))
            console.WriteLine($"Property: {pair.Value} x {pair.Key}");
    }

    /// <summary>Tests an entry against a <c>name</c> or <c>name=value</c> property expression.</summary>
    /// <param name="entry">The entry to inspect.</param>
    /// <param name="expression">The property expression.</param>
    /// <returns><see langword="true"/> when the expression matches.</returns>
    public static bool MatchesProperty(in LogEntry entry, string expression)
    {
        if (entry.Properties is null || string.IsNullOrWhiteSpace(expression))
            return false;

        var separator = expression.IndexOf('=');
        var name = separator < 0 ? expression : expression[..separator];
        var expected = separator < 0 ? null : expression[(separator + 1)..];
        var pair = entry.Properties.FirstOrDefault(candidate =>
            string.Equals(candidate.Key, name, StringComparison.OrdinalIgnoreCase));
        if (pair.Key is null)
            return false;

        return expected is null
               || string.Equals(LogProperties.FormatValue(pair.Value), expected, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns true when the file extension denotes a binary QLOG file.
    /// </summary>
    /// <param name="path">Path to inspect.</param>
    public static bool IsBinaryLog(string path)
        => Path.GetExtension(path).Equals(".qlog", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Enumerates log-like files below a path.
    /// </summary>
    /// <param name="path">File or directory to enumerate.</param>
    /// <param name="recursive">Whether subdirectories should be scanned.</param>
    public static IEnumerable<string> EnumerateLogFiles(string path, bool recursive)
    {
        if (File.Exists(path))
            return [path];

        if (!Directory.Exists(path))
            return [];

        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.EnumerateFiles(path, "*", option)
            .Where(IsSupportedLogFile)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Reads text lines while allowing the producer process to keep the file open for writing.
    /// </summary>
    /// <param name="path">Path to the text log file.</param>
    public static IEnumerable<string> ReadTextLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        while (reader.ReadLine() is { } line)
            yield return line;
    }

    private static bool IsSupportedLogFile(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".qlog", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jsonl", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<LogEntry> ReadJsonLines(string path)
    {
        var entries = new List<LogEntry>();
        foreach (var line in ReadTextLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(line);
                var root = doc.RootElement;
                entries.Add(new LogEntry(
                    ReadDate(root, "ts") ?? DateTime.UtcNow,
                    ReadLevel(root, "level"),
                    ReadString(root, "msg") ?? line,
                    "JsonLines",
                    ReadString(root, "scope"),
                    ReadString(root, "member") ?? string.Empty,
                    ReadString(root, "file") ?? string.Empty,
                    ReadInt(root, "line"),
                    ReadInt(root, "thread"),
                    ThreadRole.Unknown,
                    ReadString(root, "correlation"),
                    ReadString(root, "trace"),
                    ReadString(root, "span"),
                    new LogEventId(ReadInt(root, "eventId"), ReadString(root, "eventName")),
                    ReadProperties(root)));
            }
            catch
            {
                entries.Add(new LogEntry(DateTime.UtcNow, LogType.Info, line, "Text", null, string.Empty, string.Empty, 0, 0, ThreadRole.Unknown));
            }
        }

        return entries;
    }

    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int ReadInt(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    private static DateTime? ReadDate(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            && DateTime.TryParse(value.GetString(), out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : null;

    private static LogType ReadLevel(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            && Enum.TryParse<LogType>(value.GetString(), ignoreCase: true, out var parsed)
                ? parsed
                : LogType.Info;

    private static IReadOnlyDictionary<string, object?> ReadProperties(JsonElement element)
    {
        if (!element.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
            return LogProperties.Empty;

        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties.EnumerateObject())
            values[property.Name] = ReadValue(property.Value);
        return LogProperties.Snapshot(values);
    }

    private static object? ReadValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number when value.TryGetDouble(out var number) => number,
        _ => value.GetRawText()
    };
}

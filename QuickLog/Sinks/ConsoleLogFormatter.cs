using QuickLog.Core;

namespace QuickLog.Sinks;

/// <summary>
/// Formats console log entries using compact/full, UTC/local, and ANSI-color options.
/// </summary>
internal static class ConsoleLogFormatter
{
    /// <summary>
    /// Formats a log entry for console output.
    /// </summary>
    /// <param name="entry">Log entry to format.</param>
    /// <param name="compact">Whether to omit file and line details.</param>
    /// <param name="useLocalTime">Whether timestamps should be converted to local time.</param>
    /// <param name="ansi">Whether ANSI color escape sequences should be emitted.</param>
    public static string Format(in LogEntry entry, bool compact, bool useLocalTime, bool ansi)
    {
        var timestamp = useLocalTime ? entry.Timestamp.ToLocalTime() : entry.Timestamp;
        var level = entry.Level.ToString();
        var prefix = $"[{timestamp:HH:mm:ss}] [{level}]";
        var body = compact
            ? $"{prefix} {entry.Message}"
            : $"{prefix} [{entry.MemberName}] [{entry.FilePath}:{entry.LineNumber}] {entry.Message}";

        return ansi ? $"{AnsiFor(entry.Level)}{body}\u001b[0m" : body;
    }

    private static string AnsiFor(LogType level) => level switch
    {
        LogType.Trace => "\u001b[90m",
        LogType.Debug => "\u001b[37m",
        LogType.Info => "\u001b[0m",
        LogType.Warn => "\u001b[33m",
        LogType.Error => "\u001b[31m",
        LogType.Crit => "\u001b[35m",
        LogType.Exception => "\u001b[91m",
        _ => "\u001b[0m"
    };
}

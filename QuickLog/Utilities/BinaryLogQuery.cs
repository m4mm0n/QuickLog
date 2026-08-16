using QuickLog.Core;

namespace QuickLog.Utilities;

/// <summary>
/// Provides filtered views over binary log files without loading them fully into memory.
/// </summary>
public static class BinaryLogQuery
{
    /// <summary>
    /// Enumerates log entries within a given UTC time range.
    /// </summary>
    public static IEnumerable<LogEntry> Between(
        string path,
        DateTime utcFrom,
        DateTime utcTo,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => e.Timestamp >= utcFrom).TakeWhile(e => e.Timestamp <= utcTo);

    /// <summary>
    /// Enumerates log entries matching the given log level mask.
    /// </summary>
    public static IEnumerable<LogEntry> WithLevel(
        string path,
        LogType mask,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => (e.Level & mask) != 0);

    /// <summary>
    /// Enumerates log entries matching the given correlation identifier.
    /// </summary>
    public static IEnumerable<LogEntry> WithCorrelation(
        string path,
        string correlationId,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError)
            .Where(e => string.Equals(e.CorrelationId, correlationId, StringComparison.Ordinal));

    /// <summary>Enumerates log entries matching an event identifier.</summary>
    /// <param name="path">The QLOG path.</param>
    /// <param name="eventId">The numeric event identifier.</param>
    /// <param name="stopOnCrcError">Whether to stop at the first CRC mismatch.</param>
    /// <returns>The matching entries.</returns>
    public static IEnumerable<LogEntry> WithEventId(
        string path,
        int eventId,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(entry => entry.EventId.Id == eventId);

    /// <summary>Enumerates log entries containing a structured property with an optional expected value.</summary>
    /// <param name="path">The QLOG path.</param>
    /// <param name="name">The property name.</param>
    /// <param name="value">The optional invariant text value to match.</param>
    /// <param name="comparison">The text comparison used for values.</param>
    /// <param name="stopOnCrcError">Whether to stop at the first CRC mismatch.</param>
    /// <returns>The matching entries.</returns>
    public static IEnumerable<LogEntry> WithProperty(
        string path,
        string name,
        string? value = null,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(entry =>
            entry.Properties is not null
            && entry.Properties.TryGetValue(name, out var actual)
            && (value is null || string.Equals(LogProperties.FormatValue(actual), value, comparison)));

    /// <summary>
    /// Enumerates log entries whose message contains the given text.
    /// </summary>
    public static IEnumerable<LogEntry> ContainingText(
        string path,
        string text,
        StringComparison comparison = StringComparison.OrdinalIgnoreCase,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError)
            .Where(e => e.Message.Contains(text, comparison));

    /// <summary>
    /// Enumerates log entries matching an arbitrary predicate.
    /// </summary>
    public static IEnumerable<LogEntry> Where(
        string path,
        Func<LogEntry, bool> predicate,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => predicate(e));
}

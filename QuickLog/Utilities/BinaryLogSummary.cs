using QuickLog.Core;

namespace QuickLog.Utilities;

/// <summary>
/// Counts repeated binary-log messages.
/// </summary>
/// <param name="Message">Message text.</param>
/// <param name="Count">Number of occurrences.</param>
public sealed record BinaryLogMessageCount(string Message, int Count);

/// <summary>
/// Compact summary of binary log entries.
/// </summary>
/// <param name="EntryCount">Total entries summarized.</param>
/// <param name="FirstTimestampUtc">Earliest timestamp in the summarized entries.</param>
/// <param name="LastTimestampUtc">Latest timestamp in the summarized entries.</param>
/// <param name="LevelCounts">Counts by log level.</param>
/// <param name="TopMessages">Most repeated messages.</param>
/// <param name="Correlations">Distinct correlation identifiers.</param>
public sealed record BinaryLogSummary(
    int EntryCount,
    DateTime? FirstTimestampUtc,
    DateTime? LastTimestampUtc,
    IReadOnlyDictionary<LogType, int> LevelCounts,
    IReadOnlyList<BinaryLogMessageCount> TopMessages,
    IReadOnlyList<string> Correlations)
{
    /// <summary>
    /// Builds a summary from in-memory entries.
    /// </summary>
    /// <param name="entries">Entries to summarize.</param>
    /// <returns>A summary built from the supplied entries.</returns>
    public static BinaryLogSummary FromEntries(IEnumerable<LogEntry> entries)
    {
        var list = entries.OrderBy(entry => entry.Timestamp).ToList();
        var levelCounts = list
            .GroupBy(entry => entry.Level)
            .ToDictionary(group => group.Key, group => group.Count());

        var topMessages = list
            .GroupBy(entry => entry.Message)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Take(10)
            .Select(group => new BinaryLogMessageCount(group.Key, group.Count()))
            .ToList();

        var correlations = list
            .Select(entry => entry.CorrelationId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .Cast<string>()
            .ToList();

        return new BinaryLogSummary(
            list.Count,
            list.Count == 0 ? null : list.First().Timestamp,
            list.Count == 0 ? null : list.Last().Timestamp,
            levelCounts,
            topMessages,
            correlations);
    }

    /// <summary>
    /// Reads a QLOG file and builds a summary.
    /// </summary>
    /// <param name="path">Binary log path.</param>
    /// <returns>A summary of the readable entries in the file.</returns>
    public static BinaryLogSummary FromFile(string path) => FromEntries(BinaryLogReader.Read(path));
}

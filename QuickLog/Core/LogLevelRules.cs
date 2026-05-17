namespace QuickLog.Core;

/// <summary>
/// Evaluates global and per-sink minimum-level rules for log entries.
/// </summary>
internal sealed class LogLevelRules
{
    /// <summary>
    /// Gets or sets the minimum level accepted by the logger as a whole.
    /// </summary>
    public LogType MinimumLevel { get; set; } = LogType.Trace;

    /// <summary>
    /// Gets per-sink minimum levels keyed by a stable sink name.
    /// </summary>
    public Dictionary<string, LogType> SinkMinimumLevels { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns whether the log level is accepted globally.
    /// </summary>
    /// <param name="level">Level to test.</param>
    public bool Allows(LogType level) => level >= MinimumLevel;

    /// <summary>
    /// Returns whether the log level is accepted by a named sink.
    /// </summary>
    /// <param name="sinkName">Stable sink name.</param>
    /// <param name="level">Level to test.</param>
    public bool AllowsSink(string sinkName, LogType level)
        => !SinkMinimumLevels.TryGetValue(sinkName, out var minimum) || level >= minimum;
}

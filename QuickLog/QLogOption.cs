namespace QuickLog;

/// <summary>
/// Selects which markers a <see cref="QLOGAttribute"/> should emit when a helper runs a marked target.
/// </summary>
[Flags]
public enum QLogOption
{
    /// <summary>No automatic QLOG markers are emitted.</summary>
    None = 0,

    /// <summary>Emit an entry marker before the target runs.</summary>
    Entry = 1 << 0,

    /// <summary>Emit an exit marker after the target completes.</summary>
    Exit = 1 << 1,

    /// <summary>Include elapsed milliseconds in the exit marker.</summary>
    Timing = 1 << 2,

    /// <summary>Emit an exception marker before rethrowing failures from runner helpers.</summary>
    Exceptions = 1 << 3,

    /// <summary>Default lean instrumentation: entry, exit, timing, and exceptions.</summary>
    Default = Entry | Exit | Timing | Exceptions
}

/// <summary>
/// Convenience constants for readable <c>[QLOG(LoggingOption.Timing)]</c> usage.
/// </summary>
public static class LoggingOption
{
    /// <summary>No automatic QLOG markers are emitted.</summary>
    public const QLogOption None = QLogOption.None;

    /// <summary>Emit an entry marker before the target runs.</summary>
    public const QLogOption Entry = QLogOption.Entry;

    /// <summary>Emit an exit marker after the target completes.</summary>
    public const QLogOption Exit = QLogOption.Exit;

    /// <summary>Include elapsed milliseconds in the exit marker.</summary>
    public const QLogOption Timing = QLogOption.Timing;

    /// <summary>Emit an exception marker before rethrowing failures from runner helpers.</summary>
    public const QLogOption Exceptions = QLogOption.Exceptions;

    /// <summary>Default lean instrumentation: entry, exit, timing, and exceptions.</summary>
    public const QLogOption Default = QLogOption.Default;
}

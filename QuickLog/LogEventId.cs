namespace QuickLog;

/// <summary>
/// Identifies a stable kind of log event with an optional human-readable name.
/// </summary>
/// <param name="Id">The numeric event identifier. Zero represents an unspecified event.</param>
/// <param name="Name">The optional event name.</param>
public readonly record struct LogEventId(int Id, string? Name = null)
{
    /// <summary>Gets an event identifier that represents no specific event.</summary>
    public static LogEventId None => default;

    /// <summary>Returns the event name when available, otherwise the numeric identifier.</summary>
    /// <returns>A compact event identifier.</returns>
    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? Id.ToString() : $"{Name}({Id})";
}

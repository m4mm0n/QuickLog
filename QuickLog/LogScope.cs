namespace QuickLog;

/// <summary>
/// Public scope helper for grouping related log entries.
/// </summary>
public static class LogScope
{
    /// <summary>Gets the current async-flowing scope.</summary>
    public static string? Current => LogContext.CurrentScope;

    /// <summary>Begins a scope that is restored when the returned handle is disposed.</summary>
    public static IDisposable Begin(string name, object? value = null) => LogContext.BeginScope(name, value);
}

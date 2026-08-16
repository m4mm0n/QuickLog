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

    /// <summary>Begins a scope of structured values that flows across async continuations.</summary>
    /// <param name="properties">The properties to attach while the returned handle is active.</param>
    /// <returns>A handle that restores the previous scope when disposed.</returns>
    public static IDisposable Begin(IReadOnlyDictionary<string, object?> properties) =>
        LogContext.BeginProperties(properties);

    /// <summary>Begins a scope containing the supplied structured values.</summary>
    /// <param name="properties">The properties to attach while the returned handle is active.</param>
    /// <returns>A handle that restores the previous scope when disposed.</returns>
    public static IDisposable Begin(params LogProperty[] properties) =>
        LogContext.BeginProperties(LogProperties.Create(properties));
}

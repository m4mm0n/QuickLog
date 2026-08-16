using System.Diagnostics;
using System.Threading;

namespace QuickLog;

/// <summary>
/// Carries async-safe logging context such as scope names and correlation identifiers.
/// </summary>
public static class LogContext
{
    private sealed class Frame
    {
        public string? Scope { get; init; }
        public string? CorrelationId { get; init; }
        public IReadOnlyDictionary<string, object?> Properties { get; init; } = LogProperties.Empty;
    }

    private sealed class PopHandle(Frame? previous) : IDisposable
    {
        public void Dispose() => _current.Value = previous;
    }

    private static readonly AsyncLocal<Frame?> _current = new();

    /// <summary>Gets the current logical scope flowing with async execution.</summary>
    public static string? CurrentScope => _current.Value?.Scope;

    /// <summary>Gets the current correlation identifier flowing with async execution.</summary>
    public static string? CurrentCorrelationId => _current.Value?.CorrelationId;

    /// <summary>Gets an immutable snapshot of the structured properties flowing with async execution.</summary>
    public static IReadOnlyDictionary<string, object?> CurrentProperties =>
        _current.Value?.Properties ?? LogProperties.Empty;

    /// <summary>Gets the current <see cref="Activity"/> trace id when an activity is active.</summary>
    public static string? CurrentTraceId => Activity.Current?.TraceId.ToString();

    /// <summary>Gets the current <see cref="Activity"/> span id when an activity is active.</summary>
    public static string? CurrentSpanId => Activity.Current?.SpanId.ToString();

    /// <summary>Begins a logical scope that flows across async continuations.</summary>
    public static IDisposable BeginScope(string name, object? value = null)
    {
        var previous = _current.Value;
        _current.Value = new Frame
        {
            Scope = value is null ? name : $"{name}:{value}",
            CorrelationId = previous?.CorrelationId,
            Properties = value is null
                ? previous?.Properties ?? LogProperties.Empty
                : LogProperties.Merge(previous?.Properties, LogProperties.Create(new LogProperty(name, value)))
        };
        return new PopHandle(previous);
    }

    /// <summary>Begins a correlation identifier that flows across async continuations.</summary>
    public static IDisposable BeginCorrelation(string correlationId)
    {
        var previous = _current.Value;
        _current.Value = new Frame
        {
            Scope = previous?.Scope,
            CorrelationId = correlationId,
            Properties = previous?.Properties ?? LogProperties.Empty
        };
        return new PopHandle(previous);
    }

    /// <summary>Begins a structured-property scope that flows across async continuations.</summary>
    /// <param name="properties">The properties to merge into the current context.</param>
    /// <returns>A handle that restores the previous context when disposed.</returns>
    public static IDisposable BeginProperties(IReadOnlyDictionary<string, object?> properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var previous = _current.Value;
        _current.Value = new Frame
        {
            Scope = previous?.Scope,
            CorrelationId = previous?.CorrelationId,
            Properties = LogProperties.Merge(previous?.Properties, properties)
        };
        return new PopHandle(previous);
    }
}

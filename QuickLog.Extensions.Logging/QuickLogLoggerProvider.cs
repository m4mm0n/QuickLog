using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace QuickLog.Extensions.Logging;

/// <summary>
/// Provides <see cref="ILogger"/> instances that forward events to an <see cref="IQuickLog"/> owner.
/// </summary>
public sealed class QuickLogLoggerProvider : ILoggerProvider, ISupportExternalScope, IAsyncDisposable
{
    private readonly IQuickLog _logger;
    private readonly bool _disposeLogger;
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();
    private int _disposed;

    /// <summary>
    /// Initializes a provider around an existing QuickLog logger.
    /// </summary>
    /// <param name="logger">The QuickLog owner that receives forwarded events.</param>
    /// <param name="disposeLogger">Whether disposing this provider should also dispose the supplied logger.</param>
    public QuickLogLoggerProvider(IQuickLog logger, bool disposeLogger = false)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _disposeLogger = disposeLogger;
    }

    /// <summary>Creates a logger for a category.</summary>
    /// <param name="categoryName">The category name.</param>
    /// <returns>A category logger.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        return new AdapterLogger(_logger, categoryName, () => _scopeProvider);
    }

    /// <summary>Sets the external scope provider supplied by the logging factory.</summary>
    /// <param name="scopeProvider">The scope provider.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) =>
        _scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));

    /// <summary>Disposes the provider and optionally its QuickLog owner.</summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        if (_disposeLogger)
            _logger.Dispose();
    }

    /// <summary>Disposes the provider and optionally flushes and disposes its QuickLog owner asynchronously.</summary>
    /// <returns>An operation that completes after owned resources are released.</returns>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        if (_disposeLogger)
            await _logger.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class AdapterLogger(
        IQuickLog logger,
        string category,
        Func<IExternalScopeProvider> getScopeProvider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            getScopeProvider().Push(state);

        public bool IsEnabled(LogLevel logLevel) =>
            logLevel != LogLevel.None && logger.IsEnabled(MapLevel(logLevel));

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            ArgumentNullException.ThrowIfNull(formatter);

            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            AddState(properties, state);
            var scopeIndex = 0;
            getScopeProvider().ForEachScope((scope, target) =>
            {
                AddState(target, scope, $"Scope{scopeIndex++}");
            }, properties);
            properties["Category"] = category;

            var quickEvent = new LogEventId(eventId.Id, string.IsNullOrWhiteSpace(eventId.Name) ? null : eventId.Name);
            var message = formatter(state, exception);
            if (exception is null)
                logger.Log(MapLevel(logLevel), message, quickEvent, properties);
            else
                logger.Log(MapLevel(logLevel), message, exception, quickEvent, properties);
        }
    }

    private static void AddState(
        IDictionary<string, object?> properties,
        object? state,
        string? fallbackName = null)
    {
        if (state is IEnumerable<KeyValuePair<string, object?>> values)
        {
            foreach (var pair in values)
                properties[pair.Key == "{OriginalFormat}" ? "OriginalFormat" : pair.Key] = pair.Value;
            return;
        }

        if (state is not null)
            properties[fallbackName ?? "State"] = state;
    }

    private static LogType MapLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => LogType.Trace,
        LogLevel.Debug => LogType.Debug,
        LogLevel.Information => LogType.Info,
        LogLevel.Warning => LogType.Warn,
        LogLevel.Error => LogType.Error,
        LogLevel.Critical => LogType.Crit,
        _ => LogType.Info
    };
}

/// <summary>
/// Adds QuickLog to a Microsoft logging builder while keeping the supplied QuickLog instance as the owner.
/// </summary>
public static class QuickLogLoggingBuilderExtensions
{
    /// <summary>Adds a QuickLog provider to a logging builder.</summary>
    /// <param name="builder">The logging builder.</param>
    /// <param name="logger">The QuickLog owner that receives events.</param>
    /// <param name="disposeLogger">Whether the provider should dispose the supplied logger.</param>
    /// <returns>The same builder for chaining.</returns>
    public static ILoggingBuilder AddQuickLog(
        this ILoggingBuilder builder,
        IQuickLog logger,
        bool disposeLogger = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(logger);
        builder.Services.AddSingleton<ILoggerProvider>(new QuickLogLoggerProvider(logger, disposeLogger));
        return builder;
    }
}

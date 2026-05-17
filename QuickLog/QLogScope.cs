using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace QuickLog;

/// <summary>
/// Represents one active dependency-free QLOG instrumentation scope.
/// </summary>
public sealed class QLogScope : IDisposable
{
    private readonly IQuickLog _logger;
    private readonly QLOGAttribute? _attribute;
    private readonly string _displayName;
    private readonly string _callerName;
    private readonly string _callerFilePath;
    private readonly int _callerLineNumber;
    private readonly Stopwatch _stopwatch;
    private bool _exceptionLogged;
    private bool _disposed;

    /// <summary>
    /// Creates a scope and emits the entry marker when requested.
    /// </summary>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="attribute">Resolved QLOG marker, or <see langword="null"/> for a no-op scope.</param>
    /// <param name="displayName">Name written to QLOG marker messages.</param>
    /// <param name="callerName">Caller name used in log metadata.</param>
    /// <param name="callerFilePath">Caller file path used in log metadata.</param>
    /// <param name="callerLineNumber">Caller line number used in log metadata.</param>
    private QLogScope(
        IQuickLog logger,
        QLOGAttribute? attribute,
        string displayName,
        string callerName,
        string callerFilePath,
        int callerLineNumber)
    {
        _logger = logger;
        _attribute = attribute;
        _displayName = displayName;
        _callerName = callerName;
        _callerFilePath = callerFilePath;
        _callerLineNumber = callerLineNumber;
        _stopwatch = Stopwatch.StartNew();

        if (Has(QLogOption.Entry))
            _logger.Log(_attribute!.Level, $"QLOG ENTER {_displayName}", _callerName, _callerFilePath, _callerLineNumber);
    }

    /// <summary>
    /// Enters a QLOG scope for the calling method when the method or declaring type is marked.
    /// </summary>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="callerName">Compiler-provided fallback caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
    /// <returns>An active QLOG scope, or a no-op scope when no marker is found.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static QLogScope Enter(
        IQuickLog logger,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace(skipFrames: 1, fNeedFileInfo: false).GetFrame(0)?.GetMethod();
        return Enter(logger, method, callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Enters a QLOG scope for an explicit method or constructor.
    /// </summary>
    /// <param name="logger">Logger that receives QLOG markers.</param>
    /// <param name="method">Method or constructor whose QLOG marker should be used.</param>
    /// <param name="callerName">Fallback caller name used in emitted log metadata.</param>
    /// <param name="callerFilePath">Fallback caller file path used in emitted log metadata.</param>
    /// <param name="callerLineNumber">Fallback caller line used in emitted log metadata.</param>
    /// <returns>An active QLOG scope, or a no-op scope when no marker is found.</returns>
    public static QLogScope Enter(
        IQuickLog logger,
        MethodBase? method,
        string callerName = "",
        string callerFilePath = "",
        int callerLineNumber = 0)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var attribute = QLogMetadata.Resolve(method);
        var displayName = QLogMetadata.DisplayName(method, attribute, callerName);
        return new QLogScope(logger, attribute, displayName, callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Emits an exception marker once for the active target.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    public void Fail(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (_exceptionLogged || !Has(QLogOption.Exceptions))
            return;

        _exceptionLogged = true;
        _logger.Log(_attribute!.ExceptionLevel, $"QLOG EXCEPTION {_displayName}", exception, _callerName, _callerFilePath, _callerLineNumber);
    }

    /// <summary>
    /// Emits the exit marker once when the active target requested exit or timing markers.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _stopwatch.Stop();

        if (!Has(QLogOption.Exit) && !Has(QLogOption.Timing))
            return;

        var suffix = Has(QLogOption.Timing)
            ? $" durationMs={_stopwatch.Elapsed.TotalMilliseconds:0.###}"
            : string.Empty;
        _logger.Log(_attribute!.Level, $"QLOG EXIT {_displayName}{suffix}", _callerName, _callerFilePath, _callerLineNumber);
    }

    /// <summary>
    /// Returns whether the active marker includes a specific option.
    /// </summary>
    /// <param name="option">Option to inspect.</param>
    private bool Has(QLogOption option)
        => _attribute is not null && (_attribute.Options & option) == option;
}

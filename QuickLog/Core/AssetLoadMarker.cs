namespace QuickLog.Core;

/// <summary>
/// Logs begin, end, and failure markers for an asset-loading operation.
/// </summary>
internal sealed class AssetLoadMarker : IDisposable
{
    private readonly IQuickLog _logger;
    private readonly string _assetName;
    private readonly string _callerName;
    private readonly string _callerFilePath;
    private readonly int _callerLineNumber;
    private readonly DateTime _startedUtc;
    private bool _failed;
    private bool _disposed;

    /// <summary>
    /// Creates and writes the begin marker.
    /// </summary>
    /// <param name="logger">Logger that receives asset markers.</param>
    /// <param name="assetName">Logical asset name.</param>
    /// <param name="callerName">Caller member name.</param>
    /// <param name="callerFilePath">Caller source path.</param>
    /// <param name="callerLineNumber">Caller source line.</param>
    public AssetLoadMarker(IQuickLog logger, string assetName, string callerName, string callerFilePath, int callerLineNumber)
    {
        _logger = logger;
        _assetName = assetName;
        _callerName = callerName;
        _callerFilePath = callerFilePath;
        _callerLineNumber = callerLineNumber;
        _startedUtc = DateTime.UtcNow;
        _logger.Log(LogType.Info, $"ASSET LOAD BEGIN asset=\"{_assetName}\"", _callerName, _callerFilePath, _callerLineNumber);
    }

    /// <summary>
    /// Writes a failure marker for the asset load.
    /// </summary>
    /// <param name="exception">Failure exception.</param>
    public void Fail(Exception exception)
    {
        if (_failed)
            return;

        _failed = true;
        _logger.Log(LogType.Error, $"ASSET LOAD FAIL asset=\"{_assetName}\"", exception, _callerName, _callerFilePath, _callerLineNumber);
    }

    /// <summary>
    /// Writes the end marker when no failure marker was written.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_failed)
            return;

        var ms = (long)(DateTime.UtcNow - _startedUtc).TotalMilliseconds;
        _logger.Log(LogType.Info, $"ASSET LOAD END asset=\"{_assetName}\" durationMs={ms}", _callerName, _callerFilePath, _callerLineNumber);
    }
}

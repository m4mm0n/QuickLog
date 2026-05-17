namespace QuickLog.Core;

/// <summary>
/// Writes scoped session, checkpoint, and bookmark markers to a logger.
/// </summary>
public sealed class LogSession : IDisposable
{
    private readonly IQuickLog _logger;
    private readonly string _name;
    private readonly string _sessionId;
    private readonly DateTime _startedUtc;
    private bool _disposed;

    private LogSession(IQuickLog logger, string name, string sessionId)
    {
        _logger = logger;
        _name = name;
        _sessionId = sessionId;
        _startedUtc = DateTime.UtcNow;
        _logger.Log(LogType.Info, $"SESSION BEGIN {_name} session={_sessionId}");
    }

    /// <summary>
    /// Begins a new log session and writes the begin marker immediately.
    /// </summary>
    /// <param name="logger">Logger that receives session markers.</param>
    /// <param name="name">Human-readable session name.</param>
    /// <param name="sessionId">Optional session id. A GUID is generated when omitted.</param>
    public static LogSession Begin(IQuickLog logger, string name, string? sessionId = null)
        => new(logger, name, sessionId ?? Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Writes a checkpoint marker inside the active session.
    /// </summary>
    /// <param name="name">Checkpoint name.</param>
    public void Checkpoint(string name)
        => _logger.Log(LogType.Info, $"CHECKPOINT {name} session={_sessionId}");

    /// <summary>
    /// Writes a bookmark marker inside the active session.
    /// </summary>
    /// <param name="name">Bookmark name.</param>
    public void Bookmark(string name)
        => _logger.Log(LogType.Info, $"BOOKMARK {name} session={_sessionId}");

    /// <summary>
    /// Writes the end marker once.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        var ms = (long)(DateTime.UtcNow - _startedUtc).TotalMilliseconds;
        _logger.Log(LogType.Info, $"SESSION END {_name} session={_sessionId} durationMs={ms}");
    }
}

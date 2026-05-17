namespace QuickLog.Core;

/// <summary>
/// Builds compact startup and shutdown diagnostic messages.
/// </summary>
internal static class LogRuntimeSnapshot
{
    /// <summary>
    /// Creates a startup banner message for the current process.
    /// </summary>
    /// <param name="sessionName">Logical session name.</param>
    /// <param name="sessionId">Stable session identifier.</param>
    public static string Startup(string sessionName, string sessionId)
        => $"STARTUP app={AppDomain.CurrentDomain.FriendlyName} sessionName={sessionName} session={sessionId} pid={Environment.ProcessId} os=\"{Environment.OSVersion}\" runtime=\"{Environment.Version}\" cwd=\"{Environment.CurrentDirectory}\"";

    /// <summary>
    /// Creates a shutdown summary message from runtime duration and dispatcher stats.
    /// </summary>
    /// <param name="startedUtc">UTC time when the logger started.</param>
    /// <param name="stats">Dispatcher statistics snapshot.</param>
    /// <param name="sessionId">Stable session identifier.</param>
    public static string Shutdown(DateTime startedUtc, LogDispatcherStats stats, string sessionId)
    {
        var durationMs = (long)(DateTime.UtcNow - startedUtc).TotalMilliseconds;
        return $"SHUTDOWN durationMs={durationMs} session={sessionId} written={stats.Written} dropped={stats.DroppedTotal} sinkFailures={stats.SinkFailures}";
    }
}

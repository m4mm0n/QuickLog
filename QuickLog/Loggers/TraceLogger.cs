using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// Trace Logger for QuickLog
/// </summary>
public class TraceLogger : IQuickLog
{
    /// <summary>
    /// Event handler for all logging events
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;
    public void Log(LogType logType, string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, message, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }

    public void Log(LogType logType, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, exception, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }

    public void Log(LogType logType, string message, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }
    public void TraceMethodEntry(
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            string message = $"Entering method: {method.DeclaringType?.Name}.{method.Name}";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }

    public void TraceMethodExit(Stopwatch stopwatch,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var method = new StackTrace().GetFrame(1)?.GetMethod();
        if (method != null)
        {
            stopwatch.Stop();
            string message = $"Exiting method: {method.DeclaringType?.Name}.{method.Name}. Execution time: {stopwatch.ElapsedMilliseconds} ms.";
            Log(LogType.Trace, message, callerName, callerFilePath, callerLineNumber);
        }
    }

    private void HandleLog(LogEventArgs logEventArgs)
    {
        // Trigger the log event for any listeners
        LogEvent?.Invoke(this, logEventArgs);

        // Output the trace log (e.g., to debug output)
        Trace.WriteLine(logEventArgs.ToString());
    }
}
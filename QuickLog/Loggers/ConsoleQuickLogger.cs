using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// Console Logger for QuickLog
/// </summary>
public class ConsoleQuickLogger : IQuickLog
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

    private void HandleLog(LogEventArgs logEventArgs)
    {
        // Trigger the log event for any listeners
        LogEvent?.Invoke(this, logEventArgs);

        // Write to the console
        Console.WriteLine(logEventArgs.ToString());
    }
}
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// A logger that outputs log entries to the console. 
/// This logger is useful for quick, real-time logging to the console during application execution.
/// </summary>
public class ConsoleQuickLogger : IQuickLog
{
    /// <summary>
    /// Occurs when a log event is triggered.
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Logs a message with the specified log type and caller information.
    /// The log message is printed to the console.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void Log(LogType logType, string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0) =>
        HandleLog(new LogEventArgs(logType, message, callerName, callerFilePath, callerLineNumber));

    /// <summary>
    /// Logs an exception with the specified log type and caller information.
    /// The exception details are printed to the console.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void Log(LogType logType, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0) =>
        HandleLog(new LogEventArgs(logType, exception, callerName, callerFilePath, callerLineNumber));

    /// <summary>
    /// Logs a message and an exception with the specified log type and caller information.
    /// The log message and exception details are printed to the console.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void Log(LogType logType, string message, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0) =>
        HandleLog(new LogEventArgs(logType, message, exception, callerName, callerFilePath, callerLineNumber));

    /// <summary>
    /// Handles the logging process by invoking the log event and outputting the log message to the console.
    /// </summary>
    /// <param name="logEventArgs">The log event arguments containing the log details.</param>
    private void HandleLog(LogEventArgs logEventArgs)
    {
        // Trigger the log event for any listeners
        LogEvent?.Invoke(this, logEventArgs);

        // Write to the console
        Console.WriteLine(logEventArgs.ToString());
    }
    /// <summary>
    /// Disposes of the ConsoleQuickLogger instance.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose
    }
}

using System.Runtime.CompilerServices;

namespace QuickLog;

/// <summary>
/// Interface for QuickLog's various adoptions
/// </summary>
public interface IQuickLog : IDisposable
{
    /// <summary>
    /// Event handler for the logging events.
    /// </summary>
    event EventHandler<LogEventArgs> LogEvent;

    /// <summary>
    /// Logs a message with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="callerName">The name of the method that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerFilePath">The file path of the source code that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerLineNumber">The line number in the source file where the log was initiated. Automatically provided by the compiler.</param>
    void Log(LogType logType, string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Logs an exception with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="exception">The exception related to the log.</param>
    /// <param name="callerName">The name of the method that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerFilePath">The file path of the source code that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerLineNumber">The line number in the source file where the log was initiated. Automatically provided by the compiler.</param>
    void Log(LogType logType, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0);

    /// <summary>
    /// Logs a message and an exception with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">The exception related to the log.</param>
    /// <param name="callerName">The name of the method that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerFilePath">The file path of the source code that initiated the log. Automatically provided by the compiler.</param>
    /// <param name="callerLineNumber">The line number in the source file where the log was initiated. Automatically provided by the compiler.</param>
    void Log(LogType logType, string message, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0);
}
using System.Runtime.CompilerServices;

namespace QuickLog;

/// <summary>
/// Interface for QuickLog's various adoptions
/// </summary>
public interface IQuickLog : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Event handler for the logging events.
    /// </summary>
    event EventHandler<LogEventArgs> LogEvent;

    /// <summary>Returns whether the logger accepts the supplied level.</summary>
    /// <param name="logType">The level to test.</param>
    /// <returns><see langword="true"/> when the level is enabled.</returns>
    bool IsEnabled(LogType logType) => true;

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
    /// Logs a message with a stable event identifier and structured properties.
    /// </summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="eventId">The stable event identifier.</param>
    /// <param name="properties">The structured properties.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    void Log(
        LogType logType,
        string message,
        LogEventId eventId,
        IReadOnlyDictionary<string, object?>? properties = null,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var suffix = LogProperties.Format(properties);
        Log(logType, suffix.Length == 0 ? message : $"{message} {suffix}", callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>
    /// Logs an interpolated message without evaluating formatted values when the level is disabled.
    /// </summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="message">The lazily built interpolated message.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    void Log(
        LogType logType,
        [InterpolatedStringHandlerArgument("", "logType")] ref QuickLogInterpolatedStringHandler message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        if (IsEnabled(logType))
            Log(logType, message.GetFormattedText(), callerName, callerFilePath, callerLineNumber);
    }

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

    /// <summary>Logs a message and exception with a stable event identifier and structured properties.</summary>
    /// <param name="logType">The type of the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">The exception related to the log.</param>
    /// <param name="eventId">The stable event identifier.</param>
    /// <param name="properties">The structured properties.</param>
    /// <param name="callerName">The compiler-provided caller name.</param>
    /// <param name="callerFilePath">The compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">The compiler-provided caller line number.</param>
    void Log(
        LogType logType,
        string message,
        Exception exception,
        LogEventId eventId,
        IReadOnlyDictionary<string, object?>? properties = null,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var detail = $"{message}{Environment.NewLine}{exception}";
        Log(logType, detail, eventId, properties, callerName, callerFilePath, callerLineNumber);
    }

    /// <summary>Flushes pending entries asynchronously.</summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>An operation that completes after pending entries are flushed.</returns>
    ValueTask FlushAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <summary>Disposes the logger asynchronously.</summary>
    /// <returns>An operation that completes after disposal.</returns>
    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

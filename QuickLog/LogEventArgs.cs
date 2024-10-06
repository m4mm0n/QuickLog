﻿using System.Diagnostics;

namespace QuickLog
{
    /// <summary>
    /// Event arguments for logging, containing information about the log type, message, exception, and caller details.
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the type of the log (e.g., Trace, Debug, Information).
        /// </summary>
        public LogType LoggingType { get; }

        /// <summary>
        /// Gets the log message, if provided.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// Gets the exception related to the log, if provided.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets the name of the method that initiated the log.
        /// </summary>
        public string CallerName { get; }

        /// <summary>
        /// Gets the file path of the source code that initiated the log.
        /// </summary>
        public string CallerFilePath { get; }

        /// <summary>
        /// Gets the line number in the source file at which the log was initiated.
        /// </summary>
        public int CallerLineNumber { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs"/> class using the log type, message, and caller information.
        /// </summary>
        /// <param name="logType">The type of log.</param>
        /// <param name="message">The log message.</param>
        /// <param name="callerName">The name of the method that initiated the log.</param>
        /// <param name="callerFilePath">The file path of the source code that initiated the log.</param>
        /// <param name="callerLineNumber">The line number in the source file where the log was initiated.</param>
        public LogEventArgs(LogType logType, string message, string callerName, string callerFilePath, int callerLineNumber)
            : this(logType, message, null, callerName, callerFilePath, callerLineNumber) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs"/> class using the log type, exception, and caller information.
        /// </summary>
        /// <param name="logType">The type of log.</param>
        /// <param name="exception">The exception related to the log.</param>
        /// <param name="callerName">The name of the method that initiated the log.</param>
        /// <param name="callerFilePath">The file path of the source code that initiated the log.</param>
        /// <param name="callerLineNumber">The line number in the source file where the log was initiated.</param>
        public LogEventArgs(LogType logType, Exception exception, string callerName, string callerFilePath, int callerLineNumber)
            : this(logType, null, exception, callerName, callerFilePath, callerLineNumber) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogEventArgs"/> class using the log type, optional message, optional exception, and caller information.
        /// </summary>
        /// <param name="logType">The type of log.</param>
        /// <param name="message">The log message, if available.</param>
        /// <param name="exception">The exception related to the log, if available.</param>
        /// <param name="callerName">The name of the method that initiated the log.</param>
        /// <param name="callerFilePath">The file path of the source code that initiated the log.</param>
        /// <param name="callerLineNumber">The line number in the source file where the log was initiated.</param>
        public LogEventArgs(LogType logType, string? message, Exception? exception, string callerName, string callerFilePath, int callerLineNumber)
        {
            LoggingType = logType;
            Message = message;
            Exception = exception;
            CallerName = callerName;
            CallerFilePath = callerFilePath;
            CallerLineNumber = callerLineNumber;
        }

        /// <summary>
        /// Returns a string representation of the log event, including log type, caller information, message, and exception (if any).
        /// </summary>
        /// <returns>A string representing the log event.</returns>
        public override string ToString()
        {
            var sb = $"[{LoggingType.GetDescription()}] [{CallerName}] [{CallerFilePath}:{CallerLineNumber}]";
            if (Message != null)
                sb += $"{Environment.NewLine}{Message}";
            if (Exception != null)
                sb += $"{Environment.NewLine}{Exception.ToStringDemystified()}";
            return sb;
        }
    }
}
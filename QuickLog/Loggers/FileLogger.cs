using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// A logger that writes log entries to a file, ensuring thread safety for multiple logging instances.
/// </summary>
public class FileLogger : IQuickLog
{
    private readonly object _fileLock = new();
    private StreamWriter? _logWriter;
    private string? _filePath;

    /// <summary>
    /// Occurs when a log event is triggered.
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileLogger"/> class, specifying the file path for logging.
    /// </summary>
    /// <param name="filePath">The file path where log entries will be written.</param>
    public FileLogger(string filePath) => SetLogFilePath(filePath);

    /// <summary>
    /// Sets the log file path and ensures the file and directory are ready for logging.
    /// </summary>
    /// <param name="filePath">The file path where log entries will be written.</param>
    private void SetLogFilePath(string filePath)
    {
        var fileName = Path.GetFileName(filePath).ReplaceInvalidChars();
        var dirName = filePath.Replace(fileName, "");

        if (_logWriter == null)
        {
            lock (_fileLock)
            {
                if (_logWriter == null) // Double-check locking
                {
                    CreateDirectoryIfNotExists(dirName);
                    CheckFileWritePermissions(filePath);
                    _filePath = filePath;
                    _logWriter = new StreamWriter(_filePath, true) { AutoFlush = true };
                }
            }
        }
    }

    /// <summary>
    /// Logs a message with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="message">The message to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void Log(LogType logType, string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, message, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }

    /// <summary>
    /// Logs an exception with the specified log type and caller information.
    /// </summary>
    /// <param name="logType">The type of the log entry (e.g., Info, Debug, Error).</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="callerName">The name of the calling method. Automatically captured by the compiler.</param>
    /// <param name="callerFilePath">The file path of the calling code. Automatically captured by the compiler.</param>
    /// <param name="callerLineNumber">The line number of the calling code. Automatically captured by the compiler.</param>
    public void Log(LogType logType, Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, exception, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }

    /// <summary>
    /// Logs a message and an exception with the specified log type and caller information.
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
        [CallerLineNumber] int callerLineNumber = 0)
    {
        var logEventArgs = new LogEventArgs(logType, message, exception, callerName, callerFilePath, callerLineNumber);
        HandleLog(logEventArgs);
    }

    /// <summary>
    /// Handles the logging process by invoking the log event and writing the log message to the log file.
    /// </summary>
    /// <param name="logEventArgs">The log event arguments containing the log details.</param>
    private void HandleLog(LogEventArgs logEventArgs)
    {
        // Trigger the log event for any listeners
        LogEvent?.Invoke(this, logEventArgs);

        // Write to file in a thread-safe manner
        lock (_fileLock)
        {
            _logWriter?.WriteLine(logEventArgs.ToString());
            _logWriter?.Flush();
        }
    }

    /// <summary>
    /// Ensures that the directory for the log file exists, and if not, creates it.
    /// </summary>
    /// <param name="filePath">The path of the log file.</param>
    private void CreateDirectoryIfNotExists(string filePath)
    {
        filePath = filePath.ReplaceInvalidPathChars();
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
    }

    /// <summary>
    /// Checks if the application has write permissions for the specified file.
    /// </summary>
    /// <param name="filePath">The path of the log file to check.</param>
    private void CheckFileWritePermissions(string filePath)
    {
        filePath = filePath.ReplaceInvalidPathChars();
        if (File.Exists(filePath))
        {
            try
            {
                using var fs = File.Open(filePath, FileMode.Append, FileAccess.Write);
            }
            catch (UnauthorizedAccessException)
            {
                throw new UnauthorizedAccessException($"The application does not have permission to write to the file: {filePath}");
            }
        }
        else
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) CreateDirectoryIfNotExists(filePath);
        }
    }

    /// <summary>
    /// Cleans up resources by closing the log file when the logger is disposed.
    /// </summary>
    ~FileLogger()
    {
        Dispose(false);
    }
    private void Dispose(bool disposing)
    {
        if (disposing) _logWriter?.Dispose();
    }
    /// <summary>
    /// Disposes of the resources used by the logger.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

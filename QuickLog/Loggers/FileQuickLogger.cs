using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace QuickLog.Loggers;

/// <summary>
/// File Logger for QuickLog
/// </summary>
public class FileQuickLogger : IQuickLog
{
    #region Private Fields
    private static readonly object _fileLock = new();
    private static StreamWriter? _logWriter;
    private static string? _filePath;
    #endregion
    /// <summary>
    /// Event handler for all logging events
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Creates a new instance of <see cref="FileQuickLogger"/> with set file path
    /// </summary>
    /// <param name="filePath"></param>
    public FileQuickLogger(string filePath) => SetLogFilePath(filePath);

    #region Private Methods
    private void SetLogFilePath(string filePath)
    {
        if (_logWriter == null)
        {
            lock (_fileLock)
            {
                if (_logWriter == null) // Double-check locking
                {
                    // Ensure the directory exists and has correct permissions
                    CreateDirectoryIfNotExists(filePath);

                    // Ensure the file can be created or written to
                    CheckFileWritePermissions(filePath);
                    _filePath = filePath;
                    _logWriter = new StreamWriter(_filePath, true) { AutoFlush = true };
                }
            }
        }
    }
    private void CreateDirectoryIfNotExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(directory))
        {
            if (!Directory.Exists(directory))
            {
                // Check permissions for directory creation
                CheckDirectoryWritePermissions(directory);
                Directory.CreateDirectory(directory);
            }
            else
            {
                // Check if we have write permission in the existing directory
                CheckDirectoryWritePermissions(directory);
            }
        }
    }

    private void CheckDirectoryWritePermissions(string directory)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            CheckWindowsDirectoryPermissions(directory);
        else
            CheckUnixDirectoryPermissions(directory);
    }

    private void CheckFileWritePermissions(string filePath)
    {
        if (File.Exists(filePath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckWindowsFilePermissions(filePath);
            else
                CheckUnixFilePermissions(filePath);
        }
        else
        {
            // If the file doesn't exist, we need to check if we can create a new file
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory)) CheckDirectoryWritePermissions(directory);
        }
    }

    #region Windows Permission Checks
    private void CheckWindowsDirectoryPermissions(string directory)
    {
        var dirInfo = new DirectoryInfo(directory);
        var dirSecurity = dirInfo.GetAccessControl();
        var rules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));

        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        foreach (FileSystemAccessRule rule in rules)
        {
            if (identity.Name.Equals(rule.IdentityReference.Value, StringComparison.CurrentCultureIgnoreCase) ||
                principal.IsInRole(rule.IdentityReference.Value))
            {
                if ((rule.FileSystemRights & FileSystemRights.Write) != 0 && rule.AccessControlType == AccessControlType.Allow)
                {
                    return;
                }
            }
        }

        throw new UnauthorizedAccessException($"The user does not have write access to the directory: {directory}");
    }

    private void CheckWindowsFilePermissions(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var fileSecurity = fileInfo.GetAccessControl();
        var rules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));

        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        foreach (FileSystemAccessRule rule in rules)
        {
            if (identity.Name.Equals(rule.IdentityReference.Value, StringComparison.CurrentCultureIgnoreCase) ||
                principal.IsInRole(rule.IdentityReference.Value))
            {
                if ((rule.FileSystemRights & FileSystemRights.Write) != 0 && rule.AccessControlType == AccessControlType.Allow)
                {
                    return;
                }
            }
        }

        throw new UnauthorizedAccessException($"The user does not have write access to the file: {filePath}");
    }
    #endregion

    #region Unix Permission Checks
    private void CheckUnixDirectoryPermissions(string directory)
    {
        try
        {
            // Attempt to create a temporary file to test write permissions
            var testFilePath = Path.Combine(directory, Path.GetRandomFileName());
            using var fs = File.Create(testFilePath, 1, FileOptions.DeleteOnClose);
        }
        catch
        {
            throw new UnauthorizedAccessException($"The user does not have write access to the directory: {directory}");
        }
    }

    private void CheckUnixFilePermissions(string filePath)
    {
        try
        {
            // Attempt to open the file for writing
            using var fs = File.Open(filePath, FileMode.Open, FileAccess.Write);
        }
        catch
        {
            throw new UnauthorizedAccessException($"The user does not have write access to the file: {filePath}");
        }
    }
    #endregion
    #endregion

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

        // Write to file in a thread-safe manner
        lock (_fileLock) _logWriter?.WriteLine(logEventArgs.ToString());
    }

    // Destructor to clean up resources
    ~FileQuickLogger()
    {
        _logWriter?.Close();
        _logWriter?.Dispose();
    }
}
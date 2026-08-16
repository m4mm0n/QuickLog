using QuickLog.Utilities;

namespace QuickLog.Platform;

/// <summary>
/// Resolves platform-appropriate QuickLog output directories without external dependencies.
/// </summary>
public static class QuickLogPathResolver
{
    /// <summary>
    /// Returns the default log directory for the current platform.
    /// </summary>
    /// <param name="applicationName">Application folder name to use below the platform state directory.</param>
    public static string GetDefaultLogDirectory(string applicationName = "QuickLog")
    {
        return QuickLogPlatform.CurrentKind switch
        {
            QuickLogPlatformKind.Linux => GetLinuxLogDirectory(applicationName),
            QuickLogPlatformKind.MacOS => GetMacOSLogDirectory(applicationName),
            QuickLogPlatformKind.Android => GetAndroidLogDirectory(applicationName),
            QuickLogPlatformKind.IOS => GetIOSLogDirectory(applicationName),
            _ => GetApplicationDataLogDirectory(applicationName)
        };
    }

    /// <summary>
    /// Returns a Linux log directory that follows the XDG base-directory convention.
    /// </summary>
    /// <param name="applicationName">Application folder name to use below the state directory.</param>
    /// <param name="stateHome">Optional XDG state directory override. When <see langword="null"/>, <c>XDG_STATE_HOME</c> is read.</param>
    /// <param name="homeDirectory">Optional home directory override. When <see langword="null"/>, <c>HOME</c> and the user profile are used.</param>
    public static string GetLinuxLogDirectory(
        string applicationName = "QuickLog",
        string? stateHome = null,
        string? homeDirectory = null)
    {
        var root = FirstNonWhiteSpace(stateHome, Environment.GetEnvironmentVariable("XDG_STATE_HOME"));
        if (string.IsNullOrWhiteSpace(root))
        {
            var home = FirstNonWhiteSpace(
                homeDirectory,
                Environment.GetEnvironmentVariable("HOME"),
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                Path.GetTempPath());

            root = Path.Combine(home, ".local", "state");
        }

        return Path.Combine(root, SafeLogPath.SafeFileName(applicationName), "logs");
    }

    /// <summary>
    /// Returns a macOS log directory below the current user's <c>Library/Logs</c> directory.
    /// In a sandboxed application, the user home directory is the application's container home.
    /// </summary>
    /// <param name="applicationName">Application folder name to use below <c>Library/Logs</c>.</param>
    /// <param name="homeDirectory">Optional home directory override, mainly useful for tests and controlled hosts.</param>
    public static string GetMacOSLogDirectory(
        string applicationName = "QuickLog",
        string? homeDirectory = null)
    {
        var home = FirstNonWhiteSpace(
            homeDirectory,
            Environment.GetEnvironmentVariable("HOME"),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            Path.GetTempPath());

        return Path.Combine(home, "Library", "Logs", SafeLogPath.SafeFileName(applicationName));
    }

    /// <summary>
    /// Returns an Android log directory below the application's local-data directory.
    /// </summary>
    /// <param name="applicationName">Application folder name to use below the local-data directory.</param>
    /// <param name="localApplicationData">Optional local-data directory override, mainly useful for tests and controlled hosts.</param>
    public static string GetAndroidLogDirectory(
        string applicationName = "QuickLog",
        string? localApplicationData = null)
        => GetApplicationDataLogDirectory(applicationName, localApplicationData);

    /// <summary>
    /// Returns an iOS log directory below the application's local-data directory.
    /// </summary>
    /// <param name="applicationName">Application folder name to use below the local-data directory.</param>
    /// <param name="localApplicationData">Optional local-data directory override, mainly useful for tests and controlled hosts.</param>
    public static string GetIOSLogDirectory(
        string applicationName = "QuickLog",
        string? localApplicationData = null)
        => GetApplicationDataLogDirectory(applicationName, localApplicationData);

    /// <summary>
    /// Resolves a file path below the current platform's default log directory when the path is relative.
    /// </summary>
    /// <param name="path">Absolute or relative file path to resolve.</param>
    /// <param name="applicationName">Application folder name used for relative paths.</param>
    public static string ResolveLogFilePath(string path, string applicationName = "QuickLog")
    {
        if (Path.IsPathRooted(path) || path.Contains("://", StringComparison.Ordinal))
            return path;

        return Path.Combine(GetDefaultLogDirectory(applicationName), path);
    }

    private static string GetApplicationDataLogDirectory(
        string applicationName,
        string? localApplicationData = null)
    {
        var root = FirstNonWhiteSpace(
            localApplicationData,
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.GetTempPath());

        return Path.Combine(root, SafeLogPath.SafeFileName(applicationName), "logs");
    }

    private static string FirstNonWhiteSpace(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}

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
        if (QuickLogPlatform.CurrentKind == QuickLogPlatformKind.Linux)
            return GetLinuxLogDirectory(applicationName);

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local))
            local = Path.GetTempPath();

        return Path.Combine(local, SafeLogPath.SafeFileName(applicationName), "logs");
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

    private static string FirstNonWhiteSpace(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}

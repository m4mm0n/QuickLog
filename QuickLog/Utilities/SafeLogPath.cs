namespace QuickLog.Utilities;

/// <summary>
/// Provides safe filename and session-directory helpers for log outputs.
/// </summary>
public static class SafeLogPath
{
    private static readonly char[] PortableInvalidFileNameChars =
    [
        .. Path.GetInvalidFileNameChars(),
        '<', '>', ':', '"', '\\', '/', '|', '?', '*'
    ];

    /// <summary>
    /// Replaces invalid filename characters and returns a non-empty filename.
    /// </summary>
    /// <param name="name">Raw filename or session name.</param>
    public static string SafeFileName(string name)
    {
        var invalid = PortableInvalidFileNameChars;
        var cleaned = new string(name.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "quicklog" : cleaned;
    }

    /// <summary>
    /// Creates a timestamped session directory under the supplied root.
    /// </summary>
    /// <param name="root">Root output directory.</param>
    /// <param name="sessionName">Raw session name.</param>
    public static string CreateSessionDirectory(string root, string sessionName)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var path = Path.Combine(root, $"{stamp}-{SafeFileName(sessionName)}");
        Directory.CreateDirectory(path);
        return path;
    }
}

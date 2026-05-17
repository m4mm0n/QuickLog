namespace QuickLog.Core;

/// <summary>
/// Configures sensitive value redaction for log messages and crash dumps.
/// </summary>
public sealed class LogRedactionOptions
{
    /// <summary>Whether redaction is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The replacement text used for sensitive values.</summary>
    public string Mask { get; set; } = "***";

    /// <summary>Case-insensitive key names whose values should be masked.</summary>
    public List<string> SensitiveKeys { get; } =
    [
        "password",
        "passwd",
        "pwd",
        "token",
        "secret",
        "apikey",
        "api_key",
        "accesskey",
        "access_key",
        "connectionstring",
        "connection_string"
    ];

    /// <summary>Whether Windows and Unix user profile segments should be masked.</summary>
    public bool RedactUserProfilePaths { get; set; }

    /// <summary>
    /// Creates a preset focused on common secret and token names.
    /// </summary>
    public static LogRedactionOptions Secrets() => new();

    /// <summary>
    /// Creates a preset that also masks common network credential fields.
    /// </summary>
    public static LogRedactionOptions Network()
    {
        var options = Secrets();
        options.SensitiveKeys.AddRange(["hostpassword", "proxy_password", "bearer", "authorization"]);
        return options;
    }

    /// <summary>
    /// Creates a preset that masks user profile path segments.
    /// </summary>
    public static LogRedactionOptions UserData()
    {
        var options = Secrets();
        options.RedactUserProfilePaths = true;
        return options;
    }

    /// <summary>
    /// Creates the safest built-in preset for support bundles and crash reports.
    /// </summary>
    public static LogRedactionOptions CrashSafe()
    {
        var options = Network();
        options.RedactUserProfilePaths = true;
        options.SensitiveKeys.AddRange(["session", "cookie", "auth", "refresh_token"]);
        return options;
    }

    /// <summary>
    /// Applies the crash-safe preset to an existing options instance.
    /// </summary>
    /// <param name="options">Options instance to mutate.</param>
    public static void UseCrashSafePreset(LogRedactionOptions options)
    {
        var preset = CrashSafe();
        options.Enabled = preset.Enabled;
        options.Mask = preset.Mask;
        options.RedactUserProfilePaths = preset.RedactUserProfilePaths;
        options.SensitiveKeys.Clear();
        options.SensitiveKeys.AddRange(preset.SensitiveKeys.Distinct(StringComparer.OrdinalIgnoreCase));
    }
}

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
}

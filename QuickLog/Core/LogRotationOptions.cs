namespace QuickLog.Core;

/// <summary>
/// Controls size-based rotation for file-backed log sinks.
/// </summary>
public sealed class LogRotationOptions
{
    /// <summary>Maximum active file size in bytes before rotation. Values less than one disable rotation.</summary>
    public long MaxFileBytes { get; set; }

    /// <summary>Maximum number of files to keep, including the active file.</summary>
    public int MaxFiles { get; set; } = 5;

    /// <summary>When true, rotates a non-empty active file when the sink starts.</summary>
    public bool RotateOnStartup { get; set; }

    /// <summary>Gets whether size-based rotation is enabled.</summary>
    public bool IsEnabled => MaxFileBytes > 0;
}

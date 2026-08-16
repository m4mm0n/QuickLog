namespace QuickLog.Core;

/// <summary>
/// Controls rotation, retention, and compression for file-backed log sinks.
/// </summary>
public sealed class LogRotationOptions
{
    /// <summary>Maximum active file size in bytes before rotation. Values less than one disable rotation.</summary>
    public long MaxFileBytes { get; set; }

    /// <summary>Maximum number of files to keep, including the active file.</summary>
    public int MaxFiles { get; set; } = 5;

    /// <summary>When true, rotates a non-empty active file when the sink starts.</summary>
    public bool RotateOnStartup { get; set; }

    /// <summary>Maximum age for rotated files. <see langword="null"/> keeps files regardless of age.</summary>
    public TimeSpan? MaxAge { get; set; }

    /// <summary>Maximum total bytes across the active file and its rotations. Values below one disable the budget.</summary>
    public long MaxTotalBytes { get; set; }

    /// <summary>Whether newly rotated files are compressed with GZip.</summary>
    public bool CompressRotatedFiles { get; set; }

    /// <summary>Gets whether any rotation or retention behavior is enabled.</summary>
    public bool IsEnabled => MaxFileBytes > 0 || RotateOnStartup || MaxAge is not null || MaxTotalBytes > 0;
}

namespace QuickLog.Core;

/// <summary>
/// Configures duplicate message coalescing for the async logging path.
/// </summary>
public sealed class LogSpamControlOptions
{
    /// <summary>Whether duplicate coalescing is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Total duplicate run length required before entries are summarized.</summary>
    public int DuplicateThreshold { get; set; } = 8;
}

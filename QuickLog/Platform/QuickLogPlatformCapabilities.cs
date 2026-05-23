namespace QuickLog.Platform;

/// <summary>
/// Describes platform behaviors that affect logging, exception reporting, and support tooling.
/// </summary>
public sealed class QuickLogPlatformCapabilities
{
    /// <summary>
    /// Gets whether writing to a traditional interactive console is expected to work.
    /// </summary>
    public bool SupportsInteractiveConsole { get; init; }

    /// <summary>
    /// Gets whether QuickLog can show a native modal popup without an external UI dependency.
    /// </summary>
    public bool SupportsNativePopup { get; init; }

    /// <summary>
    /// Gets whether QuickLog can attempt a process restart after a fatal exception.
    /// </summary>
    public bool SupportsProcessRestart { get; init; }

    /// <summary>
    /// Gets whether the platform should prefer XDG base directories for default log locations.
    /// </summary>
    public bool UsesXdgDirectories { get; init; }
}

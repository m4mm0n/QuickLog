namespace QuickLog.Platform;

/// <summary>
/// Identifies the platform family QuickLog is running on.
/// </summary>
public enum QuickLogPlatformKind
{
    /// <summary>The platform could not be identified.</summary>
    Unknown = 0,

    /// <summary>Microsoft Windows desktop or server.</summary>
    Windows,

    /// <summary>Linux desktop, server, or container runtime.</summary>
    Linux,

    /// <summary>Apple macOS desktop runtime.</summary>
    MacOS,

    /// <summary>Android runtime.</summary>
    Android,

    /// <summary>Apple iOS runtime.</summary>
    IOS
}

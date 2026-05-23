namespace QuickLog.Platform;

/// <summary>
/// Provides dependency-free platform facts used by QuickLog profiles and platform-safe fallbacks.
/// </summary>
public static class QuickLogPlatform
{
    /// <summary>
    /// Gets the current platform family.
    /// </summary>
    public static QuickLogPlatformKind CurrentKind
    {
        get
        {
            if (OperatingSystem.IsAndroid())
                return QuickLogPlatformKind.Android;
            if (OperatingSystem.IsIOS())
                return QuickLogPlatformKind.IOS;
            if (OperatingSystem.IsLinux())
                return QuickLogPlatformKind.Linux;
            if (OperatingSystem.IsMacOS())
                return QuickLogPlatformKind.MacOS;
            if (OperatingSystem.IsWindows())
                return QuickLogPlatformKind.Windows;

            return QuickLogPlatformKind.Unknown;
        }
    }

    /// <summary>
    /// Gets capabilities for the current platform.
    /// </summary>
    public static QuickLogPlatformCapabilities CurrentCapabilities => GetCapabilities(CurrentKind);

    /// <summary>
    /// Returns the logging-relevant capabilities for a platform family.
    /// </summary>
    /// <param name="kind">The platform family to describe.</param>
    public static QuickLogPlatformCapabilities GetCapabilities(QuickLogPlatformKind kind) => kind switch
    {
        QuickLogPlatformKind.Windows => new QuickLogPlatformCapabilities
        {
            SupportsInteractiveConsole = true,
            SupportsNativePopup = true,
            SupportsProcessRestart = true
        },
        QuickLogPlatformKind.Linux => new QuickLogPlatformCapabilities
        {
            SupportsInteractiveConsole = true,
            SupportsNativePopup = false,
            SupportsProcessRestart = true,
            UsesXdgDirectories = true
        },
        QuickLogPlatformKind.MacOS => new QuickLogPlatformCapabilities
        {
            SupportsInteractiveConsole = true,
            SupportsNativePopup = false,
            SupportsProcessRestart = true
        },
        QuickLogPlatformKind.Android or QuickLogPlatformKind.IOS => new QuickLogPlatformCapabilities
        {
            SupportsInteractiveConsole = false,
            SupportsNativePopup = false,
            SupportsProcessRestart = false
        },
        _ => new QuickLogPlatformCapabilities()
    };
}

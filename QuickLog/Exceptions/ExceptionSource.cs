namespace QuickLog.Exceptions;

/// <summary>
/// Identifies which runtime hook captured an unhandled exception.
/// </summary>
public enum ExceptionSource
{
    /// <summary>Raised by <see cref="AppDomain.UnhandledException"/>.</summary>
    AppDomain,

    /// <summary>Raised by <see cref="TaskScheduler.UnobservedTaskException"/>.</summary>
    UnobservedTask
}

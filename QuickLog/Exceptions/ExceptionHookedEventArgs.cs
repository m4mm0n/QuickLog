namespace QuickLog.Exceptions;

/// <summary>
/// Carries information about an exception captured by <see cref="ExceptionHookManager"/>.
/// Subscribe to <see cref="ExceptionHookManager.ExceptionCaught"/> to receive these events
/// before or after the built-in log + popup handling.
/// </summary>
public sealed class ExceptionHookedEventArgs : EventArgs
{
    /// <summary>The exception that was captured.</summary>
    public Exception Exception { get; }

    /// <summary>Which runtime hook raised this event.</summary>
    public ExceptionSource Source { get; }

    /// <summary>
    /// For <see cref="ExceptionSource.AppDomain"/> events: whether the CLR will terminate
    /// the process after the event handler returns.
    /// Always <see langword="false"/> for <see cref="ExceptionSource.UnobservedTask"/>.
    /// </summary>
    public bool IsTerminating { get; }

    /// <summary>
    /// Set to <see langword="true"/> inside your event handler to prevent the
    /// <see cref="ExceptionHookManager"/> from performing its built-in log + popup.
    /// Useful when you want full custom control from the event subscriber.
    /// </summary>
    public bool SuppressDefaultHandling { get; set; }

    internal ExceptionHookedEventArgs(Exception exception, ExceptionSource source, bool isTerminating)
    {
        Exception = exception;
        Source = source;
        IsTerminating = isTerminating;
    }
}

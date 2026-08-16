namespace QuickLog.Exceptions;

/// <summary>
/// Provides a UI popup when an unhandled exception is caught by <see cref="ExceptionHookManager"/>.
/// Implement this interface to substitute the built-in <c>MessageBoxW</c> popup with your own dialog
/// (e.g. WPF window, Godot dialog, custom console prompt).
/// </summary>
public interface IExceptionPopup
{
    /// <summary>
    /// Shows the popup. This method is called on the thread that raised the exception event,
    /// so keep it blocking and thread-safe.
    /// </summary>
    /// <param name="title">Window/dialog title.</param>
    /// <param name="message">Human-readable description of the exception.</param>
    /// <param name="exception">The original exception for additional detail.</param>
    /// <param name="source">The hook that captured the exception.</param>
    void Show(string title, string message, Exception exception, ExceptionSource source);
}

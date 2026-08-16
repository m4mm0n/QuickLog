using System.Reflection;
using QuickLog.Exceptions;
using QuickLog.Utilities;

namespace QuickLog.Godot;

/// <summary>
/// <see cref="IExceptionPopup"/> implementation that uses Godot's native <c>OS.Alert()</c> dialog
/// (called via reflection — no compile-time dependency on GodotSharp).
/// Falls back to <see cref="DefaultExceptionPopup"/> when not running under Godot or when
/// reflection into <c>OS.Alert</c> fails.
/// </summary>
public sealed class GodotAlertPopup : IExceptionPopup
{
    private static readonly object _lock = new();
    private static MethodInfo? _alertMethod;
    private static readonly DefaultExceptionPopup _fallback = new();

    /// <inheritdoc/>
    public void Show(string title, string message, Exception exception, ExceptionSource source)
    {
        var mi = GetAlertMethod();
        if (mi != null)
        {
            try
            {
                // OS.Alert(string text, string title = "Alert!")
                mi.Invoke(null, [message, title]);
                return;
            }
            catch { /* fall through to default popup */ }
        }

        _fallback.Show(title, message, exception, source);
    }

    private static MethodInfo? GetAlertMethod()
    {
        lock (_lock)
        {
            _alertMethod ??= ResolveAlertMethod();
            return _alertMethod;
        }
    }

    private static MethodInfo? ResolveAlertMethod()
    {
        try
        {
            return GodotReflection.ResolveStaticMethod(
                "Godot.OS",
                "Alert",
                [typeof(string), typeof(string)]);
        }
        catch { return null; }
    }
}

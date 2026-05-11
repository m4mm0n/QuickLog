/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : GodotAlertPopup.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

using System.Reflection;
using QuickLog.Exceptions;

namespace QuickLog.Godot;

/// <summary>
/// <see cref="IExceptionPopup"/> implementation that uses Godot's native <c>OS.Alert()</c> dialog
/// (called via reflection — no compile-time dependency on GodotSharp).
/// Falls back to <see cref="DefaultExceptionPopup"/> when not running under Godot or when
/// reflection into <c>OS.Alert</c> fails.
/// </summary>
public sealed class GodotAlertPopup : IExceptionPopup
{
    private static readonly Lazy<MethodInfo?> _alertMethod = new(ResolveAlertMethod);
    private static readonly DefaultExceptionPopup _fallback = new();

    /// <inheritdoc/>
    public void Show(string title, string message, Exception exception, ExceptionSource source)
    {
        var mi = _alertMethod.Value;
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

    private static MethodInfo? ResolveAlertMethod()
    {
        try
        {
            var osType = Type.GetType("Godot.OS, GodotSharp") ?? Type.GetType("Godot.OS");
            return osType?.GetMethod("Alert",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: [typeof(string), typeof(string)],
                modifiers: null);
        }
        catch { return null; }
    }
}

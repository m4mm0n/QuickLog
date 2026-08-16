using System.Runtime.InteropServices;
using QuickLog.Platform;

namespace QuickLog.Exceptions;

/// <summary>
/// Default popup implementation.
/// On Windows this calls <c>MessageBoxW</c> from <c>user32.dll</c> directly via P/Invoke —
/// no WinForms or WPF reference needed.
/// On non-Windows platforms it falls back to writing the exception to <see cref="Console.Error"/>.
/// </summary>
public sealed class DefaultExceptionPopup : IExceptionPopup
{
    // MessageBox type flags (winuser.h)
    private const uint MB_OK = 0x00000000u;
    private const uint MB_ICONERROR = 0x00000010u;
    private const uint MB_ICONWARNING = 0x00000030u;
    private const uint MB_SYSTEMMODAL = 0x00001000u;
    private const uint MB_SETFOREGROUND = 0x00010000u;
    private const uint MB_TOPMOST = 0x00040000u;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW", SetLastError = false)]
    private static extern int NativeMessageBox(IntPtr hWnd, string text, string caption, uint type);

    /// <inheritdoc/>
    public void Show(string title, string message, Exception exception, ExceptionSource source)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ShowWindows(title, message, source);
        else
            ShowConsoleFallback(title, message);
    }

    private static void ShowWindows(string title, string message, ExceptionSource source)
    {
        var icon = source == ExceptionSource.AppDomain ? MB_ICONERROR : MB_ICONWARNING;
        NativeMessageBox(IntPtr.Zero, message, title, MB_OK | icon | MB_SYSTEMMODAL | MB_SETFOREGROUND | MB_TOPMOST);
    }

    private static void ShowConsoleFallback(string title, string message)
        => QuickLogConsole.WriteErrorBlock(title, message);
}

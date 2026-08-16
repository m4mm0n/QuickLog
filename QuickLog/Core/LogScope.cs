namespace QuickLog.Core;

/// <summary>
/// Provides a thread-local stack for managing logical logging scopes within the current thread.
/// </summary>
/// <remarks>This class is intended for use in logging scenarios where operations are grouped into nested scopes.
/// Each thread maintains its own independent scope stack. The class is static and cannot be instantiated.</remarks>
internal static class LogScope
{
    public static IDisposable Push(string name) => QuickLog.LogScope.Begin(name);

    public static string? Current => QuickLog.LogScope.Current;
}

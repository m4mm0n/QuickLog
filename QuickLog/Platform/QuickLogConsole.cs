namespace QuickLog.Platform;

/// <summary>
/// Provides console helpers that do not make exception handling fail when stderr is unavailable.
/// </summary>
internal static class QuickLogConsole
{
    /// <summary>
    /// Writes a diagnostic block to stderr and suppresses console failures.
    /// </summary>
    /// <param name="title">The diagnostic title.</param>
    /// <param name="message">The diagnostic message body.</param>
    public static void WriteErrorBlock(string title, string message)
    {
        try
        {
            var separator = new string('=', 72);
            Console.Error.WriteLine(separator);
            Console.Error.WriteLine($"  {title}");
            Console.Error.WriteLine(separator);
            Console.Error.WriteLine(message);
            Console.Error.WriteLine(separator);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
        {
            // Exception hooks must never fail because stderr is redirected, closed, or unavailable.
        }
    }
}

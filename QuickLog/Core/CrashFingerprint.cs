using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace QuickLog.Core;

/// <summary>
/// Creates stable crash fingerprints from exception type and stack shape.
/// </summary>
public static class CrashFingerprint
{
    private static readonly ConcurrentDictionary<string, int> Counts = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns a stable short fingerprint for the supplied exception.
    /// </summary>
    /// <param name="exception">Exception to fingerprint.</param>
    /// <returns>A short uppercase hexadecimal fingerprint.</returns>
    public static string From(Exception exception)
    {
        var stack = new System.Diagnostics.StackTrace(exception, false);
        var frames = stack.GetFrames()?.Take(6)
            .Select(frame => frame.GetMethod())
            .Where(method => method is not null)
            .Select(method => $"{method!.DeclaringType?.FullName}.{method.Name}") ?? [];

        var basis = $"{exception.GetType().FullName}|{string.Join("|", frames)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(basis));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }

    /// <summary>
    /// Increments and returns the occurrence count for a fingerprint.
    /// </summary>
    /// <param name="fingerprint">Fingerprint to count.</param>
    /// <returns>The one-based occurrence count for the fingerprint.</returns>
    public static int IncrementCount(string fingerprint)
        => Counts.AddOrUpdate(fingerprint, 1, (_, count) => count + 1);

    /// <summary>
    /// Clears duplicate counters. Intended for tests and process reinitialization.
    /// </summary>
    public static void ClearCounts() => Counts.Clear();
}

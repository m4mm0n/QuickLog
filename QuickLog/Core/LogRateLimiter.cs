using System.Collections.Concurrent;

namespace QuickLog.Core;

/// <summary>
/// Tracks one-shot and interval-based logging keys for low-noise helpers.
/// </summary>
internal sealed class LogRateLimiter
{
    private readonly ConcurrentDictionary<string, byte> _once = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, DateTime> _last = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns whether the key has not been logged before.
    /// </summary>
    /// <param name="key">Caller-supplied stable event key.</param>
    public bool ShouldLogOnce(string key) => _once.TryAdd(key, 0);

    /// <summary>
    /// Returns whether enough time has passed since the key last logged.
    /// </summary>
    /// <param name="key">Caller-supplied stable event key.</param>
    /// <param name="interval">Minimum interval between accepted entries.</param>
    public bool ShouldLogEvery(string key, TimeSpan interval)
    {
        var now = DateTime.UtcNow;
        var accepted = false;

        _last.AddOrUpdate(
            key,
            _ =>
            {
                accepted = true;
                return now;
            },
            (_, previous) =>
            {
                if (now - previous < interval)
                    return previous;

                accepted = true;
                return now;
            });

        return accepted;
    }
}

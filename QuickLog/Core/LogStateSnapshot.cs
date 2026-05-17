using System.Collections.Concurrent;

namespace QuickLog.Core;

/// <summary>
/// Stores small last-known-state values that can be included in crash reports.
/// </summary>
public static class LogStateSnapshot
{
    private static readonly ConcurrentDictionary<string, string> Values = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds or replaces a state value.
    /// </summary>
    /// <param name="key">State key.</param>
    /// <param name="value">State value.</param>
    public static void Set(string key, string value) => Values[key] = value;

    /// <summary>
    /// Removes a state value.
    /// </summary>
    /// <param name="key">State key to remove.</param>
    /// <returns><see langword="true"/> when a value was removed; otherwise <see langword="false"/>.</returns>
    public static bool Remove(string key) => Values.TryRemove(key, out _);

    /// <summary>
    /// Returns a point-in-time copy of the state values.
    /// </summary>
    /// <returns>A copy of the currently registered state values.</returns>
    public static IReadOnlyDictionary<string, string> Snapshot()
        => Values.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    /// <summary>
    /// Removes all state values.
    /// </summary>
    public static void Clear() => Values.Clear();
}

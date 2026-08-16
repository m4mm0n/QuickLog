using System.Text.RegularExpressions;

namespace QuickLog.Core;

/// <summary>
/// Masks configured sensitive key values in plain-text and JSON-like log fragments.
/// </summary>
public sealed class LogRedactor
{
    private readonly LogRedactionOptions _options;

    /// <summary>Creates a redactor from the supplied options.</summary>
    public LogRedactor(LogRedactionOptions options)
    {
        _options = options;
    }

    /// <summary>Returns <paramref name="value"/> with configured sensitive values masked.</summary>
    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value) || !_options.Enabled)
            return value ?? string.Empty;

        var result = value;
        foreach (var key in _options.SensitiveKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var escaped = Regex.Escape(key);
            var mask = _options.Mask;

            result = Regex.Replace(
                result,
                $"(\"{escaped}\"\\s*:\\s*\")([^\"]*)(\")",
                $"$1{mask}$3",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            result = Regex.Replace(
                result,
                $"\\b({escaped})\\b\\s*=\\s*([^\\s,;]+)",
                $"$1={mask}",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            result = Regex.Replace(
                result,
                $"(?<!\")\\b({escaped})\\b\\s*:\\s*([^\\s,;]+)",
                $"$1: {mask}",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return _options.RedactUserProfilePaths ? RedactUserPaths(result) : result;
    }

    /// <summary>Returns an immutable property snapshot with sensitive names and text values masked.</summary>
    /// <param name="properties">The properties to redact.</param>
    /// <returns>A redacted property snapshot.</returns>
    public IReadOnlyDictionary<string, object?> RedactProperties(
        IReadOnlyDictionary<string, object?>? properties)
    {
        if (properties is null || properties.Count == 0)
            return LogProperties.Empty;

        var values = new Dictionary<string, object?>(properties.Count, StringComparer.Ordinal);
        foreach (var pair in properties)
        {
            var isSensitive = _options.SensitiveKeys.Any(key =>
                string.Equals(key, pair.Key, StringComparison.OrdinalIgnoreCase));
            values[pair.Key] = isSensitive
                ? _options.Mask
                : pair.Value is string text ? Redact(text) : pair.Value;
        }

        return LogProperties.Snapshot(values);
    }

    private string RedactUserPaths(string value)
    {
        var mask = _options.Mask;

        value = Regex.Replace(
            value,
            @"(?i)\b([A-Z]:\\Users\\)([^\\\s]+)",
            $"$1{mask}",
            RegexOptions.CultureInvariant);

        value = Regex.Replace(
            value,
            @"(?i)(/Users/)([^/\s]+)",
            $"$1{mask}",
            RegexOptions.CultureInvariant);

        value = Regex.Replace(
            value,
            @"(?i)(/home/)([^/\s]+)",
            $"$1{mask}",
            RegexOptions.CultureInvariant);

        return value;
    }
}

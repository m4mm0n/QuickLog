using System.Collections.ObjectModel;
using System.Globalization;

namespace QuickLog;

/// <summary>
/// Represents one named structured value attached to a log event.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="Value">The property value.</param>
public readonly record struct LogProperty(string Name, object? Value);

/// <summary>
/// Creates immutable structured-property snapshots and deterministic text representations.
/// </summary>
public static class LogProperties
{
    private static readonly IReadOnlyDictionary<string, object?> _empty =
        new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>());

    /// <summary>Gets an empty immutable property collection.</summary>
    public static IReadOnlyDictionary<string, object?> Empty => _empty;

    /// <summary>
    /// Creates an immutable property collection from named values.
    /// </summary>
    /// <param name="properties">The values to include. Later duplicate names replace earlier values.</param>
    /// <returns>An immutable property snapshot.</returns>
    public static IReadOnlyDictionary<string, object?> Create(params LogProperty[] properties)
    {
        ArgumentNullException.ThrowIfNull(properties);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            if (string.IsNullOrWhiteSpace(property.Name))
                throw new ArgumentException("Property names cannot be empty.", nameof(properties));

            values[property.Name] = NormalizeValue(property.Value);
        }

        return values.Count == 0
            ? Empty
            : new ReadOnlyDictionary<string, object?>(values);
    }

    /// <summary>
    /// Copies a property dictionary into an immutable snapshot safe for asynchronous dispatch.
    /// </summary>
    /// <param name="properties">The source dictionary, or <see langword="null"/>.</param>
    /// <returns>An immutable snapshot.</returns>
    public static IReadOnlyDictionary<string, object?> Snapshot(
        IReadOnlyDictionary<string, object?>? properties)
    {
        if (properties is null || properties.Count == 0)
            return Empty;

        var values = new Dictionary<string, object?>(properties.Count, StringComparer.Ordinal);
        foreach (var pair in properties)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;

            values[pair.Key] = NormalizeValue(pair.Value);
        }

        return values.Count == 0
            ? Empty
            : new ReadOnlyDictionary<string, object?>(values);
    }

    /// <summary>
    /// Combines two property collections, with values from <paramref name="overrides"/> taking precedence.
    /// </summary>
    /// <param name="inherited">The inherited properties.</param>
    /// <param name="overrides">The properties that take precedence.</param>
    /// <returns>An immutable merged snapshot.</returns>
    public static IReadOnlyDictionary<string, object?> Merge(
        IReadOnlyDictionary<string, object?>? inherited,
        IReadOnlyDictionary<string, object?>? overrides)
    {
        if (inherited is null || inherited.Count == 0)
            return Snapshot(overrides);
        if (overrides is null || overrides.Count == 0)
            return Snapshot(inherited);

        var values = new Dictionary<string, object?>(inherited.Count + overrides.Count, StringComparer.Ordinal);
        foreach (var pair in inherited)
            if (!string.IsNullOrWhiteSpace(pair.Key))
                values[pair.Key] = NormalizeValue(pair.Value);
        foreach (var pair in overrides)
            if (!string.IsNullOrWhiteSpace(pair.Key))
                values[pair.Key] = NormalizeValue(pair.Value);

        return new ReadOnlyDictionary<string, object?>(values);
    }

    /// <summary>
    /// Formats properties in stable name order for text sinks.
    /// </summary>
    /// <param name="properties">The properties to format.</param>
    /// <returns>An empty string for no properties, otherwise a brace-delimited list.</returns>
    public static string Format(IReadOnlyDictionary<string, object?>? properties)
    {
        if (properties is null || properties.Count == 0)
            return string.Empty;

        return "{ " + string.Join(", ", properties
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={FormatValue(pair.Value)}")) + " }";
    }

    internal static object? NormalizeValue(object? value) => value switch
    {
        null => null,
        string or bool or byte or sbyte or short or ushort or int or uint or long or ulong
            or float or double or decimal or DateTime or DateTimeOffset or Guid => value,
        Enum enumValue => enumValue.ToString(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    /// <summary>Formats one supported property value using invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The stable text representation.</returns>
    public static string FormatValue(object? value) => value switch
    {
        null => "null",
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };
}

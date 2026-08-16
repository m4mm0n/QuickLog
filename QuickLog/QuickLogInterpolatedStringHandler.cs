using System.Runtime.CompilerServices;

namespace QuickLog;

/// <summary>
/// Builds an interpolated log message only when its level is enabled.
/// </summary>
[InterpolatedStringHandler]
public ref struct QuickLogInterpolatedStringHandler
{
    private DefaultInterpolatedStringHandler _builder;

    /// <summary>
    /// Initializes a handler for a logger and level.
    /// </summary>
    /// <param name="literalLength">The number of literal characters in the interpolation.</param>
    /// <param name="formattedCount">The number of formatted holes in the interpolation.</param>
    /// <param name="logger">The target logger.</param>
    /// <param name="level">The target log level.</param>
    /// <param name="shouldAppend">Receives whether interpolation should be evaluated.</param>
    public QuickLogInterpolatedStringHandler(
        int literalLength,
        int formattedCount,
        IQuickLog logger,
        LogType level,
        out bool shouldAppend)
    {
        shouldAppend = logger.IsEnabled(level);
        _builder = shouldAppend
            ? new DefaultInterpolatedStringHandler(literalLength, formattedCount)
            : default;
    }

    /// <summary>Appends literal text.</summary>
    /// <param name="value">The literal text.</param>
    public void AppendLiteral(string value) => _builder.AppendLiteral(value);

    /// <summary>Appends a formatted value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    public void AppendFormatted<T>(T value) => _builder.AppendFormatted(value);

    /// <summary>Appends a formatted value using a format string.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="format">The format string.</param>
    public void AppendFormatted<T>(T value, string? format) => _builder.AppendFormatted(value, format);

    /// <summary>Appends an aligned formatted value.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="alignment">The alignment width.</param>
    public void AppendFormatted<T>(T value, int alignment) => _builder.AppendFormatted(value, alignment);

    /// <summary>Appends an aligned formatted value using a format string.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value.</param>
    /// <param name="alignment">The alignment width.</param>
    /// <param name="format">The format string.</param>
    public void AppendFormatted<T>(T value, int alignment, string? format) =>
        _builder.AppendFormatted(value, alignment, format);

    /// <summary>Appends a string.</summary>
    /// <param name="value">The string.</param>
    public void AppendFormatted(string? value) => _builder.AppendFormatted(value);

    /// <summary>Appends an aligned string.</summary>
    /// <param name="value">The string.</param>
    /// <param name="alignment">The alignment width.</param>
    /// <param name="format">The optional format string.</param>
    public void AppendFormatted(string? value, int alignment = 0, string? format = null) =>
        _builder.AppendFormatted(value, alignment, format);

    /// <summary>Returns the completed message.</summary>
    /// <returns>The formatted message.</returns>
    public string GetFormattedText() => _builder.ToStringAndClear();
}

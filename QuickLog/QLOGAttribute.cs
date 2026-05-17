namespace QuickLog;

/// <summary>
/// Marks a class, struct, constructor, or method as a QuickLog instrumentation target.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class
    | AttributeTargets.Struct
    | AttributeTargets.Constructor
    | AttributeTargets.Method,
    Inherited = true,
    AllowMultiple = false)]
public class QLOGAttribute : Attribute
{
    /// <summary>
    /// Initializes a QLOG marker with the default lean instrumentation options.
    /// </summary>
    public QLOGAttribute()
        : this(QLogOption.Default)
    {
    }

    /// <summary>
    /// Initializes a QLOG marker with explicit instrumentation options.
    /// </summary>
    /// <param name="options">Markers to emit when a QLOG helper runs this target.</param>
    public QLOGAttribute(QLogOption options)
    {
        Options = options;
    }

    /// <summary>
    /// Gets the markers emitted when a QLOG helper runs this target.
    /// </summary>
    public QLogOption Options { get; }

    /// <summary>
    /// Gets or sets the level used for entry and exit markers.
    /// </summary>
    public LogType Level { get; set; } = LogType.Info;

    /// <summary>
    /// Gets or sets the level used for exception markers.
    /// </summary>
    public LogType ExceptionLevel { get; set; } = LogType.Error;

    /// <summary>
    /// Gets or sets an optional display name used instead of the reflected member name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Pascal-case alias for <see cref="QLOGAttribute"/> when projects prefer <c>[QLog]</c> spelling.
/// </summary>
public sealed class QLogAttribute : QLOGAttribute
{
    /// <summary>
    /// Initializes a QLOG marker with the default lean instrumentation options.
    /// </summary>
    public QLogAttribute()
    {
    }

    /// <summary>
    /// Initializes a QLOG marker with explicit instrumentation options.
    /// </summary>
    /// <param name="options">Markers to emit when a QLOG helper runs this target.</param>
    public QLogAttribute(QLogOption options)
        : base(options)
    {
    }
}

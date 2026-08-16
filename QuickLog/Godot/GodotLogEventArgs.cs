namespace QuickLog.Godot;

/// <summary>
/// Identifies the origin of a Godot log callback received by <see cref="GodotBridge"/>.
/// </summary>
public enum GodotLogSource
{
    /// <summary><c>GD.Print()</c> — standard output.</summary>
    Print,
    /// <summary><c>GD.PrintErr()</c> — standard error output.</summary>
    PrintError,
    /// <summary><c>GD.PushError()</c> — engine error.</summary>
    PushError,
    /// <summary><c>GD.PushWarning()</c> — engine warning.</summary>
    PushWarning,
    /// <summary>GDScript runtime error.</summary>
    ScriptError,
    /// <summary>Shader compilation error.</summary>
    ShaderError
}

/// <summary>
/// Event arguments raised by <see cref="GodotBridge.GodotLogReceived"/> when the Godot engine
/// emits a log or error message through the <c>Godot.Logger</c> callback system.
/// </summary>
public sealed class GodotLogEventArgs : EventArgs
{
    /// <summary>The categorised source of this log entry.</summary>
    public GodotLogSource Source { get; }

    /// <summary>Fully composed log message (includes context for errors).</summary>
    public string Message { get; }

    /// <summary>
    /// For error/warning callbacks: the function name where the error was raised. Otherwise empty.
    /// </summary>
    public string Function { get; }

    /// <summary>
    /// For error/warning callbacks: the source file where the error was raised. Otherwise empty.
    /// </summary>
    public string File { get; }

    /// <summary>For error/warning callbacks: the line number. Otherwise 0.</summary>
    public int Line { get; }

    /// <summary>The <see cref="LogType"/> that was (or will be) passed to the QuickLog logger.</summary>
    public LogType LoggingType { get; }

    /// <summary>
    /// Set to <see langword="true"/> inside the event handler to prevent
    /// <see cref="GodotBridge"/> from forwarding this entry to <see cref="IQuickLog"/>.
    /// </summary>
    public bool SuppressLogging { get; set; }

    internal GodotLogEventArgs(GodotLogSource source, string message, LogType loggingType,
        string function = "", string file = "", int line = 0)
    {
        Source = source;
        Message = message;
        LoggingType = loggingType;
        Function = function;
        File = file;
        Line = line;
    }
}

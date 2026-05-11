/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : GodotBridge.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

namespace QuickLog.Godot;

/// <summary>
/// Static routing core that receives Godot engine log/error callbacks and forwards them
/// to a QuickLog <see cref="IQuickLog"/> instance.
/// <para>
/// <b>Automatic use:</b> <see cref="GodotLogInterceptor.Attach"/> configures this class
/// automatically and (when possible) wires it into Godot's Logger system without any changes
/// to your Godot project.
/// </para>
/// <para>
/// <b>Manual bridge (guaranteed to work in all Godot 4 C# setups):</b>
/// If automatic registration does not work, add the following two files to your Godot project:
/// <code>
/// // QuickLogSink.cs  (inside your Godot project, NOT in QuickLog itself)
/// public partial class QuickLogSink : Godot.Logger
/// {
///     public override void _LogMessage(string message, bool error)
///         => GodotBridge.HandleMessage(message, error);
///
///     public override void _LogError(string function, string file, int line,
///         string code, string rationale, bool errorType, int errorTypeValue,
///         Godot.Collections.Array&lt;ScriptBacktrace&gt; scriptBacktraces)
///         => GodotBridge.HandleError(function, file, line, code, rationale, errorTypeValue);
/// }
///
/// // In your AutoLoad or main scene _Ready():
/// OS.AddLogger(new QuickLogSink());
/// </code>
/// </para>
/// </summary>
public static class GodotBridge
{
    private static IQuickLog? _logger;
    private static GodotLogOptions _options = new();
    private static readonly object _lock = new();

    // ── Godot ErrorType enum values (matches Godot.Logger.ErrorType) ──────────
    // Error=0, Warning=1, Script=2, Shader=3
    private const int ErrorTypeError   = 0;
    private const int ErrorTypeWarning = 1;
    private const int ErrorTypeScript  = 2;
    private const int ErrorTypeShader  = 3;

    /// <summary>
    /// Raised whenever a Godot log entry is received, before it is forwarded to QuickLog.
    /// Set <see cref="GodotLogEventArgs.SuppressLogging"/> to skip the QuickLog forward.
    /// </summary>
    public static event EventHandler<GodotLogEventArgs>? GodotLogReceived;

    /// <summary>Whether a logger is currently configured on the bridge.</summary>
    public static bool IsConfigured { get { lock (_lock) return _logger != null; } }

    internal static void Configure(IQuickLog logger, GodotLogOptions options)
    {
        lock (_lock)
        {
            _logger  = logger;
            _options = options;
        }
    }

    internal static void Clear()
    {
        lock (_lock) _logger = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public entry points — called from Godot.Logger overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from your <c>Godot.Logger._LogMessage</c> override.
    /// Routes <c>GD.Print()</c> and <c>GD.PrintErr()</c> through QuickLog.
    /// </summary>
    /// <param name="message">The message text.</param>
    /// <param name="error"><see langword="true"/> when the message came from <c>GD.PrintErr()</c>.</param>
    public static void HandleMessage(string message, bool error)
    {
        IQuickLog? logger;
        GodotLogOptions opts;
        lock (_lock) { logger = _logger; opts = _options; }
        if (logger == null) return;

        bool intercept = error ? opts.InterceptPrintError : opts.InterceptPrint;
        if (!intercept) return;

        var source     = error ? GodotLogSource.PrintError : GodotLogSource.Print;
        var logType    = error ? opts.PrintErrorLogType    : opts.PrintLogType;
        var args       = new GodotLogEventArgs(source, message, logType);

        try { GodotLogReceived?.Invoke(null, args); } catch { }
        if (args.SuppressLogging) return;

        try { logger.Log(args.LoggingType, $"[Godot] {message}"); } catch { }
    }

    /// <summary>
    /// Call this from your <c>Godot.Logger._LogError</c> override.
    /// Routes <c>GD.PushError()</c>, <c>GD.PushWarning()</c>, and GDScript/shader errors
    /// through QuickLog.
    /// </summary>
    /// <param name="function">The function name where the error was raised.</param>
    /// <param name="file">The source file where the error was raised.</param>
    /// <param name="line">The line number where the error was raised.</param>
    /// <param name="code">Short error code.</param>
    /// <param name="rationale">Human-readable error description.</param>
    /// <param name="errorTypeValue">
    /// Numeric value of <c>Godot.Logger.ErrorType</c>:
    /// 0=Error, 1=Warning, 2=Script, 3=Shader.
    /// </param>
    public static void HandleError(string function, string file, int line,
        string code, string rationale, int errorTypeValue)
    {
        IQuickLog? logger;
        GodotLogOptions opts;
        lock (_lock) { logger = _logger; opts = _options; }
        if (logger == null) return;

        var (source, logType, intercept) = errorTypeValue switch
        {
            ErrorTypeError   => (GodotLogSource.PushError,    opts.ErrorLogType,       opts.InterceptErrors),
            ErrorTypeWarning => (GodotLogSource.PushWarning,  opts.WarningLogType,     opts.InterceptWarnings),
            ErrorTypeScript  => (GodotLogSource.ScriptError,  opts.ScriptErrorLogType, opts.InterceptScriptErrors),
            ErrorTypeShader  => (GodotLogSource.ShaderError,  opts.ScriptErrorLogType, opts.InterceptScriptErrors),
            _                => (GodotLogSource.PushError,    opts.ErrorLogType,       opts.InterceptErrors)
        };

        if (!intercept) return;

        var message = BuildErrorMessage(source, function, file, line, code, rationale);
        var args    = new GodotLogEventArgs(source, message, logType, function, file, line);

        try { GodotLogReceived?.Invoke(null, args); } catch { }
        if (args.SuppressLogging) return;

        try { logger.Log(args.LoggingType, message); } catch { }
    }

    private static string BuildErrorMessage(GodotLogSource source, string function,
        string file, int line, string code, string rationale)
    {
        var tag = source switch
        {
            GodotLogSource.PushError   => "[Godot Error]",
            GodotLogSource.PushWarning => "[Godot Warning]",
            GodotLogSource.ScriptError => "[GDScript Error]",
            GodotLogSource.ShaderError => "[Shader Error]",
            _                          => "[Godot]"
        };

        var location = !string.IsNullOrWhiteSpace(file)
            ? $" @ {file}:{line} ({function})"
            : string.Empty;

        var detail = string.IsNullOrWhiteSpace(code)
            ? rationale
            : $"{code}: {rationale}";

        return $"{tag}{location} — {detail}";
    }
}

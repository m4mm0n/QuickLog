/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : GodotLogOptions.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-05-11
 *  License        : MIT — https://opensource.org/licenses/MIT
 * ====================================================================================================
 */

using QuickLog.Exceptions;

namespace QuickLog.Godot;

/// <summary>
/// Controls the behaviour of <see cref="GodotLogInterceptor"/> when attached to a running
/// Godot project.
/// </summary>
public sealed class GodotLogOptions
{
    // ── Output routing ───────────────────────────────────────────────────────

    /// <summary>
    /// Route <c>GD.Print()</c> messages through QuickLog.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool InterceptPrint { get; set; } = true;

    /// <summary>
    /// Route <c>GD.PrintErr()</c> messages through QuickLog.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool InterceptPrintError { get; set; } = true;

    /// <summary>
    /// Route <c>GD.PushError()</c> / engine errors through QuickLog.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool InterceptErrors { get; set; } = true;

    /// <summary>
    /// Route <c>GD.PushWarning()</c> through QuickLog.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool InterceptWarnings { get; set; } = true;

    /// <summary>
    /// Route GDScript and shader errors through QuickLog.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool InterceptScriptErrors { get; set; } = true;

    // ── Log levels ───────────────────────────────────────────────────────────

    /// <summary><see cref="LogType"/> used for <c>GD.Print()</c> messages. Defaults to <see cref="LogType.Info"/>.</summary>
    public LogType PrintLogType { get; set; } = LogType.Info;

    /// <summary><see cref="LogType"/> used for <c>GD.PrintErr()</c> messages. Defaults to <see cref="LogType.Error"/>.</summary>
    public LogType PrintErrorLogType { get; set; } = LogType.Error;

    /// <summary><see cref="LogType"/> used for <c>GD.PushError()</c> and engine errors. Defaults to <see cref="LogType.Error"/>.</summary>
    public LogType ErrorLogType { get; set; } = LogType.Error;

    /// <summary><see cref="LogType"/> used for <c>GD.PushWarning()</c>. Defaults to <see cref="LogType.Warn"/>.</summary>
    public LogType WarningLogType { get; set; } = LogType.Warn;

    /// <summary><see cref="LogType"/> used for GDScript/shader errors. Defaults to <see cref="LogType.Crit"/>.</summary>
    public LogType ScriptErrorLogType { get; set; } = LogType.Crit;

    // ── Exception ownership ──────────────────────────────────────────────────

    /// <summary>
    /// When <see langword="true"/>, attaches <see cref="ExceptionHookManager"/> so that all
    /// unhandled .NET exceptions in the Godot project are also captured by QuickLog.
    /// The popup will use <see cref="GodotAlertPopup"/> (native <c>OS.Alert</c> dialog) by default.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool HijackExceptions { get; set; } = true;

    /// <summary>
    /// Overrides for the exception hook behaviour. When <see langword="null"/>, sensible
    /// Godot-appropriate defaults are applied (native OS.Alert popup, Crit log level,
    /// crash dump enabled).
    /// </summary>
    public ExceptionHookOptions? ExceptionOptions { get; set; }

    // ── Dynamic Logger registration ──────────────────────────────────────────

    /// <summary>
    /// When <see langword="true"/>, <see cref="GodotLogInterceptor"/> will attempt to
    /// dynamically emit a <c>Godot.Logger</c> subclass and register it via <c>OS.AddLogger()</c>
    /// using reflection. This enables fully automatic engine-level log interception without
    /// any code changes in the Godot project.
    /// <para>
    /// Whether this succeeds depends on the Godot version and runtime environment. Check
    /// <see cref="GodotLogInterceptor.IsDynamicSinkRegistered"/> after attaching.
    /// If it fails, use <see cref="GodotBridge"/> manually in a <c>partial class</c> Logger subclass.
    /// </para>
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool TryDynamicLoggerRegistration { get; set; } = true;
}

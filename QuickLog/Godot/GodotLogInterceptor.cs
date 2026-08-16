using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using QuickLog.Exceptions;
using QuickLog.Utilities;

namespace QuickLog.Godot;

/// <summary>
/// Intercepts Godot engine logging and hijacks unhandled .NET exceptions in a Godot project,
/// routing everything through QuickLog. No compile-time dependency on GodotSharp is required.
/// <para>
/// <b>Usage (one line at application startup):</b>
/// <code>
/// LogManager.AttachGodotHooks();
/// </code>
/// </para>
/// <para>
/// Internally this class:
/// <list type="bullet">
///   <item>Configures <see cref="GodotBridge"/> with the target logger and options.</item>
///   <item>Attempts to dynamically emit a <c>Godot.Logger</c> subclass via
///         <c>Reflection.Emit</c> and register it through <c>OS.AddLogger()</c>. Whether this
///         succeeds depends on the Godot runtime — check
///         <see cref="IsDynamicSinkRegistered"/> after attaching.</item>
///   <item>Attaches <see cref="ExceptionHookManager"/> with a <see cref="GodotAlertPopup"/>
///         so that unhandled .NET exceptions are logged, dumped, and shown via
///         Godot's native <c>OS.Alert()</c> dialog.</item>
/// </list>
/// If dynamic registration fails, use <see cref="GodotBridge"/> directly in a
/// <c>partial class Godot.Logger</c> subclass inside your Godot project.
/// </para>
/// </summary>
public static class GodotLogInterceptor
{
    private static bool _attached;
    private static bool _exceptionHooksAttached;
    private static object? _dynamicSinkInstance;
    private static MethodInfo? _removeLoggerMethod;
    private static readonly object _lock = new();

    /// <summary>Whether <see cref="Attach"/> has been called successfully.</summary>
    public static bool IsAttached { get { lock (_lock) return _attached; } }

    /// <summary>
    /// Whether the current process appears to be running inside the Godot runtime.
    /// Checks for loaded assemblies whose names start with <c>"Godot"</c>.
    /// </summary>
    public static bool IsGodotPresent => GodotReflection.IsRuntimePresent();

    /// <summary>
    /// Whether the dynamic <c>Godot.Logger</c> subclass was created and registered via
    /// <c>OS.AddLogger()</c>. When <see langword="false"/>, use <see cref="GodotBridge"/>
    /// manually — see its documentation for a copy-paste bridge template.
    /// </summary>
    public static bool IsDynamicSinkRegistered { get { lock (_lock) return _dynamicSinkInstance != null; } }

    /// <summary>
    /// Forwarded from <see cref="GodotBridge.GodotLogReceived"/>. Subscribe here to receive
    /// all Godot log events before they reach QuickLog.
    /// </summary>
    public static event EventHandler<GodotLogEventArgs>? GodotLogReceived
    {
        add => GodotBridge.GodotLogReceived += value;
        remove => GodotBridge.GodotLogReceived -= value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attaches the Godot log interceptor and exception hook to <paramref name="logger"/>.
    /// Safe to call multiple times — subsequent calls replace the logger and options without
    /// re-registering the dynamic sink.
    /// </summary>
    /// <param name="logger">The QuickLog logger that will receive all intercepted output.</param>
    /// <param name="options">
    /// Behaviour options. <see langword="null"/> uses sensible Godot-appropriate defaults.
    /// </param>
    public static void Attach(IQuickLog logger, GodotLogOptions? options = null)
    {
        if (logger == null) throw new ArgumentNullException(nameof(logger));
        options ??= new GodotLogOptions();

        lock (_lock)
        {
            // 1. Configure the static bridge (always succeeds).
            GodotBridge.Configure(logger, options);

            // 2. Attempt dynamic OS.AddLogger registration (best effort). Retry if a
            // previous attach happened before Godot types were available.
            if (options.TryDynamicLoggerRegistration && _dynamicSinkInstance == null)
                TryRegisterDynamicSink();

            // 3. Attach exception hooks with Godot-native popup.
            if (options.HijackExceptions)
            {
                var exOpts = options.ExceptionOptions ?? BuildDefaultExceptionOptions();
                ExceptionHookManager.Attach(logger, exOpts);
                _exceptionHooksAttached = true;
            }
            else if (_exceptionHooksAttached)
            {
                ExceptionHookManager.Detach();
                _exceptionHooksAttached = false;
            }

            _attached = true;
        }
    }

    /// <summary>
    /// Removes the dynamic <c>Godot.Logger</c> sink (if registered), clears
    /// <see cref="GodotBridge"/>, and detaches <see cref="ExceptionHookManager"/>.
    /// Safe to call when not attached.
    /// </summary>
    public static void Detach()
    {
        lock (_lock)
        {
            if (!_attached) return;

            TryUnregisterDynamicSink();
            GodotBridge.Clear();
            if (_exceptionHooksAttached)
                ExceptionHookManager.Detach();
            _exceptionHooksAttached = false;
            _attached = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dynamic Godot.Logger registration via Reflection.Emit
    // ─────────────────────────────────────────────────────────────────────────

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "The optional dynamic Godot sink is runtime-guarded and safely skipped when metadata is unavailable.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "The optional dynamic Godot sink is runtime-guarded and safely skipped when metadata is unavailable.")]
    private static void TryRegisterDynamicSink()
    {
        try
        {
            if (!CanEmitDynamicSink(RuntimeFeature.IsDynamicCodeSupported, RuntimeFeature.IsDynamicCodeCompiled))
                return;

            // Resolve Godot.Logger type from any loaded Godot assembly.
            var loggerType = GodotReflection.ResolveType("Godot.Logger");
            if (loggerType == null) return;

            // Find the two virtual methods we need to override.
            var logMessageMethod = FindLogMessageMethod(loggerType);
            var logErrorMethod = FindLogErrorMethod(loggerType);
            if (logMessageMethod == null || logErrorMethod == null) return;

            // Emit the dynamic subclass.
            var sinkType = EmitSinkType(loggerType, logMessageMethod, logErrorMethod);
            if (sinkType == null) return;

            // Instantiate and register via OS.AddLogger(Logger).
            var sinkInstance = Activator.CreateInstance(sinkType);
            if (sinkInstance == null) return;

            var osType = GodotReflection.ResolveType("Godot.OS");
            var addLogger = osType?.GetMethod("AddLogger",
                BindingFlags.Public | BindingFlags.Static,
                binder: null, types: [loggerType], modifiers: null);
            if (addLogger == null) return;

            addLogger.Invoke(null, [sinkInstance]);

            // Cache for cleanup.
            _dynamicSinkInstance = sinkInstance;
            _removeLoggerMethod = osType?.GetMethod("RemoveLogger",
                BindingFlags.Public | BindingFlags.Static,
                binder: null, types: [loggerType], modifiers: null);
        }
        catch { /* dynamic registration is best-effort — swallow all failures */ }
    }

    internal static bool CanEmitDynamicSink(bool isDynamicCodeSupported, bool isDynamicCodeCompiled)
        => isDynamicCodeSupported && isDynamicCodeCompiled;

    private static MethodInfo? FindLogMessageMethod(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type loggerType) =>
        loggerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "_LogMessage" || !m.IsVirtual || m.ReturnType != typeof(void))
                    return false;

                var p = m.GetParameters();
                return p.Length >= 2 &&
                       p[0].ParameterType == typeof(string) &&
                       p[1].ParameterType == typeof(bool);
            });

    private static MethodInfo? FindLogErrorMethod(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type loggerType) =>
        loggerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "_LogError" || !m.IsVirtual || m.ReturnType != typeof(void))
                    return false;

                var p = m.GetParameters();
                return p.Length >= 7 &&
                       p[0].ParameterType == typeof(string) &&
                       p[1].ParameterType == typeof(string) &&
                       p[2].ParameterType == typeof(int) &&
                       p[3].ParameterType == typeof(string) &&
                       p[4].ParameterType == typeof(string);
            });

    private static void TryUnregisterDynamicSink()
    {
        try
        {
            if (_dynamicSinkInstance == null || _removeLoggerMethod == null) return;
            _removeLoggerMethod.Invoke(null, [_dynamicSinkInstance]);
        }
        catch { }
        finally
        {
            _dynamicSinkInstance = null;
            _removeLoggerMethod = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IL emission — QuickLogGodotSink : Godot.Logger
    // ─────────────────────────────────────────────────────────────────────────

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2067",
        Justification = "Reflection.Emit is runtime-guarded and the optional integration safely falls back to the manual bridge.")]
    [UnconditionalSuppressMessage(
        "Aot",
        "IL3050",
        Justification = "Reflection.Emit is runtime-guarded and the optional integration safely falls back to the manual bridge.")]
    private static Type? EmitSinkType(Type loggerBase, MethodInfo logMessageMethod, MethodInfo logErrorMethod)
    {
        try
        {
            var asmName = new AssemblyName("QuickLogGodotRuntime");
            var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);
            var modBuilder = asmBuilder.DefineDynamicModule("QuickLogGodotRuntime");

            var typeBuilder = modBuilder.DefineType(
                "QuickLogGodotSink",
                TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed,
                loggerBase);

            if (!EmitLogMessage(typeBuilder, logMessageMethod))
                return null;
            if (!EmitLogError(typeBuilder, logErrorMethod))
                return null;

            return typeBuilder.CreateType();
        }
        catch { return null; }
    }

    private static bool EmitLogMessage(TypeBuilder tb, MethodInfo baseMethod)
    {
        // _LogMessage(string message, bool error)
        var paramTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();

        var method = tb.DefineMethod(
            "_LogMessage",
            GetOverrideAttributes(baseMethod),
            typeof(void), paramTypes);

        // GodotBridge.HandleMessage(string, bool) static method
        var handleMessage = typeof(GodotBridge).GetMethod(
            nameof(GodotBridge.HandleMessage),
            BindingFlags.Public | BindingFlags.Static)!;

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1); // message (string)
        il.Emit(OpCodes.Ldarg_2); // error   (bool)
        il.Emit(OpCodes.Call, handleMessage);
        il.Emit(OpCodes.Ret);

        tb.DefineMethodOverride(method, baseMethod);
        return true;
    }

    private static bool EmitLogError(TypeBuilder tb, MethodInfo baseMethod)
    {
        // _LogError(string function, string file, int line, string code, string rationale,
        //           bool errorType, int errorTypeValue, Array<ScriptBacktrace> scriptBacktraces)
        var parameters = baseMethod.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType).ToArray();
        var errorTypeParameterIndex = FindErrorTypeParameterIndex(parameters);
        if (errorTypeParameterIndex < 0)
            return false;

        var method = tb.DefineMethod(
            "_LogError",
            GetOverrideAttributes(baseMethod),
            typeof(void), paramTypes);

        // GodotBridge.HandleError(string, string, int, string, string, int) — we skip the last param
        var handleError = typeof(GodotBridge).GetMethod(
            nameof(GodotBridge.HandleError),
            BindingFlags.Public | BindingFlags.Static)!;

        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1); // function       (string)
        il.Emit(OpCodes.Ldarg_2); // file           (string)
        il.Emit(OpCodes.Ldarg_3); // line           (int)
        il.Emit(OpCodes.Ldarg_S, (byte)4); // code      (string)
        il.Emit(OpCodes.Ldarg_S, (byte)5); // rationale (string)
        il.Emit(OpCodes.Ldarg_S, (byte)(errorTypeParameterIndex + 1));
        il.Emit(OpCodes.Conv_I4);
        il.Emit(OpCodes.Call, handleError);
        il.Emit(OpCodes.Ret);

        tb.DefineMethodOverride(method, baseMethod);
        return true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IL helper methods
    // ─────────────────────────────────────────────────────────────────────────

    private static MethodAttributes GetOverrideAttributes(MethodInfo baseMethod)
    {
        var visibility =
            baseMethod.IsPublic ? MethodAttributes.Public :
            baseMethod.IsFamily ? MethodAttributes.Family :
            baseMethod.IsFamilyOrAssembly ? MethodAttributes.FamORAssem :
            baseMethod.IsAssembly ? MethodAttributes.Assembly :
            MethodAttributes.Private;

        return visibility | MethodAttributes.Virtual | MethodAttributes.HideBySig;
    }

    private static int FindErrorTypeParameterIndex(ParameterInfo[] parameters)
    {
        for (var i = 5; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            if (parameter.ParameterType == typeof(bool))
                continue;

            if (parameter.ParameterType == typeof(int) || parameter.ParameterType.IsEnum)
                return i;
        }

        return -1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Default exception options for Godot context
    // ─────────────────────────────────────────────────────────────────────────

    private static ExceptionHookOptions BuildDefaultExceptionOptions() => new()
    {
        ShowPopup = true,
        ShowStackTraceInPopup = true,
        ExceptionLogType = LogType.Crit,
        PopupTitle = "Unhandled Exception — QuickLog",
        CustomPopup = new GodotAlertPopup(),
        MarkTaskExceptionsObserved = false,
        CrashDump = new CrashDumpOptions { Enabled = true },
        Restart = new RestartOptions { EnableAutoRestart = false }
    };
}

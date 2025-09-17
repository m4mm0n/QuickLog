using System.Reflection;

namespace QuickLog.Utilities
{
    /// <summary>
    /// Resolves Godot's user:// directory at runtime without any compile-time dependency on Godot.
    /// If the Godot runtime is present, attempts to call ProjectSettings.GlobalizePath("user://") via reflection.
    /// Otherwise returns a safe, OS-specific fallback directory.
    /// </summary>
    internal static class GodotUserPathResolver
    {
        /// <summary>
        /// Returns true if the current process appears to be running under the Godot runtime.
        /// Heuristic: checks loaded assemblies for names starting with "Godot".
        /// </summary>
        public static bool IsGodotRuntime() =>
            AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetName().Name).Any(n =>
                !string.IsNullOrEmpty(n) && n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Tries to resolve an absolute path for Godot's user:// folder.
        /// If reflection into Godot fails or Godot is not present, returns a fallback path.
        /// </summary>
        /// <param name="fallbackRoot">
        /// Optional fallback root when not in Godot. If omitted, uses
        /// %LOCALAPPDATA%/GodotUser on Windows (and similar on other platforms).
        /// </param>
        public static string GetUserDir(string? fallbackRoot = null)
        {
            // 1) If Godot looks present, attempt reflection into ProjectSettings.GlobalizePath("user://")
            if (IsGodotRuntime())
            {
                try
                {
                    // Try both typical type names; the assembly-qualified name may vary by setup.
                    var t = Type.GetType("Godot.ProjectSettings, GodotSharp")
                            ?? Type.GetType("Godot.ProjectSettings");
                    var mi = t?.GetMethod("GlobalizePath", BindingFlags.Public | BindingFlags.Static,
                                          binder: null, types: new[] { typeof(string) }, modifiers: null);
                    if (mi != null)
                    {
                        var abs = mi.Invoke(null, new object[] { "user://" }) as string;
                        if (!string.IsNullOrWhiteSpace(abs))
                            return abs!;
                    }
                }
                catch
                {
                    // Ignore and fall through to fallback
                }
            }

            // 2) Fallback (non-Godot runs)
            if (!string.IsNullOrWhiteSpace(fallbackRoot))
                return fallbackRoot;

            var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localApp, "GodotUser");
        }
    }
}

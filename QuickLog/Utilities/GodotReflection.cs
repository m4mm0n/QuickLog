using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace QuickLog.Utilities;

/// <summary>
/// Reflection helpers for optional Godot runtime integration.
/// </summary>
internal static class GodotReflection
{
    private static readonly string[] KnownAssemblyQualifiedPrefixes =
    [
        "GodotSharp",
        "GodotSharpEditor"
    ];

    public static bool IsRuntimePresent()
    {
        if (ResolveType("Godot.OS") is not null ||
            ResolveType("Godot.Logger") is not null ||
            ResolveType("Godot.ProjectSettings") is not null)
            return true;

        return AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name)
            .Any(n => !string.IsNullOrWhiteSpace(n) &&
                      n.StartsWith("Godot", StringComparison.OrdinalIgnoreCase));
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2057",
        Justification = "Optional Godot types are resolved by runtime name and all failures use a safe fallback.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "Optional Godot assembly scanning is best-effort and all failures use a safe fallback.")]
    public static Type? ResolveType(string fullName)
    {
        foreach (var assemblyName in KnownAssemblyQualifiedPrefixes)
        {
            var type = Type.GetType($"{fullName}, {assemblyName}", throwOnError: false);
            if (type is not null)
                return type;
        }

        var direct = Type.GetType(fullName, throwOnError: false);
        if (direct is not null)
            return direct;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetType(fullName, throwOnError: false, ignoreCase: false);
                if (type is not null)
                    return type;
            }
            catch
            {
                // Some dynamic or partially loaded assemblies can throw while resolving types.
            }
        }

        return null;
    }

    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Optional Godot method lookup is best-effort and callers provide a safe fallback.")]
    public static MethodInfo? ResolveStaticMethod(string typeName, string methodName, Type[] parameterTypes)
    {
        try
        {
            return ResolveType(typeName)?.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: parameterTypes,
                modifiers: null);
        }
        catch
        {
            return null;
        }
    }
}

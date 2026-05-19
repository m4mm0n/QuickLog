using System.Reflection;

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

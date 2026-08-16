using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace QuickLog;

/// <summary>
/// Identifies the kind of member marked with a QLOG attribute.
/// </summary>
public enum QLogTargetKind
{
    /// <summary>A class or struct is marked.</summary>
    Type,

    /// <summary>A method is marked.</summary>
    Method,

    /// <summary>A constructor is marked.</summary>
    Constructor
}

/// <summary>
/// Describes one explicitly marked QLOG target.
/// </summary>
/// <param name="Kind">Kind of target.</param>
/// <param name="Name">Reflected target name.</param>
/// <param name="DisplayName">Name used in emitted QLOG markers.</param>
/// <param name="Options">Options configured on the marker.</param>
/// <param name="Level">Level used for entry and exit markers.</param>
/// <param name="ExceptionLevel">Level used for exception markers.</param>
/// <param name="Member">Underlying reflected member.</param>
public sealed record QLogTarget(
    QLogTargetKind Kind,
    string Name,
    string DisplayName,
    QLogOption Options,
    LogType Level,
    LogType ExceptionLevel,
    MemberInfo Member);

/// <summary>
/// Discovers classes, structs, constructors, and methods explicitly marked with QLOG attributes.
/// </summary>
public static class QLogDiscovery
{
    /// <summary>
    /// Scans all loadable types in an assembly for explicit QLOG markers.
    /// </summary>
    /// <param name="assembly">Assembly to scan.</param>
    /// <returns>Marked targets ordered by name.</returns>
    [RequiresUnreferencedCode("Assembly-wide QLOG discovery requires marker metadata to be preserved by the application.")]
    public static IReadOnlyList<QLogTarget> Scan(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var targets = new List<QLogTarget>();
        foreach (var type in GetLoadableTypes(assembly))
            targets.AddRange(Scan(type));

        return targets
            .OrderBy(target => target.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Scans one type for explicit QLOG markers.
    /// </summary>
    /// <param name="type">Type to scan.</param>
    /// <returns>Marked targets ordered by name.</returns>
    public static IReadOnlyList<QLogTarget> Scan(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods
            | DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var targets = new List<QLogTarget>();
        AddTarget(targets, QLogTargetKind.Type, type, OwnAttribute(type));

        const BindingFlags flags = BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.DeclaredOnly;

        foreach (var constructor in type.GetConstructors(flags))
            AddTarget(targets, QLogTargetKind.Constructor, constructor, OwnAttribute(constructor));

        foreach (var method in type.GetMethods(flags).Where(method => !method.IsSpecialName))
            AddTarget(targets, QLogTargetKind.Method, method, OwnAttribute(method));

        return targets
            .OrderBy(target => target.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Adds one discovered target when a marker is present.
    /// </summary>
    /// <param name="targets">Mutable target list.</param>
    /// <param name="kind">Kind of target being added.</param>
    /// <param name="member">Reflected member.</param>
    /// <param name="attribute">Explicit marker on the member.</param>
    private static void AddTarget(List<QLogTarget> targets, QLogTargetKind kind, MemberInfo member, QLOGAttribute? attribute)
    {
        if (attribute is null)
            return;

        targets.Add(new QLogTarget(
            kind,
            QLogMetadata.MemberName(member),
            QLogMetadata.DisplayName(member, attribute, member.Name),
            attribute.Options,
            attribute.Level,
            attribute.ExceptionLevel,
            member));
    }

    /// <summary>
    /// Reads only attributes declared directly on the supplied member.
    /// </summary>
    /// <param name="member">Member to inspect.</param>
    /// <returns>The explicit marker, or <see langword="null"/> when absent.</returns>
    private static QLOGAttribute? OwnAttribute(MemberInfo member)
        => member.GetCustomAttributes(inherit: false).OfType<QLOGAttribute>().FirstOrDefault();

    /// <summary>
    /// Returns all types that can be loaded from an assembly.
    /// </summary>
    /// <param name="assembly">Assembly to inspect.</param>
    /// <returns>Loadable types from the assembly.</returns>
    [RequiresUnreferencedCode("Assembly type enumeration requires metadata to be preserved.")]
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>();
        }
    }
}

using System.Reflection;

namespace QuickLog;

/// <summary>
/// Resolves QLOG attributes and display names from reflection metadata.
/// </summary>
internal static class QLogMetadata
{
    /// <summary>
    /// Finds the marker for a method, constructor, or declaring type.
    /// </summary>
    /// <param name="method">Method or constructor to inspect.</param>
    /// <returns>The resolved marker, or <see langword="null"/> when none exists.</returns>
    public static QLOGAttribute? Resolve(MethodBase? method)
    {
        if (method is null)
            return null;

        return method.GetCustomAttributes(inherit: true).OfType<QLOGAttribute>().FirstOrDefault()
            ?? method.DeclaringType?.GetCustomAttributes(inherit: true).OfType<QLOGAttribute>().FirstOrDefault();
    }

    /// <summary>
    /// Gets the emitted display name for a target.
    /// </summary>
    /// <param name="member">Reflected member.</param>
    /// <param name="attribute">Resolved marker.</param>
    /// <param name="fallbackName">Fallback name used when reflection is unavailable.</param>
    /// <returns>The marker display name.</returns>
    public static string DisplayName(MemberInfo? member, QLOGAttribute? attribute, string fallbackName)
        => !string.IsNullOrWhiteSpace(attribute?.Name)
            ? attribute!.Name!
            : MemberName(member) is { Length: > 0 } name
                ? name
                : fallbackName;

    /// <summary>
    /// Builds a stable reflected member name.
    /// </summary>
    /// <param name="member">Reflected member.</param>
    /// <returns>A full type/member name when available.</returns>
    public static string MemberName(MemberInfo? member)
    {
        if (member is Type type)
            return type.FullName ?? type.Name;

        return member?.DeclaringType is null
            ? member?.Name ?? string.Empty
            : $"{member.DeclaringType.FullName}.{member.Name}";
    }
}

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using QuickLog.Utilities;

namespace QuickLog;

internal static class Extensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            var attribute = (DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))!;
            return attribute == null ? value.ToString() : attribute.Description;
        }

        return "";
    }

    public static string ReplaceInvalidChars(this string filename)
    {
        Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
        var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var r = new Regex($"[{Regex.Escape(regexSearch)}]");
        return r.Replace(filename, "");
    }

    /// <summary>
    /// Ensures that <paramref name="value"/> is not <see langword="null"/> and returns the validated value
    /// </summary>
    /// <typeparam name="T">Type of <paramref name="value"/></typeparam>
    /// <param name="value">Value to validate</param>
    /// <param name="name">Name of the value</param>
    /// <param name="errorMessage">Error message (optional)</param>
    /// <returns>Value (when not null)</returns>
    /// <exception cref="ArgumentNullException" />
    public static T EnsureNotNull<T>([NotNull][ValidatedNotNull] this T? value,
        [CallerArgumentExpression("value")] string name = "",
        string? errorMessage = null)
        where T : class =>
        value is null ? throw new ArgumentNullException(name, errorMessage) : value;

    public static string ReplaceInvalidPathChars(this string path)
    {
        // Get invalid characters for paths
        var invalidChars = Path.GetInvalidPathChars();

        // Create regex to match invalid characters
        var regex = new Regex($"[{Regex.Escape(new string(invalidChars))}]");

        return regex.Replace(path, "");
    }

}
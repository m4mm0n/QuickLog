/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : Extensions.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2024-10-06 09:02:53 +02:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 71B994FF
 *  
 *  Description    :
 *                   Provides extension methods for common operations on strings and enumerations, including validation, description
 *                   retrieval, and sanitization of file and path names.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 71B994FF

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace QuickLog.Utilities;

/// <summary>
/// Provides extension methods for common operations on strings and enumerations, including validation, description
/// retrieval, and sanitization of file and path names.
/// </summary>
/// <remarks>These extension methods are intended to simplify routine tasks such as extracting descriptions from
/// enumeration values, ensuring non-null references, and removing invalid characters from file or path names. The
/// methods are designed for use in application code to improve readability and reduce boilerplate. All methods are
/// static and can be called as extension methods on the appropriate types.</remarks>
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
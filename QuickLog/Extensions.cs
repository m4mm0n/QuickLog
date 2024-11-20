using System.ComponentModel;
using System.Text.RegularExpressions;

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
        var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
        var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var r = new Regex($"[{Regex.Escape(regexSearch)}]");
        return r.Replace(filename, "");
    }
}
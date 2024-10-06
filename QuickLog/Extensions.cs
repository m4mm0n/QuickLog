using System.ComponentModel;

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
}
using System.ComponentModel;

namespace QuickLog;

/// <summary>
/// Logging Type
/// </summary>
public enum LogType
{
    [Description("Trace")]
    Trace = 1 << 1,
    [Description("Debug")]
    Debug = 1 << 2,
    [Description("Information")]
    Info = 1 << 3,
    [Description("Warning")]
    Warn = 1 << 4,
    [Description("Error")]
    Error = 1 << 5,
    [Description("Critical Error")]
    Crit = 1 << 6,
    [Description("Exception Error")]
    Exception = 1 << 7
}
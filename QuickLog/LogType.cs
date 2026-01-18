/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : LogType.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2024-10-06 09:02:53 +02:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 1F5AA2F3
 *  
 *  Description    :
 *                   Specifies the type of log entry, which defines the severity or purpose of the log.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 1F5AA2F3

using System.ComponentModel;

namespace QuickLog;

/// <summary>
/// Specifies the type of log entry, which defines the severity or purpose of the log.
/// </summary>
public enum LogType
{
    /// <summary>
    /// A log entry used for tracing program execution, typically for detailed diagnostics.
    /// </summary>
    [Description("Trace")]
    Trace = 1 << 1,

    /// <summary>
    /// A log entry used for debugging information, often for development purposes.
    /// </summary>
    [Description("Debug")]
    Debug = 1 << 2,

    /// <summary>
    /// A log entry containing informational messages that highlight the progress of the application.
    /// </summary>
    [Description("Information")]
    Info = 1 << 3,

    /// <summary>
    /// A log entry that indicates a potential problem or important situation that requires attention.
    /// </summary>
    [Description("Warning")]
    Warn = 1 << 4,

    /// <summary>
    /// A log entry representing an error in the current application flow, usually non-critical.
    /// </summary>
    [Description("Error")]
    Error = 1 << 5,

    /// <summary>
    /// A log entry representing a critical error, often used to indicate system failures.
    /// </summary>
    [Description("Critical Error")]
    Crit = 1 << 6,

    /// <summary>
    /// A log entry representing an exception, typically used for unhandled or caught exceptions.
    /// </summary>
    [Description("Exception Error")]
    Exception = 1 << 7
}

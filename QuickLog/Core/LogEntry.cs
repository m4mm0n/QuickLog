/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : LogEntry.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:41:29 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 960990EF
 *  
 *  Description    :
 *                   Represents a single log entry containing details about a logging event, including timestamp, severity level,
 *                   message, and contextual information.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 960990EF

namespace QuickLog.Core;

/// <summary>
/// Represents a single log entry containing details about a logging event, including timestamp, severity level,
/// message, and contextual information.
/// </summary>
/// <remarks>This record struct is typically used to capture and transport detailed logging information for
/// diagnostics and monitoring. All string parameters except for 'Category' must not be null.</remarks>
/// <param name="Timestamp">The date and time when the log entry was created, in UTC.</param>
/// <param name="Level">The severity level of the log entry, indicating the importance or type of event.</param>
/// <param name="Message">The log message describing the event or condition being logged. Cannot be null.</param>
/// <param name="LoggerName">The name of the logger that generated the log entry. Cannot be null.</param>
/// <param name="Category">An optional category associated with the log entry, or null if not specified.</param>
/// <param name="MemberName">The name of the member (such as method or property) from which the log entry originated. Cannot be null.</param>
/// <param name="FilePath">The full file path of the source code file where the log entry was generated. Cannot be null.</param>
/// <param name="LineNumber">The line number in the source file where the log entry was created.</param>
/// <param name="ThreadId">The identifier of the thread on which the log entry was generated.</param>
/// <param name="ThreadRole">The role assigned to the thread generating the log entry.</param>
public readonly record struct LogEntry
(
    DateTime Timestamp,
    LogType Level,
    string Message,
    string LoggerName,
    string? Category,
    string MemberName,
    string FilePath,
    int LineNumber,
    int ThreadId,
    ThreadRole ThreadRole
);
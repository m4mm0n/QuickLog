/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : ILogSink.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:50:29 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : E4861007
 *  
 *  Description    :
 *                   Defines a contract for log sinks that receive and process log entries.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: E4861007

namespace QuickLog.Core;

/// <summary>
/// Defines a contract for log sinks that receive and process log entries.
/// </summary>
/// <remarks>Implementations of this interface are responsible for handling log entries, such as writing them to a
/// file, console, or external system. The interface supports flushing buffered data and releasing resources when no
/// longer needed.</remarks>
internal interface ILogSink : IDisposable
{
    void Write(in LogEntry entry);
    void Flush();
}
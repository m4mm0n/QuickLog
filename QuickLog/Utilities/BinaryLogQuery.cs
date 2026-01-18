/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogQuery.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:27:00 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 1770E95D
 *  
 *  Description    :
 *                   Provides filtered views over binary log files without loading them fully into memory.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 1770E95D

using QuickLog.Core;

namespace QuickLog.Utilities;

/// <summary>
/// Provides filtered views over binary log files without loading them fully into memory.
/// </summary>
public static class BinaryLogQuery
{
    /// <summary>
    /// Enumerates log entries within a given UTC time range.
    /// </summary>
    public static IEnumerable<LogEntry> Between(
        string path,
        DateTime utcFrom,
        DateTime utcTo,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => e.Timestamp >= utcFrom).TakeWhile(e => e.Timestamp <= utcTo);

    /// <summary>
    /// Enumerates log entries matching the given log level mask.
    /// </summary>
    public static IEnumerable<LogEntry> WithLevel(
        string path,
        LogType mask,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => (e.Level & mask) != 0);

    /// <summary>
    /// Enumerates log entries matching an arbitrary predicate.
    /// </summary>
    public static IEnumerable<LogEntry> Where(
        string path,
        Func<LogEntry, bool> predicate,
        bool stopOnCrcError = true) =>
        BinaryLogReader.Read(path, stopOnCrcError).Where(e => predicate(e));
}
/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : MemorySink.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:08:37 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 7E5B7459
 *  
 *  Description    :
 *                   Provides an in-memory log sink that writes log entries to a memory-based logger for fast, transient logging
 *                   operations.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 7E5B7459

using QuickLog.Core;
using QuickLog.Loggers;

namespace QuickLog.Sinks;

/// <summary>
/// Provides an in-memory log sink that writes log entries to a memory-based logger for fast, transient logging
/// operations.
/// </summary>
/// <remarks>This class is intended for scenarios where log data should be retained in memory rather than
/// persisted to disk or external systems. It is typically used for testing, diagnostics, or applications where
/// high-performance, temporary logging is required. The class is not thread-safe; concurrent access should be managed
/// externally if needed.</remarks>
internal sealed class MemorySink : ILogSink
{
    private readonly MemoryQuickLogger _logger;

    public MemorySink(MemoryQuickLogger logger)
    {
        _logger = logger;
    }

    public void Write(in LogEntry entry)
    {
        _logger.Log(
            entry.Level,
            entry.Message,
            entry.MemberName,
            entry.FilePath,
            entry.LineNumber);
    }

    public void Flush() { }

    public void Dispose() { }
}
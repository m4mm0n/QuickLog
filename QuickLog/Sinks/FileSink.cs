/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : FileSink.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:50:45 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : BCE4A392
 *  
 *  Description    :
 *                   Provides a log sink that writes log entries to a file in batches for improved performance.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: BCE4A392

using QuickLog.Core;
using System.Collections.Concurrent;

namespace QuickLog.Sinks;

/// <summary>
/// Provides a log sink that writes log entries to a file in batches for improved performance.
/// </summary>
/// <remarks>FileSink buffers log entries and writes them to the specified file in batches, reducing the number of
/// disk writes. The sink is not thread-safe; concurrent access should be externally synchronized if used from multiple
/// threads. The file is opened in append mode, and log entries are formatted with timestamp, log level, and message.
/// Call Dispose to ensure all buffered entries are flushed and resources are released.</remarks>
internal sealed class FileSink : ILogSink
{
    private readonly StreamWriter _writer;
    private readonly ConcurrentQueue<LogEntry> _queue = new();
    private readonly int _batchSize;

    public FileSink(string path, int batchSize)
    {
        _writer = new StreamWriter(path, append: true)
        {
            AutoFlush = false
        };
        _batchSize = Math.Max(1, batchSize);
    }

    public void Write(in LogEntry entry)
    {
        _queue.Enqueue(entry);
        if (_queue.Count >= _batchSize)
            Flush();
    }

    public void Flush()
    {
        while (_queue.TryDequeue(out var e))
            _writer.WriteLine(
                $"[{e.Timestamp:O}] [{e.Level}] {e.Message}");

        _writer.Flush();
    }

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }
}
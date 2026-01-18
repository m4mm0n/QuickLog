/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : AsyncLogDispatcher.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:50:29 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : B214B0C3
 *  
 *  Description    :
 *                   Provides asynchronous dispatching of log entries to one or more log sinks on a background thread.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: B214B0C3

using System.Collections.Concurrent;

namespace QuickLog.Core;

/// <summary>
/// Provides asynchronous dispatching of log entries to one or more log sinks on a background thread.
/// </summary>
/// <remarks>AsyncLogDispatcher is intended for internal use to decouple log entry production from log writing,
/// improving performance in multi-threaded scenarios. It ensures that log entries are processed in the order they are
/// enqueued and written to all configured sinks. This type is not thread-safe for modification of the sinks collection
/// after construction.</remarks>
internal sealed class AsyncLogDispatcher : IDisposable
{
    private readonly BlockingCollection<LogEntry> _queue =
        new(new ConcurrentQueue<LogEntry>(), 8192);

    private readonly List<ILogSink> _sinks;
    private readonly Thread _thread;

    /// <summary>
    /// Gets or sets the thread role that is protected from being preempted or interrupted.
    /// </summary>
    public ThreadRole ProtectedRole { get; set; } = ThreadRole.Audio;
    /// <summary>
    /// Gets or sets the policy to apply when the log queue is full.
    /// </summary>
    public AsyncDropPolicy DropPolicy { get; set; } = AsyncDropPolicy.DropBelowLevel;
    /// <summary>
    /// Gets or sets the minimum log level for entries to retain when using the DropBelowLevel policy.
    /// </summary>
    public LogType MinimumLevel { get; set; } = LogType.Warn;
    /// <summary>
    /// Gets the total number of log entries that have been dropped due to queue capacity limits.
    /// </summary>
    public long DroppedTotal;
    /// <summary>
    /// Gets the number of log entries dropped due to being below the minimum log level.
    /// </summary>
    public long DroppedByLevel;
    /// <summary>
    /// Gets the number of log entries dropped due to the thread role drop policy.
    /// </summary>
    public long DroppedByRole;
    /// <summary>
    /// Initializes a new instance of the AsyncLogDispatcher class that asynchronously dispatches log entries to the
    /// specified log sinks.
    /// </summary>
    /// <remarks>The dispatcher starts a background thread upon construction to process and forward log
    /// entries to the provided sinks. The sinks are used for the lifetime of the dispatcher; changes to the list after
    /// construction do not affect the dispatcher.</remarks>
    /// <param name="sinks">The collection of log sinks that will receive dispatched log entries. Cannot be null or contain null elements.</param>
    public AsyncLogDispatcher(List<ILogSink> sinks)
    {
        _sinks = sinks;

        _thread = new Thread(Consume)
        {
            IsBackground = true,
            Name = "QuickLog.AsyncDispatcher"
        };
        _thread.Start();
    }
    /// <summary>
    /// Retrieves statistics on the number of items dropped, grouped by total, by level, and by role.
    /// </summary>
    /// <returns>A tuple containing the total number of dropped items, the number dropped by level, and the number dropped by
    /// role.</returns>
    public (long total, long byLevel, long byRole) GetDropStats()
        => (DroppedTotal, DroppedByLevel, DroppedByRole);

    /// <summary>
    /// Attempts to enqueue a log entry for asynchronous processing, applying the configured drop policy if the queue is
    /// full.
    /// </summary>
    /// <remarks>If the queue is full, the behavior depends on the current <see cref="DropPolicy"/>: the
    /// method may drop the new entry, remove the oldest entry to make space, or drop entries below a minimum log level.
    /// Entries below <see cref="MinimumLevel"/> may be dropped if the drop policy is set accordingly. This method does
    /// not block and does not guarantee that the entry will be enqueued if the queue is at capacity.</remarks>
    /// <param name="entry">The log entry to enqueue. The entry is passed by reference for performance; its contents are not modified.</param>
    public void Enqueue(in LogEntry entry)
    {
        // Fast path
        if (_queue.TryAdd(entry))
            return;

        switch (DropPolicy)
        {
            case AsyncDropPolicy.DropNewest:
                Interlocked.Increment(ref DroppedTotal);
                return;

            case AsyncDropPolicy.DropOldest:
                if (_queue.TryTake(out _))
                    _queue.TryAdd(entry);
                return;

            case AsyncDropPolicy.DropBelowLevel:
                if (entry.Level < MinimumLevel)
                {
                    Interlocked.Increment(ref DroppedTotal);
                    Interlocked.Increment(ref DroppedByLevel);
                    return;
                }

                TrimBelowLevel();
                _queue.TryAdd(entry);
                return;

            case AsyncDropPolicy.DropByThreadRole:
                if (entry.ThreadRole != ProtectedRole)
                {
                    Interlocked.Increment(ref DroppedTotal);
                    Interlocked.Increment(ref DroppedByRole);
                    return;
                }
                if (entry.ThreadRole == ProtectedRole)
                {
                    // try to free space by dropping other roles
                    TrimByThreadRole();
                    _queue.TryAdd(entry);
                }
                return;
        }
    }

    private void TrimBelowLevel()
    {
        var count = _queue.Count;

        for (var i = 0; i < count; i++)
        {
            if (!_queue.TryTake(out var e))
                return;

            if (e.Level >= MinimumLevel)
                _queue.TryAdd(e);
        }
    }

    private void TrimByThreadRole()
    {
        var count = _queue.Count;

        for (var i = 0; i < count; i++)
        {
            if (!_queue.TryTake(out var e))
                return;

            if (e.ThreadRole == ProtectedRole)
                _queue.TryAdd(e);
        }
    }

    private void Consume()
    {
        foreach (var entry in _queue.GetConsumingEnumerable())
        {
            foreach (var sink in _sinks)
                sink.Write(entry);
        }
    }
    /// <summary>
    /// Flushes all pending log entries and ensures that all log sinks have processed their queued data before
    /// returning.
    /// </summary>
    /// <remarks>This method blocks the calling thread until all queued log entries have been processed and
    /// flushed to their respective sinks. It is typically used to ensure that all logs are written before application
    /// shutdown or when immediate log persistence is required. This method is not thread-safe; concurrent calls may
    /// result in undefined behavior.</remarks>
    public void Flush()
    {
        while (_queue.Count > 0)
            Thread.Sleep(1);

        foreach (var sink in _sinks)
            sink.Flush();
    }
    /// <summary>
    /// Releases all resources used by the current instance of the class.
    /// </summary>
    /// <remarks>Call this method when you are finished using the instance to ensure that all associated
    /// resources are properly released. After calling this method, the instance should not be used.</remarks>
    public void Dispose()
    {
        _queue.CompleteAdding();
        _thread.Join();

        foreach (var sink in _sinks)
            sink.Dispose();
    }
}
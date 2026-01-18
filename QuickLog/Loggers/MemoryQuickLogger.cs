/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : MemoryQuickLogger.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 05:48:19 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 30FD5AEA
 *  
 *  Description    :
 *                   Provides an in-memory implementation of the IQuickLog interface that stores recent log entries up to a specified
 *                   capacity.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 30FD5AEA

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace QuickLog.Loggers;

/// <summary>
/// Provides an in-memory implementation of the IQuickLog interface that stores recent log entries up to a specified
/// capacity.
/// </summary>
/// <remarks>MemoryQuickLogger is suitable for scenarios where quick, temporary access to recent log entries is
/// required, such as debugging or diagnostics. Log entries are stored in a thread-safe queue and older entries are
/// automatically discarded when the capacity is exceeded. This logger does not persist logs beyond the application's
/// lifetime.</remarks>
public sealed class MemoryQuickLogger : IQuickLog
{
    private readonly ConcurrentQueue<LogEventArgs> _entries = new();
    private readonly int _capacity;
    private bool _disposed;

    public event EventHandler<LogEventArgs>? LogEvent;

    public MemoryQuickLogger(int capacity = 1024)
    {
        _capacity = Math.Max(1, capacity);
    }

    /// <summary>
    /// Returns a snapshot of all currently buffered log entries.
    /// </summary>
    public IReadOnlyList<LogEventArgs> Snapshot()
        => _entries.ToArray();

    private void Add(LogEventArgs args)
    {
        if (_disposed)
            return;

        _entries.Enqueue(args);

        while (_entries.Count > _capacity)
            _entries.TryDequeue(out _);

        LogEvent?.Invoke(this, args);
    }

    // ------------------------------------------------------------------
    // IQuickLog IMPLEMENTATION (EXACT SIGNATURES)
    // ------------------------------------------------------------------

    public void Log(
        LogType logType,
        string message,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Add(new LogEventArgs(
            logType,
            message,
            callerName,
            callerFilePath,
            callerLineNumber));
    }

    public void Log(
        LogType logType,
        Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Add(new LogEventArgs(
            logType,
            exception,
            callerName,
            callerFilePath,
            callerLineNumber));
    }

    public void Log(
        LogType logType,
        string message,
        Exception exception,
        [CallerMemberName] string callerName = "",
        [CallerFilePath] string callerFilePath = "",
        [CallerLineNumber] int callerLineNumber = 0)
    {
        Add(new LogEventArgs(
            logType,
            message,
            exception,
            callerName,
            callerFilePath,
            callerLineNumber));
    }

    // ------------------------------------------------------------------
    // IDisposable
    // ------------------------------------------------------------------

    public void Dispose()
    {
        _disposed = true;
        _entries.Clear();
    }
}
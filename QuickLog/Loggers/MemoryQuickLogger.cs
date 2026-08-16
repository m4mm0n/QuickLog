using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using QuickLog.Core;

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

    /// <summary>
    /// Raised whenever an entry is stored in the memory buffer.
    /// </summary>
    public event EventHandler<LogEventArgs>? LogEvent;

    /// <summary>
    /// Initializes a new memory logger with a bounded entry buffer.
    /// </summary>
    /// <param name="capacity">Maximum number of recent entries to retain.</param>
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

    internal void Log(in LogEntry entry)
    {
        Add(new LogEventArgs(
            entry.Level,
            entry.Message,
            null,
            entry.MemberName,
            entry.FilePath,
            entry.LineNumber,
            entry.Category,
            entry.CorrelationId,
            entry.TraceId,
            entry.SpanId,
            entry.EventId,
            entry.Properties));
    }

    // ------------------------------------------------------------------
    // IQuickLog IMPLEMENTATION (EXACT SIGNATURES)
    // ------------------------------------------------------------------

    /// <summary>
    /// Stores a message entry in the in-memory buffer.
    /// </summary>
    /// <param name="logType">Severity of the entry.</param>
    /// <param name="message">Message text.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
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

    /// <summary>
    /// Stores an exception entry in the in-memory buffer.
    /// </summary>
    /// <param name="logType">Severity of the entry.</param>
    /// <param name="exception">Exception to store.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
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

    /// <summary>
    /// Stores a message and exception entry in the in-memory buffer.
    /// </summary>
    /// <param name="logType">Severity of the entry.</param>
    /// <param name="message">Message text.</param>
    /// <param name="exception">Exception to store.</param>
    /// <param name="callerName">Compiler-provided caller name.</param>
    /// <param name="callerFilePath">Compiler-provided caller file path.</param>
    /// <param name="callerLineNumber">Compiler-provided caller line number.</param>
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

    /// <summary>
    /// Clears the buffer and prevents additional entries from being stored.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        _entries.Clear();
    }
}

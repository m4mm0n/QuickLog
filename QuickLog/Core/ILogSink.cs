namespace QuickLog.Core;

/// <summary>
/// Defines a contract for log sinks that receive and process log entries.
/// </summary>
/// <remarks>Implementations of this interface are responsible for handling log entries, such as writing them to a
/// file, console, or external system. The interface supports flushing buffered data and releasing resources when no
/// longer needed.</remarks>
internal interface ILogSink : IDisposable, IAsyncDisposable
{
    void Write(in LogEntry entry);
    void Flush();

    ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Flush();
        return ValueTask.CompletedTask;
    }

    ValueTask IAsyncDisposable.DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

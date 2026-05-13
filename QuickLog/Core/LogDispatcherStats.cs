namespace QuickLog.Core;

/// <summary>
/// Snapshot of async dispatcher health counters.
/// </summary>
public readonly record struct LogDispatcherStats(
    int QueueCapacity,
    int QueueCount,
    long Enqueued,
    long Written,
    long DroppedTotal,
    long DroppedByLevel,
    long DroppedByRole,
    long SinkFailures,
    string? LastSinkError);

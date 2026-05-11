using System.Diagnostics;
using QuickLog.Core;

namespace QuickLog.Sinks;

/// <summary>
/// A log sink that routes entries to <see cref="System.Diagnostics.Trace"/>, respecting
/// the entry's severity level so that <c>TraceError</c>, <c>TraceWarning</c>, and
/// <c>TraceInformation</c> are used appropriately.
/// </summary>
internal sealed class TraceSink : ILogSink
{
    public void Write(in LogEntry entry)
    {
        var line = $"[{entry.Timestamp:O}] [{entry.Level}] [{entry.MemberName}] {entry.Message}";

        if (entry.Level >= LogType.Error)
            Trace.TraceError(line);
        else if (entry.Level >= LogType.Warn)
            Trace.TraceWarning(line);
        else
            Trace.TraceInformation(line);
    }

    public void Flush() => Trace.Flush();

    public void Dispose() => Trace.Flush();
}

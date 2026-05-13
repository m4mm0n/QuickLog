namespace QuickLog.Core;

internal sealed class LogSpamController
{
    private readonly LogSpamControlOptions _options;
    private LogEntry? _last;
    private int _repeatCount;

    public LogSpamController(LogSpamControlOptions options)
    {
        _options = options;
    }

    public IReadOnlyList<LogEntry> Process(in LogEntry entry)
    {
        if (!_options.Enabled)
            return [entry];

        if (_last is null)
        {
            _last = entry;
            return [entry];
        }

        var last = _last.Value;
        if (last.Level == entry.Level && string.Equals(last.Message, entry.Message, StringComparison.Ordinal))
        {
            _repeatCount++;
            return [];
        }

        var pending = FlushPending();
        _last = entry;
        _repeatCount = 0;

        if (pending.Count == 0)
            return [entry];

        var result = new List<LogEntry>(pending.Count + 1);
        result.AddRange(pending);
        result.Add(entry);
        return result;
    }

    public IReadOnlyList<LogEntry> Flush()
    {
        var pending = FlushPending();
        _last = null;
        _repeatCount = 0;
        return pending;
    }

    private IReadOnlyList<LogEntry> FlushPending()
    {
        if (_last is null || _repeatCount <= 0)
            return [];

        var last = _last.Value;
        var threshold = Math.Max(2, _options.DuplicateThreshold);
        if (_repeatCount + 1 >= threshold)
        {
            return
            [
                last with
                {
                    Timestamp = DateTime.UtcNow,
                    Message = $"Previous message repeated {_repeatCount} times: {last.Message}"
                }
            ];
        }

        var replay = new List<LogEntry>(_repeatCount);
        for (var i = 0; i < _repeatCount; i++)
            replay.Add(last);
        return replay;
    }
}

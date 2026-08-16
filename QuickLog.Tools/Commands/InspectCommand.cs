using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Reads and filters entries from a binary QuickLog file.</summary>
public static class InspectCommand
{
    /// <summary>Executes a QLOG inspection query and writes matching entries.</summary>
    /// <param name="command">The input path, filters, and result limit.</param>
    /// <param name="console">The destination for matching entries and errors.</param>
    /// <param name="cancellationToken">A token that cancels inspection.</param>
    /// <returns>A task containing the command result.</returns>
    public static Task<CommandResult> ExecuteAsync(
        InspectToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        var entries = BinaryLogReader.Read(command.Path, stopOnCrcError: true);

        if (!string.IsNullOrWhiteSpace(command.Level))
        {
            if (!Enum.TryParse<LogType>(command.Level, ignoreCase: true, out var level))
            {
                console.ErrorLine($"Unknown log level: {command.Level}");
                return Task.FromResult(CommandResult.Fail());
            }

            entries = entries.Where(e => (e.Level & level) != 0);
        }

        if (!string.IsNullOrWhiteSpace(command.Contains))
            entries = entries.Where(e => e.Message.Contains(command.Contains, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(command.Correlation))
            entries = entries.Where(e => string.Equals(e.CorrelationId, command.Correlation, StringComparison.Ordinal));

        if (!string.IsNullOrWhiteSpace(command.Event))
        {
            entries = int.TryParse(command.Event, out var eventId)
                ? entries.Where(entry => entry.EventId.Id == eventId)
                : entries.Where(entry => string.Equals(entry.EventId.Name, command.Event, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(command.Property))
            entries = entries.Where(entry => ToolLogUtilities.MatchesProperty(entry, command.Property));

        if (command.From is not null)
            entries = entries.Where(e => e.Timestamp >= command.From.Value);

        if (command.To is not null)
            entries = entries.Where(e => e.Timestamp <= command.To.Value);

        if (command.Limit is not null)
            entries = entries.Take(Math.Max(0, command.Limit.Value));

        var list = entries.ToList();
        cancellationToken.ThrowIfCancellationRequested();

        console.WriteLine($"Entries: {list.Count}");

        foreach (var group in list.GroupBy(e => e.Level).OrderBy(g => g.Key.ToString()))
            console.WriteLine($"{group.Key}: {group.Count()}");

        foreach (var group in list.Where(e => !string.IsNullOrWhiteSpace(e.Category))
                     .GroupBy(e => e.Category!)
                     .OrderByDescending(g => g.Count())
                     .Take(10))
            console.WriteLine($"Scope {group.Key}: {group.Count()}");

        foreach (var group in list.Where(e => !string.IsNullOrWhiteSpace(e.CorrelationId))
                     .GroupBy(e => e.CorrelationId!)
                     .OrderByDescending(g => g.Count())
                     .Take(10))
            console.WriteLine($"Correlation {group.Key}: {group.Count()}");

        foreach (var group in list.Where(entry => entry.EventId != LogEventId.None)
                     .GroupBy(entry => entry.EventId)
                     .OrderByDescending(group => group.Count())
                     .Take(10))
            console.WriteLine($"Event {group.Key}: {group.Count()}");

        foreach (var entry in list.Take(10))
        {
            var eventText = entry.EventId == LogEventId.None ? string.Empty : $" [{entry.EventId}]";
            var properties = LogProperties.Format(entry.Properties);
            if (properties.Length > 0)
                properties = $" {properties}";
            console.WriteLine($"{entry.Timestamp:O} [{entry.Level}]{eventText} {entry.Message}{properties}");
        }

        return Task.FromResult(CommandResult.Ok());
    }
}

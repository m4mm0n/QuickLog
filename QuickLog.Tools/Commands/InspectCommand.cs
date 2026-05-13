using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

public static class InspectCommand
{
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

        foreach (var entry in list.Take(10))
            console.WriteLine($"{entry.Timestamp:O} [{entry.Level}] {entry.Message}");

        return Task.FromResult(CommandResult.Ok());
    }
}

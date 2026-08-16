using System.Text.Json;
using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Replays binary log entries to the console, text, or JSON Lines.</summary>
public static class ReplayCommand
{
    /// <summary>Reads a QLOG file and writes its entries to the configured destination.</summary>
    /// <param name="command">The input path, output format, and optional output path.</param>
    /// <param name="console">The destination for console replay and status.</param>
    /// <param name="cancellationToken">A token that cancels replay.</param>
    /// <returns>A task containing the command result.</returns>
    public static async Task<CommandResult> ExecuteAsync(
        ReplayToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return CommandResult.Fail();
        }

        switch (command.To)
        {
            case "console":
                foreach (var entry in BinaryLogReader.Read(command.Path))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    console.WriteLine($"{entry.Timestamp:O} [{entry.Level}] [{entry.EventId}] {entry.Message} {LogProperties.Format(entry.Properties)}".TrimEnd());
                }

                return CommandResult.Ok();

            case "text":
                if (string.IsNullOrWhiteSpace(command.Out))
                {
                    console.ErrorLine("replay --to text requires --out <file>.");
                    return CommandResult.Fail();
                }

                EnsureParent(command.Out);
                BinaryLogExporter.ExportToText(command.Path, command.Out);
                console.WriteLine($"Wrote {command.Out}");
                return CommandResult.Ok();

            case "jsonl":
                if (string.IsNullOrWhiteSpace(command.Out))
                {
                    console.ErrorLine("replay --to jsonl requires --out <file>.");
                    return CommandResult.Fail();
                }

                EnsureParent(command.Out);
                await using (var stream = File.Create(command.Out))
                await using (var writer = new StreamWriter(stream))
                {
                    foreach (var entry in BinaryLogReader.Read(command.Path))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await writer.WriteLineAsync(JsonSerializer.Serialize(ToJson(entry)));
                    }
                }

                console.WriteLine($"Wrote {command.Out}");
                return CommandResult.Ok();

            default:
                console.ErrorLine($"Unsupported replay target: {command.To}");
                return CommandResult.Fail();
        }
    }

    private static object ToJson(LogEntry entry) => new
    {
        timestamp = entry.Timestamp.ToString("O"),
        level = entry.Level.ToString(),
        message = entry.Message,
        scope = string.IsNullOrWhiteSpace(entry.Category) ? null : entry.Category,
        correlation = string.IsNullOrWhiteSpace(entry.CorrelationId) ? null : entry.CorrelationId,
        trace = string.IsNullOrWhiteSpace(entry.TraceId) ? null : entry.TraceId,
        span = string.IsNullOrWhiteSpace(entry.SpanId) ? null : entry.SpanId,
        eventId = entry.EventId == LogEventId.None ? (int?)null : entry.EventId.Id,
        eventName = entry.EventId.Name,
        properties = entry.Properties is { Count: > 0 } ? entry.Properties : null
    };

    private static void EnsureParent(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
    }
}

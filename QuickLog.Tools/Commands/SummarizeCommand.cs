using System.Text.Json;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the summarize command for writing JSON log summaries.</summary>
public static class SummarizeCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>Executes the summarize command.</summary>
    public static async Task<CommandResult> ExecuteAsync(
        SummarizeToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return CommandResult.Fail();
        }

        var summary = BinaryLogSummary.FromEntries(ToolLogUtilities.ReadEntries(command.Path));
        Directory.CreateDirectory(Path.GetDirectoryName(command.Out) ?? ".");
        await File.WriteAllTextAsync(command.Out, JsonSerializer.Serialize(summary, JsonOptions), cancellationToken).ConfigureAwait(false);
        console.WriteLine($"Wrote {command.Out}");
        return CommandResult.Ok();
    }
}

using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the stats command for compact log summaries.</summary>
public static class StatsCommand
{
    /// <summary>Executes the stats command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        StatsToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        cancellationToken.ThrowIfCancellationRequested();
        var summary = BinaryLogSummary.FromEntries(ToolLogUtilities.ReadEntries(command.Path));
        ToolLogUtilities.WriteSummary(summary, console);
        return Task.FromResult(CommandResult.Ok());
    }
}

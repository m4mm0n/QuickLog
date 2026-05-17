using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the timeline command with interactive and non-interactive modes.</summary>
public static class TimelineCommand
{
    /// <summary>Executes the timeline command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        TimelineToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        if (console is ConsoleToolConsole && !Console.IsInputRedirected && !Console.IsOutputRedirected)
        {
            BinaryLogTimelineViewer.Run(command.Path);
            return Task.FromResult(CommandResult.Ok());
        }

        cancellationToken.ThrowIfCancellationRequested();
        ToolLogUtilities.WriteSummary(BinaryLogSummary.FromEntries(ToolLogUtilities.ReadEntries(command.Path)), console);
        return Task.FromResult(CommandResult.Ok());
    }
}

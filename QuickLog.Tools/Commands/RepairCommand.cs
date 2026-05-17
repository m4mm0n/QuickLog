using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the repair command for salvaging valid QLOG records.</summary>
public static class RepairCommand
{
    /// <summary>Executes the repair command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        RepairToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        cancellationToken.ThrowIfCancellationRequested();
        var result = BinaryLogRepair.Repair(command.Path, command.Out);
        console.WriteLine($"Recovered: {result.RecoveredEntries}");
        console.WriteLine($"Skipped bytes: {result.SkippedBytes}");
        console.WriteLine($"Wrote {result.OutputPath}");
        return Task.FromResult(CommandResult.Ok());
    }
}

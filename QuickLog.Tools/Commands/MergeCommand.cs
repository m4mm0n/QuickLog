using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the merge command for combining multiple QLOG files.</summary>
public static class MergeCommand
{
    /// <summary>Executes the merge command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        MergeToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var missing = command.Inputs.FirstOrDefault(path => !File.Exists(path));
        if (missing is not null)
        {
            console.ErrorLine($"File not found: {missing}");
            return Task.FromResult(CommandResult.Fail());
        }

        cancellationToken.ThrowIfCancellationRequested();
        BinaryLogMerge.Merge(command.Inputs, command.Out);
        var count = BinaryLogReader.Read(command.Out).Count();
        console.WriteLine($"Merged: {count}");
        console.WriteLine($"Wrote {command.Out}");
        return Task.FromResult(CommandResult.Ok());
    }
}

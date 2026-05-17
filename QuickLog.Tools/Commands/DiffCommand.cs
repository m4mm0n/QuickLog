namespace QuickLog.Tools.Commands;

/// <summary>Implements the diff command for comparing normalized log entries.</summary>
public static class DiffCommand
{
    /// <summary>Executes the diff command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        DiffToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Left) || !File.Exists(command.Right))
        {
            console.ErrorLine("diff requires two existing files.");
            return Task.FromResult(CommandResult.Fail());
        }

        var left = ToolLogUtilities.ReadEntries(command.Left)
            .Select(e => (e.Level, e.Message))
            .ToHashSet();
        var right = ToolLogUtilities.ReadEntries(command.Right)
            .Where(e => !left.Contains((e.Level, e.Message)))
            .ToList();

        cancellationToken.ThrowIfCancellationRequested();
        foreach (var entry in right)
            console.WriteLine($"+ [{entry.Level}] {entry.Message}");

        console.WriteLine($"Only in right: {right.Count}");
        return Task.FromResult(CommandResult.Ok());
    }
}

namespace QuickLog.Tools.Commands;

/// <summary>Implements the tail command for printing the end of a text log.</summary>
public static class TailCommand
{
    /// <summary>Executes the tail command.</summary>
    public static async Task<CommandResult> ExecuteAsync(
        TailToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return CommandResult.Fail();
        }

        var lines = ToolLogUtilities.ReadTextLines(command.Path).TakeLast(Math.Max(0, command.Lines)).ToList();
        foreach (var line in lines)
            console.WriteLine(line);

        if (!command.Follow)
            return CommandResult.Ok();

        var position = new FileInfo(command.Path).Length;
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            using var stream = new FileStream(command.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length <= position)
                continue;

            stream.Position = position;
            using var reader = new StreamReader(stream, leaveOpen: true);
            while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
                console.WriteLine(line);
            position = stream.Position;
        }

        return CommandResult.Ok();
    }
}

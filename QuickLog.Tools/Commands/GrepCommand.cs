namespace QuickLog.Tools.Commands;

/// <summary>Implements the grep command for searching text and QLOG files.</summary>
public static class GrepCommand
{
    /// <summary>Executes the grep command.</summary>
    public static Task<CommandResult> ExecuteAsync(
        GrepToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var files = ToolLogUtilities.EnumerateLogFiles(command.Path, command.Recursive).ToList();
        if (files.Count == 0)
        {
            console.ErrorLine($"No log files found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        var matches = 0;
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ToolLogUtilities.IsBinaryLog(file))
                matches += GrepBinary(file, command.Pattern, console);
            else
                matches += GrepText(file, command.Pattern, console);
        }

        return Task.FromResult(matches == 0 ? CommandResult.Fail() : CommandResult.Ok());
    }

    /// <summary>Searches binary QLOG entries and writes matching messages.</summary>
    private static int GrepBinary(string path, string pattern, IToolConsole console)
    {
        var matches = 0;
        foreach (var entry in ToolLogUtilities.ReadEntries(path))
        {
            if (!entry.Message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                continue;

            matches++;
            console.WriteLine($"{Path.GetFileName(path)}:{entry.Timestamp:O} [{entry.Level}] {entry.Message}");
        }

        return matches;
    }

    /// <summary>Searches text lines and writes matching line numbers.</summary>
    private static int GrepText(string path, string pattern, IToolConsole console)
    {
        var matches = 0;
        var lineNumber = 0;
        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;
            if (!line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                continue;

            matches++;
            console.WriteLine($"{Path.GetFileName(path)}:{lineNumber}: {line}");
        }

        return matches;
    }
}

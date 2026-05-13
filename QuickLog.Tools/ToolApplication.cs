namespace QuickLog.Tools;

public static class ToolApplication
{
    public static Task<int> RunAsync(string[] args, IToolConsole console, CancellationToken cancellationToken = default)
    {
        var parsed = ToolCommandParser.Parse(args);
        if (!parsed.Success)
        {
            console.ErrorLine(parsed.Error ?? "Invalid command.");
            return Task.FromResult(2);
        }

        console.WriteLine(parsed.Command?.ToString() ?? "No command.");
        return Task.FromResult(0);
    }
}

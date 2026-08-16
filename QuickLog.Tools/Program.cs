namespace QuickLog.Tools;

/// <summary>Provides the command-line entry point for the QuickLog diagnostics tool.</summary>
public static class Program
{
    /// <summary>Parses and executes a QuickLog tool command.</summary>
    /// <param name="args">The process command-line arguments.</param>
    /// <returns>A task that resolves to the process exit code.</returns>
    public static Task<int> Main(string[] args)
        => ToolApplication.RunAsync(args, new ConsoleToolConsole());
}

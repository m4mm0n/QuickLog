namespace QuickLog.Tools;

public static class Program
{
    public static Task<int> Main(string[] args)
        => ToolApplication.RunAsync(args, new ConsoleToolConsole());
}

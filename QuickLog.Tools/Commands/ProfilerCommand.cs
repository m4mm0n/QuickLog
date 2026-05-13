namespace QuickLog.Tools.Commands;

public static class ProfilerCommand
{
    public static Task<CommandResult> ExecuteAsync(
        ProfilerExplainToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        console.WriteLine("QuickLog profiler support is experimental.");
        console.WriteLine("This command group does not ship or inject a native profiler DLL.");
        console.WriteLine("Use it to understand or print the environment block needed by an external CLR profiler.");
        return Task.FromResult(CommandResult.Ok());
    }

    public static Task<CommandResult> ExecuteAsync(
        ProfilerEnvToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var clsid = $"{{{command.Clsid:D}}}";
        console.WriteLine("CORECLR_ENABLE_PROFILING=1");
        console.WriteLine($"CORECLR_PROFILER={clsid}");
        console.WriteLine($"CORECLR_PROFILER_PATH={command.Path}");
        console.WriteLine("COR_ENABLE_PROFILING=1");
        console.WriteLine($"COR_PROFILER={clsid}");
        console.WriteLine($"COR_PROFILER_PATH={command.Path}");
        return Task.FromResult(CommandResult.Ok());
    }
}

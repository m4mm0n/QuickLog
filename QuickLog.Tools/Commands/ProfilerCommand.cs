namespace QuickLog.Tools.Commands;

/// <summary>Explains and emits configuration for optional external CLR profilers.</summary>
public static class ProfilerCommand
{
    /// <summary>Writes an explanation of the external profiler integration boundary.</summary>
    /// <param name="command">The profiler explanation command.</param>
    /// <param name="console">The destination for the explanation.</param>
    /// <param name="cancellationToken">A token reserved for a consistent command contract.</param>
    /// <returns>A task containing the command result.</returns>
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

    /// <summary>Writes the environment block required to activate an external profiler.</summary>
    /// <param name="command">The profiler identifier and library path.</param>
    /// <param name="console">The destination for environment variables.</param>
    /// <param name="cancellationToken">A token reserved for a consistent command contract.</param>
    /// <returns>A task containing the command result.</returns>
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

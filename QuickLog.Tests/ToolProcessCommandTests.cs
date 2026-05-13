using System.Diagnostics;
using QuickLog.Tools;
using QuickLog.Tools.Commands;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolProcessCommandTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"ql_tools_process_{Guid.NewGuid():N}");

    public ToolProcessCommandTests()
    {
        Directory.CreateDirectory(_dir);
    }

    [Fact]
    public async Task Observe_CurrentProcess_WritesSessionArtifacts()
    {
        var output = Path.Combine(_dir, "observe");
        var console = new BufferToolConsole();

        var result = await ObserveCommand.ExecuteAsync(
            new ObserveToolCommand(Process.GetCurrentProcess().Id, DurationSeconds: 0, output),
            console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("Process:", console.OutputText);
        Assert.True(File.Exists(Path.Combine(output, "session.qlog")));
        Assert.True(File.Exists(Path.Combine(output, "session.jsonl")));
    }

    [Fact]
    public async Task Launch_DotnetInfo_CapturesStdoutAndExitCode()
    {
        var output = Path.Combine(_dir, "launch");
        var console = new BufferToolConsole();

        var result = await LaunchCommand.ExecuteAsync(
            new LaunchToolCommand("dotnet", ["--info"], output, "dotnet-info", DiagnosticEnv: true, WaitForExit: true),
            console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("ExitCode: 0", console.OutputText);
        Assert.True(File.Exists(Path.Combine(output, "session.qlog")));
        Assert.True(File.Exists(Path.Combine(output, "session.jsonl")));
    }

    [Fact]
    public async Task ProfilerExplain_MarksProfilerSupportExperimental()
    {
        var console = new BufferToolConsole();

        var result = await ProfilerCommand.ExecuteAsync(new ProfilerExplainToolCommand(), console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("experimental", console.OutputText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("native profiler DLL", console.OutputText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfilerEnv_PrintsClrProfilerEnvironmentVariables()
    {
        var clsid = Guid.NewGuid();
        var console = new BufferToolConsole();

        var result = await ProfilerCommand.ExecuteAsync(new ProfilerEnvToolCommand(clsid, "Profiler.dll"), console);

        Assert.True(result.Success, console.ErrorText);
        Assert.Contains("CORECLR_ENABLE_PROFILING=1", console.OutputText);
        Assert.Contains($"CORECLR_PROFILER={{{clsid:D}}}", console.OutputText);
        Assert.Contains("CORECLR_PROFILER_PATH=Profiler.dll", console.OutputText);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

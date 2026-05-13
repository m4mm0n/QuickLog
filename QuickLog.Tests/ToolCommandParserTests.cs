using QuickLog.Tools;
using Xunit;

namespace QuickLog.Tests;

public sealed class ToolCommandParserTests
{
    [Fact]
    public void Parse_DoctorCommand_ReturnsDoctorOptions()
    {
        var result = ToolCommandParser.Parse(["doctor", "logs", "--recursive"]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<DoctorToolCommand>(result.Command);
        Assert.Equal("logs", command.Path);
        Assert.True(command.Recursive);
    }

    [Fact]
    public void Parse_InspectCommand_ReturnsFilters()
    {
        var result = ToolCommandParser.Parse([
            "inspect", "app.qlog",
            "--level", "Error",
            "--contains", "needle",
            "--correlation", "corr-1",
            "--limit", "20"
        ]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<InspectToolCommand>(result.Command);
        Assert.Equal("app.qlog", command.Path);
        Assert.Equal("Error", command.Level);
        Assert.Equal("needle", command.Contains);
        Assert.Equal("corr-1", command.Correlation);
        Assert.Equal(20, command.Limit);
    }

    [Fact]
    public void Parse_ReplayCommand_ReturnsTargetFormat()
    {
        var result = ToolCommandParser.Parse(["replay", "app.qlog", "--to", "jsonl", "--out", "app.jsonl"]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<ReplayToolCommand>(result.Command);
        Assert.Equal("app.qlog", command.Path);
        Assert.Equal("jsonl", command.To);
        Assert.Equal("app.jsonl", command.Out);
    }

    [Fact]
    public void Parse_BenchmarkCommand_ReturnsModeAndIterations()
    {
        var result = ToolCommandParser.Parse(["benchmark", "--iterations", "12", "--mode", "binary"]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<BenchmarkToolCommand>(result.Command);
        Assert.Equal(12, command.Iterations);
        Assert.Equal("binary", command.Mode);
    }

    [Fact]
    public void Parse_BundleCommand_ReturnsBundleOptions()
    {
        var result = ToolCommandParser.Parse([
            "bundle", "--out", "support.zip", "--logs", "logs", "--crashes", "crashes",
            "--include-env", "--include-exports", "--max-file-bytes", "2048", "--redact"
        ]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<BundleToolCommand>(result.Command);
        Assert.Equal("support.zip", command.Out);
        Assert.Equal("logs", command.Logs);
        Assert.Equal("crashes", command.Crashes);
        Assert.True(command.IncludeEnv);
        Assert.True(command.IncludeExports);
        Assert.Equal(2048, command.MaxFileBytes);
        Assert.True(command.Redact);
    }

    [Fact]
    public void Parse_LaunchCommand_ReturnsAppAndArguments()
    {
        var result = ToolCommandParser.Parse([
            "launch", "--out", "session", "--name", "smoke", "--diagnostic-env", "--wait-for-exit",
            "--", "dotnet", "--info"
        ]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<LaunchToolCommand>(result.Command);
        Assert.Equal("session", command.Out);
        Assert.Equal("smoke", command.Name);
        Assert.True(command.DiagnosticEnv);
        Assert.True(command.WaitForExit);
        Assert.Equal("dotnet", command.App);
        Assert.Equal(["--info"], command.AppArgs);
    }

    [Fact]
    public void Parse_ObserveCommand_ReturnsPidDurationAndOutput()
    {
        var result = ToolCommandParser.Parse(["observe", "--pid", "1234", "--duration", "5", "--out", "session"]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<ObserveToolCommand>(result.Command);
        Assert.Equal(1234, command.Pid);
        Assert.Equal(5, command.DurationSeconds);
        Assert.Equal("session", command.Out);
    }

    [Fact]
    public void Parse_ProfilerExplain_ReturnsExplainCommand()
    {
        var result = ToolCommandParser.Parse(["profiler", "explain"]);

        Assert.True(result.Success, result.Error);
        Assert.IsType<ProfilerExplainToolCommand>(result.Command);
    }

    [Fact]
    public void Parse_ProfilerEnv_ReturnsProfilerEnvironmentCommand()
    {
        var clsid = Guid.NewGuid().ToString("D");
        var result = ToolCommandParser.Parse(["profiler", "env", "--clsid", clsid, "--path", "Profiler.dll"]);

        Assert.True(result.Success, result.Error);
        var command = Assert.IsType<ProfilerEnvToolCommand>(result.Command);
        Assert.Equal(Guid.Parse(clsid), command.Clsid);
        Assert.Equal("Profiler.dll", command.Path);
    }

    [Fact]
    public void Parse_InvalidInput_ReturnsParseError()
    {
        var result = ToolCommandParser.Parse(["launch", "--out", "session"]);

        Assert.False(result.Success);
        Assert.Null(result.Command);
        Assert.Contains("requires --", result.Error);
    }
}

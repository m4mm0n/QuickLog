using System.ComponentModel;
using System.Diagnostics;
using QuickLog.Core;
using QuickLog.Tools.Diagnostics;

namespace QuickLog.Tools.Commands;

public static class LaunchCommand
{
    public static async Task<CommandResult> ExecuteAsync(
        LaunchToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        using var session = new ToolSessionLogger(command.Out);
        var logger = session.Logger;

        var startInfo = new ProcessStartInfo(command.App)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in command.AppArgs)
            startInfo.ArgumentList.Add(arg);

        if (command.DiagnosticEnv)
        {
            startInfo.Environment["DOTNET_EnableDiagnostics"] = "1";
            startInfo.Environment["COMPlus_EnableDiagnostics"] = "1";
        }

        using var process = new Process { StartInfo = startInfo };
        var label = command.Name ?? Path.GetFileNameWithoutExtension(command.App);
        logger.Log(LogType.Info, $"Launching {label}: {command.App} {string.Join(" ", command.AppArgs)}");

        try
        {
            if (!process.Start())
            {
                console.ErrorLine($"Could not start {command.App}");
                return CommandResult.Fail();
            }
        }
        catch (Win32Exception ex)
        {
            console.ErrorLine(ex.Message);
            logger.Log(LogType.Error, "Launch failed", ex);
            return CommandResult.Fail();
        }

        console.WriteLine($"Started: {process.Id}");
        logger.Log(LogType.Info, $"Started pid={process.Id}");

        if (!command.WaitForExit)
        {
            console.WriteLine($"Wrote {session.BinaryLogPath}");
            return CommandResult.Ok();
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        foreach (var line in SplitLines(stdout))
            logger.Log(LogType.Info, $"stdout: {line}");

        foreach (var line in SplitLines(stderr))
            logger.Log(LogType.Error, $"stderr: {line}");

        logger.Log(LogType.Info, $"Process exited pid={process.Id} code={process.ExitCode}");
        console.WriteLine($"ExitCode: {process.ExitCode}");
        console.WriteLine($"Wrote {session.BinaryLogPath}");

        return process.ExitCode == 0 ? CommandResult.Ok() : CommandResult.Fail(process.ExitCode);
    }

    private static IEnumerable<string> SplitLines(string text)
    {
        using var reader = new StringReader(text);
        while (reader.ReadLine() is { } line)
            yield return line;
    }
}

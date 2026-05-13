using System.Diagnostics;
using QuickLog.Core;
using QuickLog.Tools.Diagnostics;

namespace QuickLog.Tools.Commands;

public static class ObserveCommand
{
    public static async Task<CommandResult> ExecuteAsync(
        ObserveToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        Process process;
        try
        {
            process = Process.GetProcessById(command.Pid);
        }
        catch (ArgumentException)
        {
            console.ErrorLine($"Process not found: {command.Pid}");
            return CommandResult.Fail();
        }

        using var session = new ToolSessionLogger(command.Out);
        var logger = session.Logger;
        var duration = Math.Max(0, command.DurationSeconds);
        var samples = duration == 0 ? 1 : duration + 1;

        console.WriteLine($"Process: {process.ProcessName} ({process.Id})");
        logger.Log(LogType.Info, $"Observing process {process.ProcessName} ({process.Id})");

        var probe = DiagnosticPortProbe.Probe(process.Id);
        console.WriteLine($"DiagnosticPort: {(probe.Available ? "available" : "unavailable")} - {probe.Detail}");
        logger.Log(LogType.Info, $"DiagnosticPort available={probe.Available} detail={probe.Detail}");

        for (var i = 0; i < samples; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            process.Refresh();
            logger.Log(LogType.Info, DescribeProcess(process));

            if (i < samples - 1)
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        logger.Log(LogType.Info, $"ModuleCount={GetModuleCount(process)}");
        console.WriteLine($"Wrote {session.BinaryLogPath}");
        return CommandResult.Ok();
    }

    private static string DescribeProcess(Process process)
    {
        var startTime = TryGet(() => process.StartTime.ToUniversalTime().ToString("O"), "unavailable");
        var cpu = TryGet(() => process.TotalProcessorTime.ToString(), "unavailable");
        return $"ProcessSample pid={process.Id} name={process.ProcessName} start={startTime} threads={process.Threads.Count} workingSet={process.WorkingSet64} cpu={cpu}";
    }

    private static int GetModuleCount(Process process)
        => TryGet(() => process.Modules.Cast<ProcessModule>().Count(), 0);

    private static T TryGet<T>(Func<T> getter, T fallback)
    {
        try
        {
            return getter();
        }
        catch
        {
            return fallback;
        }
    }
}

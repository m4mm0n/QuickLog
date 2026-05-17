namespace QuickLog.Tools;

using QuickLog.Tools.Commands;

public static class ToolApplication
{
    public static async Task<int> RunAsync(string[] args, IToolConsole console, CancellationToken cancellationToken = default)
    {
        var parsed = ToolCommandParser.Parse(args);
        if (!parsed.Success)
        {
            console.ErrorLine(parsed.Error ?? "Invalid command.");
            return 2;
        }

        var result = parsed.Command switch
        {
            DoctorToolCommand command => await DoctorCommand.ExecuteAsync(command, console, cancellationToken),
            InspectToolCommand command => await InspectCommand.ExecuteAsync(command, console, cancellationToken),
            ReplayToolCommand command => await ReplayCommand.ExecuteAsync(command, console, cancellationToken),
            BundleToolCommand command => await BundleCommand.ExecuteAsync(command, console, cancellationToken),
            BenchmarkToolCommand command => await BenchmarkCommand.ExecuteAsync(command, console, cancellationToken),
            LaunchToolCommand command => await LaunchCommand.ExecuteAsync(command, console, cancellationToken),
            ObserveToolCommand command => await ObserveCommand.ExecuteAsync(command, console, cancellationToken),
            ProfilerExplainToolCommand command => await ProfilerCommand.ExecuteAsync(command, console, cancellationToken),
            ProfilerEnvToolCommand command => await ProfilerCommand.ExecuteAsync(command, console, cancellationToken),
            TailToolCommand command => await TailCommand.ExecuteAsync(command, console, cancellationToken),
            GrepToolCommand command => await GrepCommand.ExecuteAsync(command, console, cancellationToken),
            DiffToolCommand command => await DiffCommand.ExecuteAsync(command, console, cancellationToken),
            StatsToolCommand command => await StatsCommand.ExecuteAsync(command, console, cancellationToken),
            RedactToolCommand command => await RedactCommand.ExecuteAsync(command, console, cancellationToken),
            SummarizeToolCommand command => await SummarizeCommand.ExecuteAsync(command, console, cancellationToken),
            ReportToolCommand command => await ReportCommand.ExecuteAsync(command, console, cancellationToken),
            RepairToolCommand command => await RepairCommand.ExecuteAsync(command, console, cancellationToken),
            MergeToolCommand command => await MergeCommand.ExecuteAsync(command, console, cancellationToken),
            TimelineToolCommand command => await TimelineCommand.ExecuteAsync(command, console, cancellationToken),
            DoctorConfigToolCommand command => await DoctorConfigCommand.ExecuteAsync(command, console, cancellationToken),
            _ => CommandResult.Fail(2)
        };

        if (result.ExitCode == 2 && parsed.Command is not (DoctorToolCommand or InspectToolCommand or ReplayToolCommand
            or BundleToolCommand or BenchmarkToolCommand or LaunchToolCommand or ObserveToolCommand
            or ProfilerExplainToolCommand or ProfilerEnvToolCommand or TailToolCommand or GrepToolCommand
            or DiffToolCommand or StatsToolCommand or RedactToolCommand or SummarizeToolCommand
            or ReportToolCommand or RepairToolCommand or MergeToolCommand or TimelineToolCommand
            or DoctorConfigToolCommand))
            console.ErrorLine($"Command not implemented yet: {parsed.Command?.GetType().Name}");

        return result.ExitCode;
    }
}

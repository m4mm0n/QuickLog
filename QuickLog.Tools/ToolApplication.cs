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
            _ => CommandResult.Fail(2)
        };

        if (result.ExitCode == 2 && parsed.Command is not (DoctorToolCommand or InspectToolCommand or ReplayToolCommand
            or BundleToolCommand or BenchmarkToolCommand))
            console.ErrorLine($"Command not implemented yet: {parsed.Command?.GetType().Name}");

        return result.ExitCode;
    }
}

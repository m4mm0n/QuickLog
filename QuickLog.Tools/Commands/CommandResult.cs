namespace QuickLog.Tools.Commands;

public readonly record struct CommandResult(bool Success, int ExitCode)
{
    public static CommandResult Ok() => new(true, 0);
    public static CommandResult Fail(int exitCode = 1) => new(false, exitCode);
}

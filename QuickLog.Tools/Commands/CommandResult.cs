namespace QuickLog.Tools.Commands;

/// <summary>Represents the success state and process exit code of a tool command.</summary>
public readonly record struct CommandResult(bool Success, int ExitCode)
{
    /// <summary>Creates a successful command result.</summary>
    /// <returns>A result with exit code zero.</returns>
    public static CommandResult Ok() => new(true, 0);

    /// <summary>Creates a failed command result.</summary>
    /// <param name="exitCode">The nonzero process exit code.</param>
    /// <returns>A failed command result.</returns>
    public static CommandResult Fail(int exitCode = 1) => new(false, exitCode);
}

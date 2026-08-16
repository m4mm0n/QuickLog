namespace QuickLog.Tools;

/// <summary>Abstracts standard and error output for command execution and testing.</summary>
public interface IToolConsole
{
    /// <summary>Writes a line to standard output.</summary>
    /// <param name="message">The message to write.</param>
    void WriteLine(string message);

    /// <summary>Writes a line to error output.</summary>
    /// <param name="message">The error message to write.</param>
    void ErrorLine(string message);
}

/// <summary>Writes tool output to the process console.</summary>
public sealed class ConsoleToolConsole : IToolConsole
{
    /// <inheritdoc />
    public void WriteLine(string message) => Console.Out.WriteLine(message);

    /// <inheritdoc />
    public void ErrorLine(string message) => Console.Error.WriteLine(message);
}

/// <summary>Captures tool output in memory for programmatic callers and tests.</summary>
public sealed class BufferToolConsole : IToolConsole
{
    private readonly List<string> _output = [];
    private readonly List<string> _errors = [];

    /// <summary>Gets the captured standard-output lines.</summary>
    public IReadOnlyList<string> Output => _output;

    /// <summary>Gets the captured error-output lines.</summary>
    public IReadOnlyList<string> Errors => _errors;

    /// <summary>Gets the captured standard output joined with platform line endings.</summary>
    public string OutputText => string.Join(Environment.NewLine, _output);

    /// <summary>Gets the captured error output joined with platform line endings.</summary>
    public string ErrorText => string.Join(Environment.NewLine, _errors);

    /// <inheritdoc />
    public void WriteLine(string message) => _output.Add(message);

    /// <inheritdoc />
    public void ErrorLine(string message) => _errors.Add(message);
}

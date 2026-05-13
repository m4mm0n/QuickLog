namespace QuickLog.Tools;

public interface IToolConsole
{
    void WriteLine(string message);
    void ErrorLine(string message);
}

public sealed class ConsoleToolConsole : IToolConsole
{
    public void WriteLine(string message) => Console.Out.WriteLine(message);
    public void ErrorLine(string message) => Console.Error.WriteLine(message);
}

public sealed class BufferToolConsole : IToolConsole
{
    private readonly List<string> _output = [];
    private readonly List<string> _errors = [];

    public IReadOnlyList<string> Output => _output;
    public IReadOnlyList<string> Errors => _errors;

    public string OutputText => string.Join(Environment.NewLine, _output);
    public string ErrorText => string.Join(Environment.NewLine, _errors);

    public void WriteLine(string message) => _output.Add(message);
    public void ErrorLine(string message) => _errors.Add(message);
}

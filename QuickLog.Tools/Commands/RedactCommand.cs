using QuickLog.Core;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the redact command for masking sensitive values in text logs.</summary>
public static class RedactCommand
{
    /// <summary>Executes the redact command.</summary>
    public static async Task<CommandResult> ExecuteAsync(
        RedactToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Input))
        {
            console.ErrorLine($"File not found: {command.Input}");
            return CommandResult.Fail();
        }

        var redactor = new LogRedactor(new LogRedactionOptions());
        var text = await File.ReadAllTextAsync(command.Input, cancellationToken).ConfigureAwait(false);
        Directory.CreateDirectory(Path.GetDirectoryName(command.Out) ?? ".");
        await File.WriteAllTextAsync(command.Out, redactor.Redact(text), cancellationToken).ConfigureAwait(false);
        console.WriteLine($"Wrote {command.Out}");
        return CommandResult.Ok();
    }
}

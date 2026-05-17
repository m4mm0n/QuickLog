using System.Text.Json;

namespace QuickLog.Tools.Commands;

/// <summary>Implements the doctor-config command for validating serialized logger options.</summary>
public static class DoctorConfigCommand
{
    /// <summary>Executes the doctor-config command.</summary>
    public static async Task<CommandResult> ExecuteAsync(
        DoctorConfigToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path))
        {
            console.ErrorLine($"File not found: {command.Path}");
            return CommandResult.Fail();
        }

        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(command.Path, cancellationToken).ConfigureAwait(false));
        var root = doc.RootElement;
        var issues = Validate(root);
        if (issues.Count == 0)
        {
            console.WriteLine("Configuration valid.");
            return CommandResult.Ok();
        }

        foreach (var issue in issues)
            console.WriteLine(issue);

        return CommandResult.Fail();
    }

    /// <summary>Validates the subset of logger options that can be represented in simple JSON configs.</summary>
    private static IReadOnlyList<string> Validate(JsonElement root)
    {
        var issues = new List<string>();
        var asyncOnly = ReadBool(root, "AsyncOnly");
        var asyncLogging = ReadBool(root, "AsyncLogging");
        var asyncBinaryLogging = ReadBool(root, "AsyncBinaryLogging");
        var asyncTraceLogging = ReadBool(root, "AsyncTraceLogging");
        var jsonLogPath = ReadString(root, "JsonLogPath");

        if (asyncOnly && !asyncLogging)
            issues.Add("QL001 Error: AsyncOnly requires AsyncLogging.");

        if (asyncOnly && string.IsNullOrWhiteSpace(jsonLogPath) && !asyncBinaryLogging && !asyncTraceLogging)
            issues.Add("QL001 Error: AsyncOnly should have at least one durable async sink.");

        if (root.TryGetProperty("Rotation", out var rotation) && rotation.ValueKind == JsonValueKind.Object)
        {
            var maxFileBytes = ReadLong(rotation, "MaxFileBytes");
            var maxFiles = ReadInt(rotation, "MaxFiles");
            if (maxFileBytes <= 0 || maxFiles <= 0)
                issues.Add("QL002 Error: Rotation requires MaxFileBytes and MaxFiles greater than zero.");
        }

        return issues;
    }

    /// <summary>Reads a boolean JSON property, returning false when it is absent.</summary>
    private static bool ReadBool(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>Reads a string JSON property, returning null when it is absent.</summary>
    private static string? ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>Reads an integer JSON property, returning zero when it is absent.</summary>
    private static int ReadInt(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.TryGetInt32(out var parsed) ? parsed : 0;

    /// <summary>Reads a long integer JSON property, returning zero when it is absent.</summary>
    private static long ReadLong(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.TryGetInt64(out var parsed) ? parsed : 0;
}

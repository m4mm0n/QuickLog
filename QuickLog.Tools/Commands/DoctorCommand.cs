using QuickLog.Core;
using QuickLog.Utilities;
using System.Text.Json;

namespace QuickLog.Tools.Commands;

public static class DoctorCommand
{
    public static Task<CommandResult> ExecuteAsync(
        DoctorToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(command.Path) && !Directory.Exists(command.Path))
        {
            console.ErrorLine($"Path not found: {command.Path}");
            return Task.FromResult(CommandResult.Fail());
        }

        var files = ResolveFiles(command.Path, command.Recursive);
        var success = true;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var extension = Path.GetExtension(file).ToLowerInvariant();
            var fileOk = extension switch
            {
                ".qlog" => InspectBinaryLog(file, console),
                ".jsonl" => InspectJsonLines(file, console),
                ".json" => InspectCrashJson(file, console),
                _ => true
            };

            success &= fileOk;
        }

        return Task.FromResult(success ? CommandResult.Ok() : CommandResult.Fail());
    }

    private static IEnumerable<string> ResolveFiles(string path, bool recursive)
    {
        if (File.Exists(path))
            return [path];

        return Directory.EnumerateFiles(
            path,
            "*",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
    }

    private static bool InspectBinaryLog(string path, IToolConsole console)
    {
        var entries = BinaryLogReader.Read(path, stopOnCrcError: true).ToList();
        if (new FileInfo(path).Length > 0 && entries.Count == 0)
        {
            console.ErrorLine($"{Path.GetFileName(path)}: invalid or corrupted binary log.");
            return false;
        }

        console.WriteLine($"{Path.GetFileName(path)}");
        console.WriteLine($"  Entries: {entries.Count}");

        if (entries.Count > 0)
        {
            console.WriteLine($"  First: {entries.Min(e => e.Timestamp):O}");
            console.WriteLine($"  Last: {entries.Max(e => e.Timestamp):O}");
        }

        foreach (var group in entries.GroupBy(e => e.Level).OrderBy(g => g.Key.ToString()))
            console.WriteLine($"  {group.Key}: {group.Count()}");

        return true;
    }

    private static bool InspectJsonLines(string path, IToolConsole console)
    {
        var lines = 0;
        var malformed = 0;

        foreach (var line in File.ReadLines(path))
        {
            lines++;
            var trimmed = line.Trim();
            if (trimmed.Length == 0)
                continue;

            if (!trimmed.StartsWith('{') || !trimmed.EndsWith('}'))
                malformed++;
        }

        console.WriteLine($"{Path.GetFileName(path)}");
        console.WriteLine($"  Lines: {lines}");
        console.WriteLine($"  Malformed: {malformed}");
        return malformed == 0;
    }

    private static bool InspectCrashJson(string path, IToolConsole console)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            var ok = root.TryGetProperty("Timestamp", out _)
                && root.TryGetProperty("Source", out _)
                && root.TryGetProperty("Exception", out _);

            console.WriteLine($"{Path.GetFileName(path)}");
            console.WriteLine($"  CrashDump: {(ok ? "valid" : "missing required fields")}");
            return ok;
        }
        catch (JsonException)
        {
            console.ErrorLine($"{Path.GetFileName(path)}: invalid crash JSON.");
            return false;
        }
    }
}

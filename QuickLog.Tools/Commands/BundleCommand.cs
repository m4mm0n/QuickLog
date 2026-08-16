using System.IO.Compression;
using System.Text.Json;
using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Tools.Commands;

/// <summary>Creates a bounded, optionally redacted diagnostics archive.</summary>
public static class BundleCommand
{
    /// <summary>Collects configured logs, crashes, exports, and environment details into a ZIP archive.</summary>
    /// <param name="command">The bundle inputs and output options.</param>
    /// <param name="console">The destination for command status.</param>
    /// <param name="cancellationToken">A token that cancels bundle creation.</param>
    /// <returns>A task containing the command result.</returns>
    public static Task<CommandResult> ExecuteAsync(
        BundleToolCommand command,
        IToolConsole console,
        CancellationToken cancellationToken = default)
    {
        var outputPath = Path.GetFullPath(command.Out);
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        var redactor = command.Redact ? new LogRedactor(new LogRedactionOptions()) : null;
        var includedFiles = new List<string>();

        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);

        AddDirectory(archive, command.Logs, "logs", command.MaxFileBytes, includedFiles, redactor, cancellationToken);
        AddDirectory(archive, command.Crashes, "crashes", command.MaxFileBytes, includedFiles, redactor, cancellationToken);

        if (command.IncludeExports && !string.IsNullOrWhiteSpace(command.Logs) && Directory.Exists(command.Logs))
            AddBinaryExports(archive, command.Logs, redactor, cancellationToken);

        if (command.IncludeEnv)
            AddTextEntry(archive, "environment.json", JsonSerializer.Serialize(new
            {
                machine = Environment.MachineName,
                os = Environment.OSVersion.ToString(),
                runtime = Environment.Version.ToString(),
                process64Bit = Environment.Is64BitProcess,
                workingDirectory = Environment.CurrentDirectory
            }, new JsonSerializerOptions { WriteIndented = true }), redactor);

        AddTextEntry(archive, "manifest.json", JsonSerializer.Serialize(new
        {
            generatedUtc = DateTime.UtcNow.ToString("O"),
            logs = command.Logs,
            crashes = command.Crashes,
            includeEnv = command.IncludeEnv,
            includeExports = command.IncludeExports,
            maxFileBytes = command.MaxFileBytes,
            redact = command.Redact,
            files = includedFiles
        }, new JsonSerializerOptions { WriteIndented = true }), redactor);

        console.WriteLine($"Wrote {outputPath}");
        return Task.FromResult(CommandResult.Ok());
    }

    private static void AddDirectory(
        ZipArchive archive,
        string? directory,
        string prefix,
        long? maxFileBytes,
        List<string> includedFiles,
        LogRedactor? redactor,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        var root = Path.GetFullPath(directory);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(file);
            if (maxFileBytes is not null && info.Length > maxFileBytes.Value)
                continue;

            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var entryName = $"{prefix}/{relative}";

            if (redactor is null)
            {
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Fastest);
            }
            else
            {
                AddTextEntry(archive, entryName, File.ReadAllText(file), redactor);
            }

            includedFiles.Add(entryName);
        }
    }

    private static void AddBinaryExports(
        ZipArchive archive,
        string logsDirectory,
        LogRedactor? redactor,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(logsDirectory);
        foreach (var file in Directory.EnumerateFiles(root, "*.qlog", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            var temp = Path.Combine(Path.GetTempPath(), $"quicklog_export_{Guid.NewGuid():N}.txt");
            try
            {
                BinaryLogExporter.ExportToText(file, temp);
                AddTextEntry(archive, $"exports/{relative}.txt", File.ReadAllText(temp), redactor);
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }
    }

    private static void AddTextEntry(ZipArchive archive, string name, string text, LogRedactor? redactor)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream);
        writer.Write(redactor?.Redact(text) ?? text);
    }
}

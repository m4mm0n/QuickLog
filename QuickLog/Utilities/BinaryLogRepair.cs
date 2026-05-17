using QuickLog.Core;
using QuickLog.Sinks;

namespace QuickLog.Utilities;

/// <summary>
/// Result returned after a QLOG repair attempt.
/// </summary>
/// <param name="RecoveredEntries">Number of valid entries written to the repaired file.</param>
/// <param name="SkippedBytes">Number of bytes skipped while scanning for records.</param>
/// <param name="OutputPath">Path of the repaired file.</param>
public sealed record BinaryLogRepairResult(int RecoveredEntries, int SkippedBytes, string OutputPath);

/// <summary>
/// Scans corrupted QLOG files and writes recoverable records to a clean file.
/// </summary>
public static class BinaryLogRepair
{
    private static readonly byte[] Magic = "QLOG"u8.ToArray();

    /// <summary>
    /// Repairs <paramref name="sourcePath"/> into <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="sourcePath">Possibly corrupted QLOG file.</param>
    /// <param name="outputPath">Output QLOG path.</param>
    /// <returns>A repair result containing recovered entry and skipped byte counts.</returns>
    public static BinaryLogRepairResult Repair(string sourcePath, string outputPath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var recovered = new List<LogEntry>();
        var skipped = 0;
        var offset = 0;

        while (offset < bytes.Length)
        {
            var magicAt = IndexOf(bytes, Magic, offset);
            if (magicAt < 0)
            {
                skipped += bytes.Length - offset;
                break;
            }

            skipped += magicAt - offset;
            if (BinaryLogReader.TryReadEntry(bytes, magicAt, out var entry, out var nextOffset, out _))
            {
                recovered.Add(entry);
                offset = nextOffset;
            }
            else
            {
                skipped++;
                offset = magicAt + 1;
            }
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using (var sink = new BinaryLogSink(outputPath))
        {
            foreach (var entry in recovered)
                sink.Write(entry);
        }

        return new BinaryLogRepairResult(recovered.Count, skipped, outputPath);
    }

    /// <summary>
    /// Finds the next occurrence of a byte pattern.
    /// </summary>
    /// <param name="bytes">The byte buffer to scan.</param>
    /// <param name="pattern">The byte pattern to find.</param>
    /// <param name="start">The first offset to inspect.</param>
    /// <returns>The matching offset, or <c>-1</c> when no match exists.</returns>
    private static int IndexOf(byte[] bytes, byte[] pattern, int start)
    {
        for (var i = Math.Max(0, start); i <= bytes.Length - pattern.Length; i++)
        {
            var match = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (bytes[i + j] == pattern[j])
                    continue;

                match = false;
                break;
            }

            if (match)
                return i;
        }

        return -1;
    }
}

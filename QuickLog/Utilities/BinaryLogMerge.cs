using QuickLog.Sinks;

namespace QuickLog.Utilities;

/// <summary>
/// Merges multiple QLOG files into one timestamp-sorted QLOG file.
/// </summary>
public static class BinaryLogMerge
{
    /// <summary>
    /// Merges the input files into <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="inputPaths">Input QLOG paths.</param>
    /// <param name="outputPath">Output QLOG path.</param>
    public static void Merge(IEnumerable<string> inputPaths, string outputPath)
    {
        var entries = inputPaths
            .SelectMany(path => BinaryLogReader.Read(path, stopOnCrcError: false))
            .OrderBy(entry => entry.Timestamp)
            .ToList();

        var directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using var sink = new BinaryLogSink(outputPath);
        foreach (var entry in entries)
            sink.Write(entry);
    }
}

/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogExporter.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:25:55 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 5B167AF6
 *  
 *  Description    :
 *                   Provides functionality to export binary log files to a human-readable text format.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 5B167AF6

using System.Text;

namespace QuickLog.Utilities;

/// <summary>
/// Provides functionality to export binary log files to a human-readable text format.
/// </summary>
/// <remarks>The BinaryLogExporter class is intended for use with log files produced in a specific binary format.
/// All members are static and thread-safe. This class cannot be instantiated.</remarks>
public static class BinaryLogExporter
{
    /// <summary>
    /// Exports log entries from a binary log file to a human-readable text file.
    /// </summary>
    /// <remarks>Each log entry is written as a single line in the text file, including timestamp, log level,
    /// member name, and message. If file path and line number information are available, they are included on a
    /// separate indented line. The output file is encoded in UTF-8 and will be overwritten if it already
    /// exists.</remarks>
    /// <param name="binaryPath">The path to the binary log file to read. Cannot be null or empty.</param>
    /// <param name="textPath">The path to the text file to create or overwrite with the exported log entries. Cannot be null or empty.</param>
    public static void ExportToText(string binaryPath, string textPath)
    {
        using var writer = new StreamWriter(textPath, append: false, Encoding.UTF8);

        foreach (var entry in BinaryLogReader.Read(binaryPath))
        {
            writer.WriteLine(
                $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] " +
                $"[{entry.Level}] " +
                $"[{entry.ThreadRole}] " +
                $"[{entry.MemberName}] " +
                $"{entry.Message}");

            if (!string.IsNullOrWhiteSpace(entry.FilePath))
                writer.WriteLine($"    at {entry.FilePath}:{entry.LineNumber}");
        }
    }
}
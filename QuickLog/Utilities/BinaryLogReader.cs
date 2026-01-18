/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogReader.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:23:50 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : 0699137F
 *  
 *  Description    :
 *                   Provides functionality to read log entries from a binary log file in the QLOG format.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: 0699137F

using QuickLog.Core;
using System.Text;

namespace QuickLog.Utilities;

/// <summary>
/// Provides functionality to read log entries from a binary log file in the QLOG format.
/// </summary>
/// <remarks>This class is intended for reading log files produced in a specific binary format identified by the
/// QLOG magic header. All members are static and thread-safe. The class cannot be instantiated.</remarks>
public static class BinaryLogReader
{
    private static readonly byte[] Magic = "QLOG"u8.ToArray();

    /// <summary>
    /// Reads log entries from a binary log file at the specified path.
    /// </summary>
    /// <remarks>Reading stops at the first invalid record, file format error, or CRC error if stopOnCrcError
    /// is set to true. If stopOnCrcError is false, corrupted records are skipped and reading continues with the next
    /// record. The method does not throw exceptions for file format or CRC errors; it simply stops or skips as
    /// described.</remarks>
    /// <param name="path">The path to the binary log file to read.</param>
    /// <param name="stopOnCrcError">true to stop reading when a CRC error is encountered; false to skip corrupted records and continue reading. The
    /// default is true.</param>
    /// <returns>An enumerable collection of LogEntry objects read from the file. The collection may be empty if the file
    /// contains no valid entries or if a format or CRC error is encountered.</returns>
    public static IEnumerable<LogEntry> Read(string path, bool stopOnCrcError = true)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var br = new BinaryReader(fs, Encoding.UTF8);
        using var crc32 = new Crc32();

        while (fs.Position < fs.Length)
        {
            LogEntry? entry = null;

            try
            {
                var recordStart = fs.Position;

                var magic = br.ReadBytes(4);
                if (!magic.SequenceEqual(Magic))
                    yield break;

                var version = br.ReadInt32();
                if (version != 1)
                    yield break;

                var ticks = br.ReadInt64();
                var level = (LogType)br.ReadInt32();
                var threadId = br.ReadInt32();
                var threadRole = (ThreadRole)br.ReadByte();
                var lineNumber = br.ReadInt32();

                var member = ReadString(br);
                var file = ReadString(br);
                var category = ReadString(br);
                var message = ReadString(br);

                var recordEnd = fs.Position;
                var storedCrc = br.ReadUInt32();

                fs.Position = recordStart;
                var raw = br.ReadBytes((int)(recordEnd - recordStart));
                var calc = crc32.CalculateChecksum(raw);

                if (calc != storedCrc)
                {
                    if (stopOnCrcError)
                        yield break;

                    continue;
                }

                entry = new LogEntry(
                    new DateTime(ticks, DateTimeKind.Utc),
                    level,
                    message,
                    "Binary",
                    category,
                    member,
                    file,
                    lineNumber,
                    threadId,
                    threadRole
                );
            }
            catch
            {
                yield break;
            }

            // ✅ yield happens OUTSIDE try/catch
            if (entry.HasValue)
                yield return entry.Value;
        }
    }

    private static string ReadString(BinaryReader br)
    {
        var len = br.ReadInt32();
        if (len <= 0)
            return string.Empty;

        var bytes = br.ReadBytes(len);
        return Encoding.UTF8.GetString(bytes);
    }
}
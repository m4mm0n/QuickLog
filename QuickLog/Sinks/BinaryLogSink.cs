/*
 * ====================================================================================================
 *  Project        : QuickLog
 *  File           : BinaryLogSink.cs
 *  Author         : Geir Gustavsen, ZeroLinez Softworx 2024 - 2026
 *  Created        : 2026-01-18 06:22:01 +01:00
 *  Last Modified  : 2026-01-18 07:12:52 +01:00
 *  CRC32          : DFDD07C1
 *  
 *  Description    :
 *                   Provides a log sink that writes log entries in a compact binary format to a file for efficient storage and later
 *                   analysis.
 * 
 *  License        :
 *                   MIT
 *                   https://opensource.org/licenses/MIT
 *
 *  Notes          :
 *                   THIS PROJECT IS A COMPLETE, AND SIMPLE TO USE LOGGER
 * ====================================================================================================
 */
// CRC32-BODY: DFDD07C1

using System.Text;
using QuickLog.Core;
using QuickLog.Utilities;

namespace QuickLog.Sinks;

/// <summary>
/// Provides a log sink that writes log entries in a compact binary format to a file for efficient storage and later
/// analysis.
/// </summary>
/// <remarks>BinaryLogSink appends log entries to the specified file using a custom binary format that includes a
/// header, log entry data, and a CRC32 checksum for integrity verification. This sink is intended for scenarios where
/// performance and log file size are important considerations. The resulting log files are not human-readable and
/// require a compatible tool for inspection or analysis. This class is not thread-safe; callers should ensure
/// appropriate synchronization if used from multiple threads.</remarks>
internal sealed class BinaryLogSink : ILogSink
{
    private readonly FileStream _stream;
    private readonly BinaryWriter _writer;
    private readonly Crc32 _crc = new();

    private static readonly byte[] Magic = "QLOG"u8.ToArray();
    private const int Version = 1;

    public BinaryLogSink(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        _writer = new BinaryWriter(_stream, Encoding.UTF8, leaveOpen: true);
    }

    public void Write(in LogEntry entry)
    {
        using var ms = new MemoryStream(256);
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        bw.Write(Magic);
        bw.Write(Version);
        bw.Write(entry.Timestamp.Ticks);
        bw.Write((int)entry.Level);
        bw.Write(entry.ThreadId);
        bw.Write((byte)entry.ThreadRole);
        bw.Write(entry.LineNumber);

        WriteString(bw, entry.MemberName);
        WriteString(bw, entry.FilePath);
        WriteString(bw, entry.Category);
        WriteString(bw, entry.Message);

        bw.Flush();

        var data = ms.ToArray();
        var crc = _crc.CalculateChecksum(data);

        _writer.Write(data);
        _writer.Write(crc);
    }

    private static void WriteString(BinaryWriter bw, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            bw.Write(0);
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value);
        bw.Write(bytes.Length);
        bw.Write(bytes);
    }

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
        _stream.Dispose();
    }
}
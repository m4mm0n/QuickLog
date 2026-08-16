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
    private readonly RotatingFileWriter _writer;
    private readonly Crc32 _crc = new();

    public BinaryLogSink(string path, LogRotationOptions? rotation = null)
    {
        _writer = new RotatingFileWriter(path, rotation);
    }

    public void Write(in LogEntry entry)
    {
        using var ms = new MemoryStream(256);
        using var bw = new BinaryWriter(ms, Encoding.UTF8, leaveOpen: true);

        bw.Write(BinaryLogFormat.Magic);
        bw.Write(BinaryLogFormat.CurrentVersion);
        bw.Write(entry.Timestamp.Ticks);
        bw.Write((int)entry.Level);
        bw.Write(entry.ThreadId);
        bw.Write((byte)entry.ThreadRole);
        bw.Write(entry.LineNumber);

        BinaryLogFormat.WriteString(bw, entry.MemberName);
        BinaryLogFormat.WriteString(bw, entry.FilePath);
        BinaryLogFormat.WriteString(bw, entry.Category);
        BinaryLogFormat.WriteString(bw, entry.Message);
        BinaryLogFormat.WriteString(bw, entry.CorrelationId);
        BinaryLogFormat.WriteString(bw, entry.TraceId);
        BinaryLogFormat.WriteString(bw, entry.SpanId);
        bw.Write(entry.EventId.Id);
        BinaryLogFormat.WriteString(bw, entry.EventId.Name);

        var properties = entry.Properties ?? LogProperties.Empty;
        bw.Write(properties.Count);
        foreach (var pair in properties.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            BinaryLogFormat.WriteString(bw, pair.Key);
            BinaryLogValueCodec.Write(bw, pair.Value);
        }

        bw.Flush();

        var data = ms.ToArray();
        var crc = _crc.CalculateChecksum(data);
        var record = new byte[data.Length + sizeof(uint)];
        Buffer.BlockCopy(data, 0, record, 0, data.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(crc), 0, record, data.Length, sizeof(uint));

        _writer.WriteBytes(record);
    }

    public void Flush() => _writer.Flush();

    public void Dispose()
    {
        Flush();
        _writer.Dispose();
    }
}

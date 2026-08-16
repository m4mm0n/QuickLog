using QuickLog.Core;
using System.Text;

namespace QuickLog.Utilities;

/// <summary>
/// Provides functionality to read log entries from a binary log file in the QLOG format.
/// </summary>
public static class BinaryLogReader
{
    /// <summary>
    /// Reads log entries from a binary log file at the specified path.
    /// </summary>
    /// <param name="path">The path to the binary log file to read.</param>
    /// <param name="stopOnCrcError">Whether reading should stop when a CRC mismatch is found.</param>
    /// <returns>The readable log entries from the file.</returns>
    public static IEnumerable<LogEntry> Read(string path, bool stopOnCrcError = true)
    {
        var bytes = ReadAllBytesShared(path);
        var offset = 0;

        while (offset < bytes.Length)
        {
            if (!TryReadEntry(bytes, offset, out var entry, out var nextOffset, out var diagnostic))
            {
                if (diagnostic.Kind is BinaryLogDiagnosticKind.CrcMismatch && !stopOnCrcError)
                {
                    offset = Math.Max(offset + 1, nextOffset);
                    continue;
                }

                yield break;
            }

            offset = nextOffset;
            yield return entry;
        }
    }

    /// <summary>
    /// Reads a binary log and returns entries plus diagnostics for the first invalid record.
    /// </summary>
    /// <param name="path">The path to the binary log file to inspect.</param>
    /// <returns>The entries read before the first diagnostic and any diagnostic that stopped the read.</returns>
    public static BinaryLogReadResult ReadWithDiagnostics(string path)
    {
        var entries = new List<LogEntry>();
        var diagnostics = new List<BinaryLogDiagnostic>();
        var bytes = ReadAllBytesShared(path);
        var offset = 0;

        while (offset < bytes.Length)
        {
            if (!TryReadEntry(bytes, offset, out var entry, out var nextOffset, out var diagnostic))
            {
                diagnostics.Add(diagnostic);
                break;
            }

            entries.Add(entry);
            offset = nextOffset;
        }

        return new BinaryLogReadResult(entries, diagnostics);
    }

    /// <summary>
    /// Reads a binary log snapshot while allowing the writer process to keep the file open.
    /// </summary>
    /// <param name="path">The binary log path to copy.</param>
    /// <returns>The bytes visible at the time the file was opened.</returns>
    private static byte[] ReadAllBytesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (stream.CanSeek && stream.Length > int.MaxValue)
            throw new IOException("Binary log is too large to read into memory.");

        using var buffer = stream.CanSeek ? new MemoryStream((int)stream.Length) : new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    /// <summary>
    /// Attempts to read one QLOG record from a byte buffer.
    /// </summary>
    /// <param name="bytes">Buffer containing QLOG data.</param>
    /// <param name="offset">Offset at which a record is expected to start.</param>
    /// <param name="entry">Parsed entry when the method succeeds.</param>
    /// <param name="nextOffset">Offset immediately after the record, when known.</param>
    /// <param name="diagnostic">Diagnostic describing a failed read.</param>
    /// <returns><see langword="true"/> when a valid record was read; otherwise <see langword="false"/>.</returns>
    internal static bool TryReadEntry(
        byte[] bytes,
        int offset,
        out LogEntry entry,
        out int nextOffset,
        out BinaryLogDiagnostic diagnostic)
    {
        entry = default;
        nextOffset = offset;
        diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.TruncatedRecord, "Record is truncated.");

        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var br = new BinaryReader(ms, Encoding.UTF8);
            using var crc32 = new Crc32();
            ms.Position = offset;

            var recordStart = ms.Position;
            var magic = br.ReadBytes(4);
            if (!magic.SequenceEqual(BinaryLogFormat.Magic))
            {
                diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.InvalidMagic, "QLOG magic was not found.");
                nextOffset = offset + 1;
                return false;
            }

            var version = br.ReadInt32();
            if (version is not (1 or 2 or 3))
            {
                diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.UnsupportedVersion, $"Unsupported QLOG version {version}.");
                nextOffset = (int)ms.Position;
                return false;
            }

            var ticks = br.ReadInt64();
            var level = (LogType)br.ReadInt32();
            var threadId = br.ReadInt32();
            var threadRole = (ThreadRole)br.ReadByte();
            var lineNumber = br.ReadInt32();

            var member = BinaryLogFormat.ReadString(br);
            var file = BinaryLogFormat.ReadString(br);
            var category = BinaryLogFormat.ReadString(br);
            var message = BinaryLogFormat.ReadString(br);
            var correlationId = version >= 2 ? BinaryLogFormat.ReadString(br) : null;
            var traceId = version >= 2 ? BinaryLogFormat.ReadString(br) : null;
            var spanId = version >= 2 ? BinaryLogFormat.ReadString(br) : null;
            var eventId = version >= 3
                ? new LogEventId(br.ReadInt32(), NullIfEmpty(BinaryLogFormat.ReadString(br)))
                : LogEventId.None;
            var properties = version >= 3 ? ReadProperties(br) : LogProperties.Empty;

            var recordEnd = ms.Position;
            var storedCrc = br.ReadUInt32();

            ms.Position = recordStart;
            var raw = br.ReadBytes((int)(recordEnd - recordStart));
            nextOffset = (int)(recordEnd + sizeof(uint));
            var calc = crc32.CalculateChecksum(raw);

            if (calc != storedCrc)
            {
                diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.CrcMismatch, "Record CRC did not match.");
                return false;
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
                threadRole,
                string.IsNullOrEmpty(correlationId) ? null : correlationId,
                string.IsNullOrEmpty(traceId) ? null : traceId,
                string.IsNullOrEmpty(spanId) ? null : spanId,
                eventId,
                properties
            );

            diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.None, string.Empty);
            return true;
        }
        catch (EndOfStreamException)
        {
            diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.TruncatedRecord, "Record ended unexpectedly.");
            nextOffset = bytes.Length;
            return false;
        }
        catch (Exception ex)
        {
            diagnostic = new BinaryLogDiagnostic(offset, BinaryLogDiagnosticKind.FormatError, ex.Message);
            nextOffset = Math.Min(bytes.Length, offset + 1);
            return false;
        }
    }

    private static IReadOnlyDictionary<string, object?> ReadProperties(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count < 0 || count > BinaryLogFormat.MaximumProperties)
            throw new InvalidDataException($"QLOG property count {count} is invalid.");
        if (count == 0)
            return LogProperties.Empty;

        var properties = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var index = 0; index < count; index++)
        {
            var name = BinaryLogFormat.ReadString(reader);
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException("QLOG property names cannot be empty.");
            properties[name] = BinaryLogValueCodec.Read(reader);
        }

        return LogProperties.Snapshot(properties);
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}

/// <summary>
/// The kind of diagnostic reported while reading a QLOG file.
/// </summary>
public enum BinaryLogDiagnosticKind
{
    /// <summary>No diagnostic was emitted.</summary>
    None = 0,

    /// <summary>The QLOG magic value was missing.</summary>
    InvalidMagic,

    /// <summary>The QLOG version is not supported by this reader.</summary>
    UnsupportedVersion,

    /// <summary>The record ended before all fields could be read.</summary>
    TruncatedRecord,

    /// <summary>The stored CRC does not match the record payload.</summary>
    CrcMismatch,

    /// <summary>The record had another format error.</summary>
    FormatError
}

/// <summary>
/// Diagnostic emitted while reading a QLOG file.
/// </summary>
/// <param name="Offset">Byte offset where the issue was detected.</param>
/// <param name="Kind">Diagnostic kind.</param>
/// <param name="Message">Human-readable detail.</param>
public sealed record BinaryLogDiagnostic(long Offset, BinaryLogDiagnosticKind Kind, string Message);

/// <summary>
/// Result returned by diagnostic QLOG reads.
/// </summary>
/// <param name="Entries">Entries read before diagnostics stopped the scan.</param>
/// <param name="Diagnostics">Diagnostics found while reading.</param>
public sealed record BinaryLogReadResult(IReadOnlyList<LogEntry> Entries, IReadOnlyList<BinaryLogDiagnostic> Diagnostics);

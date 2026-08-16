using System.Globalization;

namespace QuickLog.Utilities;

internal static class BinaryLogValueCodec
{
    private enum ValueKind : byte
    {
        Null,
        String,
        Boolean,
        SignedInteger,
        UnsignedInteger,
        FloatingPoint,
        Decimal,
        DateTime,
        DateTimeOffset,
        Guid
    }

    public static void Write(BinaryWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.Write((byte)ValueKind.Null);
                break;
            case bool boolean:
                writer.Write((byte)ValueKind.Boolean);
                writer.Write(boolean);
                break;
            case byte or sbyte or short or ushort or int or long:
                writer.Write((byte)ValueKind.SignedInteger);
                writer.Write(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case uint or ulong:
                writer.Write((byte)ValueKind.UnsignedInteger);
                writer.Write(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
                break;
            case float or double:
                writer.Write((byte)ValueKind.FloatingPoint);
                writer.Write(Convert.ToDouble(value, CultureInfo.InvariantCulture));
                break;
            case decimal decimalValue:
                writer.Write((byte)ValueKind.Decimal);
                writer.Write(decimalValue);
                break;
            case DateTime dateTime:
                writer.Write((byte)ValueKind.DateTime);
                writer.Write(dateTime.ToBinary());
                break;
            case DateTimeOffset dateTimeOffset:
                writer.Write((byte)ValueKind.DateTimeOffset);
                writer.Write(dateTimeOffset.Ticks);
                writer.Write((short)dateTimeOffset.Offset.TotalMinutes);
                break;
            case Guid guid:
                writer.Write((byte)ValueKind.Guid);
                writer.Write(guid.ToByteArray());
                break;
            default:
                writer.Write((byte)ValueKind.String);
                BinaryLogFormat.WriteString(writer, QuickLog.LogProperties.FormatValue(value));
                break;
        }
    }

    public static object? Read(BinaryReader reader)
    {
        var kind = (ValueKind)reader.ReadByte();
        return kind switch
        {
            ValueKind.Null => null,
            ValueKind.String => BinaryLogFormat.ReadString(reader),
            ValueKind.Boolean => reader.ReadBoolean(),
            ValueKind.SignedInteger => reader.ReadInt64(),
            ValueKind.UnsignedInteger => reader.ReadUInt64(),
            ValueKind.FloatingPoint => reader.ReadDouble(),
            ValueKind.Decimal => reader.ReadDecimal(),
            ValueKind.DateTime => DateTime.FromBinary(reader.ReadInt64()),
            ValueKind.DateTimeOffset => ReadDateTimeOffset(reader),
            ValueKind.Guid => new Guid(ReadExactly(reader, 16)),
            _ => throw new InvalidDataException($"Unsupported QLOG property type {(byte)kind}.")
        };
    }

    private static DateTimeOffset ReadDateTimeOffset(BinaryReader reader)
    {
        var ticks = reader.ReadInt64();
        var offset = TimeSpan.FromMinutes(reader.ReadInt16());
        return new DateTimeOffset(ticks, offset);
    }

    private static byte[] ReadExactly(BinaryReader reader, int count)
    {
        var bytes = reader.ReadBytes(count);
        return bytes.Length == count ? bytes : throw new EndOfStreamException();
    }
}

internal static class BinaryLogFormat
{
    public static readonly byte[] Magic = "QLOG"u8.ToArray();
    public const int CurrentVersion = 3;
    public const int MaximumStringBytes = 16 * 1024 * 1024;
    public const int MaximumProperties = 4096;

    public static void WriteString(BinaryWriter writer, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            writer.Write(0);
            return;
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        writer.Write(bytes.Length);
        writer.Write(bytes);
    }

    public static string ReadString(BinaryReader reader)
    {
        var length = reader.ReadInt32();
        if (length < 0 || length > MaximumStringBytes)
            throw new InvalidDataException($"QLOG string length {length} is invalid.");
        if (length == 0)
            return string.Empty;

        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
            throw new EndOfStreamException();
        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}

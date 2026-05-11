using QuickLog.Utilities;
using Xunit;

namespace QuickLog.Tests;

public sealed class Crc32Tests
{
    // Standard IEEE 802.3 CRC32 of "123456789" is 0xCBF43926
    [Fact]
    public void KnownInput_ReturnsStandardChecksum()
    {
        using var crc = new Crc32();
        Assert.Equal(0xCBF43926u, crc.CalculateChecksum("123456789"u8.ToArray()));
    }

    [Fact]
    public void EmptyInput_ReturnsZero()
    {
        using var crc = new Crc32();
        Assert.Equal(0u, crc.CalculateChecksum(Array.Empty<byte>()));
    }

    [Fact]
    public void SameInput_ProducesConsistentResult()
    {
        using var crc = new Crc32();
        var data = "QuickLog"u8.ToArray();
        Assert.Equal(crc.CalculateChecksum(data), crc.CalculateChecksum(data));
    }

    [Fact]
    public void DifferentInputs_ProduceDifferentChecksums()
    {
        using var crc = new Crc32();
        Assert.NotEqual(
            crc.CalculateChecksum("hello"u8.ToArray()),
            crc.CalculateChecksum("world"u8.ToArray()));
    }

    [Fact]
    public void ByteArray_AndStream_ProduceSameResult()
    {
        using var crc = new Crc32();
        var data = "roundtrip check"u8.ToArray();
        var fromBytes = crc.CalculateChecksum(data);
        var fromStream = crc.CalculateChecksum(new MemoryStream(data));
        Assert.Equal(fromBytes, fromStream);
    }
}

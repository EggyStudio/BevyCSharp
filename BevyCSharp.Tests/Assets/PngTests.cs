using System.Buffers.Binary;
using System.IO.Compression;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers writing a captured picture as a PNG, with its alpha, which a thumbnail on a clear
/// background needs.
/// </summary>
/// <remarks>
/// No engine, since the picture is bytes. The file is read back by hand, its header and then its
/// pixels inflated again, so the test says what the format says rather than what a decoder allows.
/// </remarks>
public sealed class PngTests
{
    [Fact]
    public void APictureIsWrittenWithItsAlphaAndReadsBackTheSame()
    {
        byte[] pixels =
        [
            255, 0, 0, 255, 0, 255, 0, 128, 0, 0, 255, 0,
            10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120,
        ];
        var png = new CapturedImage(3, 2, pixels).ToPng();

        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }, png[..8]);

        // The header: three wide, two tall, eight bits a channel, red, green, blue and alpha.
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(png, 12, 4));
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(16)));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(20)));
        Assert.Equal(8, png[24]);
        Assert.Equal(6, png[25]);

        // The header chunk's check, over its name and data, as every PNG reader verifies it.
        Assert.Equal(Crc(png.AsSpan(12, 17)), BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(29)));

        // The pixels, each row after its filter byte, as they went in.
        var at = 33;
        var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at));
        Assert.Equal("IDAT", System.Text.Encoding.ASCII.GetString(png, at + 4, 4));

        using var inflated = new MemoryStream();
        using (var unpacked = new ZLibStream(new MemoryStream(png, at + 8, length), CompressionMode.Decompress))
            unpacked.CopyTo(inflated);

        var rows = inflated.ToArray();
        Assert.Equal(2 * (1 + (3 * 4)), rows.Length);
        Assert.Equal(0, rows[0]);
        Assert.Equal(pixels[..12], rows[1..13]);
        Assert.Equal(pixels[12..], rows[14..26]);

        Assert.Equal("IEND", System.Text.Encoding.ASCII.GetString(png, png.Length - 8, 4));
    }

    /// <summary>The CRC-32 a PNG chunk is checked by, worked out the long way.</summary>
    private static uint Crc(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in data)
        {
            crc ^= value;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }

        return crc ^ 0xFFFFFFFFu;
    }
}

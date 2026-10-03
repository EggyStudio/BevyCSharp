using System.Buffers.Binary;
using System.IO.Compression;

namespace Bevy;

/// <summary>
/// A picture that was asked for and has not arrived yet.
/// </summary>
/// <remarks>
/// A capture is answered frames after it is asked for, because the picture has to come back off
/// the GPU, so what <see cref="Render.BeginCapture(AssetHandle)"/> can hand over at the time is a
/// name for the answer rather than the answer.
/// </remarks>
/// <param name="Id">What the engine knows this capture by.</param>
public readonly record struct Capture(int Id)
{
    /// <summary>True when this names a capture that was actually asked for.</summary>
    public bool IsValid => Id > 0;

    /// <inheritdoc/>
    public override string ToString() => $"capture {Id}";
}

/// <summary>
/// A picture that has come back off the GPU.
/// </summary>
/// <remarks>
/// Straight bytes rather than an engine type, because what a caller does with a picture here is
/// read a pixel out of it or hand the whole thing to something that encodes images.
/// </remarks>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Pixels">
/// Four bytes a pixel, red, green, blue then alpha, rows top to bottom.
/// </param>
public sealed record CapturedImage(uint Width, uint Height, byte[] Pixels)
{
    /// <summary>
    /// The color at a point, as four bytes.
    /// </summary>
    /// <remarks>
    /// The one thing worth having a method for, because the arithmetic is where a reader of this
    /// gets it wrong. Rows run top to bottom, and a pixel is four bytes rather than three.
    /// </remarks>
    /// <param name="x">Distance from the left, in pixels.</param>
    /// <param name="y">Distance from the top, in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the picture.</exception>
    public (byte R, byte G, byte B, byte A) At(uint x, uint y)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);

        var offset = (int)(((y * Width) + x) * 4);

        return (Pixels[offset], Pixels[offset + 1], Pixels[offset + 2], Pixels[offset + 3]);
    }

    /// <summary>The picture as a PNG file's bytes, alpha included.</summary>
    /// <remarks>
    /// <para>
    /// Written here rather than through <see cref="Render.Screenshot(string, AssetHandle)"/>, whose
    /// PNG has no alpha, so a picture drawn on a clear background keeps the background clear, as a
    /// thumbnail laid over a tile needs.
    /// </para>
    /// <para>
    /// The plainest PNG there is: eight bits a channel, red, green, blue and alpha, every row
    /// stored as it is and the whole compressed once. The bytes are the GPU's, which for an sRGB
    /// target are already the encoded colors a PNG holds.
    /// </para>
    /// </remarks>
    public byte[] ToPng()
    {
        var rows = new MemoryStream();
        using (var packed = new ZLibStream(rows, CompressionLevel.Optimal, leaveOpen: true))
        {
            var stride = (int)Width * 4;
            for (var y = 0; y < Height; y++)
            {
                // Each row starts with the filter it was stored with, which is none.
                packed.WriteByte(0);
                packed.Write(Pixels, y * stride, stride);
            }
        }

        var file = new MemoryStream();
        file.Write([0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, Width);
        BinaryPrimitives.WriteUInt32BigEndian(header[4..], Height);
        header[8] = 8; // bits a channel
        header[9] = 6; // red, green, blue and alpha
        Chunk(file, "IHDR", header);
        Chunk(file, "IDAT", rows.GetBuffer().AsSpan(0, (int)rows.Length));
        Chunk(file, "IEND", []);

        return file.ToArray();
    }

    /// <summary>Writes one chunk: its length, its name, its data and the check over the last two.</summary>
    private static void Chunk(Stream file, string name, ReadOnlySpan<byte> data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(number, (uint)data.Length);
        file.Write(number);

        Span<byte> named = stackalloc byte[4];
        for (var i = 0; i < 4; i++) named[i] = (byte)name[i];
        file.Write(named);
        file.Write(data);

        var check = Crc.Update(Crc.Update(0xFFFFFFFFu, named), data) ^ 0xFFFFFFFFu;
        BinaryPrimitives.WriteUInt32BigEndian(number, check);
        file.Write(number);
    }

    /// <summary>The CRC a PNG chunk ends with, by the table every PNG writer keeps.</summary>
    private static class Crc
    {
        private static readonly uint[] Table = Build();

        private static uint[] Build()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }

            return table;
        }

        public static uint Update(uint crc, ReadOnlySpan<byte> data)
        {
            foreach (var value in data) crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
            return crc;
        }
    }
}

/// <summary>What a render target from <see cref="Render.CreateTarget"/> holds a pixel as.</summary>
public enum TargetFormat
{
    /// <summary>Eight bits a channel, sRGB, as a screen shows it.</summary>
    Rgba8,

    /// <summary>A half float a channel, linear, with room above white.</summary>
    Rgba16Float,
}

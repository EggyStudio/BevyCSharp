using System.Buffers.Binary;
using System.IO.Compression;

namespace Bevy;

/// <summary>Writes PNG files, a still picture or an animated one, in managed code with nothing taken for it.</summary>
/// <remarks>
/// <para>
/// Eight bits a channel, red, green, blue and alpha, the rows compressed by .NET's own deflate. A
/// still picture keeps every row as it is, as <see cref="CapturedImage.ToPng"/> writes it. An
/// animated one filters each row the way that leaves it smallest, as a PNG writer's adaptive
/// filter does, since a recording of many frames is the file a page carries.
/// </para>
/// <para>
/// An animated PNG is a PNG whose first frame is its picture, so a reader that knows nothing of
/// animation shows the first frame, and whose other frames follow in chunks of their own, each
/// with its delay, as browsers and GitHub's pages play them. A frame after the first holds the
/// rectangle that changed since the frame before, laid over it, so a still part of a recording
/// costs nothing after its first frame.
/// </para>
/// </remarks>
internal static class PngWriter
{
    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>A still picture, every row stored unfiltered.</summary>
    public static byte[] Still(uint width, uint height, byte[] pixels)
    {
        var file = new MemoryStream();
        file.Write(Signature);
        Chunk(file, "IHDR", Header(width, height));
        Chunk(file, "IDAT", Rows(new RgbaFrame((int)width, (int)height, pixels), (0, 0, (int)width, (int)height), filtered: false));
        Chunk(file, "IEND", []);
        return file.ToArray();
    }

    /// <summary>Writes an animated PNG of frames of one size, each shown for its delay.</summary>
    /// <param name="output">Where the file goes.</param>
    /// <param name="frames">The frames, all the size of the first.</param>
    /// <param name="delays">How long each is shown, in milliseconds, up to a minute.</param>
    /// <param name="loops">How many times it plays, zero for forever.</param>
    public static void Animated(Stream output, IReadOnlyList<RgbaFrame> frames, IReadOnlyList<int> delays, int loops = 0)
    {
        if (frames.Count == 0) throw new ArgumentException("An animation has a frame at least.", nameof(frames));

        var width = frames[0].Width;
        var height = frames[0].Height;
        output.Write(Signature);
        Chunk(output, "IHDR", Header((uint)width, (uint)height));

        Span<byte> control = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(control, (uint)frames.Count);
        BinaryPrimitives.WriteUInt32BigEndian(control[4..], (uint)loops);
        Chunk(output, "acTL", control);

        // Frame controls and frame data are numbered together, from zero, so a reader finds a
        // chunk missing or out of order.
        var sequence = 0u;
        Span<byte> frameControl = stackalloc byte[26];

        for (var i = 0; i < frames.Count; i++)
        {
            var frame = frames[i];
            var area = i == 0 ? (0, 0, width, height) : frames[i].ChangedFrom(frames[i - 1]);

            // A frame like the one before still takes its time, as one pixel laid over itself.
            if (area.Item3 == 0) area = (0, 0, 1, 1);

            BinaryPrimitives.WriteUInt32BigEndian(frameControl, sequence++);
            BinaryPrimitives.WriteUInt32BigEndian(frameControl[4..], (uint)area.Item3);
            BinaryPrimitives.WriteUInt32BigEndian(frameControl[8..], (uint)area.Item4);
            BinaryPrimitives.WriteUInt32BigEndian(frameControl[12..], (uint)area.Item1);
            BinaryPrimitives.WriteUInt32BigEndian(frameControl[16..], (uint)area.Item2);
            BinaryPrimitives.WriteUInt16BigEndian(frameControl[20..], (ushort)Math.Clamp(delays[i], 0, 60_000));
            BinaryPrimitives.WriteUInt16BigEndian(frameControl[22..], 1000);
            frameControl[24] = 0; // left as it is for the next frame
            frameControl[25] = 0; // written over what is there, not blended
            Chunk(output, "fcTL", frameControl);

            var rows = Rows(frame, area, filtered: true);
            if (i == 0)
            {
                Chunk(output, "IDAT", rows);
                continue;
            }

            var data = new byte[rows.Length + 4];
            BinaryPrimitives.WriteUInt32BigEndian(data, sequence++);
            rows.CopyTo(data, 4);
            Chunk(output, "fdAT", data);
        }

        Chunk(output, "IEND", []);
    }

    private static byte[] Header(uint width, uint height)
    {
        var header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8; // bits a channel
        header[9] = 6; // red, green, blue and alpha
        return header;
    }

    /// <summary>A rectangle of a frame's rows, each led by its filter, compressed.</summary>
    private static byte[] Rows(RgbaFrame frame, (int X, int Y, int Width, int Height) area, bool filtered)
    {
        var stride = area.Width * 4;
        var rows = new MemoryStream();

        using (var packed = new ZLibStream(rows, CompressionLevel.Optimal, leaveOpen: true))
        {
            var above = new byte[stride];
            var row = new byte[stride];
            var best = new byte[stride];
            var trial = new byte[stride];

            for (var y = 0; y < area.Height; y++)
            {
                Array.Copy(frame.Pixels, ((area.Y + y) * frame.Width + area.X) * 4, row, 0, stride);

                if (!filtered)
                {
                    packed.WriteByte(0);
                    packed.Write(row);
                    continue;
                }

                // Each of the five filters tried, and the one whose bytes, read as signed, sum
                // smallest kept, which is the measure the format's own writers use.
                var chosen = 0;
                var smallest = long.MaxValue;
                for (var filter = 0; filter < 5; filter++)
                {
                    long sum = 0;
                    for (var i = 0; i < stride; i++)
                    {
                        var left = i >= 4 ? row[i - 4] : 0;
                        var up = above[i];
                        var corner = i >= 4 ? above[i - 4] : 0;
                        var value = (byte)(row[i] - filter switch
                        {
                            1 => left,
                            2 => up,
                            3 => (left + up) / 2,
                            4 => Paeth(left, up, corner),
                            _ => 0,
                        });
                        trial[i] = value;
                        sum += Math.Abs((int)(sbyte)value);
                    }

                    if (sum >= smallest) continue;
                    smallest = sum;
                    chosen = filter;
                    (best, trial) = (trial, best);
                }

                packed.WriteByte((byte)chosen);
                packed.Write(best);
                (above, row) = (row, above);
            }
        }

        return rows.ToArray();
    }

    /// <summary>The neighbor nearest a guess made from three, the left, the one above and the one above left.</summary>
    internal static int Paeth(int left, int up, int corner)
    {
        var guess = left + up - corner;
        var toLeft = Math.Abs(guess - left);
        var toUp = Math.Abs(guess - up);
        var toCorner = Math.Abs(guess - corner);

        if (toLeft <= toUp && toLeft <= toCorner) return left;
        return toUp <= toCorner ? up : corner;
    }

    /// <summary>Writes one chunk, its length, its name, its data and the check over the last two.</summary>
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
    internal static class Crc
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

using System.Numerics;
using Bevy.Interop;

namespace Bevy;

// An image made in code with its smaller levels, its mipmaps, made here once from its pixels.
public static unsafe partial class Render
{
    /// <summary>
    /// Makes an image out of pixels held here, with its mipmaps made from them, and hands back a
    /// handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a texture worked out in code that is drawn small or far away, such as a grid on a floor
    /// running into the distance, which drawn from its full size alone shimmers as its lines fall
    /// between the pixels sampled. Each level is half the one above, down to a pixel, each of its
    /// pixels the mean of the four above it, so a texture of 1024 pixels a side has eleven.
    /// </para>
    /// <para>
    /// The mean of colors read as sRGB is taken as light, the colors decoded, averaged and encoded
    /// again, so a thin dark line on a light face fades into the face's tone in the distance as the
    /// eye sees it, rather than darkening the face as a mean of the encoded numbers would. The levels
    /// are made here once and given to the GPU as they are, where
    /// <see cref="SetMipGeneration"/> makes them on the GPU every frame for an image drawn into.
    /// </para>
    /// </remarks>
    /// <param name="pixels">As for <see cref="CreateImage(ReadOnlySpan{byte}, uint, uint, bool)"/>, the full size alone.</param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="srgb">Whether the numbers are a color, averaged as light, or read as they are.</param>
    /// <param name="mipmaps">Whether the levels are made, which false leaves the image as the overload without it makes it.</param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="ArgumentException"><paramref name="pixels"/> is not four bytes per pixel of the size given.</exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateImage(ReadOnlySpan<byte> pixels, uint width, uint height, bool srgb, bool mipmaps)
    {
        if (!mipmaps) return CreateImage(pixels, width, height, srgb);

        if (width == 0 || height == 0)
            throw new ArgumentException("An image needs a width and a height.", nameof(width));

        if (pixels.Length != (long)width * height * 4)
            throw new ArgumentException($"A {width}x{height} image is {(long)width * height * 4} bytes of RGBA, and {pixels.Length} were given.", nameof(pixels));

        var chain = MipChain(pixels, width, height, srgb, out var levels);
        int key;
        fixed (byte* at = chain)
        {
            key = Native.bcs_render_create_image_levels(at, width, height, srgb ? 1 : 0, levels);
        }

        if (key < 0) throw NoRenderer($"Creating a {width}x{height} image with its mipmaps");

        return new AssetHandle(key);
    }

    /// <summary>
    /// Makes an image from its levels made here, the full size first and each smaller level after
    /// it, half the one above and a pixel at least, as <see cref="MipChain"/> lays them out, for an
    /// image whose levels are made otherwise than by a mean, as a tree's cards keep each card's
    /// leaves in its levels.
    /// </summary>
    internal static AssetHandle CreateImageLevels(ReadOnlySpan<byte> chain, uint width, uint height, bool srgb, uint levels)
    {
        int key;
        fixed (byte* at = chain)
        {
            key = Native.bcs_render_create_image_levels(at, width, height, srgb ? 1 : 0, levels);
        }

        if (key < 0) throw NoRenderer($"Creating a {width}x{height} image with its levels");

        return new AssetHandle(key);
    }

    /// <summary>
    /// An image's pixels with every smaller level after them, each half the one above and a pixel
    /// at least, each of its pixels the mean of those above it, as light where they are sRGB.
    /// </summary>
    internal static byte[] MipChain(ReadOnlySpan<byte> pixels, uint width, uint height, bool srgb, out uint levels)
    {
        levels = (uint)BitOperations.Log2(Math.Max(width, height)) + 1;

        var total = 0L;
        for (var level = 0; level < levels; level++) total += (long)Math.Max(1u, width >> level) * Math.Max(1u, height >> level) * 4;

        var chain = new byte[total];
        pixels.CopyTo(chain);

        // Decoded once a value, so a level's mean is of light, and encoded again by the nearest of
        // the 256 values, which is exact for every byte a mean can come back as.
        var decode = new float[256];
        for (var value = 0; value < 256; value++)
        {
            var unit = value / 255f;
            decode[value] = !srgb ? unit : unit <= 0.04045f ? unit / 12.92f : MathF.Pow((unit + 0.055f) / 1.055f, 2.4f);
        }

        var from = 0L;
        var to = (long)width * height * 4;
        var (aboveWidth, aboveHeight) = (width, height);

        for (var level = 1; level < levels; level++)
        {
            var (levelWidth, levelHeight) = (Math.Max(1u, aboveWidth >> 1), Math.Max(1u, aboveHeight >> 1));
            for (var y = 0u; y < levelHeight; y++)
            {
                for (var x = 0u; x < levelWidth; x++)
                {
                    // The two by two above, or the one row or column there is where the level above
                    // was a pixel thin that way.
                    var (left, right) = (Math.Min(x * 2, aboveWidth - 1), Math.Min(x * 2 + 1, aboveWidth - 1));
                    var (top, bottom) = (Math.Min(y * 2, aboveHeight - 1), Math.Min(y * 2 + 1, aboveHeight - 1));
                    var at = to + (y * levelWidth + x) * 4;

                    for (var channel = 0; channel < 4; channel++)
                    {
                        var sum = 0f;
                        foreach (var (sx, sy) in (ReadOnlySpan<(uint, uint)>)[(left, top), (right, top), (left, bottom), (right, bottom)])
                        {
                            var value = chain[from + (sy * aboveWidth + sx) * 4 + channel];
                            sum += channel == 3 ? value / 255f : decode[value];
                        }

                        var mean = sum / 4f;
                        chain[at + channel] = channel == 3 ? (byte)MathF.Round(mean * 255f) : Encode(mean, decode);
                    }
                }
            }

            from = to;
            to += (long)levelWidth * levelHeight * 4;
            (aboveWidth, aboveHeight) = (levelWidth, levelHeight);
        }

        return chain;

        static byte Encode(float light, float[] decode)
        {
            var index = Array.BinarySearch(decode, light);
            if (index >= 0) return (byte)index;

            var above = ~index;
            if (above == 0) return 0;
            if (above == decode.Length) return 255;
            return (byte)(light - decode[above - 1] <= decode[above] - light ? above - 1 : above);
        }
    }
}

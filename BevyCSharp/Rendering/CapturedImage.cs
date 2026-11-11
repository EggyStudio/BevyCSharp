namespace Bevy;

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
    /// The plainest PNG there is, eight bits a channel, red, green, blue and alpha, every row
    /// stored as it is and the whole compressed once. The bytes are the GPU's, which for an sRGB
    /// target are already the encoded colors a PNG holds.
    /// </para>
    /// </remarks>
    public byte[] ToPng() => PngWriter.Still(Width, Height, Pixels);
}

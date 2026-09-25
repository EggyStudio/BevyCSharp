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
}

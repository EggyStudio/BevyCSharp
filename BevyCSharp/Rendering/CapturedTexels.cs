using System.Buffers.Binary;
using System.IO.Compression;

namespace Bevy;

/// <summary>
/// A picture that has come back off the GPU in the format it was drawn in.
/// </summary>
/// <remarks>
/// What <see cref="Render.TryReadCaptureAsItIs"/> hands over, where <see cref="CapturedImage"/> is
/// the same picture as eight-bit color. The bytes are the GPU's, rows top to bottom with no padding,
/// so a half-float target keeps light brighter than white.
/// </remarks>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
/// <param name="Format">What each pixel is.</param>
/// <param name="Bytes">The pixels, little-endian.</param>
public sealed record CapturedTexels(uint Width, uint Height, ShaderImageFormat Format, byte[] Bytes)
{
    /// <summary>
    /// The color at a point as four floats, linear for a float format and the stored value
    /// divided by 255 for an eight-bit one.
    /// </summary>
    /// <remarks>
    /// An eight-bit picture's bytes are encoded colors, so the floats for one are the encoded
    /// values rather than light, the same as <see cref="CapturedImage.At"/> reads, over 255.
    /// </remarks>
    /// <param name="x">Distance from the left, in pixels.</param>
    /// <param name="y">Distance from the top, in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">The point is outside the picture.</exception>
    /// <exception cref="NotSupportedException">The format is not four channels of color.</exception>
    public System.Numerics.Vector4 ColorAt(uint x, uint y)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(x, Width);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(y, Height);

        var pixel = (int)((y * Width) + x);

        switch (Format)
        {
            case ShaderImageFormat.Rgba8:
            {
                var at = Bytes.AsSpan(pixel * 4, 4);
                return new System.Numerics.Vector4(at[0], at[1], at[2], at[3]) / 255f;
            }
            case ShaderImageFormat.Rgba16Float:
            {
                var at = Bytes.AsSpan(pixel * 8, 8);
                return new System.Numerics.Vector4(
                    (float)BinaryPrimitives.ReadHalfLittleEndian(at),
                    (float)BinaryPrimitives.ReadHalfLittleEndian(at[2..]),
                    (float)BinaryPrimitives.ReadHalfLittleEndian(at[4..]),
                    (float)BinaryPrimitives.ReadHalfLittleEndian(at[6..]));
            }
            case ShaderImageFormat.Rgba32Float:
            {
                var at = Bytes.AsSpan(pixel * 16, 16);
                return new System.Numerics.Vector4(
                    BinaryPrimitives.ReadSingleLittleEndian(at),
                    BinaryPrimitives.ReadSingleLittleEndian(at[4..]),
                    BinaryPrimitives.ReadSingleLittleEndian(at[8..]),
                    BinaryPrimitives.ReadSingleLittleEndian(at[12..]));
            }
            default:
                throw new NotSupportedException($"A {Format} picture is not four channels of color; read its bytes instead.");
        }
    }
}

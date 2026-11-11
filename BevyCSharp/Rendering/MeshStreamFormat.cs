namespace Bevy;

/// <summary>What each vertex of a <see cref="MeshStream"/> holds.</summary>
/// <remarks>
/// The Slang type the vertex shader declares the stream as must be of the same class, an integer for
/// <see cref="UInt"/> and <see cref="UInt2"/> and a float for the rest, or wgpu refuses the
/// pipeline, which is logged and draws nothing.
/// </remarks>
public enum MeshStreamFormat
{
    /// <summary>One <c>uint</c> a vertex, four bytes, for a vertex packed into four bytes.</summary>
    UInt = 0,

    /// <summary>Two, a <c>uint2</c> of eight bytes, for a packed vertex four bytes do not hold.</summary>
    UInt2 = 1,

    /// <summary>One float a vertex, a <c>float</c>.</summary>
    Float = 2,

    /// <summary>Four floats a vertex, a <c>float4</c>.</summary>
    Float4 = 3,

    /// <summary>Four bytes a vertex read as numbers from 0 to 1, a <c>float4</c>, as a color is.</summary>
    UNorm8x4 = 4,
}

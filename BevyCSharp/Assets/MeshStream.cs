using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A vertex stream of the game's own, a value each vertex carries beside its position and the
/// attributes Bevy names, read by a vertex shader of the game's at <see cref="Location"/>.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's <c>MeshVertexAttribute</c>, as its <c>custom_vertex_attribute</c> example declares one, for
/// what a vertex holds that no attribute of Bevy's says: a voxel's face and texture packed into four
/// bytes and unpacked in the shader, a blend weight, a wind phase. The vertex shader declares the
/// input in its own vertex struct at the location's <c>TEXCOORDn</c>, <c>TEXCOORD8</c> for location 8,
/// as every input here is declared by location, so the game writes one number twice and the two
/// agree. Slang's <c>vk::location</c> is not read.
/// </para>
/// <para>
/// A location is from 8 to 15, since Bevy's mesh pipeline places its own attributes at 0 to 5 and a
/// skin's at 6 and 7, and wgpu allows sixteen. The name is the stream's for the running app, so
/// every mesh carrying a stream of one name carries it in one format at one location, and a
/// material's pipeline made for one such mesh suits the next. A name given again in another format
/// or at another location is refused, as two streams of one mesh at one location are.
/// </para>
/// </remarks>
/// <param name="Name">What the stream is called, the same for every mesh that carries it.</param>
/// <param name="Format">What each vertex holds.</param>
/// <param name="Location">Where the vertex shader reads it, from 8 to 15.</param>
/// <param name="Data">The format's bytes for each vertex in turn, as <see cref="From{T}"/> lays them out.</param>
public sealed record MeshStream(string Name, MeshStreamFormat Format, uint Location, byte[] Data)
{
    /// <summary>The first location a stream may take.</summary>
    public const uint FirstLocation = 8;

    /// <summary>The last location a stream may take.</summary>
    public const uint LastLocation = 15;

    /// <summary>
    /// A stream from values laid out as the format's bytes, such as a <c>uint</c> a vertex for
    /// <see cref="MeshStreamFormat.UInt"/> or a <see cref="System.Numerics.Vector4"/> a vertex for
    /// <see cref="MeshStreamFormat.Float4"/>.
    /// </summary>
    /// <exception cref="ArgumentException">The values are not a whole number of the format's vertices.</exception>
    public static MeshStream From<T>(string name, MeshStreamFormat format, uint location, ReadOnlySpan<T> values) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(values).ToArray();
        if (bytes.Length % SizeOf(format) != 0)
        {
            throw new ArgumentException(
                $"{bytes.Length} bytes are not a whole number of {format} vertices, which are {SizeOf(format)} bytes each.",
                nameof(values));
        }

        return new MeshStream(name, format, location, bytes);
    }

    /// <summary>Bytes a vertex of <paramref name="format"/>.</summary>
    public static int SizeOf(MeshStreamFormat format) => format switch
    {
        MeshStreamFormat.UInt2 => 8,
        MeshStreamFormat.Float4 => 16,
        _ => 4,
    };
}

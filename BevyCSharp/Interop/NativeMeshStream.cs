namespace Bevy.Interop;

/// <summary>One stream of a mesh. Mirrors <c>BcsMeshStream</c>.</summary>
public unsafe struct NativeMeshStream
{
    /// <summary>The name's UTF-8 bytes, with no terminator.</summary>
    public byte* Name;

    /// <summary>How many bytes the name is.</summary>
    public int NameLength;

    /// <summary>The <see cref="MeshStreamFormat"/>.</summary>
    public int Format;

    /// <summary>Where the vertex shader reads it.</summary>
    public uint Location;

    /// <summary>The format's bytes for each vertex.</summary>
    public byte* Data;
}

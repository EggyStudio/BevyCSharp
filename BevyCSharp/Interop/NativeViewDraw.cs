namespace Bevy.Interop;

/// <summary>One draw a camera makes every frame. Mirrors <c>BcsViewDraw</c>.</summary>
public unsafe struct NativeViewDraw
{
    /// <summary>The shader instance.</summary>
    public int Instance;

    /// <summary>A <see cref="FramePoint"/>.</summary>
    public int Point;

    /// <summary>0 a fixed count, 1 counts read from a buffer, 2 workgroups of mesh shaders.</summary>
    public int Mode;

    /// <summary>How many vertices, for mode 0.</summary>
    public uint Vertices;

    /// <summary>How many instances, for mode 0.</summary>
    public uint Instances;

    /// <summary>The buffer holding the counts, for mode 1.</summary>
    public int Buffer;

    /// <summary>The byte offset of the counts in it.</summary>
    public uint Offset;

    /// <summary>A <see cref="DrawBlend"/>.</summary>
    public int Blend;

    /// <summary>One to write depth, and two to cast shadows as well.</summary>
    public int DepthWrite;

    /// <summary>NUL-terminated UTF-8 naming a camera image to draw into, or null for the picture.</summary>
    public byte* Target;

    /// <summary>Workgroups of mesh shaders across, for mode 2.</summary>
    public uint GroupsX;

    /// <summary>Down.</summary>
    public uint GroupsY;

    /// <summary>Deep.</summary>
    public uint GroupsZ;
}

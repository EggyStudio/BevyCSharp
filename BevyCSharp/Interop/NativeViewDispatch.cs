namespace Bevy.Interop;

/// <summary>One dispatch a camera runs every frame. Mirrors <c>BcsViewDispatch</c>.</summary>
public unsafe struct NativeViewDispatch
{
    /// <summary>The shader instance.</summary>
    public int Instance;

    /// <summary>A <see cref="FramePoint"/>.</summary>
    public int Point;

    /// <summary>0 counted from the picture, 1 fixed, 2 read from a buffer.</summary>
    public int Mode;

    /// <summary>The workgroup's size in pixels, or the workgroups themselves.</summary>
    public fixed uint Groups[3];

    /// <summary>The fraction of the picture covered.</summary>
    public float Scale;

    /// <summary>The buffer holding the counts, for mode 2.</summary>
    public int Buffer;

    /// <summary>The byte offset of the counts in it.</summary>
    public uint Offset;
}

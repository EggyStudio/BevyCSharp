namespace Bevy.Interop;

/// <summary>One image a camera owns. Mirrors <c>BcsViewImage</c>.</summary>
public unsafe struct NativeViewImage
{
    /// <summary>NUL-terminated UTF-8.</summary>
    public byte* Name;

    /// <summary>A <see cref="ShaderImageFormat"/>.</summary>
    public int Format;

    /// <summary>A fraction of the picture's size.</summary>
    public float Scale;

    /// <summary>Non-zero to keep last frame's as well.</summary>
    public int History;

    /// <summary>How many mip levels.</summary>
    public int Mips;

    /// <summary>The <see cref="FramePoint"/> the picture is copied in at, or -1 for none.</summary>
    public int Copy;

    /// <summary>Non-zero to clear it at the start of every frame.</summary>
    public int Clear;
}

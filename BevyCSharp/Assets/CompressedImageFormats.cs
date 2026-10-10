namespace Bevy;

/// <summary>The compressed texture formats a device decodes, as <see cref="Render.CompressedFormats"/> reports them.</summary>
/// <remarks>Bevy's <c>CompressedImageFormats</c>, bit for bit.</remarks>
[Flags]
public enum CompressedImageFormats
{
    /// <summary>None of them, as on a run with no renderer.</summary>
    None = 0,

    /// <summary>ASTC, the format of most phones and of Apple's GPUs.</summary>
    Astc = 1,

    /// <summary>ASTC's high dynamic range form, for a sky or a light probe kept compressed.</summary>
    AstcHdr = 2,

    /// <summary>The BC formats, BC1 to BC7, which desktop GPUs decode.</summary>
    Bc = 4,

    /// <summary>ETC2, which phones with OpenGL ES 3 or Vulkan decode.</summary>
    Etc2 = 8,
}

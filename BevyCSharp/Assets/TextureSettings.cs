namespace Bevy;

/// <summary>
/// How an image should be sampled, and how its bytes should be read.
/// </summary>
/// <remarks>
/// <para>
/// The defaults match Bevy's, clamped, nearest-filtered and read as sRGB. Both are worth changing
/// for most textures, which is why this exists.
/// </para>
/// <para>
/// A texture whose three filters are all <see cref="TextureFilter.Linear"/> is filtered
/// anisotropically at sixteen samples, the device's most where it offers fewer, so a floor or a
/// wall seen at a grazing angle reads sharp to its far end, at a small cost and at every quality.
/// One with any filter <see cref="TextureFilter.Nearest"/> takes one sample, since the graphics API
/// refuses anisotropy beside a nearest filter as a validation failure rather than overlooking it.
/// </para>
/// </remarks>
public sealed class TextureSettings
{
    /// <summary>What happens past the edge, in both directions.</summary>
    public TextureWrap Wrap { get; set; } = TextureWrap.Clamp;

    /// <summary>Filtering when the texture is drawn larger than it is.</summary>
    public TextureFilter MagFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>Filtering when it is drawn smaller.</summary>
    public TextureFilter MinFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>Filtering between mip levels.</summary>
    public TextureFilter MipmapFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>
    /// Whether the file holds color, in which case it is read as sRGB.
    /// </summary>
    /// <remarks>
    /// Set false for a texture whose bytes are data rather than color, such as a normal map, a
    /// metallic-roughness map, an occlusion map. Reading one as sRGB bends every value in it.
    /// </remarks>
    public bool Srgb { get; set; } = true;

    /// <summary>
    /// How many layers the file is cut into, from the top down, which loads it as an array
    /// texture. Zero or one loads it as one picture.
    /// </summary>
    /// <remarks>
    /// Several pictures of one size stacked in one file, a tile set or the frames of a terrain's
    /// blend, read by a shader as a <c>Texture2DArray</c> and picked by layer, often by
    /// <c>meshTag</c>. The file's height has to divide into this many equal rows, or the load
    /// fails and says so on the log.
    /// </remarks>
    public uint Layers { get; set; }

    /// <summary>
    /// The most samples a texture seen at a slant is filtered with, as the bridge's
    /// <c>TEXTURE_ANISOTROPY</c> gives Bevy's default sampler and a glTF file's textures, the most
    /// the graphics API takes, and a device that offers fewer takes its own most.
    /// </summary>
    internal const uint SlantSamples = 16;

    /// <summary>
    /// The samples the texture is filtered with at a slant, <see cref="SlantSamples"/> where its
    /// three filters are linear and one where any is not.
    /// </summary>
    internal uint Anisotropy =>
        MagFilter == TextureFilter.Linear && MinFilter == TextureFilter.Linear && MipmapFilter == TextureFilter.Linear
            ? SlantSamples
            : 1;

    /// <summary>Repeat filtering, for a texture meant to tile across a large surface.</summary>
    public static TextureSettings Tiling => new()
    {
        Wrap = TextureWrap.Repeat,
        MagFilter = TextureFilter.Linear,
        MinFilter = TextureFilter.Linear,
        MipmapFilter = TextureFilter.Linear,
    };

    /// <summary>Linear filtering with no color conversion, for a normal or roughness map.</summary>
    public static TextureSettings Data => new()
    {
        MagFilter = TextureFilter.Linear,
        MinFilter = TextureFilter.Linear,
        MipmapFilter = TextureFilter.Linear,
        Srgb = false,
    };
}

namespace Bevy;

public sealed class SpriteSettings
{
    /// <summary>Tint, multiplied with the image. White leaves it unchanged.</summary>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>
    /// Width and height in world units, or null to use the image's own dimensions.
    /// </summary>
    public (float Width, float Height)? Size { get; set; }

    /// <summary>
    /// The part of the image to draw, in pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// For a sprite sheet, one image holding many frames, each drawn by naming its
    /// rectangle rather than by loading a separate file.
    /// </remarks>
    public (float Left, float Top, float Right, float Bottom)? Rect { get; set; }

    /// <summary>Mirror horizontally, which is how one walk cycle faces both ways.</summary>
    public bool FlipX { get; set; }

    /// <summary>Mirror vertically.</summary>
    public bool FlipY { get; set; }

    /// <summary>
    /// The atlas layout naming the frames of a sheet, or none to draw the whole image.
    /// </summary>
    /// <remarks>
    /// Built by <see cref="Render2d.CreateAtlas"/>. With one of these a frame is named by
    /// <see cref="Frame"/> rather than by its pixel rectangle, so stepping an animation is
    /// counting rather than arithmetic.
    /// </remarks>
    public AssetHandle Atlas { get; set; } = AssetHandle.None;

    /// <summary>Which frame of <see cref="Atlas"/> to draw, counting across then down.</summary>
    public uint Frame { get; set; }

    /// <summary>
    /// Where the transform sits on the sprite, or null to leave it centered.
    /// </summary>
    /// <remarks><see cref="SpriteAnchor"/> names the nine usual points.</remarks>
    public (float X, float Y)? Anchor { get; set; }

    /// <summary>How the picture meets the size the sprite is drawn at.</summary>
    public SpriteImageMode Mode { get; set; } = SpriteImageMode.Auto;

    /// <summary>How a scaled picture is fitted.</summary>
    /// <remarks>Read only when <see cref="Mode"/> is <see cref="SpriteImageMode.Scaled"/>.</remarks>
    public SpriteScaling Scaling { get; set; } = SpriteScaling.FitCenter;

    /// <summary>
    /// How far in from each edge the nine-slice cuts are, in pixels of the source image.
    /// </summary>
    /// <remarks>Read only when <see cref="Mode"/> is <see cref="SpriteImageMode.Sliced"/>.</remarks>
    public (float Left, float Top, float Right, float Bottom) SliceBorder { get; set; }

    /// <summary>How far a sliced corner may be scaled up.</summary>
    public float CornerScale { get; set; } = 1f;

    /// <summary>Repeat horizontally when tiled.</summary>
    public bool TileX { get; set; } = true;

    /// <summary>Repeat vertically when tiled.</summary>
    public bool TileY { get; set; } = true;

    /// <summary>
    /// How far the picture is drawn before a tile repeats, as a multiple of its own size.
    /// </summary>
    public float TileStretch { get; set; } = 1f;

    /// <summary>Which parts of a sliced picture tile rather than stretch.</summary>
    /// <remarks>
    /// Read only when <see cref="Mode"/> is sliced. The repeat is measured by
    /// <see cref="TileStretch"/>, the same number a whole tiled picture uses, since both answer how
    /// much of the source is laid down before it starts again.
    /// </remarks>
    public SliceTiling SliceTiling { get; set; } = SliceTiling.None;
}

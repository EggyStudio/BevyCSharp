namespace Bevy;

/// <summary>
/// How a sprite is drawn.
/// </summary>
/// <remarks>
/// A sprite is a picture in the world rather than on the screen: it carries a
/// <see cref="Transform"/> like anything else, and a 2D camera decides what a world unit is worth
/// in pixels. For something pinned to the screen, use <see cref="Ui"/>.
/// </remarks>
/// <summary>
/// How a sprite's picture meets the size it is drawn at.
/// </summary>
public enum SpriteImageMode
{
    /// <summary>
    /// The picture's own size, stretched to <see cref="SpriteSettings.Size"/> when one is given.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Cut into nine, so the corners keep their size while the middle stretches.
    /// </summary>
    /// <remarks>A health bar or a dialogue box drawn at any width from one small image.</remarks>
    Sliced = 1,

    /// <summary>Repeated across the sprite rather than stretched.</summary>
    Tiled = 2,
}

/// <summary>
/// Where a sprite's transform sits on the picture.
/// </summary>
/// <remarks>
/// A sprite is centred on its transform unless told otherwise, which is awkward for anything
/// standing on the ground: its feet are then half a sprite below where it was placed. The
/// coordinates run from <c>-0.5</c> to <c>0.5</c> on each axis, with y upwards, so any point in
/// between is expressible as well as the nine named here.
/// </remarks>
public static class SpriteAnchor
{
    /// <summary>The middle, which is Bevy's own default.</summary>
    public static (float X, float Y) Center => (0f, 0f);

    /// <summary>The bottom left corner.</summary>
    public static (float X, float Y) BottomLeft => (-0.5f, -0.5f);

    /// <summary>The middle of the bottom edge, for anything standing on the ground.</summary>
    public static (float X, float Y) BottomCenter => (0f, -0.5f);

    /// <summary>The bottom right corner.</summary>
    public static (float X, float Y) BottomRight => (0.5f, -0.5f);

    /// <summary>The middle of the left edge.</summary>
    public static (float X, float Y) CenterLeft => (-0.5f, 0f);

    /// <summary>The middle of the right edge.</summary>
    public static (float X, float Y) CenterRight => (0.5f, 0f);

    /// <summary>The top left corner.</summary>
    public static (float X, float Y) TopLeft => (-0.5f, 0.5f);

    /// <summary>The middle of the top edge.</summary>
    public static (float X, float Y) TopCenter => (0f, 0.5f);

    /// <summary>The top right corner.</summary>
    public static (float X, float Y) TopRight => (0.5f, 0.5f);
}

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
    /// What a sprite sheet needs: one image holding many frames, each drawn by naming its
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
    /// Where the transform sits on the sprite, or null to leave it centred.
    /// </summary>
    /// <remarks><see cref="SpriteAnchor"/> names the nine usual points.</remarks>
    public (float X, float Y)? Anchor { get; set; }

    /// <summary>How the picture meets the size the sprite is drawn at.</summary>
    public SpriteImageMode Mode { get; set; } = SpriteImageMode.Auto;

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
}

namespace Bevy;

/// <summary>
/// How a picture meets the size of the node holding it.
/// </summary>
public enum UiImageMode
{
    /// <summary>
    /// The picture keeps its own size, and a node with no size of its own takes it.
    /// </summary>
    Auto = 0,

    /// <summary>Stretched to the node, ignoring the picture's proportions.</summary>
    Stretch = 1,

    /// <summary>
    /// Cut into nine, so the corners keep their size while the middle stretches.
    /// </summary>
    /// <remarks>What a panel or a bar that resizes is drawn with. The corners are
    /// <see cref="UiImageSettings.SliceBorder"/> pixels of the source image.</remarks>
    Sliced = 2,

    /// <summary>Repeated across the node rather than stretched.</summary>
    Tiled = 3,
}

/// <summary>
/// The picture a UI node draws inside itself.
/// </summary>
/// <remarks>
/// The node's own settings say where it is and how large; this says what fills it. The two are
/// separate calls because the layout is one decision and its contents another.
/// </remarks>
public sealed class UiImageSettings
{
    /// <summary>The image to draw.</summary>
    public AssetHandle Image { get; set; } = AssetHandle.None;

    /// <summary>Tint, multiplied with the image. White leaves it unchanged.</summary>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>
    /// The part of the image to draw, in pixels, or null for all of it.
    /// </summary>
    /// <remarks>
    /// What an icon sheet needs: one image holding many icons, each drawn by naming its
    /// rectangle rather than by loading a separate file.
    /// </remarks>
    public (float Left, float Top, float Right, float Bottom)? Rect { get; set; }

    /// <summary>Mirror horizontally.</summary>
    public bool FlipX { get; set; }

    /// <summary>Mirror vertically.</summary>
    public bool FlipY { get; set; }

    /// <summary>How the picture meets the node's size.</summary>
    public UiImageMode Mode { get; set; } = UiImageMode.Auto;

    /// <summary>
    /// How far in from each edge the nine-slice cuts are, in pixels of the source image.
    /// </summary>
    /// <remarks>Read only when <see cref="Mode"/> is <see cref="UiImageMode.Sliced"/>.</remarks>
    public (float Left, float Top, float Right, float Bottom) SliceBorder { get; set; }

    /// <summary>How far a sliced corner may be scaled up.</summary>
    /// <remarks>One keeps the corners at their own size, which is usually what a panel wants.</remarks>
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

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
    /// A layout that cuts the image into frames, or none to draw the whole picture.
    /// </summary>
    /// <remarks>
    /// The same layout a sprite is cut by, from <see cref="Render2d.CreateAtlas"/>, so one sheet
    /// of icons serves the world and the interface. <see cref="Frame"/> says which one to draw,
    /// which is what an icon named by number rather than by pixel rectangle wants.
    /// </remarks>
    public AssetHandle Atlas { get; set; } = AssetHandle.None;

    /// <summary>Which frame of <see cref="Atlas"/> to draw, counted from zero.</summary>
    public uint Frame { get; set; }

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

    /// <summary>Which parts of a sliced picture tile rather than stretch.</summary>
    /// <remarks>
    /// Read only when <see cref="Mode"/> is sliced. The repeat is measured by
    /// <see cref="TileStretch"/>, the same number a whole tiled picture uses, since both answer how
    /// much of the source is laid down before it starts again.
    /// </remarks>
    public SliceTiling SliceTiling { get; set; } = SliceTiling.None;
}

/// <summary>
/// Which parts of a sliced picture tile rather than stretch.
/// </summary>
/// <remarks>
/// A nine-slice stretches by default, which is wrong for anything with a pattern in it, because a
/// border of dots drawn twice as wide becomes a border of ovals. Tiling repeats the slice instead,
/// which is what keeps a drawn edge looking drawn at every size. The repeat is measured by the same
/// <c>TileStretch</c> a whole tiled picture uses.
/// </remarks>
[Flags]
public enum SliceTiling
{
    /// <summary>Every part stretches, which is Bevy's own answer.</summary>
    None = 0,

    /// <summary>The four edges between the corners tile.</summary>
    Sides = 1,

    /// <summary>The middle tiles.</summary>
    Center = 2,

    /// <summary>Both, which is what a patterned panel wants.</summary>
    All = Sides | Center,
}

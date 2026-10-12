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

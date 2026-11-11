namespace Bevy;

/// <summary>
/// Where a sprite's transform sits on the picture.
/// </summary>
/// <remarks>
/// A sprite is centered on its transform unless told otherwise, which is awkward for anything
/// standing on the ground, whose feet are then half a sprite below where it was placed. The
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

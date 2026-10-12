using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a sprite is drawn.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeSpriteConfig
{
    /// <summary>Asset key of the image.</summary>
    public int Image;

    /// <summary>Tint red.</summary>
    public float ColorR;

    /// <summary>Tint green.</summary>
    public float ColorG;

    /// <summary>Tint blue.</summary>
    public float ColorB;

    /// <summary>Tint alpha.</summary>
    public float ColorA;

    /// <summary>Non-zero to use <see cref="SizeX"/> and <see cref="SizeY"/>.</summary>
    public int HasSize;

    /// <summary>Width in world units.</summary>
    public float SizeX;

    /// <summary>Height in world units.</summary>
    public float SizeY;

    /// <summary>Non-zero to draw only part of the image.</summary>
    public int HasRect;

    /// <summary>Left of that part, in pixels.</summary>
    public float RectLeft;

    /// <summary>Top of that part, in pixels.</summary>
    public float RectTop;

    /// <summary>Right of that part, in pixels.</summary>
    public float RectRight;

    /// <summary>Bottom of that part, in pixels.</summary>
    public float RectBottom;

    /// <summary>Non-zero to mirror horizontally.</summary>
    public int FlipX;

    /// <summary>Non-zero to mirror vertically.</summary>
    public int FlipY;

    /// <summary>Asset key of the atlas layout, or negative for the whole image.</summary>
    public int Atlas;

    /// <summary>Which frame of that layout to draw.</summary>
    public uint AtlasIndex;

    /// <summary>Non-zero to move the sprite's origin off center.</summary>
    public int HasAnchor;

    /// <summary>Where the transform sits on the sprite, horizontally.</summary>
    public float AnchorX;

    /// <summary>Where the transform sits on the sprite, vertically.</summary>
    public float AnchorY;

    /// <summary>0 the image's own size, 1 sliced, 2 tiled, 3 scaled to fit.</summary>
    public int Mode;

    /// <summary>How a scaled picture is fitted, when the mode is scaled.</summary>
    public int Scaling;

    /// <summary>Left inset of the nine-slice border, in pixels.</summary>
    public float SliceLeft;

    /// <summary>Top inset of the nine-slice border, in pixels.</summary>
    public float SliceTop;

    /// <summary>Right inset of the nine-slice border, in pixels.</summary>
    public float SliceRight;

    /// <summary>Bottom inset of the nine-slice border, in pixels.</summary>
    public float SliceBottom;

    /// <summary>How far a sliced corner may be scaled up; 0 for Bevy's default.</summary>
    public float CornerScale;

    /// <summary>Non-zero to repeat horizontally when tiled.</summary>
    public int TileX;

    /// <summary>Non-zero to repeat vertically when tiled.</summary>
    public int TileY;

    /// <summary>How far the picture stretches before a tile repeats; 0 for Bevy's default.</summary>
    public float TileStretch;

    /// <summary>Which parts of a sliced picture tile: 1 the sides, 2 the middle, 3 both.</summary>
    public int SliceTiling;
}

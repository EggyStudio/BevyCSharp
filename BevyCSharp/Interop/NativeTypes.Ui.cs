using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Where a UI node sits and how large it is.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeUiNodeConfig
{
    /// <summary>Non-zero to position against the parent's edges.</summary>
    public int Absolute;

    /// <summary>Non-zero to give the node an interaction the pointer updates.</summary>
    public int Interactive;

    /// <summary>Distance from the left edge.</summary>
    public float Left;

    /// <summary>Unit of <see cref="Left"/>.</summary>
    public int LeftUnit;

    /// <summary>Distance from the top edge.</summary>
    public float Top;

    /// <summary>Unit of <see cref="Top"/>.</summary>
    public int TopUnit;

    /// <summary>Distance from the right edge.</summary>
    public float Right;

    /// <summary>Unit of <see cref="Right"/>.</summary>
    public int RightUnit;

    /// <summary>Distance from the bottom edge.</summary>
    public float Bottom;

    /// <summary>Unit of <see cref="Bottom"/>.</summary>
    public int BottomUnit;

    /// <summary>Node width.</summary>
    public float Width;

    /// <summary>Unit of <see cref="Width"/>.</summary>
    public int WidthUnit;

    /// <summary>Node height.</summary>
    public float Height;

    /// <summary>Unit of <see cref="Height"/>.</summary>
    public int HeightUnit;

    /// <summary>Padding on every side.</summary>
    public float Padding;

    /// <summary>Unit of <see cref="Padding"/>.</summary>
    public int PaddingUnit;

    /// <summary>Margin on every side.</summary>
    public float Margin;

    /// <summary>Unit of <see cref="Margin"/>.</summary>
    public int MarginUnit;

    /// <summary>Border thickness on every side.</summary>
    public float Border;

    /// <summary>Unit of <see cref="Border"/>.</summary>
    public int BorderUnit;

    /// <summary>Which way children are stacked.</summary>
    public int Direction;

    /// <summary>How children are spread along that axis.</summary>
    public int Justify;

    /// <summary>How children sit across it.</summary>
    public int Align;

    /// <summary>Space between rows of children.</summary>
    public float RowGap;

    /// <summary>Unit of <see cref="RowGap"/>.</summary>
    public int RowGapUnit;

    /// <summary>Space between columns of children.</summary>
    public float ColumnGap;

    /// <summary>Unit of <see cref="ColumnGap"/>.</summary>
    public int ColumnGapUnit;

    /// <summary>Background or text colour, red.</summary>
    public float ColorR;

    /// <summary>Background or text colour, green.</summary>
    public float ColorG;

    /// <summary>Background or text colour, blue.</summary>
    public float ColorB;

    /// <summary>Background or text colour, alpha.</summary>
    public float ColorA;

    /// <summary>Border colour, red.</summary>
    public float BorderColorR;

    /// <summary>Border colour, green.</summary>
    public float BorderColorG;

    /// <summary>Border colour, blue.</summary>
    public float BorderColorB;

    /// <summary>Border colour, alpha.</summary>
    public float BorderColorA;
}

/// <summary>The picture a UI node draws inside itself.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeUiImageConfig
{
    /// <summary>Asset key of the image.</summary>
    public int Image;

    /// <summary>Tint, red.</summary>
    public float ColorR;

    /// <summary>Tint, green.</summary>
    public float ColorG;

    /// <summary>Tint, blue.</summary>
    public float ColorB;

    /// <summary>Tint, alpha.</summary>
    public float ColorA;

    /// <summary>Non-zero to draw only the part of the image the rect names.</summary>
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

    /// <summary>0 the image's own size, 1 stretched, 2 sliced, 3 tiled.</summary>
    public int Mode;

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
}

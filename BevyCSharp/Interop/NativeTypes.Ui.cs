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

    /// <summary>Padding, left.</summary>
    public float PaddingLeft;

    /// <summary>Padding, top.</summary>
    public float PaddingTop;

    /// <summary>Padding, right.</summary>
    public float PaddingRight;

    /// <summary>Padding, bottom.</summary>
    public float PaddingBottom;

    /// <summary>Unit of <see cref="PaddingLeft"/>.</summary>
    public int PaddingLeftUnit;

    /// <summary>Unit of <see cref="PaddingTop"/>.</summary>
    public int PaddingTopUnit;

    /// <summary>Unit of <see cref="PaddingRight"/>.</summary>
    public int PaddingRightUnit;

    /// <summary>Unit of <see cref="PaddingBottom"/>.</summary>
    public int PaddingBottomUnit;

    /// <summary>Margin, left.</summary>
    public float MarginLeft;

    /// <summary>Margin, top.</summary>
    public float MarginTop;

    /// <summary>Margin, right.</summary>
    public float MarginRight;

    /// <summary>Margin, bottom.</summary>
    public float MarginBottom;

    /// <summary>Unit of <see cref="MarginLeft"/>.</summary>
    public int MarginLeftUnit;

    /// <summary>Unit of <see cref="MarginTop"/>.</summary>
    public int MarginTopUnit;

    /// <summary>Unit of <see cref="MarginRight"/>.</summary>
    public int MarginRightUnit;

    /// <summary>Unit of <see cref="MarginBottom"/>.</summary>
    public int MarginBottomUnit;

    /// <summary>Border thickness, left.</summary>
    public float BorderLeft;

    /// <summary>Border thickness, top.</summary>
    public float BorderTop;

    /// <summary>Border thickness, right.</summary>
    public float BorderRight;

    /// <summary>Border thickness, bottom.</summary>
    public float BorderBottom;

    /// <summary>Unit of <see cref="BorderLeft"/>.</summary>
    public int BorderLeftUnit;

    /// <summary>Unit of <see cref="BorderTop"/>.</summary>
    public int BorderTopUnit;

    /// <summary>Unit of <see cref="BorderRight"/>.</summary>
    public int BorderRightUnit;

    /// <summary>Unit of <see cref="BorderBottom"/>.</summary>
    public int BorderBottomUnit;

    /// <summary>0 flex, 1 block, 2 not laid out at all.</summary>
    public int Display;

    /// <summary>Which way children are stacked.</summary>
    public int Direction;

    /// <summary>0 one line, 1 wrap, 2 wrap backwards.</summary>
    public int Wrap;

    /// <summary>How this node sits across its parent's axis.</summary>
    public int AlignSelf;

    /// <summary>Share of the parent's leftover space this node takes.</summary>
    public float Grow;

    /// <summary>Share of the parent's overflow this node gives up.</summary>
    public float Shrink;

    /// <summary>Size along the parent's axis before growing or shrinking.</summary>
    public float Basis;

    /// <summary>Unit of <see cref="Basis"/>.</summary>
    public int BasisUnit;

    /// <summary>Smallest width.</summary>
    public float MinWidth;

    /// <summary>Unit of <see cref="MinWidth"/>.</summary>
    public int MinWidthUnit;

    /// <summary>Smallest height.</summary>
    public float MinHeight;

    /// <summary>Unit of <see cref="MinHeight"/>.</summary>
    public int MinHeightUnit;

    /// <summary>Largest width.</summary>
    public float MaxWidth;

    /// <summary>Unit of <see cref="MaxWidth"/>.</summary>
    public int MaxWidthUnit;

    /// <summary>Largest height.</summary>
    public float MaxHeight;

    /// <summary>Unit of <see cref="MaxHeight"/>.</summary>
    public int MaxHeightUnit;

    /// <summary>0 shown, 1 clipped, 2 hidden, 3 scrolled, left and right.</summary>
    public int OverflowX;

    /// <summary>The same for top and bottom.</summary>
    public int OverflowY;

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

/// <summary>How a run of UI text is set.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeUiTextConfig
{
    /// <summary>Asset key of the font, or negative for the one built into Bevy.</summary>
    public int Font;

    /// <summary>Glyph height in logical pixels.</summary>
    public float FontSize;

    /// <summary>0 left, 1 centred, 2 right, 3 justified, 4 start, 5 end.</summary>
    public int Justify;

    /// <summary>0 word boundaries, 1 any character, 2 word then character, 3 never.</summary>
    public int LineBreak;
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

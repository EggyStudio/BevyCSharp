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

    /// <summary>Background or text color, red.</summary>
    public float ColorR;

    /// <summary>Background or text color, green.</summary>
    public float ColorG;

    /// <summary>Background or text color, blue.</summary>
    public float ColorB;

    /// <summary>Background or text color, alpha.</summary>
    public float ColorA;

    /// <summary>Border color, red.</summary>
    public float BorderColorR;

    /// <summary>Border color, green.</summary>
    public float BorderColorG;

    /// <summary>Border color, blue.</summary>
    public float BorderColorB;

    /// <summary>Border color, alpha.</summary>
    public float BorderColorA;

    /// <summary>How the lines of a wrapped node are spread across it.</summary>
    public int AlignContent;

    /// <summary>The other axis as a multiple of the known one, or zero for neither.</summary>
    public float AspectRatio;

    /// <summary>Which box a node that clips its overflow clips at.</summary>
    public int ClipBox;

    /// <summary>How far outside that box the clipping is pushed.</summary>
    public float ClipMargin;

    /// <summary>How far the top left corner is rounded.</summary>
    public float CornerTopLeft;

    /// <summary>How far the top right corner is rounded.</summary>
    public float CornerTopRight;

    /// <summary>How far the bottom right corner is rounded.</summary>
    public float CornerBottomRight;

    /// <summary>How far the bottom left corner is rounded.</summary>
    public float CornerBottomLeft;

    /// <summary>Unit of <see cref="CornerTopLeft"/>.</summary>
    public int CornerTopLeftUnit;

    /// <summary>Unit of <see cref="CornerTopRight"/>.</summary>
    public int CornerTopRightUnit;

    /// <summary>Unit of <see cref="CornerBottomRight"/>.</summary>
    public int CornerBottomRightUnit;

    /// <summary>Unit of <see cref="CornerBottomLeft"/>.</summary>
    public int CornerBottomLeftUnit;

    /// <summary>What the sizes measure: 0 the border box, 1 the content box.</summary>
    public int BoxSizing;

    /// <summary>Which camera draws this node, or zero for the one drawing to the window.</summary>
    public ulong Camera;

    /// <summary>Room kept for a scrollbar at the far edge of each axis that scrolls, in logical pixels.</summary>
    public float ScrollbarWidth;
}

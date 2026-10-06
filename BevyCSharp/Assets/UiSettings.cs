namespace Bevy;

/// <summary>
/// Where a UI node sits and how large it is.
/// </summary>
/// <remarks>
/// Every length defaults to <see cref="Length.Auto"/>, so a node with nothing set takes the size
/// of its contents and sits where the layout puts it.
/// </remarks>
public sealed class UiSettings
{
    /// <summary>
    /// Place the node against its parent's edges rather than in the flow of its siblings.
    /// </summary>
    /// <remarks>
    /// Pinned to a corner, ignoring whatever else is on screen, as a HUD is. A node left relative
    /// is laid out beside its siblings instead.
    /// </remarks>
    public bool Absolute { get; set; }

    /// <summary>
    /// Report the pointer over this node, which makes it a button.
    /// </summary>
    /// <remarks>
    /// An interactive node also captures the pointer, so nothing behind it is hovered through it.
    /// A node left plain carries nothing to update, which is why this is off by default. A HUD is
    /// mostly nodes that never react, and every one of them would otherwise be tested against the
    /// pointer each frame. Read the result with <see cref="Ui.InteractionOf"/>.
    /// </remarks>
    public bool Interactive { get; set; }

    /// <summary>Distance from the parent's left edge.</summary>
    public Length Left { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's top edge.</summary>
    public Length Top { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's right edge.</summary>
    public Length Right { get; set; } = Length.Auto;

    /// <summary>Distance from the parent's bottom edge.</summary>
    public Length Bottom { get; set; } = Length.Auto;

    /// <summary>How wide the node is.</summary>
    public Length Width { get; set; } = Length.Auto;

    /// <summary>How tall the node is.</summary>
    public Length Height { get; set; } = Length.Auto;

    /// <summary>Space between the node's edge and its contents.</summary>
    /// <remarks>A <see cref="Length"/> assigned here is the same distance on every side.</remarks>
    public Sides Padding { get; set; } = Sides.None;

    /// <summary>Space outside the node's edge.</summary>
    /// <remarks>
    /// What separates a node from its siblings. <see cref="RowGap"/> and <see cref="ColumnGap"/>
    /// say the same thing from the parent's side, and are the better place for even spacing.
    /// A side left at <see cref="Length.Auto"/> swallows the space the parent has left over,
    /// which is how a node is pushed to one end or centered without the parent knowing.
    /// </remarks>
    public Sides Margin { get; set; } = Sides.None;

    /// <summary>How thick the node's border is.</summary>
    /// <remarks>
    /// Drawn in <see cref="BorderColor"/>, which is transparent until it is set. A side left at
    /// zero is no border, so <see cref="Sides.Vertical"/> draws a rule above and below and
    /// nothing at the ends.
    /// </remarks>
    public Sides Border { get; set; } = Sides.None;

    /// <summary>Which way the node stacks its children.</summary>
    public UiDirection Direction { get; set; } = UiDirection.Row;

    /// <summary>How the children are spread along that axis.</summary>
    public UiJustify Justify { get; set; } = UiJustify.Default;

    /// <summary>How the children sit across it.</summary>
    public UiAlign Align { get; set; } = UiAlign.Default;

    /// <summary>
    /// How the lines of a wrapped node are spread across it.
    /// </summary>
    /// <remarks>
    /// The same question <see cref="Justify"/> asks of the children within one line, asked of the
    /// lines themselves, so it does nothing until <see cref="Wrap"/> has produced more than one.
    /// A row of tiles that wraps onto three lines is spread by this and packed by that.
    /// </remarks>
    public UiJustify AlignContent { get; set; } = UiJustify.Default;

    /// <summary>
    /// The other axis as a multiple of the one that is known, or zero to size both on their own.
    /// </summary>
    /// <remarks>
    /// What keeps a tile square or a panel sixteen by nine while only one of its sides is being
    /// decided by the layout. The ratio is width over height, so <c>1</c> is a square and
    /// <c>16f / 9f</c> is a widescreen box.
    /// </remarks>
    public float AspectRatio { get; set; }

    /// <summary>
    /// Which box a node that clips its overflow clips at.
    /// </summary>
    /// <remarks>
    /// Only matters when <see cref="OverflowX"/> or <see cref="OverflowY"/> is clipping. A
    /// scrolling list with a border needs <see cref="UiClipBox.Padding"/> or
    /// <see cref="UiClipBox.Border"/>, or its rows are cut off inside the border rather than at it.
    /// </remarks>
    public UiClipBox ClipBox { get; set; } = UiClipBox.Padding;

    /// <summary>How far outside that box the clipping is pushed, in logical pixels.</summary>
    /// <remarks>A few pixels of slack, for a shadow or a focus ring that should not be cut off.</remarks>
    public float ClipMargin { get; set; }

    /// <summary>How far each corner is rounded.</summary>
    /// <remarks>
    /// A <see cref="Length"/> assigned here is the same radius on every corner, so
    /// <c>Corners = Length.Px(6f)</c> is the usual card and button. The background, the border and
    /// anything the node clips all follow the same curve.
    /// </remarks>
    public Corners Corners { get; set; } = Corners.None;

    /// <summary>What <see cref="Width"/> and the rest measure.</summary>
    public BoxSizing Sizing { get; set; } = BoxSizing.BorderBox;

    /// <summary>
    /// Which camera draws this node, or <see cref="Entity.None"/> for whichever draws the window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Left alone, Bevy picks the camera whose target is the window, which suits a game, and a run
    /// drawing into an image has none. Naming one lets a screen be drawn with no window at all, so
    /// a screen can be captured on a machine with no display.
    /// </para>
    /// <para>
    /// It carries to the node's children, so the root of a screen is the only one that has to be
    /// told.
    /// </para>
    /// </remarks>
    public Entity Camera { get; set; } = Entity.None;

    /// <summary>Whether the node lays out at all, and by which model.</summary>
    /// <remarks>
    /// <see cref="UiDisplay.None"/> takes a whole screen out of the layout without despawning it,
    /// which is how a menu is put away and brought back.
    /// </remarks>
    public UiDisplay Display { get; set; } = UiDisplay.Flex;

    /// <summary>Whether the children run onto more than one line.</summary>
    public UiWrap Wrap { get; set; } = UiWrap.NoWrap;

    /// <summary>This node's own answer to how its parent aligns things.</summary>
    public UiAlignSelf AlignSelf { get; set; } = UiAlignSelf.Auto;

    /// <summary>
    /// Share of the parent's leftover space this node takes.
    /// </summary>
    /// <remarks>
    /// Zero leaves the node at its own size. Otherwise the leftover space is split between the
    /// children in proportion to this, so one child with a <c>1</c> fills the row and two with a
    /// <c>1</c> each take half of it.
    /// </remarks>
    public float Grow { get; set; }

    /// <summary>
    /// Share of the overflow this node gives up when the children do not fit.
    /// </summary>
    /// <remarks>One is Bevy's own, so a node shrinks with its siblings unless told not to.</remarks>
    public float Shrink { get; set; } = 1f;

    /// <summary>
    /// The size this node starts at along the parent's axis, before growing or shrinking.
    /// </summary>
    public Length Basis { get; set; } = Length.Auto;

    /// <summary>The smallest the node may be laid out.</summary>
    public Length MinWidth { get; set; } = Length.Auto;

    /// <summary>The smallest the node may be laid out.</summary>
    public Length MinHeight { get; set; } = Length.Auto;

    /// <summary>The largest the node may be laid out.</summary>
    public Length MaxWidth { get; set; } = Length.Auto;

    /// <summary>The largest the node may be laid out.</summary>
    public Length MaxHeight { get; set; } = Length.Auto;

    /// <summary>What happens to contents past the left and right edges.</summary>
    public UiOverflow OverflowX { get; set; } = UiOverflow.Visible;

    /// <summary>What happens to contents past the top and bottom edges.</summary>
    public UiOverflow OverflowY { get; set; } = UiOverflow.Visible;

    /// <summary>Space between the rows of children.</summary>
    public Length RowGap { get; set; } = Length.Zero;

    /// <summary>Space between the columns of children.</summary>
    public Length ColumnGap { get; set; } = Length.Zero;

    /// <summary>
    /// The node's background, or the text's color for a run of text. Linear RGBA.
    /// </summary>
    /// <remarks>
    /// Transparent by default, so a plain node is a layout box that draws nothing. A run of text
    /// given no color is white instead (<see cref="Ui.SpawnText(string, UiSettings, UiTextSettings)"/>).
    /// </remarks>
    public (float R, float G, float B, float A) Color { get; set; } = Unset;

    /// <summary>The color a node starts with, transparent, which a run of text reads as white.</summary>
    internal static readonly (float R, float G, float B, float A) Unset = (1f, 1f, 1f, 0f);

    /// <summary>
    /// The border's color, on every side. Linear RGBA.
    /// </summary>
    /// <remarks>
    /// Transparent by default, so a border takes both this and a <see cref="Border"/> thickness
    /// to appear.
    /// </remarks>
    public (float R, float G, float B, float A) BorderColor { get; set; } = (1f, 1f, 1f, 0f);
}

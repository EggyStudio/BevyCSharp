namespace Bevy;

/// <summary>What a length is measured in.</summary>
public enum LengthUnit
{
    /// <summary>Worked out from the layout, which is the default for most fields.</summary>
    Auto = 0,

    /// <summary>Logical pixels, which are scaled with the display rather than raw device ones.</summary>
    Px = 1,

    /// <summary>A share of the parent's size on the same axis.</summary>
    Percent = 2,
}

/// <summary>
/// A distance in a UI layout.
/// </summary>
/// <remarks>
/// A number alone cannot say whether it means pixels, a share of the parent, or "work it out", so
/// the unit travels with it.
/// </remarks>
/// <param name="Value">The magnitude, ignored when the unit is <see cref="LengthUnit.Auto"/>.</param>
/// <param name="Unit">What the magnitude is measured in.</param>
public readonly record struct Length(float Value, LengthUnit Unit)
{
    /// <summary>Left to the layout.</summary>
    public static Length Auto => new(0f, LengthUnit.Auto);

    /// <summary>No distance at all.</summary>
    /// <remarks>
    /// Different from <see cref="Auto"/> where the layout has room to spare: an automatic margin
    /// takes that room, a zero one does not.
    /// </remarks>
    public static Length Zero => new(0f, LengthUnit.Px);

    /// <summary>A distance in logical pixels.</summary>
    public static Length Px(float value) => new(value, LengthUnit.Px);

    /// <summary>A share of the parent, where 100 is all of it.</summary>
    public static Length Percent(float value) => new(value, LengthUnit.Percent);

    /// <inheritdoc/>
    public override string ToString() => Unit switch
    {
        LengthUnit.Px => $"{Value}px",
        LengthUnit.Percent => $"{Value}%",
        _ => "auto",
    };
}

/// <summary>
/// A measurement taken on all four sides of a node.
/// </summary>
/// <remarks>
/// Padding, margin and border are each four lengths. One value is the common case, so a
/// <see cref="Length"/> converts to the same distance on every side and only a node that wants
/// its sides to differ has to name them.
/// </remarks>
/// <param name="Left">The left side.</param>
/// <param name="Top">The top side.</param>
/// <param name="Right">The right side.</param>
/// <param name="Bottom">The bottom side.</param>
public readonly record struct Sides(Length Left, Length Top, Length Right, Length Bottom)
{
    /// <summary>Nothing on any side.</summary>
    public static Sides None => All(Length.Zero);

    /// <summary>The same length on every side.</summary>
    public static Sides All(Length value) => new(value, value, value, value);

    /// <summary>Left and right, with nothing at the top and bottom.</summary>
    public static Sides Horizontal(Length value) =>
        new(value, Length.Zero, value, Length.Zero);

    /// <summary>Top and bottom, with nothing at the left and right.</summary>
    public static Sides Vertical(Length value) =>
        new(Length.Zero, value, Length.Zero, value);

    /// <summary>Reads a single length as the same distance on every side.</summary>
    public static implicit operator Sides(Length value) => All(value);

    /// <inheritdoc/>
    public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";
}

/// <summary>
/// Whether a node lays out at all, and by which model.
/// </summary>
public enum UiDisplay
{
    /// <summary>Flexbox, which is what every other layout field here describes.</summary>
    Flex = 0,

    /// <summary>Children stacked as blocks, each on its own line.</summary>
    Block = 1,

    /// <summary>
    /// Not laid out, drawn or measured, and neither are its children.
    /// </summary>
    /// <remarks>
    /// How a screen is put away and brought back without despawning it. Different from
    /// <see cref="Visibility.Hidden"/>, which stops the node drawing but keeps the space it
    /// occupies, so its siblings do not move.
    /// </remarks>
    None = 2,
}

/// <summary>
/// Whether a node's children run onto more than one line.
/// </summary>
public enum UiWrap
{
    /// <summary>One line, however far past the edge it runs.</summary>
    NoWrap = 0,

    /// <summary>As many lines as the children need.</summary>
    Wrap = 1,

    /// <summary>The same, with each new line before the last rather than after it.</summary>
    WrapReverse = 2,
}

/// <summary>
/// One node's own answer to how its parent aligns things.
/// </summary>
/// <remarks>
/// The same choices as <see cref="UiAlign"/>, set on the child instead of the parent, plus
/// <see cref="Auto"/> for leaving the parent to decide. What the odd item out uses.
/// </remarks>
public enum UiAlignSelf
{
    /// <summary>Whatever the parent's alignment says.</summary>
    Auto = 0,

    /// <summary>Against the start of the cross axis.</summary>
    Start = 1,

    /// <summary>Against the end of the cross axis.</summary>
    End = 2,

    /// <summary>The start of the cross axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the cross axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Centred across the axis.</summary>
    Center = 5,

    /// <summary>Lined up on the baseline of the text inside it.</summary>
    Baseline = 6,

    /// <summary>Stretched to fill the cross axis.</summary>
    Stretch = 7,
}

/// <summary>
/// What happens to contents that run past a node's edge.
/// </summary>
public enum UiOverflow
{
    /// <summary>They are drawn anyway, outside the node.</summary>
    Visible = 0,

    /// <summary>They are cut off at the edge.</summary>
    Clip = 1,

    /// <summary>Cut off at the edge, and the layout is told they do not fit.</summary>
    Hidden = 2,

    /// <summary>
    /// Cut off at the edge, and movable inside it with <see cref="Ui.SetScroll"/>.
    /// </summary>
    Scroll = 3,
}

/// <summary>
/// Which way a node stacks its children.
/// </summary>
/// <remarks>
/// A node lays its children out along one axis. This is the axis, and everything else in the
/// layout is described relative to it: <see cref="UiSettings.Justify"/> spreads them along it,
/// <see cref="UiSettings.Align"/> places them across it.
/// </remarks>
public enum UiDirection
{
    /// <summary>Left to right, which is the default.</summary>
    Row = 0,

    /// <summary>Top to bottom, which is what a menu or a list wants.</summary>
    Column = 1,

    /// <summary>Right to left.</summary>
    RowReverse = 2,

    /// <summary>Bottom to top.</summary>
    ColumnReverse = 3,
}

/// <summary>
/// How a node spreads its children along its own axis.
/// </summary>
/// <remarks>
/// The main axis is <see cref="UiSettings.Direction"/>. `Start` and `End` are the edges of the
/// node itself; the `Flex` pair follows the direction instead, so they swap when it is reversed.
/// </remarks>
public enum UiJustify
{
    /// <summary>Whatever the layout would do unasked.</summary>
    Default = 0,

    /// <summary>Packed against the start of the axis.</summary>
    Start = 1,

    /// <summary>Packed against the end of the axis.</summary>
    End = 2,

    /// <summary>The start of the axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Packed around the middle.</summary>
    Center = 5,

    /// <summary>Stretched to fill the axis.</summary>
    Stretch = 6,

    /// <summary>Spread out, with the leftover space between the children.</summary>
    SpaceBetween = 7,

    /// <summary>Spread out, with equal space between and around the children.</summary>
    SpaceEvenly = 8,

    /// <summary>Spread out, with half-size space at the two ends.</summary>
    SpaceAround = 9,
}

/// <summary>
/// How a node places its children across its axis.
/// </summary>
/// <remarks>
/// The cross axis: for a <see cref="UiDirection.Row"/> this is the vertical, for a
/// <see cref="UiDirection.Column"/> the horizontal.
/// </remarks>
public enum UiAlign
{
    /// <summary>Whatever the layout would do unasked.</summary>
    Default = 0,

    /// <summary>Against the start of the cross axis.</summary>
    Start = 1,

    /// <summary>Against the end of the cross axis.</summary>
    End = 2,

    /// <summary>The start of the cross axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the cross axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Centred across the axis, which is what a row of buttons wants.</summary>
    Center = 5,

    /// <summary>Lined up on the baselines of the text inside them.</summary>
    Baseline = 6,

    /// <summary>Stretched to fill the cross axis.</summary>
    Stretch = 7,
}

/// <summary>
/// How the pointer stands on a node.
/// </summary>
/// <remarks>
/// Only a node spawned with <see cref="UiSettings.Interactive"/> reports one. Asking a node that
/// was not is refused rather than answered <see cref="None"/>, because a button that quietly
/// never fires is the harder mistake to find.
/// </remarks>
public enum UiInteraction
{
    /// <summary>The pointer is elsewhere.</summary>
    None = 0,

    /// <summary>The pointer is over the node.</summary>
    Hovered = 1,

    /// <summary>The pointer is over the node and its primary button is down.</summary>
    Pressed = 2,
}

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
    /// What a HUD wants: pinned to a corner, ignoring whatever else is on screen. A node left
    /// relative is laid out beside its siblings instead.
    /// </remarks>
    public bool Absolute { get; set; }

    /// <summary>
    /// Report the pointer over this node, which is what makes it a button.
    /// </summary>
    /// <remarks>
    /// An interactive node also captures the pointer, so nothing behind it is hovered through it.
    /// A node left plain carries nothing to update, which is why this is off by default: a HUD is
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
    /// which is how a node is pushed to one end or centred without the parent knowing.
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
    /// <remarks>Transparent by default, so a plain node is a layout box that draws nothing.</remarks>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 0f);

    /// <summary>
    /// The border's color, on every side. Linear RGBA.
    /// </summary>
    /// <remarks>
    /// Transparent by default, so a border takes both this and a <see cref="Border"/> thickness
    /// to appear.
    /// </remarks>
    public (float R, float G, float B, float A) BorderColor { get; set; } = (1f, 1f, 1f, 0f);
}

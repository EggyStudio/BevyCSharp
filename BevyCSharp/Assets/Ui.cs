using Bevy.Interop;

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
    /// The node's background, or the text's colour for a run of text. Linear RGBA.
    /// </summary>
    /// <remarks>Transparent by default, so a plain node is a layout box that draws nothing.</remarks>
    public (float R, float G, float B, float A) Color { get; set; } = (1f, 1f, 1f, 0f);

    /// <summary>
    /// The border's colour, on every side. Linear RGBA.
    /// </summary>
    /// <remarks>
    /// Transparent by default, so a border takes both this and a <see cref="Border"/> thickness
    /// to appear.
    /// </remarks>
    public (float R, float G, float B, float A) BorderColor { get; set; } = (1f, 1f, 1f, 0f);
}

/// <summary>
/// How the lines of a run of text sit against each other.
/// </summary>
/// <remarks>
/// Alignment inside the text's own box, not where that box sits in its parent, which is
/// <see cref="UiSettings.Align"/> and <see cref="UiSettings.Justify"/>. It shows only once there
/// is more than one line, or the box is wider than the text.
/// </remarks>
public enum TextJustify
{
    /// <summary>Lines start at the left edge.</summary>
    Left = 0,

    /// <summary>Lines are centred on each other.</summary>
    Center = 1,

    /// <summary>Lines end at the right edge.</summary>
    Right = 2,

    /// <summary>Words are spaced so both edges line up, except on the last line.</summary>
    Justified = 3,

    /// <summary>The left in left-to-right text, the right in right-to-left.</summary>
    Start = 4,

    /// <summary>The right in left-to-right text, the left in right-to-left.</summary>
    End = 5,
}

/// <summary>
/// Where a run of text may be broken when it does not fit on one line.
/// </summary>
/// <remarks>
/// The width it is broken against comes from the layout, so a node free to grow sideways never
/// wraps whatever this says. Give the text or an ancestor a <see cref="UiSettings.Width"/> or
/// <see cref="UiSettings.MaxWidth"/> and the wrapping has something to happen at.
/// </remarks>
public enum TextWrap
{
    /// <summary>At spaces and the like, keeping words whole. What prose wants.</summary>
    WordBoundary = 0,

    /// <summary>Anywhere at all, which is how a terminal breaks lines.</summary>
    AnyCharacter = 1,

    /// <summary>At words, falling back to characters for a word too long to fit alone.</summary>
    WordOrCharacter = 2,

    /// <summary>
    /// Never, so a long line runs past the edge unless something clips it.
    /// </summary>
    /// <remarks>
    /// An explicit newline in the string still breaks. Pair with
    /// <see cref="UiOverflow.Clip"/> for a single line that is cut off rather than wrapped.
    /// </remarks>
    NoWrap = 3,
}

/// <summary>
/// How a run of text is set: its font and size, and what happens to it at the edges of its node.
/// </summary>
public sealed class UiTextSettings
{
    /// <summary>
    /// The font to set the text in, or none for the one built into Bevy.
    /// </summary>
    /// <remarks>
    /// Loaded with <see cref="AssetKind.Font"/> from a TrueType or OpenType file. A handle that
    /// names nothing is refused rather than quietly falling back, since a game that ships a font
    /// and then does not use it looks like a font that failed to load, which it is. Asking for a
    /// font by family name instead is not offered: that needs a Bevy feature which links against
    /// fontconfig on Linux, and this bridge builds with nothing but a C compiler.
    /// </remarks>
    public AssetHandle Font { get; set; } = AssetHandle.None;

    /// <summary>Glyph height in logical pixels.</summary>
    public float FontSize { get; set; } = 20f;

    /// <summary>How the lines sit against each other.</summary>
    public TextJustify Justify { get; set; } = TextJustify.Left;

    /// <summary>Where a line may be broken.</summary>
    public TextWrap Wrap { get; set; } = TextWrap.WordBoundary;
}

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

/// <summary>
/// Builds Bevy's UI: panels, and the text on them.
/// </summary>
/// <remarks>
/// <para>
/// Nodes are entities, so everything the ECS already does applies to them.
/// <see cref="EcsWorld.SetParent"/> nests one inside another, which is what lays a screen out,
/// and <see cref="EcsWorld.Despawn"/> takes one away.
/// </para>
/// <para>
/// Needs a render build with a window. A windowless run has nothing to draw on and says so
/// rather than spawning entities that would never appear.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var hud = Ui.SpawnText("Score: 0", new UiSettings
/// {
///     Absolute = true,
///     Left = Length.Px(16f),
///     Top = Length.Px(16f),
///     Color = (1f, 1f, 1f, 1f),
/// });
///
/// Ui.SetText(hud, $"Score: {score}");
/// </code>
/// </example>
public static unsafe class Ui
{
    /// <summary>Spawns a rectangle and returns it.</summary>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static Entity SpawnNode(UiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = ToNative(settings);
        var bits = Native.bcs_ui_spawn_node(&native);
        if (bits == 0) throw NoUi("Spawning a UI node");

        return new Entity(bits);
    }

    /// <summary>
    /// Spawns a run of text and returns it.
    /// </summary>
    /// <remarks>
    /// The font is Bevy's own, compiled into the engine, so nothing has to be loaded to put words
    /// on the screen. <see cref="UiSettings.Color"/> is the colour of the text itself, and
    /// <see cref="UiTextSettings"/> is where another font goes.
    /// </remarks>
    /// <param name="text">What it says.</param>
    /// <param name="settings">Where it sits.</param>
    /// <param name="fontSize">Height in logical pixels.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static Entity SpawnText(string text, UiSettings settings, float fontSize = 20f) =>
        SpawnText(text, settings, new UiTextSettings { FontSize = fontSize });

    /// <summary>
    /// Spawns a run of text, set as <paramref name="style"/> describes.
    /// </summary>
    /// <remarks>
    /// Where a paragraph rather than a label is wanted: the text is broken to the width the
    /// layout gives it, so a node with a <see cref="UiSettings.Width"/> or
    /// <see cref="UiSettings.MaxWidth"/> holds it and a node free to grow does not.
    /// </remarks>
    /// <param name="text">What it says.</param>
    /// <param name="settings">Where it sits.</param>
    /// <param name="style">How it is set.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    /// <example>
    /// <code>
    /// Ui.SpawnText(paragraph, new UiSettings { Width = Length.Px(280f) }, new UiTextSettings
    /// {
    ///     FontSize = 16f,
    ///     Justify = TextJustify.Center,
    ///     Wrap = TextWrap.WordBoundary,
    /// });
    /// </code>
    /// </example>
    public static Entity SpawnText(string text, UiSettings settings, UiTextSettings style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(style);

        var native = ToNative(settings);
        var nativeText = new NativeUiTextConfig
        {
            Font = style.Font.Key,
            FontSize = style.FontSize,
            Justify = (int)style.Justify,
            LineBreak = (int)style.Wrap,
        };

        var bits = Native.bcs_ui_spawn_text(text, &native, &nativeText);
        if (bits == 0) throw NoUi("Spawning UI text");

        return new Entity(bits);
    }

    /// <summary>
    /// Replaces what a text entity says.
    /// </summary>
    /// <remarks>
    /// Written in place rather than by respawning, because a score or a timer changes every frame
    /// and the entity behind it should not.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or carries no text.</exception>
    public static void SetText(Entity entity, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Native.Check(Native.bcs_ui_set_text(entity.Bits, text), $"setting the text of {entity}");
    }

    /// <summary>
    /// Reports how the pointer stands on an interactive node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="UiInteraction.Pressed"/> lasts from the frame the pointer goes down until it is
    /// released, so a click is the edge into it: keep the previous answer and compare. A release
    /// over the node reads as <see cref="UiInteraction.Hovered"/> again in the same frame.
    /// </para>
    /// <para>
    /// The pointer is tracked by a system that comes with the window, so a windowless run leaves
    /// every node at <see cref="UiInteraction.None"/> rather than failing.
    /// </para>
    /// </remarks>
    /// <param name="entity">A node spawned with <see cref="UiSettings.Interactive"/>.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, or is not a node that reports interaction.
    /// </exception>
    /// <example>
    /// <code>
    /// var state = Ui.InteractionOf(button);
    /// if (state == UiInteraction.Pressed &amp;&amp; Previous != UiInteraction.Pressed) Fire();
    /// Previous = state;
    /// </code>
    /// </example>
    public static UiInteraction InteractionOf(Entity entity)
    {
        var value = Native.bcs_ui_interaction(entity.Bits);
        Native.Check(value, $"reading the interaction of {entity}");

        return (UiInteraction)value;
    }

    /// <summary>
    /// Draws an image inside a node, or replaces the one it draws.
    /// </summary>
    /// <remarks>
    /// The node keeps its layout and the picture fills what the layout gave it. A node with no
    /// width or height of its own takes the image's, which is what an icon wants.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, the handle is not one this app is holding, or this build has no
    /// renderer.
    /// </exception>
    public static void SetImage(Entity entity, AssetHandle image) =>
        SetImage(entity, new UiImageSettings { Image = image });

    /// <summary>
    /// Draws an image inside a node, tinted, cut down or sliced.
    /// </summary>
    /// <remarks>
    /// <see cref="UiImageMode.Sliced"/> is what a panel that resizes is drawn with: the corners
    /// keep their size while the middle stretches, so one small image covers every size of box.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, the handle is not one this app is holding, or this build has no
    /// renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// Ui.SetImage(panel, new UiImageSettings
    /// {
    ///     Image = AssetServer.Load(AssetKind.Image, "ui/panel.png"),
    ///     Mode = UiImageMode.Sliced,
    ///     SliceBorder = (8f, 8f, 8f, 8f),
    /// });
    /// </code>
    /// </example>
    public static void SetImage(Entity entity, UiImageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeUiImageConfig
        {
            Image = settings.Image.Key,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            ColorA = settings.Color.A,
            HasRect = settings.Rect.HasValue ? 1 : 0,
            RectLeft = settings.Rect?.Left ?? 0f,
            RectTop = settings.Rect?.Top ?? 0f,
            RectRight = settings.Rect?.Right ?? 0f,
            RectBottom = settings.Rect?.Bottom ?? 0f,
            FlipX = settings.FlipX ? 1 : 0,
            FlipY = settings.FlipY ? 1 : 0,
            Mode = (int)settings.Mode,
            SliceLeft = settings.SliceBorder.Left,
            SliceTop = settings.SliceBorder.Top,
            SliceRight = settings.SliceBorder.Right,
            SliceBottom = settings.SliceBorder.Bottom,
            CornerScale = settings.CornerScale,
            TileX = settings.TileX ? 1 : 0,
            TileY = settings.TileY ? 1 : 0,
            TileStretch = settings.TileStretch,
        };

        Native.Check(
            Native.bcs_ui_set_image(entity.Bits, &native), $"drawing an image in {entity}");
    }

    /// <summary>
    /// Moves a scrolling node's contents inside it.
    /// </summary>
    /// <remarks>
    /// Only means anything on a node whose <see cref="UiSettings.OverflowX"/> or
    /// <see cref="UiSettings.OverflowY"/> is <see cref="UiOverflow.Scroll"/>: that is what clips
    /// the contents to the node, and this is how far they have been pushed, in logical pixels
    /// from the top left. Bevy has no scrolling of its own, so a wheel or a drag is read like any
    /// other input and turned into a call here.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetScroll(Entity entity, float x, float y) =>
        Native.Check(
            Native.bcs_ui_set_scroll(entity.Bits, x, y), $"scrolling the contents of {entity}");

    private static NativeUiNodeConfig ToNative(UiSettings settings) => new()
    {
        Absolute = settings.Absolute ? 1 : 0,
        Interactive = settings.Interactive ? 1 : 0,
        Left = settings.Left.Value,
        LeftUnit = (int)settings.Left.Unit,
        Top = settings.Top.Value,
        TopUnit = (int)settings.Top.Unit,
        Right = settings.Right.Value,
        RightUnit = (int)settings.Right.Unit,
        Bottom = settings.Bottom.Value,
        BottomUnit = (int)settings.Bottom.Unit,
        Width = settings.Width.Value,
        WidthUnit = (int)settings.Width.Unit,
        Height = settings.Height.Value,
        HeightUnit = (int)settings.Height.Unit,
        PaddingLeft = settings.Padding.Left.Value,
        PaddingTop = settings.Padding.Top.Value,
        PaddingRight = settings.Padding.Right.Value,
        PaddingBottom = settings.Padding.Bottom.Value,
        PaddingLeftUnit = (int)settings.Padding.Left.Unit,
        PaddingTopUnit = (int)settings.Padding.Top.Unit,
        PaddingRightUnit = (int)settings.Padding.Right.Unit,
        PaddingBottomUnit = (int)settings.Padding.Bottom.Unit,
        MarginLeft = settings.Margin.Left.Value,
        MarginTop = settings.Margin.Top.Value,
        MarginRight = settings.Margin.Right.Value,
        MarginBottom = settings.Margin.Bottom.Value,
        MarginLeftUnit = (int)settings.Margin.Left.Unit,
        MarginTopUnit = (int)settings.Margin.Top.Unit,
        MarginRightUnit = (int)settings.Margin.Right.Unit,
        MarginBottomUnit = (int)settings.Margin.Bottom.Unit,
        BorderLeft = settings.Border.Left.Value,
        BorderTop = settings.Border.Top.Value,
        BorderRight = settings.Border.Right.Value,
        BorderBottom = settings.Border.Bottom.Value,
        BorderLeftUnit = (int)settings.Border.Left.Unit,
        BorderTopUnit = (int)settings.Border.Top.Unit,
        BorderRightUnit = (int)settings.Border.Right.Unit,
        BorderBottomUnit = (int)settings.Border.Bottom.Unit,
        Display = (int)settings.Display,
        Direction = (int)settings.Direction,
        Wrap = (int)settings.Wrap,
        AlignSelf = (int)settings.AlignSelf,
        Grow = settings.Grow,
        Shrink = settings.Shrink,
        Basis = settings.Basis.Value,
        BasisUnit = (int)settings.Basis.Unit,
        MinWidth = settings.MinWidth.Value,
        MinWidthUnit = (int)settings.MinWidth.Unit,
        MinHeight = settings.MinHeight.Value,
        MinHeightUnit = (int)settings.MinHeight.Unit,
        MaxWidth = settings.MaxWidth.Value,
        MaxWidthUnit = (int)settings.MaxWidth.Unit,
        MaxHeight = settings.MaxHeight.Value,
        MaxHeightUnit = (int)settings.MaxHeight.Unit,
        OverflowX = (int)settings.OverflowX,
        OverflowY = (int)settings.OverflowY,
        Justify = (int)settings.Justify,
        Align = (int)settings.Align,
        RowGap = settings.RowGap.Value,
        RowGapUnit = (int)settings.RowGap.Unit,
        ColumnGap = settings.ColumnGap.Value,
        ColumnGapUnit = (int)settings.ColumnGap.Unit,
        ColorR = settings.Color.R,
        ColorG = settings.Color.G,
        ColorB = settings.Color.B,
        ColorA = settings.Color.A,
        BorderColorR = settings.BorderColor.R,
        BorderColorG = settings.BorderColor.G,
        BorderColorB = settings.BorderColor.B,
        BorderColorA = settings.BorderColor.A,
    };

    private static BevyNativeException NoUi(string operation) =>
        new(NativeStatus.Unsupported,
            $"{operation} failed: this native build has no renderer, so there is no UI to build "
            + "on. Rebuild the bridge with build/build-native.sh --render.");
}

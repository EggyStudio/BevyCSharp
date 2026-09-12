using Bevy.Interop;

namespace Bevy;

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
    /// on the screen. <see cref="UiSettings.Color"/> is the color of the text itself, and
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
    /// <remarks>
    /// The entity has to be a node. Unlike <see cref="SetImage(Entity, UiImageSettings)"/>,
    /// which turns whatever it is given into an image node, the scroll position is a bare
    /// component that no layout would read, so anything else is refused rather than quietly
    /// accepted.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a node, or this build has no renderer.
    /// </exception>
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

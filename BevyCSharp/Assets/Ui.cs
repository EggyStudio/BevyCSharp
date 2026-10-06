using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Builds Bevy's UI: panels, and the text on them.
/// </summary>
/// <remarks>
/// <para>
/// Nodes are entities, so everything the ECS already does applies to them.
/// <see cref="EcsWorld.SetParent"/> nests one inside another, which lays a screen out, and
/// <see cref="EcsWorld.Despawn"/> takes one away.
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
public static unsafe partial class Ui
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
    /// <para>
    /// The font is Bevy's own, compiled into the engine, so nothing has to be loaded to put words
    /// on the screen. <see cref="UiSettings.Color"/> is the color of the text itself, and
    /// <see cref="UiTextSettings"/> is where another font goes.
    /// </para>
    /// <para>
    /// A color left as <see cref="UiSettings"/> starts it, which is transparent so that a plain
    /// node draws nothing, is white here, as Bevy's own text is, since text nobody can see is
    /// never what a caller who named no color meant.
    /// </para>
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
    /// Where a paragraph rather than a label is wanted. The text is broken to the width the
    /// layout gives it, so a node with a <see cref="UiSettings.Width"/> or
    /// <see cref="UiSettings.MaxWidth"/> holds it and a node free to grow does not.
    /// </remarks>
    /// <param name="text">What it says.</param>
    /// <param name="settings">Where it sits.</param>
    /// <param name="style">How it is set.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    /// <example>
    /// <code>
    /// Ui.SpawnText(paragraph, new UiSettings { Width = Length.Px(280f) }, new UiTextSettings {
    /// FontSize = 16f, Justify = TextJustify.Center, Wrap = TextWrap.WordBoundary, });
    /// </code>
    /// </example>
    public static Entity SpawnText(string text, UiSettings settings, UiTextSettings style)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(style);

        var native = ToNative(settings);
        if (settings.Color == UiSettings.Unset) (native.ColorR, native.ColorG, native.ColorB, native.ColorA) = (1f, 1f, 1f, 1f);

        var nativeText = ToNative(style);

        var bits = Native.bcs_ui_spawn_text(text, &native, &nativeText);
        if (bits == 0) throw NoUi("Spawning UI text");

        return new Entity(bits);
    }

    /// <summary>
    /// Adds a run of text to an existing one, set in its own font and color.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a bold word inside a sentence is. A paragraph with more than one style in it is one
    /// text entity with a span per run rather than markup inside a string, so each run carries its
    /// own font, size and color, and the whole is broken and aligned as one block by the settings
    /// the parent was given.
    /// </para>
    /// <para>
    /// Spans read in the order they were added, after whatever the parent itself says. A span has
    /// no node of its own and so takes no <see cref="UiSettings"/>, which is why its color is a
    /// parameter here where a whole text takes the color of the node it sits in.
    /// </para>
    /// </remarks>
    /// <param name="parent">The text entity this run belongs to.</param>
    /// <param name="text">What the run says.</param>
    /// <param name="style">How it is set.</param>
    /// <param name="color">Linear RGBA.</param>
    /// <exception cref="BevyNativeException">
    /// The parent is gone, the font names nothing, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var line = Ui.SpawnText("the word ", settings, new UiTextSettings { FontSize = 16f });
    /// Ui.SpawnTextSpan(line, "bold", new UiTextSettings { Font = heavy, FontSize = 16f },
    ///     (1f, 1f, 1f, 1f));
    /// Ui.SpawnTextSpan(line, " is heavier", new UiTextSettings { FontSize = 16f },
    ///     (0.7f, 0.7f, 0.7f, 1f));
    /// </code>
    /// </example>
    public static Entity SpawnTextSpan(
        Entity parent,
        string text,
        UiTextSettings style,
        (float R, float G, float B, float A) color)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(style);

        var nativeText = ToNative(style);
        var rgba = stackalloc float[4] { color.R, color.G, color.B, color.A };

        var bits = Native.bcs_ui_spawn_text_span(parent.Bits, text, &nativeText, rgba);
        if (bits == 0) throw NoUi($"Adding a run of text to {parent}");

        return new Entity(bits);
    }

    /// <summary>How a run of text is set, as the bridge takes it.</summary>
    private static NativeUiTextConfig ToNative(UiTextSettings style) => new()
    {
        Font = style.Font.Key,
        FontSize = style.FontSize,
        Justify = (int)style.Justify,
        LineBreak = (int)style.Wrap,
        LineHeight = style.LineHeight,
        LineHeightUnit = style.LineHeightInPixels ? 1 : 0,
        LetterSpacing = style.LetterSpacing,
        LetterSpacingUnit = style.LetterSpacingInPixels ? 1 : 0,
        FontSmoothing = style.Smooth ? 0 : 1,
        ShadowOffsetX = style.ShadowOffset.X,
        ShadowOffsetY = style.ShadowOffset.Y,
        ShadowColorR = style.ShadowColor.R,
        ShadowColorG = style.ShadowColor.G,
        ShadowColorB = style.ShadowColor.B,
        ShadowColorA = style.ShadowColor.A,
    };

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
    /// released, so a click is the edge into it, found by keeping the previous answer and
    /// comparing. A release over the node reads as <see cref="UiInteraction.Hovered"/> again in the
    /// same frame.
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
    /// width or height of its own takes the image's, as an icon needs.
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
    /// A panel that resizes is drawn with <see cref="UiImageMode.Sliced"/>, because the corners
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
            SliceTiling = (int)settings.SliceTiling,
            Atlas = settings.Atlas.Key,
            AtlasIndex = settings.Frame,
        };

        Native.Check(
            Native.bcs_ui_set_image(entity.Bits, &native), $"drawing an image in {entity}");
    }

    /// <summary>
    /// Moves a scrolling node's contents inside it.
    /// </summary>
    /// <remarks>
    /// Only means anything on a node whose <see cref="UiSettings.OverflowX"/> or
    /// <see cref="UiSettings.OverflowY"/> is <see cref="UiOverflow.Scroll"/>, which clips the
    /// contents to the node, and this is how far they have been pushed, in logical pixels from the
    /// top left. Bevy has no scrolling of its own, so a wheel or a drag is read like any other
    /// input and turned into a call here.
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
        AlignContent = (int)settings.AlignContent,
        AspectRatio = settings.AspectRatio,
        ClipBox = (int)settings.ClipBox,
        ClipMargin = settings.ClipMargin,
        CornerTopLeft = settings.Corners.TopLeft.Value,
        CornerTopRight = settings.Corners.TopRight.Value,
        CornerBottomRight = settings.Corners.BottomRight.Value,
        CornerBottomLeft = settings.Corners.BottomLeft.Value,
        CornerTopLeftUnit = (int)settings.Corners.TopLeft.Unit,
        CornerTopRightUnit = (int)settings.Corners.TopRight.Unit,
        CornerBottomRightUnit = (int)settings.Corners.BottomRight.Unit,
        CornerBottomLeftUnit = (int)settings.Corners.BottomLeft.Unit,
        BoxSizing = (int)settings.Sizing,
        Camera = settings.Camera.Bits,
    };

    /// <summary>Gives <paramref name="entity"/> the input focus, so the keys go to it.</summary>
    /// <remarks>
    /// Bevy's <c>InputFocus::set</c>, which also records the change, from which Bevy tells the entity
    /// that lost the focus and the one that gained it, as a text field selects what it holds on
    /// gaining it where it carries <c>SelectAllOnFocus</c>. Writing <c>InputFocusRef.CurrentFocus</c>
    /// moves the focus without that record. Which entity has the focus is read from
    /// <c>InputFocusRef</c>.
    /// </remarks>
    /// <param name="entity">The entity to focus, usually a text field or a button.</param>
    /// <param name="cause">How it came to have it, navigated to or pressed into.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void Focus(Entity entity, FocusCause cause = FocusCause.Navigated)
    {
        var status = Native.bcs_ui_focus(entity.Bits, (int)cause);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Giving the input focus");
        Native.Check(status, $"giving {entity} the input focus");
    }

    /// <summary>The entity the focus would move to along the tab order, without moving it.</summary>
    /// <remarks>
    /// Bevy's <c>TabNavigation::navigate</c>, the order Tab moves the focus in, by each entity's
    /// <c>TabIndex</c> within its <c>TabGroup</c>, from the entity with the focus now. A game that
    /// moves the focus itself, on to the next field once one is submitted, passes the answer to
    /// <see cref="Focus"/>:
    /// <code>
    /// if (Ui.Navigate(NavAction.Next) is { } next) Ui.Focus(next);
    /// </code>
    /// </remarks>
    /// <param name="action">Which way, to the next, the previous, the first or the last.</param>
    /// <returns>The entity, or null where there is nowhere to move it, no tab group or nothing in one.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer, or this was called from outside a system.</exception>
    public static Entity? Navigate(NavAction action)
    {
        ulong next;
        var status = Native.bcs_ui_navigate((int)action, &next);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Moving the input focus");
        Native.Check(status, $"finding where the focus moves {action}");
        return status == 1 ? new Entity(next) : null;
    }

    /// <summary>Draws a run of text with a line under it, or takes the line off.</summary>
    /// <remarks>
    /// Bevy's <c>Underline</c>, on a node's text, a span or a 2D text alike. Its color is the run's
    /// <c>UnderlineColorRef</c> where it carries one and the text's own otherwise. Bevy reflects the
    /// marker but not as a component, so no wrapper puts it on, and this does.
    /// </remarks>
    /// <param name="text">The run of text.</param>
    /// <param name="underlined">Whether to draw the line.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetUnderline(Entity text, bool underlined = true) => TextLines(text, underlined ? 1 : 0, -1);

    /// <summary>Draws a run of text with a line through it, or takes the line off.</summary>
    /// <remarks>
    /// Bevy's <c>Strikethrough</c>, colored by <c>StrikethroughColorRef</c> where the run carries
    /// one, as <see cref="SetUnderline"/> is by its color.
    /// </remarks>
    /// <param name="text">The run of text.</param>
    /// <param name="struck">Whether to draw the line.</param>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetStrikethrough(Entity text, bool struck = true) => TextLines(text, -1, struck ? 1 : 0);

    private static void TextLines(Entity text, int underline, int strikethrough)
    {
        var status = Native.bcs_ui_text_lines(text.Bits, underline, strikethrough);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Drawing a line on text");
        Native.Check(status, $"drawing a line on {text}");
    }

    /// <summary>Sets the OpenType features a run of text is drawn with, replacing those it had.</summary>
    /// <remarks>
    /// Bevy's <c>FontFeatures</c>, each feature named by its four-letter tag and turned on with one
    /// and off with zero, or given a higher number where a feature has several forms to choose
    /// from, as the font defines them:
    /// <code>
    /// Ui.SetFontFeatures(price, ("tnum", 1), ("liga", 0));    // even figures, no ligatures
    /// </code>
    /// A font that has no such feature draws as it would without it.
    /// </remarks>
    /// <param name="text">The run of text.</param>
    /// <param name="features">Each feature's tag and value.</param>
    /// <exception cref="ArgumentException">A tag is not four ASCII characters.</exception>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetFontFeatures(Entity text, params ReadOnlySpan<(string Tag, uint Value)> features)
    {
        Span<NativeFontTag> tags = features.Length <= 32 ? stackalloc NativeFontTag[features.Length] : new NativeFontTag[features.Length];
        for (var i = 0; i < features.Length; i++) tags[i] = FontTag(features[i].Tag, features[i].Value);
        FontTags(text, 0, tags);
    }

    /// <summary>Sets where a variable font sits on each of its axes for a run of text, replacing what it had.</summary>
    /// <remarks>
    /// Bevy's <c>FontVariations</c>, each axis named by its four-letter tag, <c>wght</c> for the
    /// weight, <c>wdth</c> for the width, <c>slnt</c> for the slant, and the value within the range
    /// the font gives that axis:
    /// <code>
    /// Ui.SetFontVariations(heading, ("wght", 650f));
    /// </code>
    /// A font that is not variable, or has no such axis, draws as it would without it.
    /// </remarks>
    /// <param name="text">The run of text.</param>
    /// <param name="variations">Each axis's tag and value.</param>
    /// <exception cref="ArgumentException">A tag is not four ASCII characters.</exception>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetFontVariations(Entity text, params ReadOnlySpan<(string Tag, float Value)> variations)
    {
        Span<NativeFontTag> tags = variations.Length <= 32 ? stackalloc NativeFontTag[variations.Length] : new NativeFontTag[variations.Length];
        for (var i = 0; i < variations.Length; i++) tags[i] = FontTag(variations[i].Tag, variations[i].Value);
        FontTags(text, 1, tags);
    }

    private static NativeFontTag FontTag(string tag, float value)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (tag.Length != 4 || !tag.All(char.IsAscii))
            throw new ArgumentException($"An OpenType tag is four ASCII characters, as \"liga\" or \"wght\", not \"{tag}\".", nameof(tag));

        var native = new NativeFontTag { Value = value };
        for (var i = 0; i < 4; i++) native.Tag[i] = (byte)tag[i];
        return native;
    }

    private static void FontTags(Entity text, int kind, ReadOnlySpan<NativeFontTag> tags)
    {
        int status;
        fixed (NativeFontTag* at = tags) status = Native.bcs_ui_font_tags(text.Bits, kind, at, tags.Length);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Setting a font's OpenType tags");
        Native.Check(status, $"setting the font of {text}");
    }

    private static BevyNativeException NoUi(string operation) =>
        new(NativeStatus.Unsupported,
            $"{operation} failed, because this native build has no renderer, so there is no UI to "
            + "build "
            + "on. Rebuild the bridge with build/build-native.sh --render.");
}

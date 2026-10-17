// Bevy's multiline_text_input example, examples/ui/text/multiline_text_input.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;

namespace BevyCSharp.Examples.Interface;

// Demonstrates a single, minimal multiline text field, eight lines tall and wrapping at a word or
// anywhere, which Ctrl+Enter prints, with a scrollbar of its own beside it. Below it, fields set
// how many lines it shows, the size of its font and how round its selection's corners are, each
// taken as Enter is pressed in it, and a menu sets how its lines are justified.
//
// The scrollbar is built against the field's viewport and its text's laid out size, since a
// field's scroll is no node's scroll position and Bevy's scrollbar widget cannot move it.
internal static class MultilineTextInput
{
    private static readonly Color DarkSlateGray = Color.FromSrgb8(47, 79, 79);
    private static readonly Color Slate300 = Color.FromSrgb8(203, 213, 225);

    private static AssetHandle _font;

    public static void Build(App app)
    {
        app.Startup(Setup, "multiline_text_input.Setup");
        app.Update(UpdateInputScrollbar, "multiline_text_input.UpdateInputScrollbar");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.End, RowGap = Length.Px(10f) });
        ecs.Insert<TabGroupRef>(column);
        ecs.SetParent(column, root);

        // The field and its scrollbar side by side.
        var pair = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, ColumnGap = Length.Px(4f) });
        UiGrid.Set(pair, new GridSettings { Columns = [Track.Auto, Track.Auto] });
        ecs.SetParent(pair, column);

        var multiline = Field(ecs, new UiSettings { Width = Length.Px(450f), Padding = Sides.All(Length.Px(8f)) },
            new UiEditableTextSettings { VisibleLines = 8f, AllowNewlines = true });
        ecs.Wrap<TextLayoutRef>(multiline).Linebreak = TextLayoutRef.LinebreakVariant.WordOrCharacter;
        Ui.SetTextCursor(multiline, new UiTextCursorSettings { Color = Color.White, SelectedText = Color.Black });
        ecs.Insert<TabIndexRef>(multiline).Value = 0;
        ecs.Insert<AutoFocusRef>(multiline);
        ecs.Add(multiline, new MultilineInput());
        ecs.SetParent(multiline, pair);

        // Ctrl+Enter prints what it holds, where Enter alone starts a new line.
        ecs.Observe<FocusedInput<KeyboardInput>>(multiline, on =>
        {
            var input = on.Event.Input;
            if (!(input.State == ButtonState.Pressed && input.LogicalKey == LogicalKey.Enter && on.Context.Input.KeyDown(LogicalKey.Control))) return;
            if (Ui.EditableTextOf(on.Event.FocusedEntity) is { } output) Console.WriteLine(output);
        });

        // The scrollbar, outside the tab group's order and never focused, its thumb hidden until
        // the text is taller than the field.
        var track = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(10f), Color = DarkSlateGray });
        ecs.SetParent(track, pair);
        var thumb = Ui.SpawnNode(new UiSettings { Absolute = true, Width = Length.Percent(100f), Left = Length.Px(0f), Corners = Corners.All(Length.Px(4f)), Color = Slate300 });
        ecs.Insert<VisibilityRef>(thumb).Value = VisibilityRef.ValueVariant.Hidden;
        ecs.Add(thumb, new InputScrollThumb());
        ecs.Add(thumb, new InputScrollDragState());
        ecs.SetParent(thumb, track);
        ecs.Observe<Pointer<DragStart>>(thumb, OnThumbDragStart);
        ecs.Observe<Pointer<Drag>>(thumb, OnThumbDrag);
        ecs.Observe<Pointer<DragEnd>>(thumb, OnThumbDragEnd);

        // The fields below, each taken as Enter is pressed in it, the first two showing no
        // selection once they lose the focus.
        var clear = new UiTextCursorSettings { Color = Color.White, SelectedText = Color.Black, UnfocusedSelection = Color.Transparent };

        // How many lines it shows, from one to ten.
        var visibleLines = Row(ecs, column, "visible lines:", "8", "0123456789.", 1, clear, selectAll: true, (world, multiline, text) =>
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lines))
                Ui.SetVisibleLines(multiline, Math.Clamp(lines, 1f, 10f));
        });
        ecs.Add(visibleLines, new VisibleLinesInput());

        // The size of its font, from five pixels to fifty.
        var fontSize = Row(ecs, column, "font size:", "30", "0123456789", 2, clear, selectAll: true, (world, multiline, text) =>
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var size))
                world.Wrap<TextFontRef>(multiline).FontSize = new FontSize.Px(Math.Clamp(size, 5f, 50f));
        });
        ecs.Add(fontSize, new FontSizeInput());

        // How round its selection's corners are, from square to half a line.
        var cornerRadius = Row(ecs, column, "corner radius:", "0", "0123456789.", 2, new UiTextCursorSettings { Color = Color.White, SelectedText = Color.Black }, selectAll: false, (world, multiline, text) =>
        {
            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var radius))
                Ui.SetTextCursor(multiline, new UiTextCursorSettings { Color = Color.White, SelectedText = Color.Black, SelectionRadius = Math.Clamp(radius, 0f, 0.5f) });
        });
        ecs.Add(cornerRadius, new SelectionRadiusInput());

        JustifyMenu(ecs, column);
    }

    // The multiline field, which Bevy's systems and observers find as the one entity marked so.
    private static Entity? Multiline(EcsWorld ecs) => ecs.EntitiesWith<MultilineInput>() is [var multiline] ? multiline : null;

    // A label and a field a hundred pixels wide, its text at its right, and an observer on the row
    // giving what the field holds to the multiline field as Enter is pressed in it. Answers the
    // field, for the caller to mark.
    private static Entity Row(EcsWorld ecs, Entity column, string label, string text, string allowed, int index, UiTextCursorSettings cursor, bool selectAll, Action<EcsWorld, Entity, string> take)
    {
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(10f) });
        ecs.SetParent(row, column);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 30f }), row);

        var field = Field(ecs, new UiSettings { Width = Length.Px(100f) }, new UiEditableTextSettings { Text = text, Allowed = allowed });
        ecs.Wrap<TextLayoutRef>(field).Justify = Justify.End;
        Ui.SetTextCursor(field, cursor);
        if (selectAll) ecs.Insert<SelectAllOnFocusRef>(field);
        ecs.Insert<TabIndexRef>(field).Value = index;
        ecs.SetParent(field, row);

        ecs.Observe<FocusedInput<KeyboardInput>>(row, on =>
        {
            var input = on.Event.Input;
            if (!(input.State == ButtonState.Pressed && input.LogicalKey == LogicalKey.Enter) || on.Event.FocusedEntity != field) return;
            if (Ui.EditableTextOf(field) is { } value && Multiline(on.Ecs) is { } multiline) take(on.Ecs, multiline, value);
        });
        return field;
    }

    // A field in the example's font and colors, bordered two pixels wide.
    private static Entity Field(EcsWorld ecs, UiSettings node, UiEditableTextSettings settings)
    {
        node.Border = Sides.All(Length.Px(2f));
        node.BorderColor = Slate300;
        node.Color = DarkSlateGray;
        var field = Ui.SpawnNode(node);
        Ui.SetEditableText(field, settings);

        var font = ecs.Wrap<TextFontRef>(field);
        font.Font = new FontSource.Handle(_font);
        font.FontSize = new FontSize.Px(30f);
        return field;
    }

    // A menu button naming how the field's lines are justified, its menu above it listing each way.
    private static void JustifyMenu(EcsWorld ecs, Entity column)
    {
        var anchor = Ui.SpawnNode(new UiSettings());
        ecs.SetParent(anchor, column);

        var button = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(2f)), Padding = Sides.Horizontal(Length.Px(8f)), Color = DarkSlateGray, BorderColor = Slate300 });
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<MenuButtonRef>(button);
        ecs.Insert<TabIndexRef>(button).Value = 4;
        var justifyLabel = Ui.SpawnText("Justify::Left", new UiSettings(), new UiTextSettings { Font = _font, FontSize = 24f });
        ecs.Add(justifyLabel, new JustifyLabel());
        ecs.SetParent(justifyLabel, button);
        ecs.SetParent(button, anchor);

        var popup = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.None,
            Direction = UiDirection.Column,
            MinWidth = Length.Percent(100f),
            Border = Sides.All(Length.Px(2f)),
            Absolute = true,
            Color = DarkSlateGray,
            BorderColor = Slate300,
        });
        ecs.Insert<MenuPopupRef>(popup);
        ecs.Insert<PopoverRef>(popup).Positions = [new PopoverPlacement(PopoverSide.Top, PopoverAlign.End, 2f)];
        ecs.Insert<GlobalZIndexRef>(popup).Value = 1;
        ecs.SetParent(popup, anchor);

        // Bevy's observer finds the popup and the button as the one of each there is, and this one
        // is given them, since a C# program looks entities up by its own components and not by
        // Bevy's.
        ecs.Observe<MenuEvent>(anchor, on => OnMenuEvent(on, popup, button));

        foreach (var justify in new[] { Justify.Left, Justify.Center, Justify.Right, Justify.Justified, Justify.Start, Justify.End })
        {
            var label = $"Justify::{justify}";
            var item = Ui.SpawnNode(new UiSettings { Padding = Sides.Horizontal(Length.Px(8f)) });
            ecs.Insert<MenuItemRef>(item);
            ecs.Insert<TabIndexRef>(item).Value = 0;
            ecs.SetParent(Ui.SpawnText(label, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 24f }), item);
            ecs.SetParent(item, popup);

            ecs.Observe<Activate>(item, on =>
            {
                if (Multiline(on.Ecs) is not { } multiline || on.Ecs.EntitiesWith<JustifyLabel>() is not [var selected]) return;
                on.Ecs.Wrap<TextLayoutRef>(multiline).Justify = justify;
                Ui.SetText(selected, label);
            });
        }
    }

    // The menu shown and hidden as it is asked, and the focus given back to its button.
    private static void OnMenuEvent(On<MenuEvent> on, Entity popup, Entity button)
    {
        var ecs = on.Ecs;
        var node = ecs.Wrap<NodeRef>(popup);

        switch (on.Event.Action)
        {
            case MenuAction.Open:
                node.Display = NodeRef.DisplayVariant.Flex;
                ecs.Wrap<MenuFocusStateRef>(popup).Value = new MenuFocusState.Opening((NavActionValue)on.Event.Navigation);
                break;

            case MenuAction.Toggle:
                if (node.Display == NodeRef.DisplayVariant.None)
                {
                    node.Display = NodeRef.DisplayVariant.Flex;
                    ecs.Wrap<MenuFocusStateRef>(popup).Value = new MenuFocusState.Opening(NavActionValue.First);
                }
                else
                {
                    node.Display = NodeRef.DisplayVariant.None;
                }
                break;

            case MenuAction.CloseAll:
                node.Display = NodeRef.DisplayVariant.None;
                break;

            case MenuAction.FocusRoot:
                Ui.Focus(button);
                break;
        }
    }

    // Bevy's update_input_scrollbar, the thumb sized and placed from the field's viewport, and
    // hidden while the whole text fits in it.
    private static void UpdateInputScrollbar(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (Multiline(ecs) is not { } multiline || ecs.EntitiesWith<InputScrollThumb>() is not [var thumb]) return;
        if (Ui.TextViewportOf(multiline) is not { } viewport || ecs.Get<TextLayoutInfoRef>(multiline) is not { } layout) return;

        var contentHeight = layout.Size.Y;
        var visibility = ecs.Wrap<VisibilityRef>(thumb);
        if (viewport.Size.Y <= 0f || contentHeight <= viewport.Size.Y)
        {
            if (visibility.Value != VisibilityRef.ValueVariant.Hidden) visibility.Value = VisibilityRef.ValueVariant.Hidden;
            return;
        }
        if (visibility.Value != VisibilityRef.ValueVariant.Visible) visibility.Value = VisibilityRef.ValueVariant.Visible;

        var thumbFraction = Math.Clamp(viewport.Size.Y / contentHeight, 0.05f, 1f);
        var maxOffset = contentHeight - viewport.Size.Y;
        var scrollFraction = Math.Clamp(viewport.Offset.Y / maxOffset, 0f, 1f);

        var node = ecs.Wrap<NodeRef>(thumb);
        node.Height = new Val.Percent(thumbFraction * 100f);
        node.Top = new Val.Percent(scrollFraction * (1f - thumbFraction) * 100f);
    }

    private static void OnThumbDragStart(On<Pointer<DragStart>> on)
    {
        on.Propagate(false);
        if (!on.Ecs.Has<InputScrollDragState>(on.Entity) || Multiline(on.Ecs) is not { } multiline) return;
        on.Ecs.Set(on.Entity, new InputScrollDragState { Dragging = true, DragOrigin = Ui.TextViewportOf(multiline)?.Offset.Y ?? 0f });
    }

    // The text scrolled as far through its height as the thumb has been dragged through its
    // track's, the thumb's parent.
    private static void OnThumbDrag(On<Pointer<Drag>> on)
    {
        on.Propagate(false);
        var ecs = on.Ecs;
        if (!ecs.TryGet<InputScrollDragState>(on.Entity, out var drag) || !drag.Dragging || ecs.Get<ComputedNodeRef>(ecs.ParentOf(on.Entity)) is not { } track) return;

        var trackHeight = track.Size.Y * track.InverseScaleFactor;
        if (trackHeight <= 0f || Multiline(ecs) is not { } multiline) return;
        if (Ui.TextViewportOf(multiline) is not { } viewport || ecs.Get<TextLayoutInfoRef>(multiline) is not { } layout) return;

        var contentHeight = layout.Size.Y;
        var maxOffset = Math.Max(contentHeight - viewport.Size.Y, 0f);
        var delta = on.Event.Event.Distance.Y / trackHeight * contentHeight;
        Ui.ScrollText(multiline, viewport.Offset with { Y = Math.Clamp(drag.DragOrigin + delta, 0f, maxOffset) });
    }

    private static void OnThumbDragEnd(On<Pointer<DragEnd>> on)
    {
        on.Propagate(false);
        if (on.Ecs.TryGet<InputScrollDragState>(on.Entity, out var drag)) on.Ecs.Set(on.Entity, drag with { Dragging = false });
    }
}

/// <summary>The field of many lines, Bevy's example's <c>MultilineInput</c>.</summary>
[Behavior]
public partial struct MultilineInput;

/// <summary>The field setting how many lines it shows, Bevy's example's <c>VisibleLinesInput</c>.</summary>
[Behavior]
public partial struct VisibleLinesInput;

/// <summary>The field setting the size of its font, Bevy's example's <c>FontSizeInput</c>.</summary>
[Behavior]
public partial struct FontSizeInput;

/// <summary>The field setting how round its selection's corners are, Bevy's example's <c>SelectionRadiusInput</c>.</summary>
[Behavior]
public partial struct SelectionRadiusInput;

/// <summary>The menu button's label naming how its lines are justified, Bevy's example's <c>JustifyLabel</c>.</summary>
[Behavior]
public partial struct JustifyLabel;

/// <summary>
/// The thumb of the field's scrollbar, Bevy's example's <c>InputScrollThumb</c>.
/// </summary>
/// <remarks>
/// Built against the field's viewport and its text's laid out size, since a field's scroll is no
/// node's scroll position and Bevy's scrollbar widget cannot move it. It takes no part in the tab
/// order and is never focused.
/// </remarks>
[Behavior]
public partial struct InputScrollThumb;

/// <summary>A scrollbar thumb's drag, Bevy's example's <c>InputScrollDragState</c>.</summary>
[Behavior]
public partial struct InputScrollDragState
{
    /// <summary>Whether the thumb is being dragged.</summary>
    public bool Dragging;

    /// <summary>Where the field's text was scrolled to as the drag started.</summary>
    public float DragOrigin;
}

// Bevy's standard_widgets example, examples/ui/widgets/standard_widgets.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Bevy's widgets, which have no look of their own, styled here from their state. A button, a
// slider, a checkbox, a radio group choosing what a click on the slider's track does, and a menu
// button whose menu the example spawns and despawns as it is asked to. The slider and the radio
// group report what they were asked to become, and the example keeps those values itself and sets
// the widgets from them. D disables and enables them all.
internal static class StandardWidgets
{
    internal static readonly Color NormalButton = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    internal static readonly Color HoveredButton = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    internal static readonly Color PressedButton = Color.FromSrgb(0.35f, 0.75f, 0.35f);
    internal static readonly Color SliderTrack = Color.FromSrgb(0.05f, 0.05f, 0.05f);
    internal static readonly Color SliderThumb = Color.FromSrgb(0.35f, 0.75f, 0.35f);
    internal static readonly Color ElementOutline = Color.FromSrgb(0.45f, 0.45f, 0.45f);
    internal static readonly Color ElementFill = Color.FromSrgb(0.35f, 0.75f, 0.35f);
    internal static readonly Color ElementFillDisabled = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);

    // Bevy's basic palette.
    internal static readonly Color Gray = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);
    internal static readonly Color Green = Color.FromSrgb(0f, 0.5019608f, 0f);
    internal static readonly Color Red = Color.FromSrgb(1f, 0f, 0f);
    internal static readonly Color White = Color.FromSrgb(1f, 1f, 1f);
    internal static readonly Color Black = Color.FromSrgb(0f, 0f, 0f);

    // Bevy's DemoWidgetStates, the example's own record of the widgets' values, kept apart from the
    // widgets' own state as data bound to a widget would be, and whether it changed this frame.
    private static float _sliderValue;
    private static SliderRef.TrackClickVariant _sliderClick;
    private static bool _statesChanged;

    // Every widget D disables, Bevy's query over Button, MenuButton, Slider, Checkbox and
    // RadioButton.
    private static readonly List<Entity> Widgets = [];

    // The style last given to each widget, so one is written only when its state changes, as
    // Bevy's systems run only for a widget whose state changed.
    internal static readonly Dictionary<Entity, int> Shown = [];

    private static AssetHandle _font;

    public static void Build(App app)
    {
        app.Startup(Setup, "standard_widgets.Setup");
        app.Update(UpdateWidgetValues, "standard_widgets.UpdateWidgetValues");
        app.Update(ToggleDisabled, "standard_widgets.ToggleDisabled");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_sliderValue, _sliderClick, _statesChanged) = (50f, SliderRef.TrackClickVariant.Snap, true);
        Widgets.Clear();
        Shown.Clear();
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Display = UiDisplay.Flex,
            Direction = UiDirection.Column,
            RowGap = Length.Px(10f),
        });
        ecs.Insert<TabGroupRef>(root);

        var button = Button(ecs);
        ecs.SetParent(button, root);
        ecs.Observe<Activate>(button, _ => Console.WriteLine("Button clicked!"));

        var slider = Slider(ecs, 0f, 100f, 50f);
        ecs.SetParent(slider, root);
        ecs.Observe<ValueChange<float>>(slider, on => (_sliderValue, _statesChanged) = (on.Event.Value, true));

        var checkbox = Checkbox(ecs, "Checkbox");
        ecs.SetParent(checkbox, root);
        Ui.SelfUpdate(checkbox, UiWidgetKind.Checkbox);

        var radios = RadioGroup(ecs);
        ecs.SetParent(radios, root);
        ecs.Observe<ValueChange<Entity>>(radios, on =>
        {
            if (on.Ecs.Has<DemoRadio>(on.Event.Value)) (_sliderClick, _statesChanged) = (on.Ecs.GetOrDefault<DemoRadio>(on.Event.Value).Click, true);
        });

        ecs.SetParent(MenuButton(ecs), root);
        ecs.SetParent(Ui.SpawnText("Press 'D' to toggle widget disabled states", new UiSettings()), root);
    }

    // Bevy's update_widget_values, which sets the slider's value and track click and checks the
    // radio button matching the track click, as the example's own record of them changes.
    private static void UpdateWidgetValues(BehaviorContext ctx)
    {
        if (!_statesChanged) return;
        _statesChanged = false;

        var ecs = ctx.Ecs;
        foreach (var slider in ecs.EntitiesWith<StandardSlider>())
        {
            ecs.Insert<SliderValueRef>(slider).Value = _sliderValue;
            ecs.Wrap<SliderRef>(slider).TrackClick = _sliderClick;
        }

        foreach (var radio in ecs.EntitiesWith<DemoRadio>())
        {
            var willBeChecked = ecs.GetOrDefault<DemoRadio>(radio).Click == _sliderClick;
            var isChecked = ecs.Get<CheckedRef>(radio) is not null;
            if (willBeChecked && !isChecked) ecs.Insert<CheckedRef>(radio);
            else if (!willBeChecked && isChecked) ecs.Get<CheckedRef>(radio)?.Remove();
        }
    }

    private static Entity Button(EcsWorld ecs)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(150f),
            Height = Length.Px(65f),
            Border = Sides.All(Length.Px(5f)),
            Corners = Corners.All(Length.Px(1_000_000f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            BorderColor = Black,
            Color = NormalButton,
        });
        ecs.Add(button, new DemoButton());
        ecs.Insert<BevyUiWidgetsButtonRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.Insert<TabIndexRef>(button);
        Widgets.Add(button);

        ecs.SetParent(Label(ecs, "Button", 33f), button);
        return button;
    }

    private static Entity MenuButton(EcsWorld ecs)
    {
        var anchor = Ui.SpawnNode(new UiSettings());
        ecs.Add(anchor, new DemoMenuAnchor());
        ecs.Observe<MenuEvent>(anchor, OnMenuEvent);

        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(200f),
            Height = Length.Px(65f),
            Border = Sides.All(Length.Px(5f)),
            Sizing = BoxSizing.BorderBox,
            Justify = UiJustify.SpaceBetween,
            Align = UiAlign.Center,
            Padding = Sides.Horizontal(Length.Px(16f)),
            Corners = Corners.All(Length.Px(5f)),
            BorderColor = Black,
            Color = NormalButton,
        });
        ecs.Add(button, new DemoMenuButton());
        ecs.Insert<BevyUiWidgetsButtonRef>(button);
        ecs.Insert<MenuButtonRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.Insert<TabIndexRef>(button);
        ecs.SetParent(button, anchor);
        Widgets.Add(button);

        ecs.SetParent(Label(ecs, "Menu", 33f), button);
        ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Px(12f), Height = Length.Px(12f), Color = Gray }), button);
        return anchor;
    }

    // Text in the light gray Bevy's buttons use, with its default shadow.
    private static Entity Label(EcsWorld ecs, string text, float size)
    {
        var label = Ui.SpawnText(text, new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { Font = _font, FontSize = size });
        ecs.Insert<TextShadowRef>(label);
        return label;
    }

    // A demo slider, a rail across its middle and a thumb on a track inset by the thumb's width at
    // its right, so the thumb is placed by a percentage alone and stops at the rail's ends.
    private static Entity Slider(EcsWorld ecs, float min, float max, float value)
    {
        var slider = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Direction = UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Stretch,
            ColumnGap = Length.Px(4f),
            Height = Length.Px(12f),
            Width = Length.Percent(30f),
        });
        ecs.SetName(slider, "Slider");
        ecs.Insert<HoveredRef>(slider);
        ecs.Add(slider, new StandardSlider());
        ecs.Insert<SliderRef>(slider).TrackClick = SliderRef.TrackClickVariant.Snap;
        ecs.Insert<SliderValueRef>(slider).Value = value;
        var range = ecs.Insert<SliderRangeRef>(slider);
        (range.Start, range.End) = (min, max);
        ecs.Insert<TabIndexRef>(slider);
        Widgets.Add(slider);

        // Slider background rail
        var rail = Ui.SpawnNode(new UiSettings { Height = Length.Px(6f), Corners = Corners.All(Length.Px(3f)), Color = SliderTrack });
        ecs.SetParent(rail, slider);

        // Invisible track to allow absolute placement of thumb entity. This is narrower than the
        // actual slider, which allows us to position the thumb entity using simple percentages,
        // without having to measure the actual width of the slider thumb.
        var track = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Absolute = true,
            Left = Length.Px(0f),
            Right = Length.Px(12f),
            Top = Length.Px(0f),
            Bottom = Length.Px(0f),
        });
        ecs.SetParent(track, slider);

        var thumb = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Width = Length.Px(12f),
            Height = Length.Px(12f),
            Absolute = true,
            Left = Length.Percent(0f),
            Corners = Corners.All(Length.Px(1_000_000f)),
            Color = SliderThumb,
        });
        ecs.Add(thumb, new StandardSliderThumb());
        ecs.Insert<SliderThumbRef>(thumb);
        ecs.SetParent(thumb, track);
        return slider;
    }

    private static Entity Checkbox(EcsWorld ecs, string caption)
    {
        var checkbox = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Direction = UiDirection.Row,
            Justify = UiJustify.Start,
            Align = UiAlign.Center,
            AlignContent = UiJustify.Center,
            ColumnGap = Length.Px(4f),
        });
        ecs.SetName(checkbox, "Checkbox");
        ecs.Insert<HoveredRef>(checkbox);
        ecs.Add(checkbox, new DemoCheckbox());
        ecs.Insert<CheckboxRef>(checkbox);
        ecs.Insert<TabIndexRef>(checkbox);
        Widgets.Add(checkbox);

        Mark(ecs, checkbox, Length.Px(3f), Length.Zero);
        ecs.SetParent(Ui.SpawnText(caption, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 20f }), checkbox);
        return checkbox;
    }

    // A box sixteen pixels square with its outline, and the mark inside it, which is filled while
    // the widget is checked.
    private static void Mark(EcsWorld ecs, Entity widget, Length outer, Length inner)
    {
        var box = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Width = Length.Px(16f),
            Height = Length.Px(16f),
            Border = Sides.All(Length.Px(2f)),
            Corners = Corners.All(outer),
            BorderColor = ElementOutline,
        });
        ecs.SetParent(box, widget);

        var mark = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Width = Length.Px(8f),
            Height = Length.Px(8f),
            Absolute = true,
            Left = Length.Px(2f),
            Top = Length.Px(2f),
            Corners = Corners.All(inner),
            Color = ElementFill,
        });
        ecs.SetParent(mark, box);
    }

    private static Entity RadioGroup(EcsWorld ecs)
    {
        var group = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Direction = UiDirection.Column,
            Align = UiAlign.Start,
            ColumnGap = Length.Px(4f),
        });
        ecs.SetName(group, "RadioGroup");
        ecs.Insert<RadioGroupRef>(group);
        ecs.Insert<TabIndexRef>(group);

        foreach (var (click, caption) in new[]
        {
            (SliderRef.TrackClickVariant.Drag, "Slider Drag"),
            (SliderRef.TrackClickVariant.Step, "Slider Step"),
            (SliderRef.TrackClickVariant.Snap, "Slider Snap"),
        })
        {
            var radio = Ui.SpawnNode(new UiSettings
            {
                Display = UiDisplay.Flex,
                Direction = UiDirection.Row,
                Justify = UiJustify.Start,
                Align = UiAlign.Center,
                AlignContent = UiJustify.Center,
                ColumnGap = Length.Px(4f),
            });
            ecs.SetName(radio, "RadioButton");
            ecs.Insert<HoveredRef>(radio);
            ecs.Add(radio, new DemoRadio { Click = click });
            ecs.Insert<RadioButtonRef>(radio);
            ecs.SetParent(radio, group);
            Widgets.Add(radio);

            var round = Length.Px(1_000_000f);
            Mark(ecs, radio, round, round);
            ecs.SetParent(Ui.SpawnText(caption, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 20f }), radio);
        }

        return group;
    }

    // Bevy's on_menu_event, at the menu's owner, where the event comes up to from the button, the
    // popup and its items. The menu is spawned when asked to open and despawned when asked to
    // close, and Escape gives the focus back to the owner.
    private static void OnMenuEvent(On<MenuEvent> on)
    {
        var ecs = on.Ecs;
        var anchor = on.Entity;
        var popup = ecs.ChildrenOf(anchor).Where(child => ecs.Get<MenuPopupRef>(child) is not null).Cast<Entity?>().FirstOrDefault();
        Console.WriteLine($"Menu action: {on.Event.Action}");

        switch (on.Event.Action)
        {
            case MenuAction.Open:
                if (popup is null) SpawnMenu(ecs, anchor);
                break;

            case MenuAction.Toggle:
                if (popup is { } open) ecs.Despawn(open);
                else SpawnMenu(ecs, anchor);
                break;

            case MenuAction.CloseAll:
                if (popup is { } shown) ecs.Despawn(shown);
                break;

            case MenuAction.FocusRoot:
                if (ecs.Resource<InputFocusRef>() is { } focus) focus.CurrentFocus = anchor;
                break;
        }
    }

    private static void SpawnMenu(EcsWorld ecs, Entity anchor)
    {
        var menu = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Direction = UiDirection.Column,
            MinHeight = Length.Px(10f),
            MinWidth = Length.Percent(100f),
            Border = Sides.All(Length.Px(1f)),
            Absolute = true,
            BorderColor = Green,
            Color = Gray,
        });
        ecs.Insert<MenuPopupRef>(menu);
        ecs.Insert<BoxShadowRef>(menu).Value = [new ShadowStyle(Color.FromSrgb(0f, 0f, 0f, 0.9f), new Val.Px(0f), new Val.Px(0f), new Val.Px(1f), new Val.Px(4f))];
        ecs.Insert<GlobalZIndexRef>(menu).Value = 100;
        // Below its owner, and else above it.
        var popover = ecs.Insert<PopoverRef>(menu);
        popover.Positions = [new PopoverPlacement(PopoverSide.Bottom, PopoverAlign.Start, 2f), new PopoverPlacement(PopoverSide.Top, PopoverAlign.Start, 2f)];
        popover.WindowMargin = 10f;
        ecs.Insert<OverrideClipRef>(menu);

        for (var i = 0; i < 4; i++) ecs.SetParent(MenuItem(ecs), menu);
        ecs.SetParent(menu, anchor);
    }

    private static Entity MenuItem(EcsWorld ecs)
    {
        var item = Ui.SpawnNode(new UiSettings
        {
            Padding = new Sides(Length.Px(8f), Length.Px(2f), Length.Px(8f), Length.Px(2f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Start,
            Color = NormalButton,
        });
        ecs.Add(item, new DemoMenuItem());
        ecs.Insert<MenuItemRef>(item);
        ecs.Insert<HoveredRef>(item);
        ecs.Insert<TabIndexRef>(item);

        ecs.SetParent(Label(ecs, "Menu Item", 33f), item);
        return item;
    }

    // Bevy's toggle_disabled, D disabling every widget, or enabling it where it was disabled.
    private static void ToggleDisabled(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.D)) return;

        var ecs = ctx.Ecs;
        foreach (var widget in Widgets)
        {
            if (ecs.Get<InteractionDisabledRef>(widget) is { } disabled)
            {
                Console.WriteLine("Widget enabled");
                disabled.Remove();
            }
            else
            {
                Console.WriteLine("Widget disabled");
                ecs.Insert<InteractionDisabledRef>(widget);
            }
        }
    }

    // Whether a widget is disabled, hovered, pressed and checked, as bits, the style it is shown in.
    internal static int StateOf(EcsWorld ecs, Entity widget) =>
        (ecs.Get<InteractionDisabledRef>(widget) is not null ? 1 : 0)
        | (ecs.Get<HoveredRef>(widget)?.Value == true ? 2 : 0)
        | (ecs.Get<PressedRef>(widget) is not null ? 4 : 0)
        | (ecs.Get<CheckedRef>(widget) is not null ? 8 : 0);

    // Whether the widget's state differs from the one it was last shown in, which it is then shown in.
    internal static bool Changed(Entity widget, int state)
    {
        if (Shown.TryGetValue(widget, out var shown) && shown == state) return false;
        Shown[widget] = state;
        return true;
    }

    internal static void SetBorder(EcsWorld ecs, Entity node, Color color)
    {
        var border = ecs.Wrap<BorderColorRef>(node);
        (border.Top, border.Right, border.Bottom, border.Left) = (color, color, color, color);
    }
}

/// <summary>A button in the demo's style.</summary>
[Behavior]
public partial struct DemoButton
{
    /// <summary>
    /// Bevy's update_button_style and its second half for removed markers, the button's colors and
    /// words following whether it is disabled, hovered and pressed.
    /// </summary>
    [OnUpdate]
    public void UpdateButtonStyle(BehaviorContext ctx)
    {
        var (ecs, button) = (ctx.Ecs, ctx.Entity);
        var state = StandardWidgets.StateOf(ecs, button);
        if (!StandardWidgets.Changed(button, state)) return;

        var (disabled, hovered, pressed) = ((state & 1) != 0, (state & 2) != 0, (state & 4) != 0);
        var (text, color, border) = (disabled, hovered, pressed) switch
        {
            (true, _, _) => ("Disabled", StandardWidgets.NormalButton, StandardWidgets.Gray),
            (false, true, true) => ("Press", StandardWidgets.PressedButton, StandardWidgets.Red),
            (false, true, false) => ("Hover", StandardWidgets.HoveredButton, StandardWidgets.White),
            (false, false, _) => ("Button", StandardWidgets.NormalButton, StandardWidgets.Black),
        };

        Ui.SetText(ecs.ChildrenOf(button)[0], text);
        ecs.Wrap<BackgroundColorRef>(button).Value = color;
        StandardWidgets.SetBorder(ecs, button, border);
    }
}

/// <summary>
/// A slider in the demo's style, Bevy's <c>DemoSlider</c> under another name since
/// vertical_slider's has it.
/// </summary>
[Behavior]
public partial struct StandardSlider
{
    /// <summary>
    /// Bevy's update_slider_style, the thumb placed at the slider's value and lit while the slider is
    /// hovered or dragged. Bevy's runs when those change, which no filter here asks of Bevy's
    /// components, so this runs each frame.
    /// </summary>
    [OnUpdate]
    public void UpdateSliderStyle(BehaviorContext ctx)
    {
        var (ecs, slider) = (ctx.Ecs, ctx.Entity);
        var value = ecs.Wrap<SliderValueRef>(slider).Value;
        var range = ecs.Wrap<SliderRangeRef>(slider);
        var position = Math.Clamp((value - range.Start) / (range.End - range.Start), 0f, 1f) * 100f;
        var disabled = ecs.Get<InteractionDisabledRef>(slider) is not null;
        var active = ecs.Wrap<HoveredRef>(slider).Value || (ecs.Get<SliderDragStateRef>(slider)?.Dragging ?? false);
        var color = disabled ? StandardWidgets.ElementFillDisabled
            : active ? VerticalSliderExample.Lighter(StandardWidgets.SliderThumb, 0.3f)
            : StandardWidgets.SliderThumb;

        foreach (var thumb in ecs.Descendants(slider))
        {
            if (!ecs.Has<StandardSliderThumb>(thumb)) continue;
            var node = ecs.Wrap<NodeRef>(thumb);
            if (node.Left is not Val.Percent at || at.Value != position) node.Left = new Val.Percent(position);
            var background = ecs.Wrap<BackgroundColorRef>(thumb);
            if (background.Value != color) background.Value = color;
        }
    }
}

/// <summary>
/// The demo slider's thumb, Bevy's <c>DemoSliderThumb</c> under another name since vertical_slider's
/// has it.
/// </summary>
[Behavior]
public partial struct StandardSliderThumb;

/// <summary>A checkbox in the demo's style.</summary>
[Behavior]
public partial struct DemoCheckbox
{
    /// <summary>
    /// Bevy's update_checkbox_or_radio_style for a checkbox, its outline lighter while hovered and
    /// faint while disabled, and its mark filled while it is checked.
    /// </summary>
    [OnUpdate]
    public void UpdateCheckboxStyle(BehaviorContext ctx) => DemoRadio.Style(ctx.Ecs, ctx.Entity);
}

/// <summary>A radio button in the demo's style, with the track click it chooses.</summary>
[Behavior]
public partial struct DemoRadio
{
    /// <summary>What a click on the slider's track does while this one is chosen.</summary>
    public SliderRef.TrackClickVariant Click;

    /// <summary>Bevy's update_checkbox_or_radio_style for a radio button.</summary>
    [OnUpdate]
    public void UpdateRadioStyle(BehaviorContext ctx) => Style(ctx.Ecs, ctx.Entity);

    // Bevy's set_checkbox_or_radio_style, on the widget's first child, the box, and the box's
    // first, the mark.
    internal static void Style(EcsWorld ecs, Entity widget)
    {
        var state = StandardWidgets.StateOf(ecs, widget) & ~4;
        if (!StandardWidgets.Changed(widget, state)) return;

        var (disabled, hovering, isChecked) = ((state & 1) != 0, (state & 2) != 0, (state & 8) != 0);
        var box = ecs.ChildrenOf(widget)[0];
        var mark = ecs.ChildrenOf(box)[0];

        var outline = disabled ? StandardWidgets.ElementOutline.WithAlpha(0.2f)
            : hovering ? VerticalSliderExample.Lighter(StandardWidgets.ElementOutline, 0.2f)
            : StandardWidgets.ElementOutline;
        StandardWidgets.SetBorder(ecs, box, outline);

        ecs.Wrap<BackgroundColorRef>(mark).Value = (disabled, isChecked) switch
        {
            (true, true) => StandardWidgets.ElementFillDisabled,
            (false, true) => StandardWidgets.ElementFill,
            (_, false) => new Color(0f, 0f, 0f, 0f),
        };
    }
}

/// <summary>The node the menu is spawned under, which observes what the menu asks.</summary>
[Behavior]
public partial struct DemoMenuAnchor;

/// <summary>The menu's button.</summary>
[Behavior]
public partial struct DemoMenuButton;

/// <summary>An item of the menu.</summary>
[Behavior]
public partial struct DemoMenuItem
{
    /// <summary>
    /// Bevy's update_menu_item_style and its second half for removed markers, the item lit while
    /// hovered and green while pressed.
    /// </summary>
    [OnUpdate]
    public void UpdateMenuItemStyle(BehaviorContext ctx)
    {
        var (ecs, item) = (ctx.Ecs, ctx.Entity);
        var state = StandardWidgets.StateOf(ecs, item);
        if (!StandardWidgets.Changed(item, state)) return;

        var (disabled, hovered, pressed) = ((state & 1) != 0, (state & 2) != 0, (state & 4) != 0);
        ecs.Wrap<BackgroundColorRef>(item).Value = (disabled, hovered, pressed) switch
        {
            (false, true, true) => StandardWidgets.PressedButton,
            (false, true, false) => StandardWidgets.HoveredButton,
            _ => StandardWidgets.NormalButton,
        };
    }
}

// Bevy's standard_widgets_observers example, examples/ui/widgets/standard_widgets_observers.rs at
// v0.19.1, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Bevy's widgets styled by observers rather than by systems. A button, a slider and a checkbox are
// each restyled as Bevy's own components on them come and go, pressed, hovered, checked or
// disabled, and the slider's thumb is moved as its value is inserted. D disables and enables them
// all.
internal static class StandardWidgetsObservers
{
    private static readonly Color CheckboxOutline = Color.FromSrgb(0.45f, 0.45f, 0.45f);
    private static readonly Color CheckboxCheck = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    // Bevy's DemoWidgetStates, the example's own record of the slider's value, which the slider is
    // set from, and whether it changed this frame.
    private static float _sliderValue;
    private static bool _statesChanged;

    // Every widget D disables, Bevy's query over Button, Slider and Checkbox.
    private static readonly List<Entity> Widgets = [];

    private static AssetHandle _font;

    public static void Build(App app)
    {
        app.Startup(Setup, "standard_widgets_observers.Setup");
        app.Update(UpdateWidgetValues, "standard_widgets_observers.UpdateWidgetValues");
        app.Update(ToggleDisabled, "standard_widgets_observers.ToggleDisabled");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_sliderValue, _statesChanged) = (50f, true);
        Widgets.Clear();
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        // Bevy's observers of the widgets' own components, added to its app before anything is
        // spawned, each restyling the widget it hears of where that is one of the demo's.
        void AddedAndRemoved<TComponent>(Action<EcsWorld, Entity> restyle) where TComponent : struct
        {
            ecs.Observe<Add<TComponent>>(on => restyle(on.Ecs, on.Event.Entity));
            ecs.Observe<Remove<TComponent>>(on => restyle(on.Ecs, on.Event.Entity));
        }

        AddedAndRemoved<PressedRef>(ButtonOnInteraction);
        AddedAndRemoved<InteractionDisabledRef>(ButtonOnInteraction);
        ecs.Observe<Insert<HoveredRef>>(on => ButtonOnInteraction(on.Ecs, on.Event.Entity));
        AddedAndRemoved<InteractionDisabledRef>(SliderOnInteraction);
        ecs.Observe<Insert<HoveredRef>>(on => SliderOnInteraction(on.Ecs, on.Event.Entity));
        ecs.Observe<Insert<SliderValueRef>>(on => SliderOnChangeValue(on.Ecs, on.Event.Entity));
        ecs.Observe<Insert<SliderRangeRef>>(on => SliderOnChangeValue(on.Ecs, on.Event.Entity));
        AddedAndRemoved<InteractionDisabledRef>(CheckboxOnInteraction);
        ecs.Observe<Insert<HoveredRef>>(on => CheckboxOnInteraction(on.Ecs, on.Event.Entity));
        AddedAndRemoved<CheckedRef>(CheckboxOnInteraction);

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

        ecs.SetParent(Ui.SpawnText("Press 'D' to toggle widget disabled states", new UiSettings()), root);
    }

    private static Entity Button(EcsWorld ecs)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(150f),
            Height = Length.Px(65f),
            Border = Sides.All(Length.Px(5f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Corners = Corners.All(Length.Px(1_000_000f)),
            BorderColor = StandardWidgets.Black,
            Color = StandardWidgets.NormalButton,
        });
        ecs.Add(button, new ObservedButton());
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.Insert<TabIndexRef>(button);
        Widgets.Add(button);

        var label = Ui.SpawnText("Button", new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { Font = _font, FontSize = 33f });
        ecs.Insert<TextShadowRef>(label);
        ecs.SetParent(label, button);
        return button;
    }

    // Bevy's button_on_interaction, the button's colors and words following whether it is
    // disabled, hovered and pressed. Bevy's observer of a removal runs while the component is still
    // there, so it checks which event it was, and one here runs once the component has gone.
    private static void ButtonOnInteraction(EcsWorld ecs, Entity button)
    {
        if (!ecs.Has<ObservedButton>(button) || ecs.Get<HoveredRef>(button) is not { } hovered) return;
        var children = ecs.ChildrenOf(button);
        if (children.Length == 0) return;

        var disabled = ecs.Get<InteractionDisabledRef>(button) is not null;
        var pressed = ecs.Get<PressedRef>(button) is not null;
        var (text, color, border) = (disabled, hovered.Value, pressed) switch
        {
            (true, _, _) => ("Disabled", StandardWidgets.NormalButton, StandardWidgets.Gray),
            (false, true, true) => ("Press", StandardWidgets.PressedButton, StandardWidgets.Red),
            (false, true, false) => ("Hover", StandardWidgets.HoveredButton, StandardWidgets.White),
            (false, false, _) => ("Button", StandardWidgets.NormalButton, StandardWidgets.Black),
        };

        Ui.SetText(children[0], text);
        ecs.Wrap<BackgroundColorRef>(button).Value = color;
        StandardWidgets.SetBorder(ecs, button, border);
    }

    // A demo slider, a rail across its middle and a thumb on a track inset by the thumb's width at
    // its right, so the thumb is placed by a percentage alone.
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
        ecs.Add(slider, new ObservedSlider());
        ecs.Insert<HoveredRef>(slider);
        ecs.Insert<SliderRef>(slider);
        Widgets.Add(slider);

        var rail = Ui.SpawnNode(new UiSettings { Height = Length.Px(6f), Corners = Corners.All(Length.Px(3f)), Color = StandardWidgets.SliderTrack });
        ecs.SetParent(rail, slider);

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
            Color = StandardWidgets.SliderThumb,
        });
        ecs.Add(thumb, new ObservedSliderThumb());
        ecs.Insert<SliderThumbRef>(thumb);
        ecs.SetParent(thumb, track);

        // The value and range last, once the thumb is there for their observer to place.
        ecs.Insert<SliderValueRef>(slider).Value = value;
        var range = ecs.Insert<SliderRangeRef>(slider);
        (range.Start, range.End) = (min, max);
        ecs.Insert<TabIndexRef>(slider);
        return slider;
    }

    // Bevy's slider_on_interaction, the thumb lit while the slider is hovered and gray while it is
    // disabled.
    private static void SliderOnInteraction(EcsWorld ecs, Entity slider)
    {
        if (!ecs.Has<ObservedSlider>(slider) || ecs.Get<HoveredRef>(slider) is not { } hovered) return;

        var disabled = ecs.Get<InteractionDisabledRef>(slider) is not null;
        var color = disabled ? StandardWidgets.Gray
            : hovered.Value ? VerticalSliderExample.Lighter(StandardWidgets.SliderThumb, 0.3f)
            : StandardWidgets.SliderThumb;

        foreach (var thumb in ecs.Descendants(slider))
        {
            if (ecs.Has<ObservedSliderThumb>(thumb)) ecs.Wrap<BackgroundColorRef>(thumb).Value = color;
        }
    }

    // Bevy's slider_on_change_value, the thumb placed at the slider's value within its range as
    // either is inserted.
    private static void SliderOnChangeValue(EcsWorld ecs, Entity slider)
    {
        if (!ecs.Has<ObservedSlider>(slider)
            || ecs.Get<SliderValueRef>(slider) is not { } value
            || ecs.Get<SliderRangeRef>(slider) is not { } range)
            return;

        var position = Math.Clamp((value.Value - range.Start) / (range.End - range.Start), 0f, 1f) * 100f;
        foreach (var thumb in ecs.Descendants(slider))
        {
            if (ecs.Has<ObservedSliderThumb>(thumb)) ecs.Wrap<NodeRef>(thumb).Left = new Val.Percent(position);
        }
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
        ecs.Add(checkbox, new ObservedCheckbox());

        // Checkbox outer
        var box = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Width = Length.Px(16f),
            Height = Length.Px(16f),
            Border = Sides.All(Length.Px(2f)),
            Corners = Corners.All(Length.Px(3f)),
            BorderColor = CheckboxOutline,
        });
        ecs.SetParent(box, checkbox);

        // Checkbox inner
        var mark = Ui.SpawnNode(new UiSettings
        {
            Display = UiDisplay.Flex,
            Width = Length.Px(8f),
            Height = Length.Px(8f),
            Absolute = true,
            Left = Length.Px(2f),
            Top = Length.Px(2f),
        });
        ecs.SetParent(mark, box);
        ecs.SetParent(Ui.SpawnText(caption, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 20f }), checkbox);

        // The widget last, once its box and mark are there for its observers to style.
        ecs.Insert<HoveredRef>(checkbox);
        ecs.Insert<CheckboxRef>(checkbox);
        ecs.Insert<TabIndexRef>(checkbox);
        Widgets.Add(checkbox);
        return checkbox;
    }

    // Bevy's checkbox_on_interaction, the outline lighter while hovered and faint while disabled,
    // and the mark filled while checked, half seen while also disabled.
    private static void CheckboxOnInteraction(EcsWorld ecs, Entity checkbox)
    {
        if (!ecs.Has<ObservedCheckbox>(checkbox) || ecs.Get<HoveredRef>(checkbox) is not { } hovered) return;
        var children = ecs.ChildrenOf(checkbox);
        if (children.Length == 0) return;
        var box = children[0];
        var marks = ecs.ChildrenOf(box);
        if (marks.Length == 0) return;

        var disabled = ecs.Get<InteractionDisabledRef>(checkbox) is not null;
        var isChecked = ecs.Get<CheckedRef>(checkbox) is not null;

        var outline = disabled ? CheckboxOutline.WithAlpha(0.2f)
            : hovered.Value ? VerticalSliderExample.Lighter(CheckboxOutline, 0.2f)
            : CheckboxOutline;
        StandardWidgets.SetBorder(ecs, box, outline);

        var mark = (disabled, isChecked) switch
        {
            (true, true) => CheckboxCheck.WithAlpha(0.5f),
            (false, true) => CheckboxCheck,
            (_, false) => new Color(0f, 0f, 0f, 0f),
        };
        var background = ecs.Wrap<BackgroundColorRef>(marks[0]);
        if (background.Value != mark) background.Value = mark;
    }

    // Bevy's update_widget_values, the slider's value inserted again as the example's own record
    // of it changes.
    private static void UpdateWidgetValues(BehaviorContext ctx)
    {
        if (!_statesChanged) return;
        _statesChanged = false;

        foreach (var slider in ctx.Ecs.EntitiesWith<ObservedSlider>()) ctx.Ecs.Insert<SliderValueRef>(slider).Value = _sliderValue;
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
}

/// <summary>
/// A button in the demo's style, Bevy's <c>DemoButton</c> under another name since
/// standard_widgets' has it.
/// </summary>
[Behavior]
public partial struct ObservedButton;

/// <summary>
/// A slider in the demo's style, Bevy's <c>DemoSlider</c> under another name since
/// vertical_slider's has it.
/// </summary>
[Behavior]
public partial struct ObservedSlider;

/// <summary>
/// The slider's thumb, Bevy's <c>DemoSliderThumb</c> under another name since vertical_slider's has
/// it.
/// </summary>
[Behavior]
public partial struct ObservedSliderThumb;

/// <summary>
/// A checkbox in the demo's style, Bevy's <c>DemoCheckbox</c> under another name since
/// standard_widgets' has it.
/// </summary>
[Behavior]
public partial struct ObservedCheckbox;

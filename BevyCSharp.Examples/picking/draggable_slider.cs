// Bevy's draggable_slider example, examples/picking/draggable_slider.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Demonstrates why pointer capture matters during drag operations. A slider sits above a row of
// buttons. Without capture, dragging the slider thumb over the buttons makes them show a hover
// highlight, as if one were about to be pressed. With capture the thumb holds the pointer for the
// whole drag, so no other widget is hovered. A button at the bottom turns capture on and off, a
// fill bar grows with the value between the minimum and maximum labels, and Reset puts the value
// back.
internal static class DraggableSlider
{
    private const float TrackWidth = 500f, TrackHeight = 14f;
    private const float ThumbSize = 28f, ThumbHalf = ThumbSize / 2f;
    private const float FillHeight = 8f;
    private const float DefaultValue = 0.5f;

    private static readonly Color Track = Color.FromSrgb(0.2f, 0.2f, 0.2f);
    private static readonly Color Fill = Color.FromSrgb(0.35f, 0.65f, 0.35f);
    private static readonly Color FillDragging = Color.FromSrgb(0.45f, 0.85f, 0.45f);
    private static readonly Color ThumbIdle = Color.FromSrgb(0.85f, 0.55f, 0.1f);
    private static readonly Color ThumbHover = Color.FromSrgb(1f, 0.75f, 0.2f);
    private static readonly Color DecoyIdle = Color.FromSrgb(0.2f, 0.45f, 0.8f);
    private static readonly Color DecoyHover = Color.FromSrgb(0.9f, 0.2f, 0.2f);
    private static readonly Color ToggleOn = Color.FromSrgb(0.15f, 0.65f, 0.3f);
    private static readonly Color ToggleOff = Color.FromSrgb(0.55f, 0.15f, 0.15f);
    private static readonly Color ResetIdle = Color.FromSrgb(0.3f, 0.3f, 0.55f);
    private static readonly Color ResetHover = Color.FromSrgb(0.5f, 0.5f, 0.75f);

    // Bevy's SliderValue and CaptureEnabled resources.
    private static float _value;
    private static bool _capture;

    public static void Build(App app)
    {
        app.Startup(Setup, "draggable_slider.Setup");
        app.Update(SyncThumbAndLabel, "draggable_slider.SyncThumbAndLabel");
        app.Update(SyncFill, "draggable_slider.SyncFill");
    }

    // The left edge of the thumb, and the width of the fill, which reaches the thumb's middle.
    private static float ThumbLeft(float value) => Math.Clamp(value, 0f, 1f) * (TrackWidth - ThumbSize);

    private static float FillWidth(float value) => ThumbLeft(value) + ThumbHalf;

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_value, _capture) = (DefaultValue, true);
        Render2d.SpawnCamera2d();

        // The page, a column in the middle, which the pointer passes through.
        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Column,
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            RowGap = Length.Px(28f),
        });
        Ignored(ecs, root);

        ecs.SetParent(Text(ecs, "Drag the slider over the blue buttons.\nWith capture ON they stay blue. With capture OFF they incorrectly turn red.", 15f, Color.FromSrgb(0.75f, 0.75f, 0.75f), pickable: true), root);

        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, ColumnGap = Length.Px(10f) });
        Ignored(ecs, row);
        ecs.SetParent(row, root);
        ecs.SetParent(Text(ecs, "0.0", 13f, Color.FromSrgb(0.55f, 0.55f, 0.55f), pickable: true), row);

        var track = Ui.SpawnNode(new UiSettings { Width = Length.Px(TrackWidth), Height = Length.Px(TrackHeight), Align = UiAlign.Center, Color = Track });
        Ignored(ecs, track);
        ecs.SetParent(track, row);

        var fill = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Width = Length.Px(FillWidth(DefaultValue)), Height = Length.Px(FillHeight), Color = Fill });
        Ignored(ecs, fill);
        ecs.Add(fill, new SliderFill());
        ecs.SetParent(fill, track);

        var thumb = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(ThumbLeft(DefaultValue)),
            Width = Length.Px(ThumbSize),
            Height = Length.Px(ThumbSize),
            Corners = Corners.All(Length.Px(4f)),
            Color = ThumbIdle,
        });
        ecs.Add(thumb, new SliderThumb());
        ecs.SetParent(thumb, track);

        // The thumb takes the pointer as the drag starts, where capture is on, and lets it go as it
        // ends. Bevy releases it too when the button is let go.
        ecs.Observe<Pointer<DragStart>>(thumb, on =>
        {
            if (_capture) Picking.CapturePointer(on.Event.PointerId, on.Event.Entity, on.Event.Event.Hit);
            if (on.Ecs.EntitiesWith<SliderFill>() is [var fill]) on.Ecs.Wrap<BackgroundColorRef>(fill).Value = FillDragging;
        });
        ecs.Observe<Pointer<Drag>>(thumb, on => _value = Math.Clamp(_value + on.Event.Event.Delta.X / (TrackWidth - ThumbSize), 0f, 1f));
        ecs.Observe<Pointer<DragEnd>>(thumb, on =>
        {
            Picking.ReleaseCapture(on.Event.PointerId);
            if (on.Ecs.EntitiesWith<SliderFill>() is [var fill]) on.Ecs.Wrap<BackgroundColorRef>(fill).Value = Fill;
        });
        Hover(ecs, thumb, ThumbIdle, ThumbHover);

        ecs.SetParent(Text(ecs, "1.0", 13f, Color.FromSrgb(0.55f, 0.55f, 0.55f), pickable: true), row);

        var valueLabel = Text(ecs, $"Value: {DefaultValue:F2}", 18f, Color.White, pickable: true);
        ecs.Add(valueLabel, new SliderLabel());
        ecs.SetParent(valueLabel, root);

        // The buttons the thumb is dragged across, which redden when the pointer is over them.
        var decoys = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(16f) });
        Ignored(ecs, decoys);
        ecs.SetParent(decoys, root);
        foreach (var label in new[] { "Button A", "Button B", "Button C", "Button D", "Button E" })
        {
            var (decoy, _) = Button(ecs, label, 15f, DecoyIdle, new Sides(Length.Px(24f), Length.Px(14f), Length.Px(24f), Length.Px(14f)));
            ecs.Add(decoy, new DecoyButton());
            ecs.SetParent(decoy, decoys);
            Hover(ecs, decoy, DecoyIdle, DecoyHover);
        }

        var controls = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(20f), Align = UiAlign.Center });
        Ignored(ecs, controls);
        ecs.SetParent(controls, root);

        var (toggle, toggleLabel) = Button(ecs, "Capture: ON  (click to toggle)", 16f, ToggleOn, Padding());
        ecs.Add(toggle, new ToggleButton());
        ecs.Add(toggleLabel, new ToggleLabel());
        ecs.SetParent(toggle, controls);
        ecs.Observe<Pointer<Click>>(toggle, on =>
        {
            _capture = !_capture;
            if (on.Ecs.EntitiesWith<ToggleLabel>() is [var label])
                Ui.SetText(label, _capture ? "Capture: ON  (click to toggle)" : "Capture: OFF  (click to toggle)");
            if (on.Ecs.EntitiesWith<ToggleButton>() is [var button])
                on.Ecs.Wrap<BackgroundColorRef>(button).Value = _capture ? ToggleOn : ToggleOff;
        });

        var (reset, _) = Button(ecs, "Reset", 16f, ResetIdle, Padding());
        ecs.Add(reset, new ResetButton());
        ecs.SetParent(reset, controls);
        Hover(ecs, reset, ResetIdle, ResetHover);
        ecs.Observe<Pointer<Click>>(reset, _ => _value = DefaultValue);
    }

    private static Sides Padding() => new(Length.Px(20f), Length.Px(10f), Length.Px(20f), Length.Px(10f));

    // A rounded button with its label, the label passing the pointer through to it.
    private static (Entity Button, Entity Label) Button(EcsWorld ecs, string label, float size, Color color, Sides padding)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Padding = padding,
            Corners = Corners.All(Length.Px(6f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = color,
        });
        var text = Text(ecs, label, size, Color.White, pickable: false);
        ecs.SetParent(text, button);
        return (button, text);
    }

    private static Entity Text(EcsWorld ecs, string text, float size, Color color, bool pickable)
    {
        var entity = Ui.SpawnText(text, new UiSettings { Color = color }, new UiTextSettings { FontSize = size });
        if (!pickable) Ignored(ecs, entity);
        return entity;
    }

    // Colored one way while the pointer is over it and another once it leaves.
    private static void Hover(EcsWorld ecs, Entity entity, Color idle, Color hover)
    {
        ecs.Observe<Pointer<Over>>(entity, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = hover);
        ecs.Observe<Pointer<Out>>(entity, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = idle);
    }

    // Bevy's Pickable::IGNORE, passed through and never hovered.
    private static void Ignored(EcsWorld ecs, Entity entity)
    {
        var pickable = ecs.Insert<PickableRef>(entity);
        (pickable.ShouldBlockLower, pickable.IsHoverable) = (false, false);
    }

    // The thumb placed and the readout written from the value.
    private static void SyncThumbAndLabel(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ecs.EntitiesWith<SliderThumb>() is not [var thumb] || ecs.EntitiesWith<SliderLabel>() is not [var label]) return;
        ecs.Wrap<NodeRef>(thumb).Left = new Val.Px(ThumbLeft(_value));
        Ui.SetText(label, $"Value: {_value:F2}");
    }

    private static void SyncFill(BehaviorContext ctx)
    {
        if (ctx.Ecs.EntitiesWith<SliderFill>() is [var fill]) ctx.Ecs.Wrap<NodeRef>(fill).Width = new Val.Px(FillWidth(_value));
    }
}

/// <summary>The slider's thumb, which the pointer drags, Bevy's example's <c>SliderThumb</c>.</summary>
[Behavior]
public partial struct SliderThumb;

/// <summary>The bar growing with the value, Bevy's example's <c>SliderFill</c>.</summary>
[Behavior]
public partial struct SliderFill;

/// <summary>The readout of the value, Bevy's example's <c>SliderLabel</c>.</summary>
[Behavior]
public partial struct SliderLabel;

/// <summary>The label of the button turning capture on and off, Bevy's example's <c>ToggleLabel</c>.</summary>
[Behavior]
public partial struct ToggleLabel;

/// <summary>The button turning capture on and off, Bevy's example's <c>ToggleButton</c>.</summary>
[Behavior]
public partial struct ToggleButton;

/// <summary>The button putting the value back, Bevy's example's <c>ResetButton</c>.</summary>
[Behavior]
public partial struct ResetButton;

/// <summary>A button the thumb is dragged across, Bevy's example's <c>DecoyButton</c>.</summary>
[Behavior]
public partial struct DecoyButton;

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows Bevy's slider widget standing up and lying down, each with its value written above it, the
// thumb lighter while the pointer is on the slider or drags it.
internal static class VerticalSlider
{
    private static readonly Color Track = Color.FromSrgb(0.05f, 0.05f, 0.05f);
    private static readonly Color Thumb = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static readonly List<(Entity Slider, Entity Thumb, Entity Label, bool Vertical)> Sliders = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "vertical_slider.Setup");
        app.Update(UpdateSliders, "vertical_slider.UpdateSliders");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Sliders.Clear();
        Render2d.SpawnCamera2d();

        var page = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Direction = UiDirection.Row,
            ColumnGap = Length.Px(50f),
        });
        ecs.Insert<TabGroupRef>(page);

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        var light = Scene.Srgb(0.9f, 0.9f, 0.9f);
        foreach (var (title, vertical) in new[] { ("Vertical", true), ("Horizontal", false) })
        {
            var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, RowGap = Length.Px(10f) });
            ecs.SetParent(column, page);
            ecs.SetParent(Ui.SpawnText(title, new UiSettings { Color = light }, new UiTextSettings { Font = font, FontSize = 20f }), column);
            var label = Ui.SpawnText("50", new UiSettings { Color = light }, new UiTextSettings { Font = font, FontSize = 24f });
            ecs.SetParent(label, column);

            var (slider, thumb) = SpawnSlider(ecs, vertical);
            ecs.SetParent(slider, column);
            Sliders.Add((slider, thumb, label, vertical));
        }
    }

    // A track down the middle and a thumb that travels along it, its travel inset by its own size
    // at one end so it stops at the track's ends.
    private static (Entity Slider, Entity Thumb) SpawnSlider(EcsWorld ecs, bool vertical)
    {
        var slider = Ui.SpawnNode(new UiSettings
        {
            Direction = vertical ? UiDirection.Row : UiDirection.Column,
            Justify = UiJustify.Center,
            Align = UiAlign.Stretch,
            ColumnGap = Length.Px(4f),
            Width = Length.Px(vertical ? 12f : 200f),
            Height = Length.Px(vertical ? 200f : 12f),
        });
        ecs.Insert<HoveredRef>(slider);
        ecs.Insert<SliderRef>(slider).TrackClick = SliderRef.TrackClickVariant.Snap;
        ecs.Insert<SliderValueRef>(slider).Value = 50f;
        var range = ecs.Insert<SliderRangeRef>(slider);
        (range.Start, range.End) = (0f, 100f);
        ecs.Insert<TabIndexRef>(slider);
        Ui.SelfUpdate(slider, UiWidgetKind.Slider);

        var track = Ui.SpawnNode(new UiSettings
        {
            Width = vertical ? Length.Px(6f) : Length.Auto,
            Height = vertical ? Length.Auto : Length.Px(6f),
            Corners = Corners.All(Length.Px(3f)),
            Color = (Track.R, Track.G, Track.B, 1f),
        });
        ecs.SetParent(track, slider);

        var travel = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Top = Length.Px(vertical ? 12f : 0f),
            Bottom = Length.Px(0f),
            Left = Length.Px(0f),
            Right = Length.Px(vertical ? 0f : 12f),
        });
        ecs.SetParent(travel, slider);

        var thumb = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Width = Length.Px(12f),
            Height = Length.Px(12f),
            Bottom = vertical ? Length.Percent(0f) : Length.Auto,
            Left = vertical ? Length.Auto : Length.Percent(0f),
            Corners = Corners.All(Length.Px(1_000_000f)),
            Color = (Thumb.R, Thumb.G, Thumb.B, 1f),
        });
        ecs.Insert<SliderThumbRef>(thumb);
        ecs.SetParent(thumb, travel);
        return (slider, thumb);
    }

    // Each slider's thumb placed at its value and lit while the slider is hovered or dragged, and
    // its value written above it.
    private static void UpdateSliders(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var (slider, thumb, label, vertical) in Sliders)
        {
            var value = ecs.Wrap<SliderValueRef>(slider).Value;
            var range = ecs.Wrap<SliderRangeRef>(slider);
            var position = Math.Clamp((value - range.Start) / (range.End - range.Start), 0f, 1f) * 100f;

            var node = ecs.Wrap<NodeRef>(thumb);
            if (vertical) node.Bottom = new Val.Percent(position);
            else node.Left = new Val.Percent(position);

            var active = ecs.Wrap<HoveredRef>(slider).Value || (ecs.Get<SliderDragStateRef>(slider)?.Dragging ?? false);
            ecs.Wrap<BackgroundColorRef>(thumb).Value = active ? Lighter(Thumb, 0.3f) : Thumb;
            Ui.SetText(label, value.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    // Bevy's Color::lighter, which raises a linear color's luminance by the amount by mixing it
    // toward white.
    private static Color Lighter(Color color, float amount)
    {
        var luminance = 0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;
        var target = Math.Clamp(luminance + amount, 0f, 1f);
        var t = (target - luminance) / (1f - luminance);
        return new Color(color.R + (1f - color.R) * t, color.G + (1f - color.G) * t, color.B + (1f - color.B) * t, color.A);
    }
}

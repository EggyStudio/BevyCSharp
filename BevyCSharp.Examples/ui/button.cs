// Bevy's button example, examples/ui/widgets/button.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Illustrates a button of Bevy's widgets, which tracks its own press and reports a click as an
// Activate and comes with no look of its own, so its label, background and border are chosen each
// frame from whether it is hovered and pressed.
internal static class ButtonExample
{
    private static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static Entity _button, _label;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
            _button = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Px(150f),
                Height = Length.Px(65f),
                Border = Sides.All(Length.Px(5f)),
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                Corners = Corners.All(Length.Px(1_000_000f)),
                BorderColor = (0f, 0f, 0f, 1f),
                Color = Normal,
            });
            ecs.Insert<ButtonRef>(_button);
            ecs.Insert<HoveredRef>(_button);
            ecs.SetParent(_button, middle);

            var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
            _label = Ui.SpawnText("Button", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f });
            ecs.Insert<TextShadowRef>(_label);
            ecs.SetParent(_label, _button);

            // The button reports a completed click as an Activate, which an observer on it answers.
            ecs.Observe<Activate>(_button, _ => Console.WriteLine("Button clicked!"));
        }, "button.Setup");

        // Bevy's update_button_appearance, the look read from Hovered and Pressed every frame.
        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var hovered = ecs.Get<HoveredRef>(_button)?.Value == true;
            var pressed = ecs.Get<PressedRef>(_button) is not null;
            var (text, color, border) = (hovered, pressed) switch
            {
                (_, true) => ("Press", Pressed, Color.FromSrgb(1f, 0f, 0f)),
                (true, false) => ("Hover", Hovered, Color.White),
                _ => ("Button", Normal, Color.Black),
            };
            Ui.SetText(_label, text);
            ecs.Wrap<BackgroundColorRef>(_button).Value = color;
            var edge = ecs.Wrap<BorderColorRef>(_button);
            edge.Top = edge.Right = edge.Bottom = edge.Left = border;
        }, "button.UpdateButtonAppearance");
    }
}

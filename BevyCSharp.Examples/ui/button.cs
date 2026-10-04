using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Illustrates a button, its label, background and border changing as the pointer moves over it and
// presses it. Bevy's also gives the button the input focus, which is not reachable here, so this
// is written in part.
internal static class ButtonExample
{

    private static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static Entity _button, _label;
    private static UiInteraction _last;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _last = UiInteraction.None;
            Render2d.SpawnCamera2d();

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
            _button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Width = Length.Px(150f),
                Height = Length.Px(65f),
                Border = Sides.All(Length.Px(5f)),
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                Corners = Corners.All(Length.Px(1_000_000f)),
                BorderColor = (1f, 1f, 1f, 1f),
                Color = (0f, 0f, 0f, 1f),
            });
            ecs.SetParent(_button, middle);

            var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
            _label = Ui.SpawnText("Button", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f });
            ecs.Insert<TextShadowRef>(_label);
            ecs.SetParent(_label, _button);
        }, "button.Setup");

        // Each change of the pointer over the button, as Bevy's Changed<Interaction> sees it.
        app.Update(ctx =>
        {
            var interaction = Ui.InteractionOf(_button);
            if (interaction == _last) return;
            _last = interaction;

            var (text, color, border) = interaction switch
            {
                UiInteraction.Pressed => ("Press", Pressed, Color.FromSrgb(1f, 0f, 0f)),
                UiInteraction.Hovered => ("Hover", Hovered, Color.White),
                _ => ("Button", Normal, Color.Black),
            };
            Ui.SetText(_label, text);
            ctx.Ecs.Wrap<BackgroundColorRef>(_button).Value = color;
            var edge = ctx.Ecs.Wrap<BorderColorRef>(_button);
            edge.Top = edge.Right = edge.Bottom = edge.Left = border;
        }, "button.ButtonSystem");
    }
}

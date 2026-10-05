// Bevy's scale_factor_override example, examples/window/scale_factor_override.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Windowing;

// A window of 500 by 300 whose scale factor is overridden to one, shown in its title and beside a
// panel. Enter turns the override off and on, and the up and down arrows raise and lower it.
internal static class ScaleFactorOverride
{
    private static Entity _text;

    public static void Configure(Config config) => (config.Width, config.Height) = (500, 300);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var window = Window.Entity();
            if (window != Entity.None) ecs.Wrap<WindowRef>(window).ResolutionScaleFactorOverride = 1f;

            Render2d.SpawnCamera2d();
            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.SpaceBetween });
            var panel = Ui.SpawnNode(new UiSettings { Width = Length.Px(300f), Height = Length.Percent(100f), Border = Sides.All(Length.Px(2f)), Color = Scene.Srgb(0.65f, 0.65f, 0.65f) });
            ecs.SetParent(panel, root);
            _text = Ui.SpawnText("Example text", new UiSettings { AlignSelf = UiAlignSelf.FlexEnd }, 25f);
            ecs.SetParent(_text, panel);
        }, "scale_factor_override.Setup");

        app.Update(ctx =>
        {
            var window = Window.Entity();
            if (window == Entity.None) return;
            var settings = ctx.Ecs.Wrap<WindowRef>(window);
            var overridden = settings.ResolutionScaleFactorOverride;

            if (ctx.Input.KeyPressed(Key.Enter)) settings.ResolutionScaleFactorOverride = overridden is null ? 1f : null;
            else if (ctx.Input.KeyPressed(Key.ArrowUp) && overridden is { } up) settings.ResolutionScaleFactorOverride = up + 1f;
            else if (ctx.Input.KeyPressed(Key.ArrowDown) && overridden is { } down) settings.ResolutionScaleFactorOverride = MathF.Max(down - 1f, 1f);
        }, "scale_factor_override.ChangeScaleFactor");

        // The window's scale factor, in its title and in the panel, and whether it is overridden.
        app.Update(ctx =>
        {
            var window = Window.Entity();
            if (window == Entity.None) return;
            var settings = ctx.Ecs.Wrap<WindowRef>(window);
            var text = string.Format(CultureInfo.InvariantCulture, "Scale factor: {0:0.0} {1}",
                Window.Scale(), settings.ResolutionScaleFactorOverride is null ? "(default)" : "(overridden)");
            Window.SetTitle(text);
            Ui.SetText(_text, text);
        }, "scale_factor_override.DisplayOverride");
    }
}

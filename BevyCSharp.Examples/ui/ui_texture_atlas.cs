using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows a frame of a sprite sheet drawn by an interface node, outlined in crimson, Space stepping
// through the frames.
internal static class UiTextureAtlas
{
    private const string Outline = "bevy_ui::ui_node::Outline";

    private static Entity _image;
    private static UiImageSettings _settings = new();

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();

            var column = Ui.SpawnNode(new UiSettings
            {
                Width = Length.Percent(100f),
                Height = Length.Percent(100f),
                Direction = UiDirection.Column,
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                RowGap = Length.Px(40f),
            });

            _image = Ui.SpawnNode(new UiSettings { Width = Length.Px(256f), Height = Length.Px(256f), Color = Scene.Srgb8(250, 235, 215) });
            _settings = new UiImageSettings
            {
                Image = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings()),
                Atlas = Render2d.CreateAtlas(24, 24, 7, 1),
            };
            Ui.SetImage(_image, _settings);
            ecs.InsertReflected(_image, Outline);
            ecs.SetReflected(_image, Outline, ".width", "{\"Px\":8.0}");
            ecs.SetReflected(_image, Outline, ".offset", "{\"Px\":0.0}");
            ecs.SetReflectedColor(_image, Outline, ".color", Color.FromSrgb(220f / 255f, 20f / 255f, 60f / 255f));
            ecs.SetParent(_image, column);

            var style = new UiTextSettings { FontSize = 20f };
            var text = Ui.SpawnText("press ", new UiSettings(), style);
            Ui.SpawnTextSpan(text, "space", style, Scene.Srgb(1f, 1f, 0f));
            Ui.SpawnTextSpan(text, " to advance frames", style, (1f, 1f, 1f, 1f));
            ecs.SetParent(text, column);
        }, "ui_texture_atlas.Setup");

        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.Space)) return;
            _settings.Frame = (_settings.Frame + 1) % 6;
            Ui.SetImage(_image, _settings);
        }, "ui_texture_atlas.IncrementAtlasIndex");
    }
}

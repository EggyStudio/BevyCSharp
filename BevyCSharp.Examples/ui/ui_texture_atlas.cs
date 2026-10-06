// Bevy's ui_texture_atlas example, examples/ui/images/ui_texture_atlas.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows a frame of a sprite sheet drawn by an interface node, outlined in crimson, Space stepping
// through the frames.
internal static class UiTextureAtlas
{

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

            _image = Ui.SpawnNode(new UiSettings { Width = Length.Px(256f), Height = Length.Px(256f), Color = Color.FromSrgb8(250, 235, 215) });
            _settings = new UiImageSettings
            {
                Image = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings()),
                Atlas = Render2d.CreateAtlas(24, 24, 7, 1),
            };
            Ui.SetImage(_image, _settings);
            var outline = ecs.Insert<OutlineRef>(_image);
            (outline.Width, outline.Offset, outline.Color) = (new Val.Px(8f), new Val.Px(0f), Color.FromSrgb(220f / 255f, 20f / 255f, 60f / 255f));
            ecs.SetParent(_image, column);

            var style = new UiTextSettings { FontSize = 20f };
            var text = Ui.SpawnText("press ", new UiSettings(), style);
            Ui.SpawnTextSpan(text, "space", style, Color.FromSrgb(1f, 1f, 0f));
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

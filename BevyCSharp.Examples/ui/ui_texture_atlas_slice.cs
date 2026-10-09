// Bevy's ui_texture_atlas_slice example, examples/ui/images/ui_texture_atlas_slice.rs at v0.20.0,
// by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Illustrates nine-slice frames of a sprite sheet in the interface, three buttons each sliced from
// a different border in the sheet, a press moving a button on to the next border.
internal static class UiTextureAtlasSlice
{
    private static readonly List<(Entity Button, Entity Label, UiImageSettings Image)> Buttons = [];
    private static readonly Dictionary<Entity, UiInteraction> Last = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Buttons.Clear();
            Last.Clear();
            Render2d.SpawnCamera2d();

            var sheet = AssetServer.Load(AssetKind.Image, "textures/fantasy_ui_borders/border_sheet.png");
            var layout = Render2d.CreateAtlas(50, 50, 6, 6, padding: (2, 2));
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });

            foreach (var (index, w, h) in new[] { (0u, 150f, 150f), (7u, 300f, 150f), (13u, 150f, 300f) })
            {
                var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(w), Height = Length.Px(h), Justify = UiJustify.Center, Align = UiAlign.Center, Margin = Sides.All(Length.Px(20f)) });
                var image = new UiImageSettings { Image = sheet, Atlas = layout, Frame = index, Mode = UiImageMode.Sliced, SliceBorder = (24f, 24f, 24f, 24f) };
                Ui.SetImage(button, image);
                ecs.SetParent(button, middle);

                var label = Ui.SpawnText("Button", new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { Font = font, FontSize = 33f });
                ecs.SetParent(label, button);
                Buttons.Add((button, label, image));
            }
        }, "ui_texture_atlas_slice.Setup");

        app.Update(ctx =>
        {
            foreach (var (button, label, image) in Buttons)
            {
                var interaction = Ui.InteractionOf(button);
                if (Last.TryGetValue(button, out var last) && last == interaction) continue;
                Last[button] = interaction;

                if (interaction == UiInteraction.Pressed) image.Frame = (image.Frame + 1) % 30;
                (var text, image.Color) = interaction switch
                {
                    UiInteraction.Pressed => ("Press", Color.FromSrgb8(255, 215, 0)),
                    UiInteraction.Hovered => ("Hover", Color.FromSrgb8(255, 165, 0)),
                    _ => ("Button", (1f, 1f, 1f, 1f)),
                };
                Ui.SetText(label, text);
                Ui.SetImage(button, image);
            }
        }, "ui_texture_atlas_slice.ButtonSystem");
    }
}

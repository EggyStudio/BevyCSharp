// Bevy's transparency_ui example, examples/ui/styling/transparency_ui.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates transparency in the interface, two buttons whose labels are white at a fifth of
// full strength, so the buttons' colors show through them.
internal static class TransparencyUi
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render.SetClearColor((0f, 0f, 0f, 1f));
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        var row = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.SpaceAround });
        foreach (var (label, color) in new[] { ("Button 1", Color.FromSrgb(0.1f, 0.5f, 0.1f)), ("Button 2", Color.FromSrgb(0.5f, 0.1f, 0.5f)) })
        {
            var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(150f), Height = Length.Px(65f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = color });
            ecs.SetParent(button, row);
            ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = Color.FromSrgb(1f, 1f, 1f, 0.2f) }, new UiTextSettings { Font = font, FontSize = 33f }), button);
        }
    }, "transparency_ui.Setup");
}

// Bevy's image_node example, examples/ui/images/image_node.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows an image drawn by an interface node, the logo in the middle of the window inside a white
// border, padded from it.
internal static class ImageNode
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();
        var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        var image = Ui.SpawnNode(new UiSettings
        {
            Border = Sides.All(Length.Px(5f)),
            Padding = Sides.All(Length.Px(10f)),
            Width = Length.Px(256f),
            Height = Length.Px(256f),
            BorderColor = (1f, 1f, 1f, 1f),
        });
        Ui.SetImage(image, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ctx.Ecs.SetParent(image, middle);
    }, "image_node.Setup");
}

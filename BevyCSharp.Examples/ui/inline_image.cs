// Bevy's inline_image example, examples/ui/text/inline_image.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Images set among the runs of a line of text, each tinted, one held to thirty pixels high and the
// other to thirty wide, the line in the middle of the window on maroon with a white outline.
internal static class InlineImageExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var style = new UiTextSettings { FontSize = 40f };
        var white = (1f, 1f, 1f, 1f);

        var text = Ui.SpawnText("[Text]", new UiSettings { Margin = Sides.All(Length.Px(25f)), AlignSelf = UiAlignSelf.Center }, style);
        ecs.Wrap<NodeRef>(text).JustifySelf = NodeRef.JustifySelfVariant.Center;
        ecs.Insert<BackgroundColorRef>(text).Value = Color.FromSrgb8(128, 0, 0);
        ecs.Insert<OutlineRef>(text).Width = new Val.Px(2f);

        // An image is an entity of its own among the line's runs, in the order they are added.
        void Image(Color tint, string path, float? width, float? height)
        {
            var image = ecs.Spawn();
            var inline = ecs.Insert<InlineImageRef>(image);
            (inline.Color, inline.Image, inline.Width, inline.Height) = (tint, AssetServer.Load(AssetKind.Image, path), width, height);
            ecs.SetParent(image, text);
        }

        Ui.SpawnTextSpan(text, "[span before image]", style, white);
        Image(new Color(1f, 0f, 0f), "branding/bevy_logo_dark.png", null, 30f);
        Ui.SpawnTextSpan(text, "[span between images]", style, white);
        Image(new Color(1f, 1f, 0f), "branding/bevy_bird_dark.png", 30f, null);
        Ui.SpawnTextSpan(text, "[span after image]", style, white);
    }, "inline_image.Setup");
}

// Bevy's font_variations example, examples/ui/text/font_variations.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates a variable font's axes, its weight axis set from thin to black, where font_weights
// asks for a weight and leaves the font to choose.
internal static class FontVariationsExample
{
    public static void Build(App app) => app.Startup(Setup, "font_variations.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var font = AssetServer.Load(AssetKind.Font, "fonts/MonaSans-VariableFont.ttf");
        Render2d.SpawnCamera2d();

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, AlignSelf = UiAlignSelf.Center, Align = UiAlign.Center });
        ecs.Wrap<NodeRef>(column).JustifySelf = NodeRef.JustifySelfVariant.Center;

        var heading = Ui.SpawnText("Font Variations (wght axis)", new UiSettings(), new UiTextSettings { Font = font, FontSize = 32f });
        Ui.SetUnderline(heading);
        ecs.SetParent(heading, column);

        var weights = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Padding = Sides.All(Length.Px(8f)), RowGap = Length.Px(8f) });
        ecs.SetParent(weights, column);

        foreach (var weight in new[] { 100, 200, 300, 400, 500, 600, 700, 800, 900 })
        {
            var line = Ui.SpawnText($"wght {weight}", new UiSettings(), new UiTextSettings { Font = font, FontSize = 32f });
            Ui.SetFontVariations(line, ("wght", weight));
            ecs.SetParent(line, weights);
        }
    }
}

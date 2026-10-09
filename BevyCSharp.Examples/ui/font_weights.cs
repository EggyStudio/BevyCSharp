// Bevy's font_weights example, examples/ui/text/font_weights.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates font weights with text, a variable font drawn at eleven weights from hairline to
// the heaviest it has, under an underlined heading.
internal static class FontWeights
{
    public static void Build(App app) => app.Startup(Setup, "font_weights.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var font = AssetServer.Load(AssetKind.Font, "fonts/MonaSans-VariableFont.ttf");
        Render2d.SpawnCamera2d();

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, AlignSelf = UiAlignSelf.Center, Align = UiAlign.Center });
        ecs.Wrap<NodeRef>(column).JustifySelf = NodeRef.JustifySelfVariant.Center;

        var heading = Ui.SpawnText("Font Weights", new UiSettings(), new UiTextSettings { Font = font, FontSize = 32f });
        Ui.SetUnderline(heading);
        ecs.SetParent(heading, column);

        var weights = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Padding = Sides.All(Length.Px(8f)), RowGap = Length.Px(8f) });
        ecs.SetParent(weights, column);

        foreach (var weight in new ushort[] { 100, 134, 200, 300, 400, 500, 600, 700, 800, 900, 950 })
        {
            var line = Ui.SpawnText($"Weight {weight}", new UiSettings(), new UiTextSettings { Font = font, FontSize = 32f });
            ecs.Wrap<TextFontRef>(line).Weight = weight;
            ecs.SetParent(line, weights);
        }
    }
}

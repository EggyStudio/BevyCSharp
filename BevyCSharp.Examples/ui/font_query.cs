// Bevy's font_query example, examples/ui/text/font_query.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates font weights, widths and styles asked of one variable font, in three columns under
// an underlined heading, the font finding each in its axes.
internal static class FontQuery
{
    public static void Build(App app) => app.Startup(Setup, "font_query.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var family = AssetServer.Load(AssetKind.Font, "fonts/MonaSans-VariableFont.ttf");
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            AlignSelf = UiAlignSelf.Center,
            Align = UiAlign.Center,
            Padding = Sides.All(Length.Px(16f)),
            RowGap = Length.Px(16f),
        });
        ecs.Wrap<NodeRef>(root).JustifySelf = NodeRef.JustifySelfVariant.Center;

        var heading = Ui.SpawnText("Font Weights, Widths & Styles", new UiSettings(), new UiTextSettings { Font = family, FontSize = 32f });
        Ui.SetUnderline(heading);
        ecs.SetParent(heading, root);

        // The weights, the widths and the styles side by side.
        var columns = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(32f) });
        ecs.SetParent(columns, root);

        Entity Column()
        {
            var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Padding = Sides.All(Length.Px(8f)), RowGap = Length.Px(8f) });
            ecs.SetParent(column, columns);
            return column;
        }

        Entity Line(Entity column, string text)
        {
            var line = Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = family });
            ecs.SetParent(line, column);
            return line;
        }

        // Left column, weights.
        var weights = Column();
        foreach (var (weight, name) in new (ushort, string)[]
        {
            (100, "Thin"), (200, "Extra Light"), (300, "Light"), (400, "Normal"), (500, "Medium"),
            (600, "Semibold"), (700, "Bold"), (800, "Extra Bold"), (900, "Black"),
        })
        {
            ecs.Wrap<TextFontRef>(Line(weights, $"Weight {weight} ({name})")).Weight = weight;
        }

        // Middle column, widths, as the share of the font's normal width.
        var widths = Column();
        foreach (var (width, name) in new (float, string)[]
        {
            (0.5f, "ULTRA_CONDENSED"), (0.625f, "EXTRA_CONDENSED"), (0.75f, "CONDENSED"),
            (0.875f, "SEMI_CONDENSED"), (1f, "NORMAL"), (1.125f, "SEMI_EXPANDED"),
            (1.25f, "EXPANDED"), (1.5f, "EXTRA_EXPANDED"), (2f, "ULTRA_EXPANDED"),
        })
        {
            ecs.Wrap<TextFontRef>(Line(widths, $"FontWidth::{name}")).Width = width;
        }

        // Right column, styles.
        var styles = Column();
        foreach (var (style, name) in new (FontStyle, string)[] { (new FontStyle.Normal(), "Normal"), (new FontStyle.Oblique(null), "Oblique"), (new FontStyle.Italic(), "Italic") })
        {
            ecs.Wrap<TextFontRef>(Line(styles, $"FontStyle::{name}")).Style = style;
        }
    }
}

// Bevy's letter_spacing example, examples/ui/text/letter_spacing.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows letter spacing on text justified left, center and right, under an underlined heading, the
// arrow keys widening and narrowing it and Space switching between pixels and rem.
internal static class LetterSpacingExample
{
    // Bevy's RemSize, which this example never changes from its default of twenty pixels.
    internal const float RemSize = 20f;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
        var column = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Direction = UiDirection.Column });
        var layout = ecs.Wrap<NodeRef>(column);
        (layout.PaddingLeft, layout.PaddingRight) = (new Val.Vw(5f), new Val.Vw(5f));
        (layout.PaddingTop, layout.PaddingBottom) = (new Val.Vh(10f), new Val.Vh(10f));
        layout.RowGap = new Val.Vh(6f);
        ecs.SetParent(column, root);

        var hello = Text(ecs, "HELLO", font, 6f);
        Ui.SetUnderline(hello);
        ecs.Wrap<NodeRef>(hello).PaddingBottom = new Val.Vh(2f);
        ecs.SetParent(hello, column);

        foreach (var justify in new[] { TextJustify.Left, TextJustify.Center, TextJustify.Right })
        {
            var group = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Width = Length.Percent(100f) });
            ecs.SetParent(group, column);
            ecs.SetParent(Text(ecs, $"Justify::{justify}", font, 2f), group);

            var text = Ui.SpawnText("letter spacing", new UiSettings { Width = Length.Percent(100f) }, new UiTextSettings { Font = font, Justify = justify });
            FontSize(ecs, text, 6f);
            ecs.Insert<LetterSpacingRef>(text).Value = new LetterSpacing.Px(0f);
            ecs.SetParent(text, group);
            ecs.Add(text, new AnimatedLetterSpacing());
        }

        var label = Text(ecs, "LetterSpacing::Px(0.0)", font, 3f);
        ecs.SetParent(label, root);
        Corner(ecs, label, left: true);
        ecs.Add(label, new LetterSpacingLabel());

        var help = Text(ecs, "← → to adjust   Space to toggle Px / Rem", font, 2.5f);
        ecs.SetParent(help, root);
        Corner(ecs, help, left: false);
    }, "letter_spacing.Setup");

    // Text sized by the window's height, as Bevy's FontSize::Vh is.
    private static Entity Text(EcsWorld ecs, string text, AssetHandle font, float vh)
    {
        var entity = Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = font });
        FontSize(ecs, entity, vh);
        return entity;
    }

    private static void FontSize(EcsWorld ecs, Entity text, float vh) =>
        ecs.Wrap<TextFontRef>(text).FontSize = new Bevy.Reflected.FontSize.Vh(vh);

    // Two percent of the window in from the bottom and from one side.
    private static void Corner(EcsWorld ecs, Entity node, bool left)
    {
        var layout = ecs.Wrap<NodeRef>(node);
        layout.PositionType = NodeRef.PositionTypeVariant.Absolute;
        layout.Bottom = new Val.Vh(2f);
        if (left) layout.Left = new Val.Vw(2f);
        else layout.Right = new Val.Vw(2f);
    }
}

/// <summary>The label that says the spacing and its unit.</summary>
[Behavior]
public partial struct LetterSpacingLabel;

/// <summary>A text whose letter spacing the keys change.</summary>
[Behavior]
public partial struct AnimatedLetterSpacing
{
    /// <summary>
    /// Space keeps the spacing the same and changes its unit, a rem being twenty pixels, and the
    /// label says so.
    /// </summary>
    [OnUpdate]
    public void ToggleMode(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;

        var spacing = ctx.Ecs.Wrap<LetterSpacingRef>(ctx.Entity);
        spacing.Value = spacing.Value switch
        {
            LetterSpacing.Px px => new LetterSpacing.Rem(px.Value / LetterSpacingExample.RemSize),
            LetterSpacing.Rem rem => new LetterSpacing.Px(rem.Value * LetterSpacingExample.RemSize),
            var other => other,
        };
        Label(ctx, spacing.Value);
    }

    /// <summary>The held arrows widen or narrow the spacing, by half a pixel or a twentieth of a rem a frame, and the label says so.</summary>
    [OnUpdate]
    public void UpdateLetterSpacing(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var delta = input.KeyDown(Key.ArrowRight) ? 0.5f : input.KeyDown(Key.ArrowLeft) ? -0.5f : 0f;
        if (delta == 0f) return;

        var spacing = ctx.Ecs.Wrap<LetterSpacingRef>(ctx.Entity);
        spacing.Value = spacing.Value switch
        {
            LetterSpacing.Px px => new LetterSpacing.Px(Math.Clamp(px.Value + delta, -100f, 100f)),
            LetterSpacing.Rem rem => new LetterSpacing.Rem(Math.Clamp(rem.Value + delta * 0.1f, -10f, 10f)),
            var other => other,
        };
        Label(ctx, spacing.Value);
    }

    // The label written with the spacing, in its unit.
    private static void Label(BehaviorContext ctx, LetterSpacing spacing)
    {
        var text = spacing is LetterSpacing.Rem rem
            ? FormattableString.Invariant($"LetterSpacing::Rem({rem.Value:0.00})")
            : FormattableString.Invariant($"LetterSpacing::Px({(spacing as LetterSpacing.Px)?.Value ?? 0f:0.0})");
        foreach (var label in ctx.Ecs.EntitiesWith<LetterSpacingLabel>()) Ui.SetText(label, text);
    }
}

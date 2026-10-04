using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows letter spacing on text justified left, center and right, the arrow keys widening and
// narrowing it and Space switching between pixels and rem. Bevy's underlines the heading, which is
// not reachable here, so this is written in part.
internal static class LetterSpacingExample
{
    private const string Node = "bevy_ui::ui_node::Node";
    private const string TextFont = "bevy_text::text::TextFont";
    private const string Spacing = "bevy_text::text::LetterSpacing";

    // Bevy's RemSize, which this example never changes from its default of twenty pixels.
    private const float RemSize = 20f;

    private static readonly List<Entity> Animated = [];
    private static Entity _label;
    private static bool _rem;
    private static float _value;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Animated.Clear();
            (_rem, _value) = (false, 0f);
            Render2d.SpawnCamera2d();
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
            var column = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Direction = UiDirection.Column });
            Set(ecs, column, ".padding.left", "Vw", 5f);
            Set(ecs, column, ".padding.right", "Vw", 5f);
            Set(ecs, column, ".padding.top", "Vh", 10f);
            Set(ecs, column, ".padding.bottom", "Vh", 10f);
            Set(ecs, column, ".row_gap", "Vh", 6f);
            ecs.SetParent(column, root);

            var hello = Text(ecs, "HELLO", font, 6f);
            Set(ecs, hello, ".padding.bottom", "Vh", 2f);
            ecs.SetParent(hello, column);

            foreach (var justify in new[] { TextJustify.Left, TextJustify.Center, TextJustify.Right })
            {
                var group = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Width = Length.Percent(100f) });
                ecs.SetParent(group, column);
                ecs.SetParent(Text(ecs, $"Justify::{justify}", font, 2f), group);

                var text = Ui.SpawnText("letter spacing", new UiSettings { Width = Length.Percent(100f) }, new UiTextSettings { Font = font, Justify = justify });
                FontSize(ecs, text, 6f);
                ecs.InsertReflected(text, Spacing, "{\"Px\":0.0}");
                ecs.SetParent(text, group);
                Animated.Add(text);
            }

            _label = Text(ecs, "LetterSpacing::Px(0.0)", font, 3f);
            ecs.SetParent(_label, root);
            Corner(ecs, _label, ".left", "Vw");

            var help = Text(ecs, "← → to adjust   Space to toggle Px / Rem", font, 2.5f);
            ecs.SetParent(help, root);
            Corner(ecs, help, ".right", "Vw");
        }, "letter_spacing.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var changed = false;

            // Space keeps the spacing the same and changes its unit, a rem being RemSize pixels.
            if (input.KeyPressed(Key.Space))
            {
                (_rem, _value) = _rem ? (false, _value * RemSize) : (true, _value / RemSize);
                changed = true;
            }

            var delta = input.KeyDown(Key.ArrowRight) ? 0.5f : input.KeyDown(Key.ArrowLeft) ? -0.5f : 0f;
            if (delta != 0f)
            {
                _value = _rem ? Math.Clamp(_value + delta * 0.1f, -10f, 10f) : Math.Clamp(_value + delta, -100f, 100f);
                changed = true;
            }

            if (!changed) return;
            var unit = _rem ? "Rem" : "Px";
            foreach (var text in Animated) ctx.Ecs.SetReflected(text, Spacing, string.Empty, FormattableString.Invariant($"{{\"{unit}\":{_value}}}"));
            Ui.SetText(_label, _rem ? FormattableString.Invariant($"LetterSpacing::Rem({_value:0.00})") : FormattableString.Invariant($"LetterSpacing::Px({_value:0.0})"));
        }, "letter_spacing.Update");
    }

    // Text sized by the window's height, as Bevy's FontSize::Vh is.
    private static Entity Text(EcsWorld ecs, string text, AssetHandle font, float vh)
    {
        var entity = Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = font });
        FontSize(ecs, entity, vh);
        return entity;
    }

    private static void FontSize(EcsWorld ecs, Entity text, float vh) =>
        ecs.SetReflected(text, TextFont, ".font_size", FormattableString.Invariant($"{{\"Vh\":{vh}}}"));

    private static void Set(EcsWorld ecs, Entity node, string field, string unit, float value) =>
        ecs.SetReflected(node, Node, field, FormattableString.Invariant($"{{\"{unit}\":{value}}}"));

    // Two percent of the window in from the bottom and from one side.
    private static void Corner(EcsWorld ecs, Entity node, string side, string unit)
    {
        ecs.SetVariant(node, Node, ".position_type", "Absolute");
        Set(ecs, node, ".bottom", "Vh", 2f);
        Set(ecs, node, side, unit, 2f);
    }
}

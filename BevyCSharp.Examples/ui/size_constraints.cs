using Bevy;

namespace BevyCSharp.Examples.Interface;

// Demonstrates size constraints on a node, a white bar whose flex basis, width, minimum width and
// maximum width are each set from a row of buttons, the chosen one in each row lit.
internal static class SizeConstraints
{
    private const string Node = "bevy_ui::ui_node::Node";
    private const string BorderColor = "bevy_ui::ui_node::BorderColor";
    private const string Background = "bevy_ui::ui_node::BackgroundColor";
    private const string TextColor = "bevy_text::text::TextColor";

    private static readonly Color ActiveBorder = Color.FromSrgb(250f / 255f, 235f / 255f, 215f / 255f);
    private static readonly Color InactiveBorder = Color.Black;
    private static readonly Color ActiveInner = Color.White;
    private static readonly Color InactiveInner = Color.FromSrgb(0f, 0f, 128f / 255f);
    private static readonly Color ActiveText = Color.Black;
    private static readonly Color HoveredText = Color.White;
    private static readonly Color UnhoveredText = Color.FromSrgb(0.5f, 0.5f, 0.5f);

    // A button sets one field of the bar to one value, and remembers its inner box and label to
    // recolor them.
    private sealed record Choice(Entity Button, Entity Inner, Entity Label, string Field, string Value);

    private static readonly List<Choice> Choices = [];
    private static readonly Dictionary<Entity, UiInteraction> Last = [];
    private static readonly Dictionary<string, Entity> Active = [];
    private static Entity _bar;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Choices.Clear();
            Last.Clear();
            Active.Clear();
            Render2d.SpawnCamera2d();
            var style = new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f };
            var light = Scene.Srgb(0.9f, 0.9f, 0.9f);

            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = (0f, 0f, 0f, 1f) });
            var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Justify = UiJustify.Center });
            ecs.SetParent(column, root);
            ecs.SetParent(Ui.SpawnText("Size Constraints Example", new UiSettings { Color = light, Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(25f)) }, style), column);

            // The bar, white inside a black track inside yellow.
            var yellow = Scene.Srgb8(255, 255, 0);
            var frame = Ui.SpawnNode(new UiSettings { Basis = Length.Percent(100f), AlignSelf = UiAlignSelf.Stretch, Padding = Sides.All(Length.Px(10f)), Color = yellow });
            ecs.SetParent(frame, column);
            var track = Ui.SpawnNode(new UiSettings { Align = UiAlign.Stretch, Width = Length.Percent(100f), Height = Length.Px(100f), Padding = Sides.All(Length.Px(4f)), Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(track, frame);
            _bar = Ui.SpawnNode(new UiSettings { Color = (1f, 1f, 1f, 1f) });
            ecs.SetParent(_bar, track);

            var rows = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Stretch, Padding = Sides.All(Length.Px(10f)), Margin = new Sides(Length.Zero, Length.Px(50f), Length.Zero, Length.Zero), Color = yellow });
            ecs.SetParent(rows, column);
            foreach (var (label, field) in new[] { ("min_size", ".min_width"), ("flex_basis", ".flex_basis"), ("size", ".width"), ("max_size", ".max_width") })
                Row(ecs, rows, label, field, style);
        }, "size_constraints.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            foreach (var choice in Choices)
            {
                var interaction = Ui.InteractionOf(choice.Button);
                if (Last.TryGetValue(choice.Button, out var last) && last == interaction) continue;
                Last[choice.Button] = interaction;

                if (interaction == UiInteraction.Pressed)
                {
                    ecs.SetReflected(_bar, Node, choice.Field, choice.Value);
                    Active[choice.Field] = choice.Button;
                    foreach (var other in Choices.Where(other => other.Field == choice.Field)) Paint(ecs, other);
                }
                else if (Active[choice.Field] != choice.Button)
                {
                    ecs.SetReflectedColor(choice.Label, TextColor, ".0", interaction == UiInteraction.Hovered ? HoveredText : UnhoveredText);
                }
            }
        }, "size_constraints.UpdateButtons");
    }

    // A row of seven choices for one field, Auto lit to begin with.
    private static void Row(EcsWorld ecs, Entity parent, string label, string field, UiTextSettings style)
    {
        var outer = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Padding = Sides.All(Length.Px(2f)), Align = UiAlign.Stretch, Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(outer, parent);
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Justify = UiJustify.End, Padding = Sides.All(Length.Px(2f)) });
        ecs.SetParent(row, outer);

        var name = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(200f), MaxWidth = Length.Px(200f), Justify = UiJustify.Center, Align = UiAlign.Center });
        ecs.SetParent(name, row);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = Scene.Srgb(0.9f, 0.9f, 0.9f) }, style), name);

        var buttons = Ui.SpawnNode(new UiSettings());
        ecs.SetParent(buttons, row);
        var values = new List<(string Text, string Json)> { ("Auto", "\"Auto\"") };
        foreach (var percent in new[] { 0, 25, 50, 75, 100, 125 }) values.Add(($"{percent}%", $"{{\"Percent\":{percent}.0}}"));

        foreach (var (text, json) in values)
        {
            var button = Ui.SpawnNode(new UiSettings { Interactive = true, Align = UiAlign.Center, Justify = UiJustify.Center, Border = Sides.All(Length.Px(2f)), Margin = Sides.Horizontal(Length.Px(2f)), BorderColor = (0f, 0f, 0f, 1f) });
            ecs.SetParent(button, buttons);
            var inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Justify = UiJustify.Center, Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(inner, button);
            var labelText = Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = style.Font, FontSize = style.FontSize, Justify = TextJustify.Center });
            ecs.SetParent(labelText, inner);

            var choice = new Choice(button, inner, labelText, field, json);
            Choices.Add(choice);
            if (text == "Auto") Active[field] = button;
        }

        foreach (var choice in Choices.Where(choice => choice.Field == field)) Paint(ecs, choice);
    }

    // Lit where it is the chosen value of its row, dark where it is not.
    private static void Paint(EcsWorld ecs, Choice choice)
    {
        var active = Active[choice.Field] == choice.Button;
        foreach (var side in new[] { ".top", ".right", ".bottom", ".left" })
            ecs.SetReflectedColor(choice.Button, BorderColor, side, active ? ActiveBorder : InactiveBorder);
        ecs.SetReflectedColor(choice.Inner, Background, ".0", active ? ActiveInner : InactiveInner);
        ecs.SetReflectedColor(choice.Label, TextColor, ".0", active ? ActiveText : UnhoveredText);
    }
}
